using System;
using System.Collections.Generic;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Compose;

/// <summary>
/// A mounted node in the reconciled instance tree. Instances are the durable
/// counterpart of the transient <see cref="VNode"/> descriptions: an instance is
/// reused across renders whenever its identity (kind + key) matches, which is
/// what preserves its <see cref="Hooks"/> (state and effects). When identity
/// changes the old instance is unmounted and a fresh one is created, resetting
/// its state.
/// </summary>
internal sealed class ComposeInstance
{
    public string Kind = "";
    public object? Key;

    public List<ComposeInstance> Children = new();

    /// <summary>Ordered hook slots (each a <see cref="StateSlot"/> or <see cref="EffectSlot"/>).</summary>
    public readonly List<object> Hooks = new();

    /// <summary>Cursor into <see cref="Hooks"/>, reset at the start of each component render.</summary>
    public int HookIndex;

    // Leaf payloads.
    public string? Text;
    public Rgb24? Foreground;
    public string? ElementType;
}

/// <summary>Backing storage for a single <c>UseState</c> hook.</summary>
internal sealed class StateSlot
{
    public object? Value;
}

/// <summary>Backing storage for a single <c>UseEffect</c> hook.</summary>
internal sealed class EffectSlot
{
    public bool Initialized;
    public EffectPhase Phase;
    public object?[]? Deps;
    public Func<Action?>? PendingEffect;
    public Action? Cleanup;
}
