using Andy.Tui.Core.Reactive;
using Xunit;

namespace Andy.Tui.Core.Tests;

public class SignalTests
{
    private sealed class FakeComputed : IComputed
    {
        public int InvalidateCalls { get; private set; }
        public void Invalidate() => InvalidateCalls++;
    }
    [Fact]
    public void Signal_Notifies_On_Change()
    {
        var s = new Signal<string>("a");
        string? last = null;
        s.ValueChanged += (_, v) => last = v;
        s.Value = "b";
        Assert.Equal("b", s.Value);
        Assert.Equal("b", last);
    }

    [Fact]
    public void Signal_No_Notify_When_Same()
    {
        var s = new Signal<int>(1);
        int calls = 0;
        s.ValueChanged += (_, v) => calls++;
        s.Value = 1;
        Assert.Equal(0, calls);
    }

    [Fact]
    public void ValueChanged_Fires_Only_On_Actual_Change()
    {
        var s = new Signal<int>(1);
        int calls = 0;
        int? last = null;
        EventHandler<int> handler = (_, v) => { calls++; last = v; };
        s.ValueChanged += handler;

        s.Value = 1; // same -> no fire
        s.Value = 2; // change -> fire
        s.Value = 2; // same -> no fire
        s.Value = 3; // change -> fire

        Assert.Equal(2, calls);
        Assert.Equal(3, last);

        // Unsubscribe and ensure no further notifications
        s.ValueChanged -= handler;
        s.Value = 4;
        Assert.Equal(2, calls);
    }

    private sealed record Person(string Name, int Age);

    [Fact]
    public void Equality_Uses_EqualityComparer_Default()
    {
        // record has value-based equality
        var p1 = new Person("Ann", 30);
        var p2 = new Person("Ann", 30); // equal by value, different instance
        var s = new Signal<Person>(p1);

        int calls = 0;
        s.ValueChanged += (_, _) => calls++;

        s.Value = p2; // equal by value -> should NOT fire
        Assert.Equal(0, calls);

        var p3 = new Person("Bob", 30); // different by value -> should fire
        s.Value = p3;
        Assert.Equal(1, calls);
    }

    [Fact]
    public void Change_Notifies_Computed_Through_Subscription()
    {
        var a = new Signal<int>(1);
        int invalidations = 0;
        var c = new Computed<int>(() => a.Value * 10, invalidate =>
        {
            a.ValueChanged += (_, _) => { invalidations++; invalidate(); };
        });

        // Prime computed
        Assert.Equal(10, c.Value);
        // Change -> invalidate -> next read recomputes
        a.Value = 2;
        Assert.Equal(20, c.Value);
        Assert.Equal(1, invalidations);

        a.Value = 3;
        Assert.Equal(30, c.Value);
        Assert.Equal(2, invalidations);
    }

    [Fact]
    public void Multiple_Subscribers_All_Notified()
    {
        var s = new Signal<string>("x");
        int a = 0, b = 0, c = 0;
        s.ValueChanged += (_, _) => a++;
        s.ValueChanged += (_, _) => b++;
        s.ValueChanged += (_, _) => c++;

        s.Value = "y";

        Assert.Equal(1, a);
        Assert.Equal(1, b);
        Assert.Equal(1, c);
    }

    [Fact]
    public void RegisterDependent_Invalidate_On_Value_Change()
    {
        var s = new Signal<int>(1);
        var dep = new FakeComputed();

        s.RegisterDependent(dep);
        // No invalidate when setting same value
        s.Value = 1;
        Assert.Equal(0, dep.InvalidateCalls);

        s.Value = 2;
        Assert.Equal(1, dep.InvalidateCalls);
    }

    [Fact]
    public void RegisterDependent_Multiple_Dependents_All_Invalidate()
    {
        var s = new Signal<string>("a");
        var d1 = new FakeComputed();
        var d2 = new FakeComputed();

        s.RegisterDependent(d1);
        s.RegisterDependent(d2);

        s.Value = "b";
        Assert.Equal(1, d1.InvalidateCalls);
        Assert.Equal(1, d2.InvalidateCalls);
    }
}
