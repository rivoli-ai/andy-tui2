using BenchmarkDotNet.Attributes;
using Andy.Tui.Core.Reactive;

namespace Andy.Tui.Benchmarks;

[MemoryDiagnoser]
public class ComputedBenchmarks
{
    private Signal<int> _a = null!;
    private Computed<int> _sum = null!;

    [GlobalSetup]
    public void Setup()
    {
        _a = new Signal<int>(0);
        _sum = new Computed<int>(() => _a.Value + 1, invalidate => _a.ValueChanged += (_, _) => invalidate());
        var _ = _sum.Value; // prime cache
    }

    [Benchmark]
    public int Computed_NoInvalidate_Read()
    {
        return _sum.Value; // cached
    }

    [Benchmark]
    public int Computed_Invalidate_Then_Read()
    {
        _a.Value++;
        return _sum.Value;
    }
}
