using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Layout;

public sealed class VStack
{
    public int Spacing { get; private set; } = 0;
    private readonly List<(IRenderable child, int Height)> _children = new();

    public VStack Spaced(int spacing) { Spacing = Math.Max(0, spacing); return this; }
    public VStack Add(IRenderable child, int height = 1) { _children.Add((child, height)); return this; }

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int curY = y;
        for (int i = 0; i < _children.Count; i++)
        {
            var (child, h) = _children[i];
            child.Render(new L.Rect(x, curY, w, h), baseDl, builder);
            curY += h + Spacing;
            if (curY >= y + rect.Height) break;
        }
    }
}

public sealed class HStack
{
    public int Spacing { get; private set; } = 0;
    private readonly List<(IRenderable child, int Width)> _children = new();

    public HStack Spaced(int spacing) { Spacing = Math.Max(0, spacing); return this; }
    public HStack Add(IRenderable child, int width = 10) { _children.Add((child, width)); return this; }

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int h = (int)rect.Height;
        int curX = x;
        for (int i = 0; i < _children.Count; i++)
        {
            var (child, w) = _children[i];
            child.Render(new L.Rect(curX, y, w, h), baseDl, builder);
            curX += w + Spacing;
            if (curX >= x + rect.Width) break;
        }
    }
}
