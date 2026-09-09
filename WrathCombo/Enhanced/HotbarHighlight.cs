using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Data.Files;
using System;
using System.Numerics;
using WrathCombo.Services;

namespace WrathCombo.Enhanced;

internal static unsafe class HotbarHighlight
{
    private static IDalamudTextureWrap? glowTexture;
    private static bool glowLoadAttempted;
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
                if (showDamage) Highlight(start, size, c.Damage.Color, 0);
                if (showHeal) Highlight(start, size, c.Healing.Color,
                    showDamage ? Math.Min(size.X, size.Y) * 0.12f : 0);
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

    private static void Highlight(Vector2 start, Vector2 size, Vector4 color, float inset)
    {
        var c = EnhancedSettings.Current;
        if (c.Pulse) color.W *= 0.7f + 0.3f * (float)(0.5 + 0.5 * Math.Sin(ImGui.GetTime() * 5));
        var draw = ImGui.GetForegroundDrawList();
        if (c.HighlightStyle == 0 && GetGlowTexture() is { } texture)
        {
            var bounds = HotbarGlowFrame.Bounds(start, size, inset);
            var tint = color;
            tint.W *= Math.Min(c.GlowIntensity, 1);
            draw.AddImage(texture.Handle, bounds.Start, bounds.End, Vector2.Zero, Vector2.One,
                ImGui.ColorConvertFloat4ToU32(tint));
            // A second pass strengthens the soft bloom without changing its color.
            if (c.GlowIntensity > 1)
            {
                tint.W = color.W * (c.GlowIntensity - 1);
                draw.AddImage(texture.Handle, bounds.Start, bounds.End, Vector2.Zero, Vector2.One,
                    ImGui.ColorConvertFloat4ToU32(tint));
            }
            return;
        }

        var end = start + size - new Vector2(inset);
        start += new Vector2(inset);
        var glow = color;
        glow.W *= 0.25f;
        draw.AddRect(start, end, ImGui.ColorConvertFloat4ToU32(glow), 5, ImDrawFlags.None, c.BorderWidth + 4);
        draw.AddRect(start, end, ImGui.ColorConvertFloat4ToU32(color), 5, ImDrawFlags.None, c.BorderWidth);
    }

    private static IDalamudTextureWrap? GetGlowTexture()
    {
        if (glowLoadAttempted) return glowTexture;
        glowLoadAttempted = true;
        try
        {
            var atlas = Svc.Data.GetFile<TexFile>(HotbarGlowFrame.TexturePath)
                ?? throw new InvalidOperationException("Hotbar glow frame not found.");
            var mask = HotbarGlowFrame.ExtractMask(atlas.ImageData, atlas.Header.Width, atlas.Header.Height);
            glowTexture = Svc.Texture.CreateFromRaw(
                RawImageSpecification.Rgba32(HotbarGlowFrame.FrameSize, HotbarGlowFrame.FrameSize), mask);
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "Could not load the hotbar glow; using outlines.");
        }
        return glowTexture;
    }

    internal static void Dispose()
    {
        glowTexture?.Dispose();
        glowTexture = null;
        glowLoadAttempted = false;
    }
}
