using Xunit;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Widgets.Tests;

public class CommandPaletteTests
{
    [Fact]
    public void Filters_And_Selects()
    {
        var cp = new CommandPalette();
        cp.SetCommands(new[] { "Open File", "Save All", "Close Folder" });
        cp.SetQuery("o");
        Assert.NotNull(cp.GetSelected());
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        cp.Render(new Andy.Tui.Layout.Rect(0, 0, 80, 24), baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops, op => op is DL.Rect);
        Assert.Contains(dl.Ops, op => op is DL.TextRun);
    }

    [Fact]
    public void Pinned_And_Recent_Affect_Sort_Order()
    {
        var cp = new CommandPalette();
        cp.SetCommands(new[] { "Open File", "Save All", "Close Folder" });
        cp.SetPinnedCommands(new[] { "Close Folder" });
        cp.SetRecentCommands(new[] { "Save All" });
        cp.SetQuery("o"); // matches Open File and Close Folder
        var order = cp.GetFilteredForTesting();
        Assert.Equal("Close Folder", order[0]); // pinned first
    }
}
