using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Andy.Tui.Core.Reactive;

namespace Andy.Tui.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<SignalBenchmarks>();
    }
}

[MemoryDiagnoser]
public class SignalBenchmarks
{
    private Signal<int> _signal = null!;
    private int _sink;

    [GlobalSetup]
    public void Setup()
    {
        _signal = new Signal<int>(0);
        _signal.ValueChanged += (_, v) => { _sink = v; };
    }

    [Benchmark]
    public void Update_NoChange()
    {
        _signal.Value = 0; // should not notify
    }

    [Benchmark]
    public void Update_WithChange()
    {
        _signal.Value = _signal.Value + 1;
    }
}
