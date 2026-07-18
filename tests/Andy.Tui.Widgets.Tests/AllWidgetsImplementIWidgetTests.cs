using System;
using System.Linq;
using System.Reflection;
using Andy.Tui.Widgets;
using Xunit;

namespace Andy.Tui.Widgets.Tests;

/// <summary>
/// Enforces the outcome of issue #78: every built-in widget in the Andy.Tui.Widgets
/// assembly implements the single composable <see cref="IWidget"/> contract (via
/// <see cref="WidgetBase"/>). This is a drift guard — a newly added widget that paints
/// itself but forgets the contract fails this test.
/// </summary>
public class AllWidgetsImplementIWidgetTests
{
    private static readonly Assembly WidgetsAssembly = typeof(WidgetBase).Assembly;

    private static bool DerivesWidgetBase(Type t)
    {
        for (var b = t.BaseType; b != null; b = b.BaseType)
            if (b == typeof(WidgetBase)) return true;
        return false;
    }

    private static bool ImplementsIWidget(Type t) =>
        t.GetInterfaces().Any(i => i == typeof(IWidget));

    // "Widget-like" = derives WidgetBase, or declares a render-shaped method (a
    // Render/RenderCore that takes a DisplayListBuilder) — i.e. it paints itself.
    private static bool IsWidgetLike(Type t)
    {
        if (DerivesWidgetBase(t)) return true;
        var methods = t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                   BindingFlags.Instance | BindingFlags.DeclaredOnly);
        return methods.Any(m => (m.Name == "Render" || m.Name == "RenderCore") &&
                                m.GetParameters().Any(p => p.ParameterType.Name.Contains("DisplayListBuilder")));
    }

    private static Type[] WidgetLikeTypes() =>
        WidgetsAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && (t.IsPublic || t.IsNestedPublic))
            .Where(IsWidgetLike)
            .ToArray();

    [Fact]
    public void Every_Widget_Like_Type_Implements_IWidget()
    {
        var offenders = WidgetLikeTypes()
            .Where(t => !ImplementsIWidget(t))
            .Select(t => t.FullName)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        Assert.True(offenders.Length == 0,
            "Widget-like types that do not implement IWidget: " + string.Join(", ", offenders));
    }

    [Fact]
    public void Widget_Catalog_Is_Non_Trivial()
    {
        // Non-tautological floor: #78 migrated ~67 widgets atop the 6 already on WidgetBase.
        var count = WidgetLikeTypes().Length;
        Assert.True(count >= 60, $"Expected at least 60 widget types, found {count}.");
    }

    [Theory]
    [InlineData("BarChart")]
    [InlineData("Panel")]
    [InlineData("ScrollView")]
    [InlineData("TextInput")]
    [InlineData("Tabs")]
    [InlineData("MenuBar")]
    [InlineData("Toast")]
    [InlineData("ModalDialog")]
    [InlineData("TreeView")]
    [InlineData("EditorView")]
    [InlineData("FocusRing")]
    [InlineData("VirtualizedList")]
    public void Representative_Migrated_Widgets_Are_IWidget(string simpleName)
    {
        var type = WidgetsAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == simpleName || t.Name.StartsWith(simpleName + "`", StringComparison.Ordinal));
        Assert.True(type != null, $"Widget type '{simpleName}' not found in the assembly.");
        Assert.True(ImplementsIWidget(type!), $"{simpleName} does not implement IWidget.");
    }
}
