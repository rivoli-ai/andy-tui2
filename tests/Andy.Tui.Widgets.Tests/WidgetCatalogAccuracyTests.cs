using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Andy.Tui.Widgets.Tests;

/// <summary>
/// Guards docs/WIDGETS.md against drift (issue #46). Every widget the catalog
/// claims to be implemented must map to a real public type, and every type the
/// catalog documents as "Planned / not implemented" must genuinely be absent.
/// </summary>
public class WidgetCatalogAccuracyTests
{
    // Representative widgets documented as implemented in docs/WIDGETS.md.
    // Each must resolve to a public type in the Andy.Tui.Widgets assembly.
    private static readonly string[] DocumentedWidgets =
    {
        "Label", "RichText", "Link", "LargeText", "FigletViewer", "Badge",
        "TitleBadge", "KeyValueList", "CodeViewer", "MarkdownRenderer",
        "Button", "TextInput", "Checkbox", "Toggle", "RadioGroup", "Slider",
        "Select", "ColorChooser",
        "Panel", "Card", "GroupBox", "ScrollView", "Accordion", "Tabs",
        "Carousel", "Splitter", "DockLayout", "Align", "StackLayers", "ResizeHandle",
        "Table", "TreeTable", "DataGrid", "ListBox", "ListView", "TreeView",
        "VirtualizedList", "VirtualizedGrid", "Pager",
        "BarChart", "LineChart", "PieChart", "ScatterPlot", "Sparkline",
        "Histogram", "Heatmap", "BoxPlot", "BulletChart", "Candlestick",
        "AsciiGraph", "Gauge", "ProgressBar", "GanttChart", "Timeline", "MapView",
        "MenuBar", "MenuPopup", "ContextMenu", "CommandPalette", "Breadcrumbs",
        "HintPanel", "FocusRing", "Router",
        "ModalDialog", "AboutDialog", "FileDialog", "Toast", "Tooltip",
        "PreferencesPanel", "FindReplacePanel",
        "StatusBar", "Spinner", "Bell",
        "EditorView", "DiffViewer", "ChatView", "RealTimeLogView",
    };

    // Types the catalog documents as absent (older drafts referenced them).
    private static readonly string[] AbsentTypes =
    {
        "TextField", "TextArea", "Container", "Form", "FormField", "DatePicker",
        "TimePicker", "ColorPicker", "FilePicker", "Widget", "IRenderContext",
        "Editor", "Kanban", "NetworkIndicator", "BatteryIndicator", "QRCode",
        "Avatar", "JsonViewer", "RadioButton",
    };

    private static readonly Assembly WidgetsAssembly = typeof(Andy.Tui.Widgets.Button).Assembly;
    private static readonly Assembly CliWidgetsAssembly = typeof(Andy.Tui.CliWidgets.Toast).Assembly;

    [Fact]
    public void Every_documented_cli_widget_maps_to_a_public_type()
    {
        // Reflect over the CLI widgets assembly so the "CLI widgets" catalog
        // section is guarded too (previously it was unchecked, which is how the
        // fabricated "ToastStatus" entry slipped past the drift guard).
        var publicTypeNames = CliWidgetsAssembly.GetExportedTypes()
            .Select(NormalizeTypeName)
            .ToHashSet(StringComparer.Ordinal);

        var documentedCliWidgets = ExtractCliCatalogEntries();

        // Sanity: the section really was parsed and yielded entries.
        Assert.True(documentedCliWidgets.Count > 0,
            "Could not extract any CLI widget entries from the 'CLI widgets' section of docs/WIDGETS.md.");

        var missing = documentedCliWidgets.Where(name => !publicTypeNames.Contains(name)).ToList();

        Assert.True(missing.Count == 0,
            "docs/WIDGETS.md lists CLI widgets that are not public types in Andy.Tui.CliWidgets: " +
            string.Join(", ", missing));
    }

    // Parse the bold entries (e.g. "**Toast**") from the "## CLI widgets" section
    // of docs/WIDGETS.md, up to the next "## " heading. Names inside backticks or
    // file references are not bold, so they are naturally excluded.
    private static IReadOnlyList<string> ExtractCliCatalogEntries()
    {
        var path = FindRepoFile(Path.Combine("docs", "WIDGETS.md"));
        var text = File.ReadAllText(path);

        const string cliHeader = "## CLI widgets";
        var start = text.IndexOf(cliHeader, StringComparison.Ordinal);
        Assert.True(start >= 0, "docs/WIDGETS.md must contain a '## CLI widgets' section.");

        var afterHeader = start + cliHeader.Length;
        var nextHeader = text.IndexOf("\n## ", afterHeader, StringComparison.Ordinal);
        var section = nextHeader >= 0
            ? text.Substring(afterHeader, nextHeader - afterHeader)
            : text.Substring(afterHeader);

        var names = new List<string>();
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(section, @"\*\*([A-Za-z][A-Za-z0-9]*)\*\*"))
        {
            names.Add(m.Groups[1].Value);
        }

        return names;
    }

    [Fact]
    public void Every_documented_widget_maps_to_a_public_type()
    {
        var publicTypeNames = WidgetsAssembly.GetExportedTypes()
            .Select(NormalizeTypeName)
            .ToHashSet(StringComparer.Ordinal);

        var missing = DocumentedWidgets.Where(name => !publicTypeNames.Contains(name)).ToList();

        Assert.True(missing.Count == 0,
            "docs/WIDGETS.md lists widgets that are not public types in Andy.Tui.Widgets: " +
            string.Join(", ", missing));
    }

    [Fact]
    public void Types_documented_as_absent_do_not_exist()
    {
        var publicTypeNames = WidgetsAssembly.GetExportedTypes()
            .Select(NormalizeTypeName)
            .ToHashSet(StringComparer.Ordinal);

        var unexpectedlyPresent = AbsentTypes.Where(publicTypeNames.Contains).ToList();

        Assert.True(unexpectedlyPresent.Count == 0,
            "The catalog documents these as not implemented, but they exist in Andy.Tui.Widgets: " +
            string.Join(", ", unexpectedlyPresent));
    }

    [Fact]
    public void Catalog_file_has_planned_section_and_does_not_present_absent_types_as_implemented()
    {
        var path = FindRepoFile(Path.Combine("docs", "WIDGETS.md"));
        var text = File.ReadAllText(path);

        const string plannedHeader = "## Planned / not implemented";
        var plannedIndex = text.IndexOf(plannedHeader, StringComparison.Ordinal);
        Assert.True(plannedIndex >= 0, "docs/WIDGETS.md must contain a 'Planned / not implemented' section.");

        // The portion of the document that describes implemented widgets.
        var implementedPortion = text.Substring(0, plannedIndex);

        // Absent types must not be presented as bullet-listed implemented widgets
        // (e.g. "- **TextField**") in the implemented portion.
        foreach (var absent in AbsentTypes)
        {
            var asImplementedBullet = "**" + absent + "**";
            Assert.False(implementedPortion.Contains(asImplementedBullet, StringComparison.Ordinal),
                $"docs/WIDGETS.md presents absent type '{absent}' as an implemented widget.");
        }
    }

    // Generic types report a name like "VirtualizedList`1"; the catalog uses the
    // plain name. Strip the arity suffix so both sides compare equal.
    private static string NormalizeTypeName(Type t)
    {
        var name = t.Name;
        var tick = name.IndexOf('`');
        return tick >= 0 ? name.Substring(0, tick) : name;
    }

    private static string FindRepoFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            $"Could not locate '{relativePath}' walking up from {AppContext.BaseDirectory}.");
    }
}
