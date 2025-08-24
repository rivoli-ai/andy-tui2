using Andy.Tui.DisplayList;
using Andy.Tui.Observability;

namespace Andy.Tui.Observability.Tests;

public class HudOverlayTests
{
    [Fact]
    public void Contributes_Text_When_Enabled()
    {
        var baseDl = new DisplayListBuilder();
        baseDl.PushClip(new ClipPush(0, 0, 100, 5));
        baseDl.DrawRect(new Rect(0, 0, 100, 5, new Rgb24(0, 0, 0)));
        baseDl.Pop();
        var overlay = new HudOverlay { Enabled = true, Fps = 60.0, DirtyPercent = 0.1, BytesPerFrame = 512 };
        overlay.UpdateTimings(new FrameTimings(0, 0, 0, 0, 1, 2, 3, 4, 5));
        var builder = new DisplayListBuilder();
        overlay.Contribute(baseDl.Build(), builder);
        var dl = builder.Build();
        Assert.True(dl.Ops.OfType<TextRun>().Count() >= 2);
    }

    [Fact]
    public void Cpu_Line_Is_Emitted_When_Available()
    {
        var baseDl = new DisplayListBuilder().Build();
        var overlay = new HudOverlay { Enabled = true, ViewportCols = 80, ViewportRows = 24 };
        var b = new DisplayListBuilder();
        overlay.Contribute(baseDl, b);
        var dl = b.Build();
        var hasCpu = dl.Ops.OfType<TextRun>().Any(tr => tr.Content.Contains("CPU(proc):"));
        // On first call CPU may be null; call again to get a delta
        if (!hasCpu)
        {
            var b2 = new DisplayListBuilder();
            overlay.Contribute(baseDl, b2);
            dl = b2.Build();
            hasCpu = dl.Ops.OfType<TextRun>().Any(tr => tr.Content.Contains("CPU(proc):"));
        }
        Assert.True(hasCpu);
    }
}