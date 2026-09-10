using System;
using System.Numerics;

namespace WrathCombo.Enhanced;

internal static class HotbarGlowFrame
{
    internal const string TexturePath = "ui/uld/icona_frame_hr1.tex";
    internal const int FrameSize = 144;
    private const int FrameLeft = 480;

    // This frame includes the bloom around a 96px high-resolution action icon.
    internal static (Vector2 Start, Vector2 End) Bounds(Vector2 start, Vector2 size, float inset, float padding = 0)
    {
        var center = start + size / 2;
        var half = (size + new Vector2((padding - inset) * 2)) * 0.75f;
        return (center - half, center + half);
    }

    // Keep the game's soft alpha falloff; tint the neutral mask at draw time.
    internal static byte[] ExtractMask(ReadOnlySpan<byte> pixels, int width, int height)
    {
        if (width < FrameLeft + FrameSize || height < FrameSize || (long)width * height * 4 > pixels.Length)
            throw new ArgumentException("Unexpected hotbar frame atlas dimensions.");
        var mask = new byte[FrameSize * FrameSize * 4];
        for (var y = 0; y < FrameSize; y++)
        for (var x = 0; x < FrameSize; x++)
        {
            var destination = (y * FrameSize + x) * 4;
            mask[destination] = mask[destination + 1] = mask[destination + 2] = 255;
            mask[destination + 3] = pixels[(y * width + FrameLeft + x) * 4 + 3];
        }
        return mask;
    }
}
