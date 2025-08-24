using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class GroupBoxTests
{
    [Fact]
    public void Renders_Border_And_Title_Wipe()
    {
        var gb = new Andy.Tui.Widgets.GroupBox();
        gb.SetTitle("Settings");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        gb.Render(new L.Rect(0, 0, 20, 6), baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.Border>(), _ => true);
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("Settings"));
    }

    [Fact]
    public void Content_Renders_Inside_Padding()
    {
        var gb = new Andy.Tui.Widgets.GroupBox();
        gb.SetTitle("X");
        gb.SetPadding(1,1,1,1);
        gb.SetContent((r, bd, b) =>
            b.DrawText(new DL.TextRun((int)r.X, (int)r.Y, "Payload", new DL.Rgb24(255,255,255), null, DL.CellAttrFlags.None))
        );
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        gb.Render(new L.Rect(0, 0, 20, 6), baseDl, b);
        var dl = b.Build();
        var payload = dl.Ops.OfType<DL.TextRun>().FirstOrDefault(t => t.Content == "Payload");
        Assert.True(payload.X >= 2 && payload.Y >= 2);
    }
}
