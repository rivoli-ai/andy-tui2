using System;

namespace Andy.Tui.Compose;

/// <summary>
/// A virtual component node whose subtree is produced by a render function.
/// The render function receives a <see cref="ComposeContext"/> that exposes
/// state and effect hooks, so a component can hold state that survives
/// reconciliation and register lifecycle effects.
/// </summary>
public sealed class VComponent : VNode
{
    /// <summary>
    /// Gets the render function that produces this component's subtree. It may
    /// return <c>null</c> to render nothing.
    /// </summary>
    public Func<ComposeContext, VNode?> Render { get; }

    /// <summary>
    /// Creates a component that renders via the given function.
    /// </summary>
    public VComponent(Func<ComposeContext, VNode?> render)
    {
        Render = render ?? throw new ArgumentNullException(nameof(render));
    }
}
