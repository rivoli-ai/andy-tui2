using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using Andy.Tui.Examples;

namespace Andy.Tui.Examples.Demos;

public static class TreeViewInteractiveDemo
{
    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            bool running = true;
            var tv = new Andy.Tui.Widgets.TreeView();
            var mammals = new RenderNode("mammals", "Mammals", false, new[]{
                new RenderNode("human","Homo sapiens", true),
                new RenderNode("wolf","Canis lupus", true),
                new RenderNode("whale","Balaenoptera musculus", true)
            });
            var birds = new RenderNode("birds", "Birds", false, new[]{
                new RenderNode("eagle","Aquila chrysaetos", true),
                new RenderNode("sparrow","Passer domesticus", true)
            });
            var reptiles = new RenderNode("reptiles", "Reptiles", false, new[]{
                new RenderNode("cobra","Naja naja", true),
                new RenderNode("tortoise","Testudo graeca", true)
            });
            tv.SetRoots(new[] { new RenderNode("life", "Tree of Life", false, new[] { mammals, birds, reptiles }) });
            tv.Expand("life"); tv.Select("mammals");

            while (running)
            {
                viewport = TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    else if (k.Key == ConsoleKey.UpArrow) tv.SelectPrevious();
                    else if (k.Key == ConsoleKey.DownArrow) tv.SelectNext();
                    else if (k.Key is ConsoleKey.LeftArrow or ConsoleKey.RightArrow) tv.ToggleExpandSelected();
                    else if (k.Key == ConsoleKey.Home) { tv.Select("life"); }
                }

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                b.DrawText(new DL.TextRun(2, 1, "TreeView — Up/Down select; Left/Right expand/collapse; Home to root; ESC back; F2 HUD", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                var baseDl = b.Build();
                var wb = new DL.DisplayListBuilder();
                var rect = new Andy.Tui.Layout.Rect(2, 3, viewport.Width - 4, viewport.Height - 4);
                var panel = new Andy.Tui.Widgets.Panel(); panel.SetTitle("Tree of Life"); panel.Render(rect, baseDl, wb);
                tv.Render(new Andy.Tui.Layout.Rect((int)rect.X + 1, (int)rect.Y + 1, (int)rect.Width - 2, (int)rect.Height - 2), baseDl, wb);
                var combined = Combine(baseDl, wb.Build());
                var overlay = new DL.DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(combined, overlay);
                await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                await Task.Delay(16);
            }
        }
        finally
        {
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    private static DL.DisplayList Combine(DL.DisplayList a, DL.DisplayList b)
    {
        var builder = new DL.DisplayListBuilder();
        Append(a); Append(b);
        return builder.Build();
        void Append(DL.DisplayList dl)
        {
            foreach (var op in dl.Ops)
            {
                switch (op)
                {
                    case DL.Rect r: builder.DrawRect(r); break;
                    case DL.Border br: builder.DrawBorder(br); break;
                    case DL.TextRun tr: builder.DrawText(tr); break;
                    case DL.ClipPush cp: builder.PushClip(cp); break;
                    case DL.LayerPush lp: builder.PushLayer(lp); break;
                    case DL.Pop: builder.Pop(); break;
                }
            }
        }
    }

    sealed class RenderNode : Andy.Tui.Widgets.ITreeNode
    {
        public string Id { get; }
        public string Label { get; }
        public bool IsLeaf { get; }
        public System.Collections.Generic.IEnumerable<Andy.Tui.Widgets.ITreeNode> Children { get; }
        public RenderNode(string id, string label, bool leaf = false, System.Collections.Generic.IEnumerable<RenderNode>? children = null)
        { Id = id; Label = label; IsLeaf = leaf; Children = children ?? System.Linq.Enumerable.Empty<RenderNode>(); }
    }
}
