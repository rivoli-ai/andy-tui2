using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class ListBoxTests
{
    [Fact]
    public void Move_Page_Home_End_Adjust_Scroll_And_Selection()
    {
        var lb = new Andy.Tui.Widgets.ListBox();
        lb.SetItems(Enumerable.Range(1, 100).Select(i => $"Item {i:D2}"));
        lb.SetSelectedIndex(0);
        int viewport = 5;
        // Page down
        lb.Page(1, viewport);
        Assert.True(lb.SelectedIndex >= viewport - 1);
        // Move down
        lb.MoveSelection(1, viewport);
        // Home
        lb.Home(viewport);
        Assert.Equal(0, lb.SelectedIndex);
        // End
        lb.End(viewport);
        Assert.Equal(99, lb.SelectedIndex);
    }
}
