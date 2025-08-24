using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class GanttGraphTests
{
    [Fact]
    public void Gantt_Renders_Tasks()
    {
        var g = new Andy.Tui.Widgets.GanttChart();
        g.SetHorizon(10);
        g.SetTasks(new[]{ new Andy.Tui.Widgets.GanttChart.TaskItem("T1", 0, 3) });
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        g.Render(new L.Rect(0,0,30,5), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.Rect>().Any());
    }

    [Fact]
    public void Graph_Renders_Nodes_And_Edges()
    {
        var gr = new Andy.Tui.Widgets.AsciiGraph();
        gr.SetNodes(new[]{ new Andy.Tui.Widgets.AsciiGraph.Node(0,0,"A"), new Andy.Tui.Widgets.AsciiGraph.Node(5,0,"B") });
        gr.SetEdges(new[]{ (0,1) });
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        gr.Render(new L.Rect(0,0,10,3), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.TextRun>().Any());
    }
}
