using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

/// <summary>
/// Minimal rendering contract. Anything that can paint itself into a
/// <see cref="DL.DisplayListBuilder"/> for a given rectangle is renderable and can be
/// composed by the stack and container widgets.
/// </summary>
public interface IRenderable
{
    /// <summary>
    /// Paints the renderable into <paramref name="builder"/> for the supplied
    /// <paramref name="rect"/>.
    /// </summary>
    /// <param name="rect">The target rectangle, in cell coordinates.</param>
    /// <param name="baseDl">
    /// The display list already composited for the current frame beneath this renderable.
    /// It is read-only context: widgets that need to sample what has been drawn so far
    /// (for backdrop blending, diffing, or hit-test avoidance) may inspect it. Leaf
    /// widgets that paint unconditionally simply ignore it.
    /// </param>
    /// <param name="builder">The builder that accumulates this frame's display operations.</param>
    void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder);
}

public delegate void RenderFn(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder);
