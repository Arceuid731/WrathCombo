using System;
using System.Numerics;

namespace WrathCombo.Enhanced;

internal static class ActionWindowLayout
{
    internal const float PaddingX = 8;
    internal const float PaddingY = 5;
    internal const float Gap = 8;

    internal static float TextHeight(ChannelSettings settings, float lineHeight, float spacing) =>
        (settings.ShowActionName ? lineHeight : 0) + (settings.ShowTarget ? lineHeight : 0) +
        (settings.ShowActionName && settings.ShowTarget ? spacing : 0);

    internal static Vector2 Size(ChannelSettings settings, float textHeight, bool minimum = false)
    {
        var icon = minimum ? 24 : settings.IconSize;
        return new(icon + (textHeight > 0 ? Gap + (minimum ? 32 : 86) : 0) + PaddingX * 2,
            Math.Max(icon, textHeight) + PaddingY * 2 + (settings.ShowCooldown ? 3 : 0));
    }
}
