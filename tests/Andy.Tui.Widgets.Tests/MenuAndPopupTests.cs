using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class MenuAndPopupTests
{
    [Fact]
    public void MenuBehaviorOptions_Defaults_And_Customize()
    {
        var opt = new MenuBehaviorOptions();
        Assert.True(opt.SubmenuOpenDelayMs >= 200);
        opt.SubmenuOpenDelayMs = 500;
        Assert.Equal(500, opt.SubmenuOpenDelayMs);
    }
    [Fact]
    public void Menu_Accelerator_Matching_Finds_Correct_Index()
    {
        var m = new Andy.Tui.Widgets.Menu()
            .Add("New", 'N')
            .Add("Open", 'O')
            .Add("Exit", 'X');
        Assert.Equal(0, m.IndexOfAccelerator('n'));
        Assert.Equal(1, m.IndexOfAccelerator('O'));
        Assert.Equal(2, m.IndexOfAccelerator('x'));
        Assert.Equal(-1, m.IndexOfAccelerator('z'));
    }

    [Fact]
    public void MenuPopup_Measure_Computes_Size_From_Items()
    {
        var m = new Andy.Tui.Widgets.Menu().Add("Short").Add("MuchLongerItem");
        var popup = new Andy.Tui.Widgets.MenuPopup();
        popup.SetMenu(m);
        var (w, h) = popup.Measure();
        Assert.True(w >= "MuchLongerItem".Length + 4);
        Assert.Equal(2 + m.Items.Count, h);
    }

    [Fact]
    public void MenuBar_ComputeHeaderPositions_Returns_Expected_Xs()
    {
        var mb = new Andy.Tui.Widgets.MenuBar()
            .Add("File", new Andy.Tui.Widgets.Menu())
            .Add("Edit", new Andy.Tui.Widgets.Menu())
            .Add("View", new Andy.Tui.Widgets.Menu());
        var pos = mb.ComputeHeaderPositions(2, 4, 80);
        Assert.Equal(3, pos.Count);
        Assert.Equal(2, pos[0].X);
        Assert.Equal(2 + "File".Length + 4, pos[1].X);
    }

    [Fact]
    public void MenuBar_Render_Highlights_Active_Header()
    {
        var mb = new Andy.Tui.Widgets.MenuBar()
            .Add("File", new Andy.Tui.Widgets.Menu())
            .Add("Edit", new Andy.Tui.Widgets.Menu())
            .Add("View", new Andy.Tui.Widgets.Menu());
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        mb.Render(new L.Rect(0, 0, 80, 1), baseDl, b, activeHeaderIndex: 1);
        var dl = b.Build();
        // Expect at least one rect on Y=0 (underline highlight present)
        Assert.Contains(dl.Ops.OfType<DL.Rect>(), r => r.Y == 0);
    }

    [Fact]
    public void MenuHelpers_DrawStatusLine_Renders_Bar_And_Text()
    {
        var b = new DL.DisplayListBuilder();
        // Draw a status line at Y=2
        Andy.Tui.Widgets.MenuHelpers.DrawStatusLine(b, 2, 40, "Selected: File › Open");
        var dl = b.Build();
        // Expect one rect at Y=2 and a text run containing "Selected:"
        Assert.Contains(dl.Ops.OfType<DL.Rect>(), r => r.Y == 2);
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Y == 2 && t.Content.Contains("Selected:"));
    }

    [Fact]
    public void VirtualizedGrid_Renders_Visible_Viewport()
    {
        var grid = new Andy.Tui.Widgets.VirtualizedGrid();
        grid.SetDimensions(100, 3);
        grid.SetColumnWidths(new[] { 3, 4, 2 });
        grid.SetCellTextProvider((row, col) => $"{row:D2}{(char)('A' + col)}");
        grid.SetScrollRows(10);

        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        grid.Render(new L.Rect(0, 0, 12, 3), baseDl, b); // fits columns 3+1+4+1+2 = 11 + gaps
        var dl = b.Build();

        var runs = dl.Ops.OfType<DL.TextRun>().ToList();
        Assert.Contains(runs, r => r.Y == 0 && r.Content.StartsWith("10A"));
        Assert.Contains(runs, r => r.Y == 0 && r.Content.Contains("10B"));
        Assert.Contains(runs, r => r.Y == 1 && r.Content.StartsWith("11A"));
    }

    [Fact]
    public void VirtualizedGrid_Scroll_Clamp_And_Measure()
    {
        var grid = new Andy.Tui.Widgets.VirtualizedGrid();
        grid.SetDimensions(50, 2);
        grid.SetColumnWidths(new[] { 5, 5 });
        grid.SetCellTextProvider((r, c) => $"{r:D2}{c}");
        grid.SetScrollRows(10000); // excessive
        Assert.True(grid.GetFirstVisibleRow() >= 0);
        var (rows, cols) = grid.MeasureVisible(new L.Rect(0, 0, 12, 3));
        Assert.True(rows <= 3);
        Assert.Equal(2, cols);
    }

    [Fact]
    public void MenuHelpers_Popup_Clamping_Works()
    {
        // Popup near right/bottom should clamp inside viewport
        var (x, y) = Andy.Tui.Widgets.MenuHelpers.ComputePopupPosition(100, 40, 20, 5, 110, 42);
        Assert.True(x >= 0 && x <= 90);
        Assert.True(y >= 0 && y <= 37);

        // Submenu position clamps left if overflowing
        var (sx, sy) = Andy.Tui.Widgets.MenuHelpers.ComputeSubmenuPosition(100, 10, 12, 2, 20, 8, 110, 20);
        Assert.True(sx <= 100); // clamped left or equal
        Assert.True(sy >= 0 && sy <= 12);
    }

    [Fact]
    public void ContextMenu_Renders_Items_And_Border()
    {
        var menu = new Andy.Tui.Widgets.Menu().Add("Cut").Add("Copy").Add("Paste");
        var cm = new Andy.Tui.Widgets.ContextMenu();
        cm.SetMenu(menu);
        cm.SetSelectedIndex(1);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        var (w, h) = cm.Measure();
        cm.Render(new L.Rect(0, 0, w, h), baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.Border>(), _ => true);
        var texts = dl.Ops.OfType<DL.TextRun>().Select(t => t.Content).ToList();
        Assert.Contains("Copy", texts);
    }

    [Fact]
    public void MenuHelpers_Submenu_Clamping_For_Tall_Submenus()
    {
        // Parent near bottom; submenu would overflow; expect clamped Y
        int parentX = 70, parentY = 18, parentW = 10;
        int popupW = 20, popupH = 10;
        int viewportW = 80, viewportH = 24;
        var (sx, sy) = Andy.Tui.Widgets.MenuHelpers.ComputeSubmenuPosition(parentX, parentY, parentW, itemIndex: 5, popupW, popupH, viewportW, viewportH);
        Assert.True(sy + popupH <= viewportH);
        // If overflowing right, clamps left of parent
        Assert.True(sx <= parentX);
    }

    [Fact]
    public void MenuBehaviorOptions_SubmenuDelay_Configurable()
    {
        var opt = new MenuBehaviorOptions { SubmenuOpenDelayMs = 750 };
        Assert.Equal(750, opt.SubmenuOpenDelayMs);
    }
}
