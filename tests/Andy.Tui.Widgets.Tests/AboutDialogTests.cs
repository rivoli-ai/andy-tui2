using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class AboutDialogTests
{
    [Fact]
    public void Renders_Title_And_Body_And_Border()
    {
        var about = new Andy.Tui.Widgets.AboutDialog();
        about.SetContent("About", "Hello world");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        about.RenderCentered((80, 24), baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.Border>(), _ => true);
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("About"));
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("Hello world"));
    }
}
