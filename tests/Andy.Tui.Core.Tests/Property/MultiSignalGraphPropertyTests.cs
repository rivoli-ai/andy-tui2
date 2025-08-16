using System;
using FsCheck;
using FsCheck.Xunit;
using Andy.Tui.Core.Reactive;

namespace Andy.Tui.Core.Tests.Property;

public class MultiSignalGraphPropertyTests
{
    // Graph: d = a + b; e = d * c
    [Property(MaxTest = 50)]
    public bool TwoLevel_Computed_Tracks_Three_Signals(int a0, int b0, int c0, int a1, int b1, int c1)
    {
        var a = new Signal<int>(a0);
        var b = new Signal<int>(b0);
        var c = new Signal<int>(c0);

        var d = new Computed<int>(() => a.Value + b.Value, invalidate =>
        {
            a.ValueChanged += (_, _) => invalidate();
            b.ValueChanged += (_, _) => invalidate();
        });

        var e = new Computed<int>(() => d.Value * c.Value, invalidate =>
        {
            // depend on d and c
            d.ValueChanged += (_, _) => invalidate();
            c.ValueChanged += (_, _) => invalidate();
        });

        var expected0 = (a0 + b0) * c0;
        if (e.Value != expected0) return false;

        a.Value = a1;
        b.Value = b1;
        c.Value = c1;
        var expected1 = (a1 + b1) * c1;
        return e.Value == expected1;
    }
}
