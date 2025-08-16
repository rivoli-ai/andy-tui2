using FsCheck;
using FsCheck.Xunit;
using Andy.Tui.Core.Reactive;

namespace Andy.Tui.Core.Tests.Property;

public class MultiSignalPropertyTests
{
    [Property(MaxTest = 50)]
    public bool Computed_Tracks_Two_Signals_Correctly(int a0, int b0, int a1, int b1)
    {
        var a = new Signal<int>(a0);
        var b = new Signal<int>(b0);
        var c = new Computed<int>(() => a.Value + b.Value, invalidate =>
        {
            a.ValueChanged += (_, _) => invalidate();
            b.ValueChanged += (_, _) => invalidate();
        });

        var expected0 = a0 + b0;
        if (c.Value != expected0) return false;

        a.Value = a1;
        b.Value = b1;
        var expected1 = a1 + b1;
        return c.Value == expected1;
    }
}
