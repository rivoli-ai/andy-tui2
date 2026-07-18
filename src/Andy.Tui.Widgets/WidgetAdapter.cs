using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

/// <summary>
/// Adapts truly external rendering — a raw <see cref="RenderFn"/> or a bare
/// <see cref="IRenderable"/> that does not implement <see cref="IWidget"/> — into the
/// composable widget contract so it can be nested in stack and container widgets. Prefer
/// inheriting <see cref="WidgetBase"/> for first-party widgets; this adapter exists only
/// for custom rendering owned outside the widget runtime.
/// </summary>
public sealed class WidgetAdapter : WidgetBase
{
    private readonly RenderFn _render;
    private readonly L.Size _desired;

    private WidgetAdapter(RenderFn render, L.Size desired)
    {
        _render = render;
        _desired = desired;
    }

    /// <summary>Wraps a render function as a widget with the given desired size.</summary>
    public static WidgetAdapter FromRender(RenderFn render, L.Size desired = default)
        => new(render, desired);

    /// <summary>Wraps an external <see cref="IRenderable"/> as a widget.</summary>
    public static WidgetAdapter From(IRenderable renderable, L.Size desired = default)
        => new(renderable.Render, desired);

    protected override void RenderCore(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
        => _render(in rect, baseDl, builder);

    protected override L.Size MeasureCore(L.Size available) => _desired;
}
