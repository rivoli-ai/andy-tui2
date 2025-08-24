using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class RealTimeLogView
{
    private readonly List<string> _lines = new();
    private int _firstVisibleLine;
    private bool _followTail = true;

    public DL.Rgb24 Bg { get; private set; } = new DL.Rgb24(8, 8, 8);
    public DL.Rgb24 Fg { get; private set; } = new DL.Rgb24(220, 220, 220);
    public DL.Rgb24 Border { get; private set; } = new DL.Rgb24(70, 70, 70);

    public void AppendLine(string line)
    {
        _lines.Add(line);
    }

    public void AppendBatch(IEnumerable<string> lines)
    {
        _lines.AddRange(lines);
    }

    public void FollowTail(bool follow)
    {
        _followTail = follow;
    }

    public void AdjustScroll(int delta, int viewportRows)
    {
        if (viewportRows <= 0) return;
        int lastWindowStart = Math.Max(0, _lines.Count - viewportRows);
        int start = _followTail ? lastWindowStart : _firstVisibleLine;
        start = Math.Max(0, Math.Min(lastWindowStart, start + delta));
        _firstVisibleLine = start;
        _followTail = false;
    }

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X;
        int y = (int)rect.Y;
        int w = (int)rect.Width;
        int h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, Bg));

        int visibleRows = Math.Max(0, h);
        int start = _followTail ? Math.Max(0, _lines.Count - visibleRows) : _firstVisibleLine;
        start = Math.Max(0, Math.Min(Math.Max(0, _lines.Count - visibleRows), start));

        int yy = y;
        for (int i = 0; i < visibleRows; i++)
        {
            int idx = start + i;
            if (idx >= 0 && idx < _lines.Count)
            {
                string content = _lines[idx];
                // Clip content width-wise by drawing a substring that fits in w-2 inner width (leave 1 col padding)
                string toDraw = content.Length > Math.Max(0, w - 2) ? content[..Math.Max(0, w - 2)] : content;
                builder.DrawText(new DL.TextRun(x + 1, yy, toDraw, Fg, null, DL.CellAttrFlags.None));
            }
            yy++;
            if (yy >= y + h) break;
        }

        builder.DrawBorder(new DL.Border(x, y, w, h, "single", Border));
        builder.Pop();
    }
}
