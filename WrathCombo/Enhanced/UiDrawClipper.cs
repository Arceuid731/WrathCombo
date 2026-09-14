using Dalamud.Bindings.ImGui;
using System.Collections.Generic;

namespace WrathCombo.Enhanced;

internal static unsafe class UiDrawClipper
{
    internal static void Apply(ImDrawListPtr draw, int firstCommand, IReadOnlyList<UiRect> blockers)
    {
        if (blockers.Count == 0 || firstCommand < 0 || firstCommand >= draw.CmdBuffer.Size) return;
        ref var commands = ref draw.CmdBuffer;
        var original = commands.AsSpan()[firstCommand..].ToArray();
        commands.Resize(firstCommand);
        foreach (var command in original)
        {
            // Keep callbacks and the final empty command used by subsequent ImGui drawing.
            if (command.UserCallback != null || command.ElemCount == 0) { commands.PushBack(command); continue; }
            var clip = command.ClipRect;
            foreach (var part in UiOcclusionGeometry.Visible(new(clip.X, clip.Y, clip.Z, clip.W), blockers))
            {
                var copy = command;
                copy.ClipRect = part.Clip;
                commands.PushBack(copy);
            }
        }
    }
}