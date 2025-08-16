using System.Collections.Generic;

namespace Andy.Tui.Core.Determinism;

public interface IStubbedOutput
{
    void Write(string text);
    IReadOnlyList<(long Ticks, string Text)> Entries { get; }
}

public sealed class StubbedOutput : IStubbedOutput
{
    private readonly IDeterminismClock _clock;
    private readonly List<(long Ticks, string Text)> _entries = new();

    public StubbedOutput(IDeterminismClock clock)
    {
        _clock = clock;
    }

    public IReadOnlyList<(long Ticks, string Text)> Entries => _entries;

    public void Write(string text)
    {
        _entries.Add((_clock.NowTicks, text));
    }
}
