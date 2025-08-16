using System;
using FsCheck;
using FsCheck.Xunit;
using Andy.Tui.Core.Reactive;

namespace Andy.Tui.Core.Tests.Property;

public class ReactivePropertyTests
{
    [Property(MaxTest = 50)]
    public bool Signal_OnlyNotifies_On_Actual_Change(int initial, int next)
    {
        var s = new Signal<int>(initial);
        int calls = 0;
        s.ValueChanged += (_, _) => calls++;
        s.Value = next;
        // If next equals initial, expect 0; else expect 1
        if (next == initial) return calls == 0;
        return calls == 1;
    }
}
