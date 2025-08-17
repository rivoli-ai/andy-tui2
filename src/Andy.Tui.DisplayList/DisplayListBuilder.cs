namespace Andy.Tui.DisplayList;

/// <summary>
/// Verb-based builder to construct a <see cref="DisplayList"/>.
/// </summary>
public sealed class DisplayListBuilder
{
    private readonly List<IDisplayOp> _ops = new();

    public void DrawRect(in Rect rect) => _ops.Add(rect);
    public void DrawBorder(in Border border) => _ops.Add(border);
    public void DrawText(in TextRun text) => _ops.Add(text);
    public void PushLayer(in LayerPush layer) => _ops.Add(layer);
    public void PushClip(in ClipPush clip) => _ops.Add(clip);
    public void Pop() => _ops.Add(new Pop());

    public DisplayList Build() => new(_ops);
}