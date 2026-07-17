using System;

namespace Andy.Tui.Compose;

/// <summary>
/// A handle to a piece of component state. Reading <see cref="Value"/> returns
/// the current value; <see cref="Set"/> stores a new value and invalidates the
/// owning component so a frame is scheduled. The handle stays valid only for the
/// render that produced it; capture the value or call <see cref="Set"/> to drive
/// subsequent renders.
/// </summary>
public readonly struct StateRef<T>
{
    private readonly StateSlot _slot;
    private readonly ComposeInstance _instance;
    private readonly Composer _composer;

    internal StateRef(StateSlot slot, ComposeInstance instance, Composer composer)
    {
        _slot = slot;
        _instance = instance;
        _composer = composer;
    }

    /// <summary>Gets the current state value.</summary>
    public T Value => (T)_slot.Value!;

    /// <summary>
    /// Stores a new value. If the value differs from the current one, the owning
    /// component is invalidated and a frame is requested from the scheduler.
    /// </summary>
    public void Set(T value)
    {
        if (Equals(_slot.Value, value)) return;
        _slot.Value = value;
        _composer.Invalidate(_instance);
    }

    /// <summary>Applies a function to the current value and stores the result.</summary>
    public void Update(Func<T, T> updater)
    {
        if (updater is null) throw new ArgumentNullException(nameof(updater));
        Set(updater(Value));
    }
}

/// <summary>
/// The per-render context handed to a <see cref="VComponent"/>. It exposes the
/// state and effect hooks a component uses. Hooks are positional: they must be
/// called in the same order on every render of the same component instance.
/// </summary>
public sealed class ComposeContext
{
    private readonly Composer _composer;
    private readonly ComposeInstance _instance;

    internal ComposeContext(Composer composer, ComposeInstance instance)
    {
        _composer = composer;
        _instance = instance;
    }

    /// <summary>
    /// Declares a piece of state with the given initial value. The initial value
    /// is used only on first mount; subsequent renders return the stored value,
    /// which survives keyed reorders and resets when the component's identity
    /// changes.
    /// </summary>
    public StateRef<T> UseState<T>(T initialValue)
    {
        StateSlot slot;
        if (_instance.HookIndex < _instance.Hooks.Count)
        {
            slot = (StateSlot)_instance.Hooks[_instance.HookIndex];
        }
        else
        {
            slot = new StateSlot { Value = initialValue };
            _instance.Hooks.Add(slot);
        }
        _instance.HookIndex++;
        return new StateRef<T>(slot, _instance, _composer);
    }

    /// <summary>
    /// Declares an effect that runs during the given <paramref name="phase"/>.
    /// The effect may return a cleanup action. It runs on first mount and again
    /// whenever <paramref name="deps"/> change (a <c>null</c> <paramref name="deps"/>
    /// runs every commit; an empty array runs only once). The cleanup runs before
    /// the effect re-runs and when the component unmounts.
    /// </summary>
    public void UseEffect(Func<Action?> effect, EffectPhase phase = EffectPhase.Paint, object?[]? deps = null)
    {
        if (effect is null) throw new ArgumentNullException(nameof(effect));

        EffectSlot slot;
        if (_instance.HookIndex < _instance.Hooks.Count)
        {
            slot = (EffectSlot)_instance.Hooks[_instance.HookIndex];
        }
        else
        {
            slot = new EffectSlot();
            _instance.Hooks.Add(slot);
        }
        _instance.HookIndex++;

        bool shouldRun = !slot.Initialized || deps is null || !DepsEqual(slot.Deps, deps);
        if (shouldRun)
        {
            if (slot.Cleanup is not null)
            {
                _composer.QueueCleanup(slot.Cleanup);
                slot.Cleanup = null;
            }
            slot.PendingEffect = effect;
            _composer.QueueEffect(slot);
        }

        slot.Phase = phase;
        slot.Deps = deps;
        slot.Initialized = true;
    }

    /// <summary>
    /// Declares an effect with no cleanup. See <see cref="UseEffect(Func{Action?}, EffectPhase, object?[])"/>.
    /// </summary>
    public void UseEffect(Action effect, EffectPhase phase = EffectPhase.Paint, object?[]? deps = null)
    {
        if (effect is null) throw new ArgumentNullException(nameof(effect));
        UseEffect(() => { effect(); return null; }, phase, deps);
    }

    private static bool DepsEqual(object?[]? a, object?[]? b)
    {
        if (a is null || b is null) return false;
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (!Equals(a[i], b[i])) return false;
        }
        return true;
    }
}
