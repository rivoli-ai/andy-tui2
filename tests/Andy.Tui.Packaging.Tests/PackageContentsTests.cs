using System.IO.Compression;
using System.Xml.Linq;

namespace Andy.Tui.Packaging.Tests;

[Collection("packaging")]
public sealed class PackageContentsTests
{
    private readonly PackagingFixture _fx;

    public PackageContentsTests(PackagingFixture fx) => _fx = fx;

    /// <summary>Every documented library must produce exactly one package.</summary>
    private static readonly string[] DocumentedPackages =
    {
        "Andy.Tui",
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

    /// <summary>The 13 libraries the umbrella meta-package must depend on.</summary>
    private static readonly string[] MetaPackageDependencies =
    {
        "Andy.Tui.Animations",
        "Andy.Tui.Backend.Terminal",
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
    public void Release_produces_every_documented_package_exactly_once()
    {
        // No duplicates.
        Assert.Equal(_fx.ProducedPackageIds.Count, _fx.ProducedPackageIds.Distinct().Count());
        // Exactly the documented set, no more and no fewer.
        Assert.Equal(
            DocumentedPackages.OrderBy(x => x, StringComparer.Ordinal),
            _fx.ProducedPackageIds.OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void Umbrella_is_a_dependency_meta_package_with_no_bundled_assemblies()
    {
        using var zip = ZipFile.OpenRead(_fx.NupkgPath("Andy.Tui"));

        // A meta-package ships no compiled assemblies of its own.
        Assert.DoesNotContain(zip.Entries, e => e.FullName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase));

        // It must declare a real dependency on every library instead of hiding them.
        var deps = ReadDependencyIds(zip, "Andy.Tui");
        foreach (var expected in MetaPackageDependencies)
        {
            Assert.Contains(expected, deps);
        }
        Assert.Equal(MetaPackageDependencies.Length, deps.Count);

        // Documentation, icon, and SourceLink metadata must be present.
        Assert.Contains(zip.Entries, e => e.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(zip.Entries, e => e.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase));

        var nuspec = ReadNuspec(zip, "Andy.Tui");
        Assert.NotNull(Descendant(nuspec, "repository"));
        Assert.NotNull(Descendant(nuspec, "projectUrl"));
        Assert.NotNull(Descendant(nuspec, "icon"));
        Assert.NotNull(Descendant(nuspec, "readme"));
    }

    [Theory]
    [InlineData("Andy.Tui.Core")]
    [InlineData("Andy.Tui.Widgets")]
    [InlineData("Andy.Tui.Layout")]
    [InlineData("Andy.Tui.CliWidgets")]
    public void Library_package_bundles_assembly_xmldoc_readme_and_icon(string packageId)
    {
        using var zip = ZipFile.OpenRead(_fx.NupkgPath(packageId));

        Assert.Contains(zip.Entries, e =>
            e.FullName.Equals($"lib/net8.0/{packageId}.dll", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(zip.Entries, e =>
            e.FullName.Equals($"lib/net8.0/{packageId}.xml", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(zip.Entries, e => e.Name.Equals("README.md", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(zip.Entries, e => e.Name.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Every_documented_library_produces_a_symbols_package()
    {
        foreach (var id in DocumentedPackages)
        {
            // The umbrella meta-package carries no assemblies, so it has no symbols.
            if (id == "Andy.Tui")
                continue;
            var snupkg = Path.Combine(_fx.FeedDir, $"{id}.{_fx.Version}.snupkg");
            Assert.True(File.Exists(snupkg), $"Missing symbols package for {id}: {snupkg}");
        }
    }

    [Fact]
    public void CliWidgets_depends_on_the_umbrella_meta_package()
    {
        using var zip = ZipFile.OpenRead(_fx.NupkgPath("Andy.Tui.CliWidgets"));
        var deps = ReadDependencyIds(zip, "Andy.Tui.CliWidgets");
        Assert.Contains("Andy.Tui", deps);
    }

    [Fact]
    public void Umbrella_csproj_no_longer_hand_copies_dlls_or_hides_dependencies()
    {
        var csproj = File.ReadAllText(
            Path.Combine(_fx.RepoRoot, "src", "Andy.Tui", "Andy.Tui.csproj"));

        // Old brittle model: hide project references and manually copy built DLLs.
        Assert.DoesNotContain("PrivateAssets=\"all\"", csproj);
        Assert.DoesNotContain("PackagePath=\"lib/", csproj);
        Assert.DoesNotContain("bin\\$(Configuration)", csproj);
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
