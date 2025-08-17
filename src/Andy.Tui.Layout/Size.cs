namespace Andy.Tui.Layout;

/// <summary>
/// Basic geometry structs used by layout engine.
/// </summary>
public readonly record struct Size(double Width, double Height)
{
    public static readonly Size Zero = new(0, 0);
}