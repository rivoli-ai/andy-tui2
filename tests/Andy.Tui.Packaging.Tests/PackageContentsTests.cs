using System.IO.Compression;
using System.Xml.Linq;

namespace Andy.Tui.Packaging.Tests;

[Collection("packaging")]
public sealed class PackageContentsTests
{
    private readonly PackagingFixture _fx;

    public PackageContentsTests(PackagingFixture fx) => _fx = fx;

    /// <summary>The assemblies bundled into the single public package.</summary>
    private static readonly string[] BundledAssemblies =
    {
        "Andy.Tui.Animations",
        "Andy.Tui.Backend.Terminal",
        "Andy.Tui.CliWidgets",
        "Andy.Tui.Compose",
        "Andy.Tui.Compositor",
        "Andy.Tui.Core",
        "Andy.Tui.DisplayList",
        "Andy.Tui.Input",
        "Andy.Tui.Layout",
        "Andy.Tui.Observability",
        "Andy.Tui.Style",
        "Andy.Tui.Text",
        "Andy.Tui.Virtualization",
        "Andy.Tui.Widgets",
    };

    [Fact]
    public void Release_produces_only_the_Andy_Tui_package()
    {
        Assert.Equal(new[] { "Andy.Tui" }, _fx.ProducedPackageIds);
    }

    [Fact]
    public void Andy_Tui_bundles_every_framework_assembly_without_component_dependencies()
    {
        using var zip = ZipFile.OpenRead(_fx.NupkgPath("Andy.Tui"));

        var deps = ReadDependencyIds(zip, "Andy.Tui");
        Assert.Empty(deps);

        var packagedAssemblies = zip.Entries
            .Where(e => e.FullName.StartsWith("lib/net8.0/", StringComparison.OrdinalIgnoreCase)
                && e.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(e => Path.GetFileNameWithoutExtension(e.Name))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        var expectedAssemblies = BundledAssemblies
            .Append("Andy.Tui")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
        Assert.Equal(expectedAssemblies, packagedAssemblies);

        foreach (var expected in BundledAssemblies)
        {
            Assert.Contains(zip.Entries, e =>
                e.FullName.Equals($"lib/net8.0/{expected}.dll", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(zip.Entries, e =>
                e.FullName.Equals($"lib/net8.0/{expected}.xml", StringComparison.OrdinalIgnoreCase));
        }

        Assert.Contains(zip.Entries, e => e.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase));

        var nuspec = ReadNuspec(zip, "Andy.Tui");
        Assert.NotNull(Descendant(nuspec, "repository"));
        Assert.NotNull(Descendant(nuspec, "projectUrl"));
        Assert.NotNull(Descendant(nuspec, "icon"));
        Assert.NotNull(Descendant(nuspec, "readme"));
    }

    [Fact]
    public void Package_uses_the_designated_Andy_Tui_icon_exactly()
    {
        using var zip = ZipFile.OpenRead(_fx.NupkgPath("Andy.Tui"));
        var nuspec = ReadNuspec(zip, "Andy.Tui");
        var iconPath = Descendant(nuspec, "icon")?.Value;
        Assert.Equal("andy_tui_icon.png", iconPath);

        var iconEntry = zip.Entries.Single(e =>
            e.FullName.Equals(iconPath, StringComparison.OrdinalIgnoreCase));
        Assert.Single(zip.Entries, e =>
            e.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
        using var packagedIcon = iconEntry.Open();
        using var expectedIcon = File.OpenRead(
            Path.Combine(_fx.RepoRoot, "assets", "andy_tui_icon.png"));
        using var packagedBytes = new MemoryStream();
        using var expectedBytes = new MemoryStream();
        packagedIcon.CopyTo(packagedBytes);
        expectedIcon.CopyTo(expectedBytes);
        Assert.Equal(expectedBytes.ToArray(), packagedBytes.ToArray());
    }

    [Fact]
    public void Package_project_suppresses_component_dependencies_and_uses_pack_extension_point()
    {
        var csproj = File.ReadAllText(
            Path.Combine(_fx.RepoRoot, "src", "Andy.Tui", "Andy.Tui.csproj"));

        Assert.Contains("<SuppressDependenciesWhenPacking>true</SuppressDependenciesWhenPacking>", csproj);
        Assert.Contains("BundleProjectReferenceOutputs", csproj);
        Assert.DoesNotContain("PackagePath=\"lib/", csproj);
        Assert.DoesNotContain("bin\\$(Configuration)", csproj);
    }

    [Fact]
    public void Release_workflow_packs_only_Andy_Tui_and_guards_the_publish_input()
    {
        var workflow = File.ReadAllText(Path.Combine(
            _fx.RepoRoot, ".github", "workflows", "build-and-release.yml"));

        Assert.Contains("dotnet pack src/Andy.Tui/Andy.Tui.csproj", workflow);
        Assert.DoesNotContain("find src", workflow);
        Assert.Contains("Expected exactly one versioned Andy.Tui package", workflow);
        Assert.Contains("Refusing to publish: expected exactly one Andy.Tui package", workflow);
    }

    [Fact]
    public void Retirement_workflow_is_guarded_and_targets_exactly_the_bundled_components()
    {
        var scriptPath = Path.Combine(
            _fx.RepoRoot, "scripts", "retire-nuget-component-packages.sh");
        var retirementIds = File.ReadLines(scriptPath)
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("\"Andy.Tui.", StringComparison.Ordinal)
                && line.EndsWith('"'))
            .Select(line => line.Trim('"'))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(
            BundledAssemblies.OrderBy(id => id, StringComparer.Ordinal),
            retirementIds);
        Assert.DoesNotContain("Andy.Tui", retirementIds);

        var workflow = File.ReadAllText(Path.Combine(
            _fx.RepoRoot,
            ".github",
            "workflows",
            "retire-nuget-component-packages.yml"));
        Assert.Contains("github.ref == 'refs/heads/main'", workflow);
        Assert.Contains("RETIRE_ANDY_TUI_COMPONENTS", workflow);
        Assert.Contains("scripts/retire-nuget-component-packages.sh --execute", workflow);
        Assert.Contains("NUGET_API_KEY: ${{ secrets.NUGET_RETIRE_API_KEY }}", workflow);
        Assert.Contains("NUGET_DELETE_DELAY_SECONDS: '15'", workflow);
        Assert.Contains("scripts/retire-nuget-component-packages.sh --assert-none-listed", workflow);
    }

    private static XElement ReadNuspec(ZipArchive zip, string packageId)
    {
        var entry = zip.Entries.Single(e =>
            e.FullName.Equals($"{packageId}.nuspec", StringComparison.OrdinalIgnoreCase));
        using var stream = entry.Open();
        return XDocument.Load(stream).Root!;
    }

    private static List<string> ReadDependencyIds(ZipArchive zip, string packageId)
    {
        var root = ReadNuspec(zip, packageId);
        return root
            .Descendants()
            .Where(e => e.Name.LocalName == "dependency")
            .Select(e => e.Attribute("id")?.Value ?? string.Empty)
            .Where(id => id.Length > 0)
            .ToList();
    }

    private static XElement? Descendant(XElement root, string localName) =>
        root.Descendants().FirstOrDefault(e => e.Name.LocalName == localName);
}
