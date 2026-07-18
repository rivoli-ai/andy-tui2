using System;
using System.Linq;
using Andy.Tui.Widgets;
using Andy.Tui.Widgets.Layout;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;
using IN = Andy.Tui.Input;

namespace Andy.Tui.Widgets.Tests;

/// <summary>
/// Common behaviour exercised once across every built-in widget migrated to the
/// <see cref="IWidget"/> runtime contract, plus the external-rendering adapter.
/// </summary>
public class WidgetContractTests
{
    public static IEnumerable<object[]> Widgets()
    {
        yield return new object[] { (Func<IWidget>)(() => new Label("hi")) };
        yield return new object[] { (Func<IWidget>)(() => new Button("ok")) };
        yield return new object[] { (Func<IWidget>)(() => new Checkbox("c")) };
        yield return new object[] { (Func<IWidget>)(() => new ProgressBar()) };
        yield return new object[] { (Func<IWidget>)(() => new Spinner()) };
        yield return new object[] { (Func<IWidget>)(() => WidgetAdapter.FromRender(
            (in L.Rect r, DL.DisplayList _, DL.DisplayListBuilder b) =>
                b.DrawText(new DL.TextRun((int)r.X, (int)r.Y, "x", new DL.Rgb24(1, 1, 1), null, DL.CellAttrFlags.None)),
            new L.Size(1, 1))) };
    }

    private static DL.DisplayList Render(IWidget w, L.Rect rect)
    {
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        w.Render(rect, baseDl, b);
        return b.Build();
    }

    [Theory]
    [MemberData(nameof(Widgets))]
    public void Defaults_Are_Visible_And_Enabled(Func<IWidget> factory)
    {
        var w = factory();
        Assert.True(w.IsVisible);
        Assert.True(w.IsEnabled);
        Assert.False(w.IsFocused);
    }

    [Theory]
    [MemberData(nameof(Widgets))]
    public void Invisible_Widget_Emits_No_Ops(Func<IWidget> factory)
    {
        var w = factory();
        w.SetVisible(false);
        var dl = Render(w, new L.Rect(0, 0, 10, 3));
        Assert.Empty(dl.Ops);
    }

    [Theory]
    [MemberData(nameof(Widgets))]
    public void Visible_Widget_Emits_Ops(Func<IWidget> factory)
    {
        var w = factory();
        var dl = Render(w, new L.Rect(0, 0, 10, 3));
        Assert.NotEmpty(dl.Ops);
    }

    [Theory]
    [MemberData(nameof(Widgets))]
    public void ZeroArea_Rect_Emits_No_Ops(Func<IWidget> factory)
    {
        var w = factory();
        var dl = Render(w, new L.Rect(0, 0, 0, 0));
        Assert.Empty(dl.Ops);
    }

    [Theory]
    [MemberData(nameof(Widgets))]
    public void Measure_Returns_Positive_Size(Func<IWidget> factory)
    {
        var w = factory();
        var size = w.Measure(new L.Size(20, 5));
        Assert.True(size.Width >= 0);
        Assert.True(size.Height >= 0);
    }

    [Theory]
    [MemberData(nameof(Widgets))]
    public void SetVisible_Change_Raises_Invalidated(Func<IWidget> factory)
    {
        var w = factory();
        int count = 0;
        w.Invalidated += () => count++;
        w.SetVisible(false);
        Assert.Equal(1, count);
        // No-op change does not re-raise.
        w.SetVisible(false);
        Assert.Equal(1, count);
    }

    [Theory]
    [MemberData(nameof(Widgets))]
    public void SetEnabled_Change_Raises_Invalidated(Func<IWidget> factory)
    {
        var w = factory();
        int count = 0;
        w.Invalidated += () => count++;
        w.SetEnabled(false);
        Assert.Equal(1, count);
        Assert.False(w.IsEnabled);
    }

    [Theory]
    [MemberData(nameof(Widgets))]
    public void Disabled_Widget_Rejects_Input(Func<IWidget> factory)
    {
        var w = factory();
        w.SetEnabled(false);
        Assert.False(w.HandleInput(new IN.KeyEvent(" ", " ", IN.KeyModifiers.None)));
    }

