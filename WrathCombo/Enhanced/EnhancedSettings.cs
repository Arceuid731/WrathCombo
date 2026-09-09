using ECommons.DalamudServices;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Numerics;

namespace WrathCombo.Enhanced;

internal sealed class EnhancedSettings
{
    public static EnhancedSettings Current { get; private set; } = new();
    public bool Enabled = true;
    public bool ShareRotationSettings = true;
    public bool CombatOnly;
    public bool HideWhenIdle = true;
    public bool Preview;
    public bool UseWrathTargeting;
    // 0: native glow, 1: simple outline.
    public int HighlightStyle;
    public float GlowIntensity = 1;
    public float BorderWidth = 3;
    public bool Pulse = true;
    public ChannelSettings Damage = new() { Color = new(1, 0.15f, 0.12f, 1) };
    public ChannelSettings Healing = new() { Color = new(0.1f, 1, 0.3f, 1) };

    private static string FilePath => Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "TeachingMode.json");

    public static void Load()
    {
        if (File.Exists(FilePath))
            Current = JsonConvert.DeserializeObject<EnhancedSettings>(File.ReadAllText(FilePath))
                ?? throw new InvalidDataException("Empty teaching mode configuration.");
        Current.Preview = false;
        Current.HighlightStyle = Math.Clamp(Current.HighlightStyle, 0, 1);
        Current.GlowIntensity = float.IsFinite(Current.GlowIntensity) ? Math.Clamp(Current.GlowIntensity, 0.2f, 2) : 1;
        Current.BorderWidth = Math.Clamp(Current.BorderWidth, 1, 8);
        Current.Damage ??= new() { Color = new(1, 0.15f, 0.12f, 1) };
        Current.Healing ??= new() { Color = new(0.1f, 1, 0.3f, 1) };
        Current.Damage.Sanitize();
        Current.Healing.Sanitize();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        AtomicJson.Write(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
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
    public float IconSize = 48;
    public float Opacity = 0.65f;
    public Vector4 Color;
    // 0: automatic, 1: single target, 2: area of effect.
    public int Rotation;
    public Vector2? Position;
    // Window dimensions in logical pixels, independent of Dalamud UI scale.
    public Vector2? WindowSize;

    [JsonIgnore] internal bool PassThrough => Locked && ClickThrough;

    internal void Sanitize()
    {
        IconSize = Math.Clamp(IconSize, 24, 160);
        Opacity = Math.Clamp(Opacity, 0, 1);
        Rotation = Math.Clamp(Rotation, 0, 2);
        if (WindowSize is { } size && (!float.IsFinite(size.X) || !float.IsFinite(size.Y) || size.X <= 0 || size.Y <= 0))
            WindowSize = null;
    }
}
