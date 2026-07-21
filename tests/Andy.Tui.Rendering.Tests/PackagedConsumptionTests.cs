using System.Diagnostics;

namespace Andy.Tui.Rendering.Tests;

/// <summary>
/// Installed-package harness. The heavyweight path packs Andy.Tui to a temporary local
/// feed and builds/runs a throwaway console app against it (see scripts/package-smoke-test.sh);
/// it is opt-in via the ANDY_TUI_PACKAGE_TEST=1 environment variable so the default test run stays
/// fast and hermetic across the many concurrent build workspaces in CI. The always-on assertions
/// guard the harness itself: the smoke-test script must exist and be runnable, and the packed
/// surface it depends on (the package's bundled assembly set) must stay complete.
/// </summary>
public class PackagedConsumptionTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Andy.Tui.sln")))
            dir = dir.Parent;
        Assert.NotNull(dir);
        return dir!.FullName;
    }

    [Fact]
    public void Package_Smoke_Test_Script_Is_Present_And_Consistent()
    {
        var root = RepoRoot();
        var script = Path.Combine(root, "scripts", "package-smoke-test.sh");
        Assert.True(File.Exists(script), $"missing package smoke-test script at {script}");

        var scriptText = File.ReadAllText(script);
        Assert.Contains("dotnet pack", scriptText);
        Assert.Contains("dotnet run", scriptText);
        Assert.Contains("CONSUMER_OK", scriptText);

        // The consumer references one package, which must bundle every runtime
        // library the pipeline needs. Guard the critical project references so a
        // packed-and-consumed app cannot silently lose an assembly.
        var pkgProj = File.ReadAllText(Path.Combine(root, "src", "Andy.Tui", "Andy.Tui.csproj"));
        foreach (var lib in new[]
        {
            "Andy.Tui.DisplayList", "Andy.Tui.Compositor", "Andy.Tui.Backend.Terminal",
            "Andy.Tui.Layout", "Andy.Tui.Widgets", "Andy.Tui.Input",
        })
        {
            Assert.Contains($"{lib}.csproj", pkgProj);
        }
    }

    [Fact]
    public void Packed_Artifact_Is_Consumable_By_A_Temporary_App()
    {
        if (Environment.GetEnvironmentVariable("ANDY_TUI_PACKAGE_TEST") != "1")
        {
            // Opt-in: skipped-by-default without leaving a permanently skipped test. Set
            // ANDY_TUI_PACKAGE_TEST=1 to exercise the full pack -> restore -> build -> run flow.
            return;
        }

        var root = RepoRoot();
        var script = Path.Combine(root, "scripts", "package-smoke-test.sh");
        var psi = new ProcessStartInfo("bash", $"\"{script}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = root,
        };
        using var proc = Process.Start(psi)!;
        string stdout = proc.StandardOutput.ReadToEnd();
        string stderr = proc.StandardError.ReadToEnd();
        proc.WaitForExit();

        Assert.True(proc.ExitCode == 0,
            $"package smoke test failed (exit {proc.ExitCode}).\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
        Assert.Contains("CONSUMER_OK", stdout);
    }
}
