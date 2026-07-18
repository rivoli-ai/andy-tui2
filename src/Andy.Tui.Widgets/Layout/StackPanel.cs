using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Layout;

public sealed class VStack : WidgetBase
{
    public int Spacing { get; private set; } = 0;
    // Height of 0 means "measure the child" when it implements IWidget.
    private readonly List<(IRenderable child, int Height)> _children = new();

    public VStack Spaced(int spacing) { Spacing = Math.Max(0, spacing); return this; }
    public VStack Add(IRenderable child, int height = 1) { _children.Add((child, height)); return this; }

    /// <summary>Adds a widget and derives its height from <see cref="IWidget.Measure"/>.</summary>
    public VStack Add(IWidget child) { _children.Add((child, 0)); return this; }

    protected override void RenderCore(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int curY = y;
        for (int i = 0; i < _children.Count; i++)
        {
            var (child, h) = _children[i];
            // A widget contract lets the stack skip hidden children uniformly.
            if (child is IWidget widget && !widget.IsVisible) continue;
            int height = h > 0 ? h : MeasuredHeight(child, w);
            child.Render(new L.Rect(x, curY, w, height), baseDl, builder);
            curY += height + Spacing;
            if (curY >= y + rect.Height) break;
        }
    }

    private static int MeasuredHeight(IRenderable child, int width)
        => child is IWidget w ? Math.Max(1, (int)w.Measure(new L.Size(width, 0)).Height) : 1;
}

public sealed class HStack : WidgetBase
{
    public int Spacing { get; private set; } = 0;
    private readonly List<(IRenderable child, int Width)> _children = new();

    public HStack Spaced(int spacing) { Spacing = Math.Max(0, spacing); return this; }
    public HStack Add(IRenderable child, int width = 10) { _children.Add((child, width)); return this; }

    /// <summary>Adds a widget and derives its width from <see cref="IWidget.Measure"/>.</summary>
    public HStack Add(IWidget child) { _children.Add((child, 0)); return this; }

    protected override void RenderCore(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int h = (int)rect.Height;
        int curX = x;
        for (int i = 0; i < _children.Count; i++)
        {
            var (child, w) = _children[i];
            if (child is IWidget widget && !widget.IsVisible) continue;
            int width = w > 0 ? w : MeasuredWidth(child, h);
            child.Render(new L.Rect(curX, y, width, h), baseDl, builder);
            curX += width + Spacing;
            if (curX >= x + rect.Width) break;
        }
    }

    private static int MeasuredWidth(IRenderable child, int height)
        => child is IWidget w ? Math.Max(1, (int)w.Measure(new L.Size(0, height)).Width) : 10;
}
