using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Andy.Tui.CliWidgets.Tests;

/// <summary>
/// Guards the integrity of the project-reference graph for the CLI widgets area.
///
/// These tests exist because Andy.Tui.CliWidgets previously referenced only the
/// packaging project instead of its concrete compile-time dependencies. The
/// packaging project now references CliWidgets so it can bundle the assembly;
/// a reverse reference would create a cycle. The tests below detect:
///   1. Missing references — a &lt;ProjectReference&gt; that points at a file that does
///      not exist on disk (a dangling / typo'd reference).
///   2. Cyclic references — a dependency cycle between source projects.
/// They also assert the concrete compile-time dependencies that CLI widgets rely on.
/// </summary>
public class ProjectReferenceGraphTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src", "Andy.Tui.CliWidgets")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate repository root from " + AppContext.BaseDirectory);
    }

    private static IReadOnlyList<string> ReferencedProjectPaths(string csprojPath)
    {
        var doc = XDocument.Load(csprojPath);
        var baseDir = Path.GetDirectoryName(csprojPath)!;
        var refs = new List<string>();
        foreach (var pr in doc.Descendants().Where(e => e.Name.LocalName == "ProjectReference"))
        {
            var include = pr.Attribute("Include")?.Value;
            if (string.IsNullOrWhiteSpace(include))
                continue;
            var normalized = include.Replace('\\', Path.DirectorySeparatorChar);
            refs.Add(Path.GetFullPath(Path.Combine(baseDir, normalized)));
        }
        return refs;
    }

    private static IReadOnlyList<string> AllSourceProjects()
    {
        var srcDir = Path.Combine(RepoRoot(), "src");
        return Directory
            .EnumerateFiles(srcDir, "*.csproj", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();
    }

    [Fact]
    public void All_Project_References_Point_At_Existing_Files()
    {
        var missing = new List<string>();
        foreach (var proj in AllSourceProjects())
        {
            foreach (var reference in ReferencedProjectPaths(proj))
            {
                if (!File.Exists(reference))
                    missing.Add($"{Path.GetFileName(proj)} -> {reference}");
            }
        }

        Assert.True(missing.Count == 0,
            "Dangling project references detected:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void Source_Project_Reference_Graph_Is_Acyclic()
    {
        var projects = AllSourceProjects();
        // Adjacency keyed by full path, restricted to references that resolve to a source project.
        var graph = projects.ToDictionary(
            p => p,
            p => ReferencedProjectPaths(p).Where(File.Exists).ToList(),
            StringComparer.Ordinal);

        var state = new Dictionary<string, int>(StringComparer.Ordinal); // 0=unvisited,1=on-stack,2=done
        var cyclePath = new List<string>();

        bool HasCycle(string node, List<string> stack)
        {
            state[node] = 1;
            stack.Add(node);
            if (graph.TryGetValue(node, out var neighbors))
            {
                foreach (var next in neighbors)
                {
                    var s = state.TryGetValue(next, out var v) ? v : 0;
                    if (s == 1)
                    {
                        stack.Add(next);
                        cyclePath.AddRange(stack);
                        return true;
                    }
                    if (s == 0 && graph.ContainsKey(next) && HasCycle(next, stack))
                        return true;
                }
            }
            stack.RemoveAt(stack.Count - 1);
            state[node] = 2;
            return false;
        }

        foreach (var proj in projects)
        {
            if ((state.TryGetValue(proj, out var v) ? v : 0) == 0)
            {
                if (HasCycle(proj, new List<string>()))
                {
                    var names = cyclePath.Select(Path.GetFileNameWithoutExtension);
                    Assert.Fail("Cyclic project reference detected: " + string.Join(" -> ", names));
                }
            }
        }
    }

    [Fact]
    public void CliWidgets_Declares_Its_Compile_Time_Dependencies()
    {
        var cliWidgets = Path.Combine(RepoRoot(), "src", "Andy.Tui.CliWidgets", "Andy.Tui.CliWidgets.csproj");
        var referencedNames = ReferencedProjectPaths(cliWidgets)
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        // CLI widgets consume types from these namespaces directly, so they must
        // be referenced explicitly rather than through the packaging project.
        foreach (var required in new[] { "Andy.Tui.DisplayList", "Andy.Tui.Layout", "Andy.Tui.Widgets" })
        {
            Assert.True(referencedNames.Contains(required),
                $"Andy.Tui.CliWidgets must reference {required}. Actual references: {string.Join(", ", referencedNames)}");
        }
    }
}
