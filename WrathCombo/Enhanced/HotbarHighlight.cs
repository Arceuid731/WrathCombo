using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Numerics;
using WrathCombo.Services;

namespace WrathCombo.Enhanced;

internal static unsafe class HotbarHighlight
{
    private static readonly string[] Bars =
    ["_ActionBar", "_ActionBar01", "_ActionBar02", "_ActionBar03", "_ActionBar04",
     "_ActionBar05", "_ActionBar06", "_ActionBar07", "_ActionBar08", "_ActionBar09",
     "_ActionCross", "_ActionDoubleCrossL", "_ActionDoubleCrossR"];

    internal static void Draw()
    {
        if (!TeachingController.CanShow || P?.Teaching == null) return;
        var damage = P.Teaching.Damage;
        var healing = P.Teaching.Healing;
        var c = EnhancedSettings.Current;
        if ((!c.Damage.Highlight || !TeachingController.IsFresh(damage)) &&
            (!c.Healing.Highlight || !TeachingController.IsFresh(healing))) return;
        var hotbars = RaptureHotbarModule.Instance();
        if (hotbars == null) return;
        for (var index = 0; index < Bars.Length; index++)
        {
            var address = Svc.GameGui.GetAddonByName(Bars[index]);
            if (address == nint.Zero) continue;
            var addon = (AddonActionBarBase*)address.Address;
            if (!addon->AtkUnitBase.IsVisible || addon->AtkUnitBase.VisibilityFlags == 1 ||
                !Visible(addon->AtkUnitBase.RootNode)) continue;
            var barId = index switch
            {
                0 => ((AddonActionBar*)addon)->RaptureHotbarId,
                10 => ((AddonActionCross*)addon)->RaptureHotbarId,
                11 or 12 => ((AddonActionDoubleCrossBase*)addon)->BarTarget,
                _ => index,
            };
            if (barId < 0 || barId >= hotbars->Hotbars.Length) continue;
            var slots = addon->ActionBarSlotVector.AsSpan();
            for (var slotIndex = 0; slotIndex < slots.Length && slotIndex < hotbars->Hotbars[barId].Slots.Length; slotIndex++)
            {
                var slot = slots[slotIndex];
                var hot = hotbars->Hotbars[barId].Slots[slotIndex];
                if (hot.CommandType != RaptureHotbarModule.HotbarSlotType.Action || slot.Icon == null) continue;
                var node = &slot.Icon->AtkResNode;
                if (!Visible(node)) continue;
                var showDamage = c.Damage.Highlight && Matches(damage, (uint)slot.ActionId, hot.CommandId);
                var showHeal = c.Healing.Highlight && Matches(healing, (uint)slot.ActionId, hot.CommandId);
                if (!showDamage && !showHeal) continue;
                // Screen coordinates already include addon/parent translations. Multiply
                // dimensions by the node ancestry scale, including HUD scale.
                var scale = Vector2.One;
                for (var parent = node; parent != null; parent = parent->ParentNode)
                    scale *= new Vector2(parent->ScaleX, parent->ScaleY);
                var start = new Vector2(node->ScreenX, node->ScreenY) + ImGui.GetMainViewport().Pos;
                var size = new Vector2(node->Width, node->Height) * scale;
                if (size.X <= 0 || size.Y <= 0) continue;
                if (showDamage) Outline(start, size, c.Damage.Color, 0);
                if (showHeal) Outline(start, size, c.Healing.Color, showDamage ? c.BorderWidth + 2 : 0);
            }
        }
    }

    private static bool Matches(Recommendation? action, uint displayed, uint original)
    {
        if (!TeachingController.IsFresh(action)) return false;
        return RecommendationRules.Matches(action!.ActionId, action.SourceAction, displayed, original,
            Service.ActionReplacer.OriginalHook(original), Service.Configuration.ActionChanging);
    }

    private static bool Visible(AtkResNode* node)
    {
        if (node == null) return false;
        for (; node != null; node = node->ParentNode)
            if (!node->IsVisible()) return false;
        return true;
    }

    private static void Outline(Vector2 start, Vector2 size, Vector4 color, float inset)
    {
        var c = EnhancedSettings.Current;
        if (c.Pulse) color.W *= 0.7f + 0.3f * (float)(0.5 + 0.5 * Math.Sin(ImGui.GetTime() * 5));
        var draw = ImGui.GetForegroundDrawList();
        var end = start + size - new Vector2(inset);
        start += new Vector2(inset);
        var glow = color;
        glow.W *= 0.25f;
        draw.AddRect(start, end, ImGui.ColorConvertFloat4ToU32(glow), 5, ImDrawFlags.None, c.BorderWidth + 4);
        draw.AddRect(start, end, ImGui.ColorConvertFloat4ToU32(color), 5, ImDrawFlags.None, c.BorderWidth);
    }
}
