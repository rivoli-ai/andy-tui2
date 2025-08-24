using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

public static class GanttGraphMapDemo
{
    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler(targetFps: 30);
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            bool running = true;
            var gantt = new Andy.Tui.Widgets.GanttChart();
            gantt.SetHorizon(30);
            gantt.SetTasks(new[]{
                new Andy.Tui.Widgets.GanttChart.TaskItem("Design", 0, 7, new DL.Rgb24(80,160,240)),
                new Andy.Tui.Widgets.GanttChart.TaskItem("Build", 7, 15, new DL.Rgb24(200,200,80)),
                new Andy.Tui.Widgets.GanttChart.TaskItem("Test", 22, 8, new DL.Rgb24(120,200,120)),
            });

            var graph = new Andy.Tui.Widgets.AsciiGraph();
            graph.SetNodes(new[]{ new Andy.Tui.Widgets.AsciiGraph.Node(2,0,"A"), new Andy.Tui.Widgets.AsciiGraph.Node(12,4,"B"), new Andy.Tui.Widgets.AsciiGraph.Node(22,1,"C")});
            graph.SetEdges(new[]{(0,1),(1,2)});

            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                }

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                b.DrawText(new DL.TextRun(2, 1, "Gantt / Graph — ESC back; F2 HUD", new DL.Rgb24(200,200,50), null, DL.CellAttrFlags.Bold));
                var baseDl = b.Build();

                var wb = new DL.DisplayListBuilder();
                int halfH = Math.Max(5, (viewport.Height - 4) / 2);
                gantt.Render(new L.Rect(2, 3, Math.Max(0, viewport.Width - 4), halfH), baseDl, wb);
                graph.Render(new L.Rect(2, 3 + halfH + 1, Math.Max(0, viewport.Width - 4), Math.Max(5, viewport.Height - (3 + halfH + 1))), baseDl, wb);

                var combined = Combine(baseDl, wb.Build());
                var overlay = new DL.DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(combined, overlay);
                await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
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
        Append(a); Append(b);
        return builder.Build();
    }
}
