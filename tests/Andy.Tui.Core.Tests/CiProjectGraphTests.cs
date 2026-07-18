using System;
using System.IO;
using System.Linq;

namespace Andy.Tui.Core.Tests;

/// <summary>
/// Guards the CI project-graph contract from issue #27: CI must validate the
/// complete project graph from a clean checkout, both workflows must share one
/// build+test definition, the release workflow must not publish when the graph
/// fails, every test assembly must run or be an explicit tracked exclusion, and
/// documented local commands must match CI commands.
///
/// These tests read the repository's workflow and script files directly, so a
/// regression in the CI harness fails the .NET test suite rather than silently
/// drifting.
/// </summary>
public class CiProjectGraphTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Andy.Tui.sln")))
        {
            dir = dir.Parent;
        }

        Assert.True(dir is not null, "Could not locate repository root (Andy.Tui.sln) from the test base directory.");
        return dir!.FullName;
    }

    private static string ReadRepoFile(string relativePath)
    {
        var full = Path.Combine(RepoRoot(), relativePath);
        Assert.True(File.Exists(full), $"Expected repository file to exist: {relativePath}");
        return File.ReadAllText(full);
    }

    [Fact]
    public void Shared_Ci_Script_Exists_And_Defines_The_Graph_Contract()
    {
        var script = ReadRepoFile(Path.Combine("scripts", "ci-graph-test.sh"));

        // Restores and builds the whole solution (the complete graph).
        Assert.Contains("dotnet restore \"${SOLUTION}\"", script);
        Assert.Contains("dotnet build \"${SOLUTION}\"", script);
        Assert.Contains("Andy.Tui.sln", script);

        // Clean-checkout guard.
        Assert.Contains("--require-clean", script);
        Assert.Contains("clean checkout", script);

        // Runs test projects only after confirming their binaries were produced.
        Assert.Contains("was NOT produced by this build", script);

        // Lists every test assembly in the job output.
        Assert.Contains("Test project inventory", script);
    }

    [Fact]
    public void Ci_Workflow_Uses_Shared_Script_For_Debug_And_Release()
    {
        var ci = ReadRepoFile(Path.Combine(".github", "workflows", "ci.yml"));

        Assert.Contains("scripts/ci-graph-test.sh", ci);

        // The word "Release" also appears in ci.yml's header comment, so a bare
        // Contains("Release") check would still pass even if the Release matrix
        // entry were dropped. Assert the strategy matrix itself lists BOTH
        // configurations by parsing the inline "configuration: [ ... ]" list.
        var configurations = ParseMatrixConfigurations(ci);
        Assert.Contains("Debug", configurations);
        Assert.Contains("Release", configurations);
    }

    /// <summary>
    /// Extracts the values of the strategy matrix's inline "configuration"
    /// list from a workflow file, e.g. "configuration: [ Debug, Release ]".
    /// </summary>
    private static System.Collections.Generic.HashSet<string> ParseMatrixConfigurations(string workflow)
    {
        var line = workflow
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(l => l.Trim())
            .FirstOrDefault(l => l.StartsWith("configuration:")
                              && l.Contains('[') && l.Contains(']'));

        Assert.False(line is null,
            "ci.yml must define an inline strategy matrix list 'configuration: [ ... ]'.");

        var open = line!.IndexOf('[');
        var close = line.IndexOf(']');
        var inner = line.Substring(open + 1, close - open - 1);

        return inner
            .Split(',')
            .Select(e => e.Trim())
            .Where(e => e.Length > 0)
            .ToHashSet();
    }

    [Fact]
    public void Release_Workflow_Uses_Shared_Script_And_Gates_Publishing()
    {
        var release = ReadRepoFile(Path.Combine(".github", "workflows", "build-and-release.yml"));

        // Same build+test definition as ci.yml, exercised in Release.
        Assert.Contains("scripts/ci-graph-test.sh --configuration Release", release);

        // Publish jobs must depend on the build job so they cannot publish when
        // any source or test project in the graph fails.
        Assert.Contains("publish-prerelease:", release);
        Assert.Contains("publish-release:", release);

        var lines = release.Replace("\r\n", "\n").Split('\n');
        int needsBuildCount = lines.Count(l => l.Trim() == "needs: build");
        Assert.True(needsBuildCount >= 2,
            $"Both publish jobs must declare 'needs: build'; found {needsBuildCount}.");
    }

    [Fact]
    public void Documented_Commands_Reference_The_Shared_Script()
    {
        // "Local documented commands match CI commands."
        foreach (var doc in new[] { "README.md", "CLAUDE.md", "agents.md" })
        {
            var content = ReadRepoFile(doc);
            Assert.True(content.Contains("scripts/ci-graph-test.sh"),
                $"{doc} must document the shared CI script so local commands match CI.");
        }
    }

    [Fact]
    public void Every_Test_Project_Is_A_Solution_Member_Or_A_Tracked_Exclusion()
    {
        var root = RepoRoot();
        var sln = ReadRepoFile("Andy.Tui.sln");
        var script = ReadRepoFile(Path.Combine("scripts", "ci-graph-test.sh"));

        // Parse the tracked exclusion directories from the script's EXCLUSIONS
        // array (entries look like: "tests/Andy.Tui.Foo|reason").
        var excluded = script
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("\"tests/") && l.Contains('|'))
            .Select(l => l.Trim('"').Split('|')[0])
            .Select(p => p.Replace('/', Path.DirectorySeparatorChar))
            .ToHashSet();

        // The two known gaps must be tracked explicitly.
        Assert.Contains(Path.Combine("tests", "Andy.Tui.Parity.Playwright"), excluded);
        Assert.Contains(Path.Combine("tests", "Andy.Tui.CliWidgets.Tests"), excluded);

        var testProjects = Directory
            .GetFiles(Path.Combine(root, "tests"), "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToList();

        Assert.NotEmpty(testProjects);

        foreach (var projPath in testProjects)
        {
            var projDir = Path.GetRelativePath(root, Path.GetDirectoryName(projPath)!);
            var fileName = Path.GetFileName(projPath);

            bool inSolution = sln.Contains(fileName, StringComparison.OrdinalIgnoreCase);
            bool isExcluded = excluded.Contains(projDir);

            Assert.True(inSolution || isExcluded,
                $"Test project '{projDir}' is neither a member of the solution nor a tracked exclusion. " +
                "Add it to Andy.Tui.sln (#28) or to the EXCLUSIONS list in scripts/ci-graph-test.sh.");
        }
    }
}
