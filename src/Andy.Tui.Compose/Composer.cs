using System;
using System.Collections.Generic;
using Andy.Tui.DisplayList;
using DisplayListType = Andy.Tui.DisplayList.DisplayList;

namespace Andy.Tui.Compose;

/// <summary>
/// Hosts a composition: it reconciles a <see cref="VNode"/> description into a
/// durable instance tree, drives component lifecycle (mount, update, move,
/// unmount), preserves keyed state across reorders, orders effects and cleanups,
/// integrates state invalidation with a <see cref="IFrameScheduler"/>, and
/// renders the reconciled tree into a <see cref="DisplayList"/>.
/// </summary>
public sealed class Composer
{
    private readonly VNode _root;
    private readonly IFrameScheduler _scheduler;

    private ComposeInstance? _rootInstance;

    // Commit queues, populated during reconciliation and drained in CommitEffects.
    private readonly List<Action> _cleanups = new();
    private readonly List<EffectSlot> _effects = new();

    /// <summary>
    /// Number of reconciliation passes (frames) that have run. Useful to assert
    /// that a burst of invalidations coalesces into a single frame.
    /// </summary>
    public int FrameCount { get; private set; }

    /// <summary>
    /// Creates a composer for the given root description. If no scheduler is
    /// supplied a <see cref="ManualFrameScheduler"/> is used, so invalidations
    /// queue a frame that runs on <see cref="ManualFrameScheduler.Flush"/>.
    /// </summary>
    public Composer(VNode root, IFrameScheduler? scheduler = null)
    {
        _root = root ?? throw new ArgumentNullException(nameof(root));
        _scheduler = scheduler ?? new ManualFrameScheduler();
    }

    /// <summary>
    /// Creates a composer whose root description is produced by building the view
    /// once. Dynamic, stateful subtrees should be expressed as <see cref="VComponent"/>
    /// nodes within that description.
    /// </summary>
    public static Composer FromView(View view, IFrameScheduler? scheduler = null)
    {
        if (view is null) throw new ArgumentNullException(nameof(view));
        return new Composer(view.Build(), scheduler);
    }

    /// <summary>
    /// Runs a reconciliation pass against the root description, committing any
    /// resulting effects. Call once to mount; the scheduler calls it for you on
    /// subsequent frames after an invalidation.
    /// </summary>
    public void Recompose()
    {
        _cleanups.Clear();
        _effects.Clear();
        _rootInstance = ReconcileNode(_rootInstance, _root);
        CommitEffects();
        FrameCount++;
    }

    /// <summary>
    /// Unmounts the entire tree, running every cleanup so all effects and
    /// subscriptions are released.
    /// </summary>
    public void Unmount()
    {
        if (_rootInstance is not null)
        {
            UnmountInstance(_rootInstance);
            _rootInstance = null;
        }
    }

    // --- Invalidation & scheduling -------------------------------------------------

    internal void Invalidate(ComposeInstance instance)
    {
        // Bounded work: a burst of Set calls collapses into one requested frame.
        _scheduler.Request(Recompose);
    }

    internal void QueueCleanup(Action cleanup) => _cleanups.Add(cleanup);
    internal void QueueEffect(EffectSlot slot) => _effects.Add(slot);

    // --- Reconciliation ------------------------------------------------------------

    private ComposeInstance ReconcileNode(ComposeInstance? existing, VNode node)
    {
        string kind = KindOf(node);

        // Identity change: unmount the stale instance and mount fresh (state resets).
        if (existing is not null && (existing.Kind != kind || !Equals(existing.Key, node.Key)))
        {
            UnmountInstance(existing);
            existing = null;
        }

        var instance = existing ?? new ComposeInstance { Kind = kind, Key = node.Key };

        switch (node)
        {
            case VText text:
                instance.Text = text.Text;
                instance.Foreground = text.Foreground;
                break;

            case VComponent component:
                ReconcileComponent(instance, component);
                break;

            case VElement element:
                instance.ElementType = element.Type;
                ReconcileChildren(instance, element.Children);
                break;
        }

        return instance;
    }

    private void ReconcileComponent(ComposeInstance instance, VComponent component)
    {
        instance.HookIndex = 0;
        var ctx = new ComposeContext(this, instance);
        VNode? rendered = component.Render(ctx);

        ComposeInstance? oldChild = instance.Children.Count > 0 ? instance.Children[0] : null;

        if (rendered is null)
        {
            if (oldChild is not null) UnmountInstance(oldChild);
            instance.Children = new List<ComposeInstance>();
            return;
        }

        var childInstance = ReconcileNode(oldChild, rendered);
        instance.Children = new List<ComposeInstance> { childInstance };
    }

