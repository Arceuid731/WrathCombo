using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Numerics;
using ActionRow = Lumina.Excel.Sheets.Action;

namespace WrathCombo.Enhanced;

internal sealed class NextActionWindow(TeachingController controller, TeachingChannel channel)
    : Dalamud.Interface.Windowing.Window($"{channel.Label()}###WrathEnhanced{channel}")
{
    private ChannelSettings Settings => EnhancedSettings.Current.Get(channel);
    private Recommendation? Action => controller.Get(channel);
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
        if (Settings.PassThrough || GameUiOcclusion.CoversMouse()) Flags |= ImGuiWindowFlags.NoInputs;
        var scale = ImGuiHelpers.GlobalScale;
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.025f, 0.03f, 0.04f, Settings.Opacity));
        ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(1, 1, 1, 0.12f * Settings.Opacity));
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 6 * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(ActionWindowLayout.PaddingX, ActionWindowLayout.PaddingY) * scale);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 2) * scale);
        var textHeight = ActionWindowLayout.TextHeight(Settings, ImGui.GetTextLineHeight() / scale, 2);
        var minimum = ActionWindowLayout.Size(Settings, textHeight, true);
        SizeConstraints = new WindowSizeConstraints { MinimumSize = minimum, MaximumSize = new Vector2(800) };
        Size = Vector2.Max(minimum, Settings.WindowSize ?? ActionWindowLayout.Size(Settings, textHeight));
        SizeCondition = Settings.Locked ? ImGuiCond.Always : ImGuiCond.Appearing;
        var single = EnhancedSettings.Current.Get(TeachingChannels.From(channel.IsHealing(), false));
        var origin = single.Position ?? new Vector2(channel.IsHealing() ? 442 : 260, 300);
        var rowHeight = (single.WindowSize?.Y ?? ActionWindowLayout.Size(single, textHeight).Y) * scale;
        Position = Settings.Position ?? origin + (channel.IsArea() ? new Vector2(0, rowHeight + 8 * scale) : Vector2.Zero);
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
        var scale = ImGuiHelpers.GlobalScale;
        var start = ImGui.GetCursorScreenPos();
        var available = ImGui.GetContentRegionAvail();
        var bodyHeight = Math.Max(1, available.Y - (Settings.ShowCooldown ? 3 * scale : 0));
        var textHeight = ActionWindowLayout.TextHeight(Settings, ImGui.GetTextLineHeight(), 2 * scale);
        var textReserve = textHeight > 0 ? (ActionWindowLayout.Gap + 32) * scale : 0;
        var size = Math.Max(1, Math.Min(Settings.IconSize * scale, Math.Min(bodyHeight, available.X - textReserve)));
        var draw = ImGui.GetWindowDrawList();
        var windowStart = ImGui.GetWindowPos();
        var windowSize = ImGui.GetWindowSize();
        var accent = ImGui.ColorConvertFloat4ToU32(Settings.Color);
        draw.AddRectFilled(windowStart + new Vector2(3, 7) * scale,
            windowStart + new Vector2(5 * scale, windowSize.Y - 7 * scale), accent, scale);

        var actionId = action?.ActionId ?? (preview ? (channel.IsHealing() ? 120u : 119u) : 0);
        var row = actionId != 0 ? Svc.Data.GetExcelSheet<ActionRow>().GetRowOrDefault(actionId) : null;
        var icon = row.HasValue ? Svc.Texture.GetFromGameIcon((uint)row.Value.Icon).GetWrapOrDefault() : null;
        var iconStart = start + new Vector2(textHeight > 0 ? 0 : (available.X - size) / 2, (bodyHeight - size) / 2);
        ImGui.SetCursorScreenPos(iconStart);
        if (icon != null)
        {
            ImGui.Image(icon.Handle, new Vector2(size));
            if (action != null && Settings.ClickToUse && ImGui.IsItemClicked(ImGuiMouseButton.Left))
                controller.Click(action, false);
            TeachingSettings.Tooltip($"{row?.Name}\n{channel.Label()}" +
                (Settings.Locked ? "" : "\nDrag to move. Drag a corner to resize."));
        }
        else
        {
            draw.AddRectFilled(iconStart, iconStart + new Vector2(size), ImGui.GetColorU32(new Vector4(1, 1, 1, 0.05f)), 4 * scale);
            ImGui.Dummy(new Vector2(size));
            TeachingSettings.Tooltip(channel.Label());
        }
        if (channel.IsArea())
        {
            const string badge = "AoE";
            var fontSize = Math.Min(10 * scale, size * 0.3f);
            var badgeSize = ImGui.CalcTextSize(badge) * (fontSize / ImGui.GetFontSize());
            var badgeEnd = iconStart + new Vector2(size);
            var badgeStart = badgeEnd - badgeSize - new Vector2(4, 2) * scale;
            draw.AddRectFilled(badgeStart, badgeEnd, ImGui.GetColorU32(new Vector4(0.02f, 0.025f, 0.035f, 0.92f)), 2 * scale);
            draw.AddText(ImGui.GetFont(), fontSize, badgeStart + new Vector2(2, 1) * scale, accent, badge);
        }

        var textStart = start + new Vector2(size + ActionWindowLayout.Gap * scale, (bodyHeight - textHeight) / 2);
        if (Settings.ShowActionName)
        {
            ImGui.SetCursorScreenPos(textStart);
            DrawLine(row?.Name.ToString() ?? "Waiting");
            textStart.Y += ImGui.GetTextLineHeight() + 2 * scale;
        }
        if (Settings.ShowTarget && (action != null || preview))
        {
            ImGui.SetCursorScreenPos(textStart);
            var name = preview ? Player.Name ?? "Target" : action!.TargetName;
            var selected = action?.TargetId == Svc.Targets.Target?.GameObjectId || action?.TargetId == Player.Object?.GameObjectId;
            var clicked = DrawLine(name, selected || preview ? new Vector4(0.35f, 1, 0.5f, 1) : new Vector4(1, 0.65f, 0.2f, 1),
                preview ? name : $"{name}\nClick to target.");
            if (!preview && action != null && clicked) controller.Click(action, true);
        }
        if (Settings.ShowCooldown)
        {
            var am = ActionManager.Instance();
            var group = am == null ? null : am->GetRecastGroupDetail(57);
            var total = group == null ? 0 : group->Total;
            var elapsed = group == null ? 0 : group->Elapsed;
            var progress = total > 0 ? Math.Clamp(elapsed / total, 0, 1) : 1;
            var barStart = start + new Vector2(0, available.Y - 2 * scale);
            draw.AddRectFilled(barStart, barStart + new Vector2(available.X, 2 * scale),
                ImGui.GetColorU32(new Vector4(1, 1, 1, 0.1f)), scale);
            draw.AddRectFilled(barStart, barStart + new Vector2(available.X * progress, 2 * scale), accent, scale);
        }

        GameUiOcclusion.Clip(ImGui.GetWindowDrawList());

        if (!Settings.Locked)
        {
            var position = ImGui.GetWindowPos();
            var logicalSize = ImGui.GetWindowSize() / scale;
            if (Settings.Position != position || Settings.WindowSize is not { } savedSize ||
                Vector2.DistanceSquared(savedSize, logicalSize) > 0.25f)
            {
                Settings.Position = position;
                Settings.WindowSize = logicalSize;
                layoutDirty = true;
            }
            if (layoutDirty && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                EnhancedSettings.Current.Save();
                layoutDirty = false;
            }
        }
    }

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
