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
    private bool positionDirty;
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
        Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings |
                ImGuiWindowFlags.NoFocusOnAppearing | ImGuiWindowFlags.NoNav;
        if (Settings.Locked) Flags |= ImGuiWindowFlags.NoMove;
        if (Settings.ClickThrough) Flags |= ImGuiWindowFlags.NoInputs;
        Position = Settings.Position ?? new Vector2(healing ? 470 : 260, 300);
        PositionCondition = ImGuiCond.Appearing;
        if (lastReset != P.TeachingWindowReset)
        {
            PositionCondition = ImGuiCond.Always;
            lastReset = P.TeachingWindowReset;
        }
        ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.03f, 0.035f, 0.05f, Settings.Opacity));
        ImGui.PushStyleColor(ImGuiCol.Border, Settings.Color);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8);
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(2);
        ImGui.PopStyleColor(2);
    }

    public override unsafe void Draw()
    {
        var preview = EnhancedSettings.Current.Preview;
        var action = TeachingController.IsFresh(Action) && !preview ? Action : null;
        var label = TeachingSettings.L(healing ? "Next heal" : "Next damage", healing ? "Prochain soin" : "Prochaine attaque");
        var size = Settings.IconSize * ImGuiHelpers.GlobalScale;
        var width = Math.Max(size, 150 * ImGuiHelpers.GlobalScale);
        ImGui.TextColored(Settings.Color, label);
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
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + width);
            ImGui.TextUnformatted(row?.Name.ToString() ?? TeachingSettings.L("Waiting", "En attente"));
            ImGui.PopTextWrapPos();
        }
        if (Settings.ShowTarget && (action != null || preview))
        {
            var name = preview ? Player.Name : action!.TargetName;
            var selected = action?.TargetId == Svc.Targets.Target?.GameObjectId || action?.TargetId == Player.Object?.GameObjectId;
            ImGui.PushStyleColor(ImGuiCol.Text, selected || preview ? new Vector4(0.35f, 1, 0.5f, 1) : new Vector4(1, 0.65f, 0.2f, 1));
            ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + width);
            ImGui.TextUnformatted(name);
            ImGui.PopTextWrapPos();
            ImGui.PopStyleColor();
            if (!preview && action != null && ImGui.IsItemClicked(ImGuiMouseButton.Left)) controller.Click(action, true);
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(TeachingSettings.L("Click to target", "Cliquer pour cibler"));
        }

        if (!Settings.Locked && !Settings.ClickThrough)
        {
            var position = ImGui.GetWindowPos();
            if (Settings.Position != position)
            {
                Settings.Position = position;
                positionDirty = true;
            }
            if (positionDirty && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                EnhancedSettings.Current.Save();
                positionDirty = false;
            }
        }
    }
}
