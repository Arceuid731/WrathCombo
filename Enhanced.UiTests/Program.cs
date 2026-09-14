using Dalamud.Bindings.ImGui;
using System.Numerics;
using WrathCombo.Enhanced;

void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); }
var area = new UiRect(0, 0, 100, 100);
var tooltip = new UiRect(20, 20, 80, 80);
var parts = UiOcclusionGeometry.Visible(area, [tooltip]);
Check(parts.Count == 4 && parts.Sum(r => (r.Right-r.Left)*(r.Bottom-r.Top)) == 6400, "Only the tooltip area is removed");
Check(UiOcclusionGeometry.Visible(area, [new(-10,-10,110,110)]).Count == 0, "Fully covered overlay is hidden");
Check(UiOcclusionGeometry.Visible(area, [new(200,200,300,300)]).Single() == area, "Non-overlapping UI is unchanged");
var multiple = UiOcclusionGeometry.Visible(area, [tooltip, new(0,0,30,50), tooltip]);
for (var y=0;y<100;y++) for(var x=0;x<100;x++)
{
    var p = new Vector2(x+.5f,y+.5f);
    var expected = !tooltip.Contains(p) && !new UiRect(0,0,30,50).Contains(p);
    if (multiple.Count(r=>r.Contains(p)) != (expected ? 1 : 0)) throw new Exception("Overlapping window union mismatch");
}
Console.WriteLine("PASS overlapping UI windows preserve each visible pixel exactly once");
unsafe
{
    var context = ImGui.CreateContext();
    try
    {
        var io = ImGui.GetIO();
        io.IniFilename = null;
        io.DisplaySize = new(800,600);
        io.DeltaTime = 1f/60;
        io.Fonts.AddFontDefault();
        byte* pixels;
        int width,height;
        io.Fonts.GetTexDataAsRGBA32(0,&pixels,&width,&height);
        ImGui.NewFrame();
        ImGui.SetNextWindowPos(new(0,0));
        ImGui.SetNextWindowSize(new(200,200));
        ImGui.Begin("Teaching window", ImGuiWindowFlags.NoTitleBar);
        ImGui.TextUnformatted("Waiting");
        var draw = ImGui.GetWindowDrawList();
        draw.AddDrawCmd();
        var original = draw.CmdBuffer.AsSpan().ToArray();
        var vertices = draw.VtxBuffer.Size;
        var indices = draw.IdxBuffer.Size;
        UiDrawClipper.Apply(draw, 0, [tooltip]);
        Check(vertices == draw.VtxBuffer.Size && indices == draw.IdxBuffer.Size, "Clipping preserves window geometry and index buffers");
        foreach(var command in draw.CmdBuffer.AsSpan())
        {
            if(command.ElemCount == 0) continue;
            Check(original.Any(c=>c.IdxOffset==command.IdxOffset && c.VtxOffset==command.VtxOffset && c.ElemCount==command.ElemCount && c.TextureId==command.TextureId), "Native command references preserved");
            var c=command.ClipRect;
            Check(c.Z<=tooltip.Left || c.X>=tooltip.Right || c.W<=tooltip.Top || c.Y>=tooltip.Bottom, "No draw command covers tooltip");
        }
        ImGui.End();
        var foreground=ImGui.GetForegroundDrawList();
        foreground.AddRectFilled(new(0,0),new(5,5),0xffffffff);
        foreground.AddDrawCmd();
        var first=foreground.CmdBuffer.Size-1;
        foreground.AddRect(new(0,0),new(100,100),0xff00ff00);
        foreground.AddDrawCmd();
        var untouched=foreground.CmdBuffer[0];
        UiDrawClipper.Apply(foreground, first, [tooltip]);
        Check(foreground.CmdBuffer[0].ElemCount==untouched.ElemCount && foreground.CmdBuffer[0].ClipRect==untouched.ClipRect, "Other plugins' foreground drawing is unchanged");
        foreground.AddRectFilled(new(110,110),new(120,120),0xffffffff);
        ImGui.Render();
        Check(ImGui.GetDrawData().CmdListsCount > 0, "Native ImGui frame renders after clipping and subsequent drawing");
    }
    finally { ImGui.DestroyContext(context); }
}