using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;
using System.Diagnostics;

namespace Andy.Tui.Rendering.Tests;

public class PerfSmokeTests
{
    [Fact]
    public void Full_Repaint_200x60_Is_Fast_Enough_Smoke()
    {
        var dlb = new DisplayListBuilder();
        dlb.PushClip(new ClipPush(0,0,200,60));
        dlb.DrawRect(new Rect(0,0,200,60,new Rgb24(0,0,0)));
        for (int r = 0; r < 60; r++)
        {
            dlb.DrawText(new TextRun(1,r,$"row{r}", new Rgb24(255,255,255), null, CellAttrFlags.None));
        }
        dlb.Pop();
        var dl = dlb.Build();

        var comp = new TtyCompositor();
        var sw = Stopwatch.StartNew();
        var grid = comp.Composite(dl, (200,60));
        sw.Stop();
        // Smoke threshold generous for CI
        Assert.True(sw.ElapsedMilliseconds < 100);
    }
}
