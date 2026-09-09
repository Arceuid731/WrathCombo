using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Numerics;
using ActionRow = Lumina.Excel.Sheets.Action;

namespace WrathCombo.Enhanced;

internal sealed class NextActionWindow(TeachingController controller, bool healing)
    : Dalamud.Interface.Windowing.Window(healing ? "Next heal###WrathEnhancedHeal" : "Next damage###WrathEnhancedDamage")
{
    private ChannelSettings Settings => healing ? EnhancedSettings.Current.Healing : EnhancedSettings.Current.Damage;
    private Recommendation? Action => healing ? controller.Healing : controller.Damage;
    private bool layoutDirty;
    private int lastReset = -1;

    public override bool DrawConditions()
    {
        IsOpen = true;
        return EnhancedSettings.Current.Enabled && Settings.ShowWindow && Player.Available &&
            (TeachingController.CanShow || EnhancedSettings.Current.Preview) &&
            (EnhancedSettings.Current.Preview || TeachingController.IsFresh(Action) || !EnhancedSettings.Current.HideWhenIdle);
    }

    public override void PreDraw()
    {
        IsOpen = true;
        RespectCloseHotkey = false;
        Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav | ImGuiWindowFlags.NoScrollbar;
        if (Settings.Locked) Flags |= ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize;
        if (Settings.PassThrough) Flags |= ImGuiWindowFlags.NoInputs;
        var scale = ImGuiHelpers.GlobalScale;
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.03f, 0.035f, 0.05f, Settings.Opacity));
        ImGui.PushStyleColor(ImGuiCol.Border, Settings.Color);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(6 * scale));
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 3) * scale);
        var overhead = (ImGui.GetTextLineHeightWithSpacing() + DetailsHeight() + ImGui.GetStyle().WindowPadding.Y * 2) / scale;
        var minimum = new Vector2(64, 24 + overhead);
        SizeConstraints = new WindowSizeConstraints { MinimumSize = minimum, MaximumSize = new Vector2(800) };
        Size = Vector2.Max(minimum, Settings.WindowSize ?? new Vector2(Math.Max(Settings.IconSize, 96) + 12, Settings.IconSize + overhead));
        SizeCondition = Settings.Locked ? ImGuiCond.Always : ImGuiCond.Appearing;
        Position = Settings.Position ?? new Vector2(healing ? 470 : 260, 300);
        PositionCondition = ImGuiCond.Appearing;
        if (lastReset != P.TeachingWindowReset)
        {
            PositionCondition = ImGuiCond.Always;
            SizeCondition = ImGuiCond.Always;
            lastReset = P.TeachingWindowReset;
        }
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(4);
        ImGui.PopStyleColor(2);
    }

    public override unsafe void Draw()
    {
        var preview = EnhancedSettings.Current.Preview;
        var action = TeachingController.IsFresh(Action) && !preview ? Action : null;
        var label = healing ? "Next heal" : "Next damage";
        DrawLine(label, Settings.Color, Settings.Locked ? label : "Drag to move. Drag a corner to resize.");
        var available = ImGui.GetContentRegionAvail();
        var width = available.X;
        var size = Math.Max(1, Math.Min(Settings.IconSize * ImGuiHelpers.GlobalScale,
            Math.Min(width, available.Y - DetailsHeight())));
        var actionId = action?.ActionId ?? (preview ? (healing ? 120u : 119u) : 0);
        var row = actionId != 0 ? Svc.Data.GetExcelSheet<ActionRow>().GetRowOrDefault(actionId) : null;
        var icon = row.HasValue ? Svc.Texture.GetFromGameIcon((uint)row.Value.Icon).GetWrapOrDefault() : null;
        var left = ImGui.GetCursorPosX();
        ImGui.SetCursorPosX(left + (width - size) / 2);
        if (icon != null)
        {
            var start = ImGui.GetCursorScreenPos();
            ImGui.Image(icon.Handle, new Vector2(size));
            ImGui.GetWindowDrawList().AddRect(start, start + new Vector2(size),
                ImGui.ColorConvertFloat4ToU32(Settings.Color), 5, ImDrawFlags.None, 2);
            if (action != null && Settings.ClickToUse && ImGui.IsItemClicked(ImGuiMouseButton.Left))
                controller.Click(action, false);
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(row?.Name.ToString() ?? "");
        }
        else ImGui.Dummy(new Vector2(size));
        ImGui.SetCursorPosX(left);

        if (Settings.ShowCooldown)
        {
            var am = ActionManager.Instance();
            var group = am == null ? null : am->GetRecastGroupDetail(57);
            var total = group == null ? 0 : group->Total;
            var elapsed = group == null ? 0 : group->Elapsed;
            var progress = total > 0 ? Math.Clamp(elapsed / total, 0, 1) : 1;
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Settings.Color);
            ImGui.ProgressBar(progress, new Vector2(width, 4 * ImGuiHelpers.GlobalScale), "");
            ImGui.PopStyleColor();
        }

        if (Settings.ShowActionName)
        {
            DrawLine(row?.Name.ToString() ?? "Waiting");
        }
        if (Settings.ShowTarget && (action != null || preview))
        {
            var name = preview ? Player.Name ?? "Target" : action!.TargetName;
            var selected = action?.TargetId == Svc.Targets.Target?.GameObjectId || action?.TargetId == Player.Object?.GameObjectId;
            var clicked = DrawLine(name, selected || preview ? new Vector4(0.35f, 1, 0.5f, 1) : new Vector4(1, 0.65f, 0.2f, 1),
                preview ? name : $"{name}\nClick to target.");
            if (!preview && action != null && clicked) controller.Click(action, true);
        }

        if (!Settings.Locked)
        {
            var position = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize() / ImGuiHelpers.GlobalScale;
            if (Settings.Position != position || Settings.WindowSize is not { } savedSize ||
                Vector2.DistanceSquared(savedSize, windowSize) > 0.25f)
            {
                Settings.Position = position;
                Settings.WindowSize = windowSize;
                layoutDirty = true;
            }
            if (layoutDirty && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                EnhancedSettings.Current.Save();
                layoutDirty = false;
            }
        }
    }

    // Reserve the target line even while idle so recommendations do not change the icon size.
    private float DetailsHeight() =>
        (Settings.ShowActionName ? ImGui.GetTextLineHeightWithSpacing() : 0) +
        (Settings.ShowTarget ? ImGui.GetTextLineHeightWithSpacing() : 0) +
        (Settings.ShowCooldown ? 4 * ImGuiHelpers.GlobalScale + ImGui.GetStyle().ItemSpacing.Y : 0);

    private static bool DrawLine(string text, Vector4? color = null, string? tooltip = null)
    {
        var available = ImGui.GetContentRegionAvail().X;
        var shown = text;
        if (ImGui.CalcTextSize(text).X > available)
        {
            var low = 0;
            var high = text.Length;
            while (low < high)
            {
                var middle = (low + high + 1) / 2;
                if (ImGui.CalcTextSize(text[..middle] + "…").X <= available) low = middle;
                else high = middle - 1;
            }
            shown = text[..low] + "…";
        }
        if (color.HasValue) ImGui.PushStyleColor(ImGuiCol.Text, color.Value);
        ImGui.TextUnformatted(shown);
        if (color.HasValue) ImGui.PopStyleColor();
        var clicked = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        TeachingSettings.Tooltip(tooltip ?? text);
        return clicked;
    }
}
