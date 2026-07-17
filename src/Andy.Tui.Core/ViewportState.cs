namespace Andy.Tui.Core;

/// <summary>
/// Mutable runtime viewport dimensions shared between the input pipeline and the
/// render loop. Resize events update this state so the next arranged and rendered
/// frame reflects the new terminal size.
/// </summary>
public sealed class ViewportState
{
    public int Width { get; private set; }
    public int Height { get; private set; }

    public ViewportState(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public (int W, int H) Size => (Width, Height);

    /// <summary>
    /// Update the viewport dimensions. Returns true if the size actually changed,
    /// so callers can decide whether a recompose/relayout is required.
    /// </summary>
    public bool Resize(int width, int height)
    {
        if (width == Width && height == Height) return false;
        Width = width;
        Height = height;
        return true;
    }
}
