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
        var c = EnhancedSettings.Current;
        Span<bool> active = stackalloc bool[4];
        var any = false;
        foreach (var channel in TeachingChannels.All)
            any |= active[(int)channel] = c.Get(channel).Highlight && TeachingController.IsFresh(P.Teaching.Get(channel));
        if (!any) return;
        var hotbars = RaptureHotbarModule.Instance();
        if (hotbars == null) return;
        var overlayDraw = ImGui.GetForegroundDrawList();
        overlayDraw.AddDrawCmd();
        var firstCommand = overlayDraw.CmdBuffer.Size - 1;
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
                var matches = 0;
                foreach (var channel in TeachingChannels.All)
                    if (active[(int)channel] && Matches(P.Teaching.Get(channel), (uint)slot.ActionId, hot.CommandId))
                        matches |= 1 << (int)channel;
                if (matches == 0) continue;
                // Screen coordinates already include addon/parent translations. Multiply
                // dimensions by the node ancestry scale, including HUD scale.
                var scale = Vector2.One;
                for (var parent = node; parent != null; parent = parent->ParentNode)
                    scale *= new Vector2(parent->ScaleX, parent->ScaleY);
                var start = new Vector2(node->ScreenX, node->ScreenY) + ImGui.GetMainViewport().Pos;
                var size = new Vector2(node->Width, node->Height) * scale;
                if (size.X <= 0 || size.Y <= 0) continue;
                var offset = 0f;
                for (var role = 0; role < 4; role += 2)
                {
                    var single = (matches & (1 << role)) != 0;
                    var area = (matches & (1 << (role + 1))) != 0;
                    if (!single && !area) continue;
                    var padding = c.HighlightPadding + offset;
                    Highlight(start, size, c.Get((TeachingChannel)(single ? role : role + 1)).Color, padding);
                    if (area) AreaBorder(start, size, c.Get((TeachingChannel)(role + 1)).Color, padding);
                    // A shared spell uses one glow per role, plus its AoE marker. Place
                    // a second role outside the first without covering the icon's center.
                    offset += area ? 8 : 4;
                }
            }
        }
        overlayDraw.AddDrawCmd();
        GameUiOcclusion.Clip(overlayDraw, firstCommand);
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

    private static void Highlight(Vector2 start, Vector2 size, Vector4 color, float padding)
    {
        var c = EnhancedSettings.Current;
        if (c.Pulse) color.W *= 0.7f + 0.3f * (float)(0.5 + 0.5 * Math.Sin(ImGui.GetTime() * 5));
        var draw = ImGui.GetForegroundDrawList();
        if (c.HighlightStyle == 0 && GetGlowTexture() is { } texture)
        {
            var bounds = HotbarGlowFrame.Bounds(start, size, 0, padding);
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
        }
        else
        {
            var glow = color;
            glow.W *= 0.25f;
            draw.AddRect(start - new Vector2(padding), start + size + new Vector2(padding),
                ImGui.ColorConvertFloat4ToU32(glow), 5, ImDrawFlags.None, c.BorderWidth + 4);
            draw.AddRect(start - new Vector2(padding), start + size + new Vector2(padding),
                ImGui.ColorConvertFloat4ToU32(color), 5, ImDrawFlags.None, c.BorderWidth);
        }
    }

    private static void AreaBorder(Vector2 start, Vector2 size, Vector4 color, float padding)
    {
        var c = EnhancedSettings.Current;
        // A crisp second frame distinguishes AoE even with pulse disabled.
        var outer = new Vector2(padding + 5 + (c.HighlightStyle == 1 ? c.BorderWidth / 2 : 0));
        color.W *= 0.85f;
        ImGui.GetForegroundDrawList().AddRect(start - outer, start + size + outer,
            ImGui.ColorConvertFloat4ToU32(color), 8, ImDrawFlags.None, 1.5f);
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
