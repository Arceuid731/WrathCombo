using Newtonsoft.Json;
using System;
using System.Numerics;

namespace WrathCombo.Enhanced;

internal class TeachingPreferences
{
    public bool Enabled = true;
    public bool GameUiOnTop = true;
    public bool ShareRotationSettings = true;
    public bool CombatOnly;
    public bool HideWhenIdle = true;
    public bool Preview;
    public bool UseWrathTargeting;
    // 0: native glow, 1: simple outline.
    public int HighlightStyle;
    public float GlowIntensity = 1;
    public float HighlightPadding = 3;
    public float BorderWidth = 3;
    public bool Pulse = true;
    public int LayoutVersion;
    public ChannelSettings Damage = new() { Color = new(1, 0.15f, 0.12f, 1) };
    public ChannelSettings Healing = new() { Color = new(0.1f, 1, 0.3f, 1) };
    public ChannelSettings? DamageArea;
    public ChannelSettings? HealingArea;

    internal ChannelSettings Get(TeachingChannel channel) => channel switch
    {
        TeachingChannel.Damage => Damage,
        TeachingChannel.Healing => Healing,
        TeachingChannel.DamageArea => DamageArea ??= Damage.CopyForArea(),
        _ => HealingArea ??= Healing.CopyForArea(),
    };

    internal void Normalize()
    {
        Preview = false;
        HighlightStyle = Math.Clamp(HighlightStyle, 0, 1);
        GlowIntensity = float.IsFinite(GlowIntensity) ? Math.Clamp(GlowIntensity, 0.2f, 2) : 1;
        HighlightPadding = float.IsFinite(HighlightPadding) ? Math.Clamp(HighlightPadding, 0, 20) : 3;
        BorderWidth = float.IsFinite(BorderWidth) ? Math.Clamp(BorderWidth, 1, 8) : 3;
        Damage ??= new() { Color = new(1, 0.15f, 0.12f, 1) };
        Healing ??= new() { Color = new(0.1f, 1, 0.3f, 1) };
        foreach (var channel in TeachingChannels.All)
        {
            var settings = Get(channel);
            settings.Sanitize();
            // Adopt the compact layout once, then preserve all later manual resizing.
            if (LayoutVersion < 1) settings.WindowSize = null;
        }
        LayoutVersion = 1;
    }
}

internal sealed class ChannelSettings
{
    public bool Highlight = true;
    public bool ShowWindow = true;
    public bool Locked;
    public bool ClickThrough;
    public bool ClickToUse;
    public bool ShowActionName = true;
    public bool ShowTarget = true;
    public bool ShowCooldown = true;
    public float IconSize = 40;
    public float Opacity = 0.65f;
    public Vector4 Color;
    public Vector2? Position;
    // Window dimensions in logical pixels, independent of Dalamud UI scale.
    public Vector2? WindowSize;

    [JsonIgnore] internal bool PassThrough => Locked && ClickThrough;

    internal ChannelSettings CopyForArea()
    {
        var copy = (ChannelSettings)MemberwiseClone();
        copy.Position = copy.WindowSize = null;
        return copy;
    }

    internal void Sanitize()
    {
        IconSize = float.IsFinite(IconSize) ? Math.Clamp(IconSize, 24, 160) : 40;
        Opacity = float.IsFinite(Opacity) ? Math.Clamp(Opacity, 0, 1) : 0.65f;
        if (Position is { } p && (!float.IsFinite(p.X) || !float.IsFinite(p.Y))) Position = null;
        if (WindowSize is { } size && (!float.IsFinite(size.X) || !float.IsFinite(size.Y) || size.X <= 0 || size.Y <= 0))
            WindowSize = null;
    }
}
