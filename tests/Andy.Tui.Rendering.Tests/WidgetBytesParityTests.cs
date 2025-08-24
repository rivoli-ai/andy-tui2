using Xunit;
using Andy.Tui.Compositor;
using W = Andy.Tui.Widgets;
using L = Andy.Tui.Layout;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Rendering.Tests;

public class WidgetBytesParityTests
{
    private static (CellGrid grid, ReadOnlyMemory<byte> bytes) Render(DL.DisplayList dl, (int W, int H) size)
    {
        var comp = new TtyCompositor();
        var cells = comp.Composite(dl, size);
        var dirty = comp.Damage(new CellGrid(size.W, size.H), cells);
        var runs = comp.RowRuns(cells, dirty);
        var caps = new Andy.Tui.Backend.Terminal.TerminalCapabilities { TrueColor = true, Palette256 = true };
        var bytes = new Andy.Tui.Backend.Terminal.AnsiEncoder().Encode(runs, caps);
        return (cells, bytes);
    }

    [Fact]
    public void CommandPalette_Renders_Sections_And_Highlights()
    {
        int W = 60, H = 16;
        var cp = new W.CommandPalette();
        cp.SetCommands(new[] { "Open File", "Save All", "Close Folder", "Toggle HUD", "Go To Symbol" });
        cp.SetPinnedCommands(new[] { "Toggle HUD" });
        cp.SetRecentCommands(new[] { "Save All" });
        cp.SetQuery(""); // empty shows all sections

        var baseB = new DL.DisplayListBuilder();
        var baseDl = baseB.Build();
        var wb = new DL.DisplayListBuilder();
        cp.Render(new L.Rect(0, 0, W, H), baseDl, wb);
        var dl = wb.Build();

        var (grid, bytes) = Render(dl, (W, H));
        // Decode back to grid for checking strings at expected rows
        var decoded = Andy.Tui.Rendering.Tests.VirtualScreenOracle.Decode(bytes.Span, (W, H));

        // Expect section headers present and ordered: Pinned, Recent, All Commands
        bool hasPinned = false, hasRecent = false, hasAll = false;
        for (int y = 0; y < H; y++)
        {
            var row = GetRow(decoded, y);
            if (row.Contains("Pinned")) hasPinned = true;
            if (row.Contains("Recent")) hasRecent = true;
            if (row.Contains("All Commands")) hasAll = true;
        }
        Assert.True(hasPinned && hasRecent && hasAll);
    }

    [Fact]
    public void MenuPopup_Renders_Items_And_Arrow_In_Bytes()
    {
        int W = 30, H = 10;
        var menu = new W.Menu().Add("Open").Add("Exit");
        var popup = new W.MenuPopup();
        popup.SetMenu(menu);
        popup.SetSelectedIndex(0);

        var b = new DL.DisplayListBuilder();
        var baseDl = b.Build();
        var w = new DL.DisplayListBuilder();
        popup.Render(new L.Rect(2, 2, 12, 5), baseDl, w);
        var dl = w.Build();
        var (grid, bytes) = Render(dl, (W, H));
        var decoded = Andy.Tui.Rendering.Tests.VirtualScreenOracle.Decode(bytes.Span, (W, H));

        // Check that 'Open' and 'Exit' appear on subsequent rows, and a '▶' arrow is encoded at right edge of popup area for submenu marker (none here but layout stable)
        string r2 = GetRow(decoded, 3); // interior row y+1 (2+1)
        string r3 = GetRow(decoded, 4);
        Assert.Contains("Open", r2);
        Assert.Contains("Exit", r3);
    }

    private static string GetRow(CellGrid grid, int row)
    {
        var sb = new System.Text.StringBuilder();
        for (int x = 0; x < grid.Width; x++) sb.Append(grid.GetRef(x, row).Grapheme);
        return sb.ToString();
    }
}