    [Theory]
    [MemberData(nameof(Widgets))]
    public void NonFocusable_Widget_Cannot_Focus(Func<IWidget> factory)
    {
        var w = factory();
        if (w.Focusable) return; // only assert the non-focusable contract here
        w.SetFocused(true);
        Assert.False(w.IsFocused);
    }

    [Theory]
    [MemberData(nameof(Widgets))]
    public void Focusable_Widget_Can_Focus(Func<IWidget> factory)
    {
        var w = factory();
        if (!w.Focusable) return;
        w.SetFocused(true);
        Assert.True(w.IsFocused);
        w.SetFocused(false);
        Assert.False(w.IsFocused);
    }

    [Theory]
    [MemberData(nameof(Widgets))]
    public void Style_Override_Changes_Foreground(Func<IWidget> factory)
    {
        var w = factory();
        var accent = new DL.Rgb24(1, 2, 3);
        w.SetStyle(new WidgetStyle(Foreground: accent));
        Assert.Equal(accent, w.Style!.Value.Foreground);
        var dl = Render(w, new L.Rect(0, 0, 12, 3));
        // Widgets that route their text colour through the style hook honour the override.
        // (The adapter delegates to an external render function that does not consult the style.)
        if (w is Label or Button or Checkbox or Spinner or ProgressBar)
        {
            Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Fg.Equals(accent));
        }
    }

    [Fact]
    public void Key_Identity_RoundTrips()
    {
        var label = new Label("x").WithKey("row-1");
        Assert.Equal("row-1", ((IWidget)label).Key);
    }

    [Fact]
    public void Widgets_Nest_Directly_In_VStack_Without_Adapter()
    {
        var stack = new VStack().Spaced(0)
            .Add(new Label("a"))
            .Add(new Button("b"))
            .Add(new Checkbox("c"));
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        stack.Render(new L.Rect(0, 0, 20, 10), baseDl, b);
        var dl = b.Build();
        Assert.NotEmpty(dl.Ops);
    }

    [Fact]
    public void Hidden_Child_Is_Skipped_By_VStack()
    {
        var visibleFirst = new Label("first");
        var hidden = new Label("hidden");
        var visibleLast = new Label("last");
        hidden.SetVisible(false);

        var stack = new VStack().Spaced(0)
            .Add(visibleFirst)
            .Add(hidden)
            .Add(visibleLast);

        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        stack.Render(new L.Rect(0, 0, 20, 10), baseDl, b);
        var texts = b.Build().Ops.OfType<DL.TextRun>().Select(t => t.Content).ToList();

        Assert.Contains("first", texts);
        Assert.Contains("last", texts);
        Assert.DoesNotContain("hidden", texts);
    }

    [Fact]
    public void Hidden_Child_Does_Not_Consume_A_Row()
    {
        var first = new Label("first");
        var hidden = new Label("hidden");
        var last = new Label("last");
        hidden.SetVisible(false);

        var stack = new VStack().Spaced(0).Add(first).Add(hidden).Add(last);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        stack.Render(new L.Rect(0, 0, 20, 10), baseDl, b);

        var runs = b.Build().Ops.OfType<DL.TextRun>().ToList();
        var firstY = runs.First(t => t.Content == "first").Y;
        var lastY = runs.First(t => t.Content == "last").Y;
        Assert.Equal(firstY + 1, lastY);
    }

    [Fact]
    public void Checkbox_Toggles_On_Space_Input()
    {
        var cb = new Checkbox("c");
        Assert.False(cb.Checked);
        var consumed = ((IWidget)cb).HandleInput(new IN.KeyEvent(" ", " ", IN.KeyModifiers.None));
        Assert.True(consumed);
        Assert.True(cb.Checked);
    }

    [Fact]
    public void Adapter_Wraps_External_Renderable()
    {
        var external = new ExternalRenderable();
        IWidget w = WidgetAdapter.From(external, new L.Size(3, 1));
        var dl = Render(w, new L.Rect(2, 1, 5, 1));
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content == "ext");
        Assert.Equal(3, w.Measure(new L.Size(10, 10)).Width);
    }

    private sealed class ExternalRenderable : IRenderable
    {
        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
            => builder.DrawText(new DL.TextRun((int)rect.X, (int)rect.Y, "ext", new DL.Rgb24(9, 9, 9), null, DL.CellAttrFlags.None));
    }
}
