using System;
using System.Diagnostics;
using Andy.Tui.Core.Reactive;
using Xunit;

namespace Andy.Tui.Core.Tests;

public class PerformanceBudgetTests
{
    private static double MeasureAverageNanoseconds(Action action, int iterations)
    {
        // Warmup
        for (int i = 0; i < 10_000; i++) action();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++) action();
        sw.Stop();
        var ns = (double)sw.ElapsedTicks / Stopwatch.Frequency * 1_000_000_000.0;
        return ns / iterations;
    }

    [Fact(Skip = "Performance varies in CI environment - exceeding 350ns budget")]
    public void Signal_Update_WithChange_Stays_Under_Budget()
    {
        // Relaxed threshold for CI jitter, target <200ns, gate at 350ns
        const double budgetNs = 350.0;
        var signal = new Signal<int>(0);
        // Light subscriber to include event overhead
        int sink = 0;
        signal.ValueChanged += (_, v) => sink = v;

        int counter = 0;
        double avgNs = MeasureAverageNanoseconds(() => { signal.Value = ++counter; }, iterations: 200_000);
        Assert.True(avgNs <= budgetNs, $"Signal update avg {avgNs:F1} ns exceeds budget {budgetNs} ns");
    }

    [Fact]
    public void Computed_Read_After_Invalidate_Stays_Reasonable()
    {
        // Not as strict: target < 500ns, gate at 800ns
        const double budgetNs = 800.0;
        var a = new Signal<int>(0);
        var c = new Computed<int>(() => a.Value + 1, invalidate => a.ValueChanged += (_, _) => invalidate());
        // Prime cache
        _ = c.Value;

        double avgNs = MeasureAverageNanoseconds(() => { a.Value++; var _ = c.Value; }, iterations: 200_000);
        Assert.True(avgNs <= budgetNs, $"Computed read (post-invalidate) avg {avgNs:F1} ns exceeds budget {budgetNs} ns");
    }
}
