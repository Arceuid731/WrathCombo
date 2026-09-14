using System;
using System.Collections.Generic;
using System.Numerics;

namespace WrathCombo.Enhanced;

internal readonly record struct UiRect(float Left, float Top, float Right, float Bottom)
{
    internal bool Valid => float.IsFinite(Left) && float.IsFinite(Top) && float.IsFinite(Right) && float.IsFinite(Bottom) && Right > Left && Bottom > Top;
    internal Vector4 Clip => new(Left, Top, Right, Bottom);
    internal bool Contains(Vector2 point) => point.X >= Left && point.X < Right && point.Y >= Top && point.Y < Bottom;
}

internal static class UiOcclusionGeometry
{
    internal static List<UiRect> Visible(UiRect source, IReadOnlyList<UiRect> blockers)
    {
        var current = new List<UiRect>();
        if (source.Valid) current.Add(source);
        foreach (var blocker in blockers)
        {
            if (!blocker.Valid) continue;
            var next = new List<UiRect>();
            foreach (var r in current)
            {
                var left = Math.Max(r.Left, blocker.Left);
                var top = Math.Max(r.Top, blocker.Top);
                var right = Math.Min(r.Right, blocker.Right);
                var bottom = Math.Min(r.Bottom, blocker.Bottom);
                if (left >= right || top >= bottom) { next.Add(r); continue; }
                if (r.Top < top) next.Add(new(r.Left, r.Top, r.Right, top));
                if (bottom < r.Bottom) next.Add(new(r.Left, bottom, r.Right, r.Bottom));
                if (r.Left < left) next.Add(new(r.Left, top, left, bottom));
                if (right < r.Right) next.Add(new(right, top, r.Right, bottom));
            }
            current = next;
            if (current.Count == 0) break;
        }
        return current;
    }
}