    private void ReconcileChildren(ComposeInstance parent, IReadOnlyList<VNode> newNodes)
    {
        var oldChildren = parent.Children;

        // Index the previous children: keyed by (kind, key), plus an ordered pool
        // of unkeyed children matched positionally.
        var keyed = new Dictionary<(string, object), ComposeInstance>();
        var unkeyed = new List<ComposeInstance>();
        foreach (var oc in oldChildren)
        {
            if (oc.Key is not null) keyed[(oc.Kind, oc.Key)] = oc;
            else unkeyed.Add(oc);
        }

        int unkeyedPtr = 0;
        var used = new HashSet<ComposeInstance>();
        var result = new List<ComposeInstance>(newNodes.Count);

        foreach (var vn in newNodes)
        {
            string kind = KindOf(vn);
            ComposeInstance? match = null;

            if (vn.Key is not null)
            {
                if (keyed.TryGetValue((kind, vn.Key), out var m) && !used.Contains(m))
                    match = m;
            }
            else
            {
                while (unkeyedPtr < unkeyed.Count &&
                       (used.Contains(unkeyed[unkeyedPtr]) || unkeyed[unkeyedPtr].Kind != kind))
                {
                    unkeyedPtr++;
                }
                if (unkeyedPtr < unkeyed.Count)
                {
                    match = unkeyed[unkeyedPtr];
                    unkeyedPtr++;
                }
            }

            if (match is not null) used.Add(match);
            var childInstance = ReconcileNode(match, vn);
            used.Add(childInstance);
            result.Add(childInstance);
        }

        // Anything not reused is removed: unmount it so its effects are released.
        foreach (var oc in oldChildren)
        {
            if (!used.Contains(oc)) UnmountInstance(oc);
        }

        parent.Children = result;
    }

    private void UnmountInstance(ComposeInstance instance)
    {
        // Depth-first: unmount children before the node itself.
        foreach (var child in instance.Children)
        {
            UnmountInstance(child);
        }

        // Run this instance's effect cleanups in reverse registration order.
        for (int i = instance.Hooks.Count - 1; i >= 0; i--)
        {
            if (instance.Hooks[i] is EffectSlot slot && slot.Cleanup is not null)
            {
                slot.Cleanup();
                slot.Cleanup = null;
            }
        }
    }

    // --- Effect commit -------------------------------------------------------------

    private void CommitEffects()
    {
        // Cleanups of re-run effects first, in reverse encounter order.
        for (int i = _cleanups.Count - 1; i >= 0; i--)
        {
            _cleanups[i]();
        }
        _cleanups.Clear();

        // Layout effects before paint effects, each in encounter order.
        RunEffects(EffectPhase.Layout);
        RunEffects(EffectPhase.Paint);
        _effects.Clear();
    }

    private void RunEffects(EffectPhase phase)
    {
        foreach (var slot in _effects)
        {
            if (slot.Phase != phase || slot.PendingEffect is null) continue;
            slot.Cleanup = slot.PendingEffect();
            slot.PendingEffect = null;
        }
    }

    // --- Rendering -----------------------------------------------------------------

    /// <summary>
    /// Renders the current reconciled tree into a display list. Nodes flow
    /// top-to-bottom: each text node occupies one row, and elements and
    /// components lay their children out in order.
    /// </summary>
    public DisplayListType Render()
    {
        var builder = new DisplayListBuilder();
        if (_rootInstance is not null)
        {
            int y = 0;
            LayoutInstance(_rootInstance, builder, 0, ref y);
        }
        return builder.Build();
    }

    private static void LayoutInstance(ComposeInstance instance, DisplayListBuilder builder, int x, ref int y)
    {
        if (instance.Kind == "text")
        {
            builder.DrawText(new TextRun(x, y, instance.Text ?? string.Empty, instance.Foreground, null, CellAttrFlags.None));
            y++;
            return;
        }

        foreach (var child in instance.Children)
        {
            LayoutInstance(child, builder, x, ref y);
        }
    }

    private static string KindOf(VNode node) => node switch
    {
        VText => "text",
        VComponent => "component",
        VElement e => "element:" + e.Type,
        _ => "node",
    };
}
