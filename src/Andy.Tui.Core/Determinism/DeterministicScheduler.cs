using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Andy.Tui.Core.Determinism;

public interface IDeterminismClock
{
    long NowTicks { get; }
}

public sealed class ManualClock : IDeterminismClock
{
    private long _ticks;
    public ManualClock(long initialTicks = 0) { _ticks = initialTicks; }
    public long NowTicks => _ticks;
    public void AdvanceTicks(long delta) { _ticks += delta; }
}

public sealed class DeterministicScheduler
{
    private readonly IDeterminismClock _clock;
    private readonly ConcurrentQueue<Action> _queue = new();

    public DeterministicScheduler(IDeterminismClock clock)
    {
        _clock = clock;
    }

    public void Enqueue(Action action)
    {
        _queue.Enqueue(action);
    }

    public int Drain(int maxSteps = int.MaxValue)
    {
        int steps = 0;
        while (steps < maxSteps && _queue.TryDequeue(out var action))
        {
            action();
            steps++;
        }
        return steps;
    }
}
