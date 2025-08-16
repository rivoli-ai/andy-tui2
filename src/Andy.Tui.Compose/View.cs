namespace Andy.Tui.Compose;

/// <summary>
/// Base class for declarative views. Override <see cref="Build"/> to emit a virtual node tree.
/// </summary>
public abstract class View
{
    /// <summary>
    /// Builds the virtual node tree for this view.
    /// </summary>
    public abstract VNode Build();
}
