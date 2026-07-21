using System.Diagnostics;

namespace Andy.Tui.Packaging.Tests;

/// <summary>
/// Invokes pack across every source project into a private local NuGet feed. Only
/// Andy.Tui is allowed to produce a package. Shared across the packaging test
/// classes so the relatively expensive pack happens once per test run.
/// </summary>
public sealed class PackagingFixture : IDisposable
{
    /// <summary>Absolute path to the repository root.</summary>
    public string RepoRoot { get; }

    /// <summary>Directory containing the produced *.nupkg files.</summary>
    public string FeedDir { get; }

    /// <summary>Unique package version used for this test run.</summary>
    public string Version { get; }

    /// <summary>Ids of the packages that were produced, in sorted order.</summary>
    public IReadOnlyList<string> ProducedPackageIds { get; }

    private readonly string _workDir;

    public PackagingFixture()
    {
        RepoRoot = FindRepoRoot();
        Version = "0.0.0-packtest" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");

        _workDir = Path.Combine(Path.GetTempPath(), "andytui-pack-" + Guid.NewGuid().ToString("N"));
        FeedDir = Path.Combine(_workDir, "feed");
        Directory.CreateDirectory(FeedDir);

        var projects = Directory
            .GetFiles(Path.Combine(RepoRoot, "src"), "*.csproj", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

        Assert.NotEmpty(projects);

        // Pack every source project in a SINGLE MSBuild invocation. This proves
        // that IsPackable defaults prevent accidental component packages even if
        // a future workflow mistakenly targets the complete project graph.
        PackAll(projects);

        ProducedPackageIds = Directory
            .GetFiles(FeedDir, "*.nupkg")
            .Select(Path.GetFileName)
            .Where(n => n is not null)
            .Select(n => n!.Substring(0, n!.Length - (".nupkg".Length + Version.Length + 1)))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
    }

    private void PackAll(IReadOnlyList<string> projects)
    {
        // A throwaway MSBuild "traversal" project: restore all listed projects,
        // then pack them in parallel into the local feed with our test version.
        var items = string.Join(
            Environment.NewLine,
            projects.Select(p => $"    <ProjectFile Include=\"{p}\" />"));

        var proj = $"""
            <Project>
              <ItemGroup>
            {items}
              </ItemGroup>
              <Target Name="Build">
                <MSBuild Projects="@(ProjectFile)"
                         Targets="Restore"
                         Properties="Configuration=Release" />
                <MSBuild Projects="@(ProjectFile)"
                         Targets="Pack"
                         BuildInParallel="false"
                         Properties="Configuration=Release;PackageVersion={Version};PackageOutputPath={FeedDir}" />
              </Target>
            </Project>
            """;

        var projPath = Path.Combine(_workDir, "packall.proj");
        File.WriteAllText(projPath, proj);

        var (exit, stdout, stderr) = RunDotnet($"build \"{projPath}\" -nodeReuse:false", RepoRoot);
        if (exit != 0)
        {
            throw new InvalidOperationException(
                $"Packing the documented libraries failed (exit {exit}).\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        }
    }

    /// <summary>Full path to the produced .nupkg for the given package id.</summary>
    public string NupkgPath(string packageId) =>
        Path.Combine(FeedDir, $"{packageId}.{Version}.nupkg");

    public (int ExitCode, string StdOut, string StdErr) RunDotnet(string arguments, string workingDir)
    {
        var psi = new ProcessStartInfo("dotnet", arguments)
        {
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        // Use the warm machine NuGet cache; produced packages use a unique version
        // per run so there is no risk of resolving a stale cached copy.
        psi.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        psi.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

        using var proc = Process.Start(psi)!;
        // Read both streams concurrently to avoid a full-buffer deadlock.
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        if (!proc.WaitForExit(300_000))
        {
            try { proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
            return (-1, stdoutTask.GetAwaiter().GetResult(), "Timed out after 300s.\n" + stderrTask.GetAwaiter().GetResult());
        }
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();
        return (proc.ExitCode, stdout, stderr);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Andy.Tui.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate repository root (Andy.Tui.sln not found).");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_workDir))
                Directory.Delete(_workDir, recursive: true);
        }
        catch
        {
            // Best-effort cleanup of a temp directory.
        }
    }
}

[CollectionDefinition("packaging")]
public sealed class PackagingCollection : ICollectionFixture<PackagingFixture>
{
}
