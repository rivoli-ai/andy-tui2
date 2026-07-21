namespace Andy.Tui.Packaging.Tests;

/// <summary>
/// End-to-end smoke test: a fresh consumer project that has NO project references
/// restores only the produced package, compiles against documented public APIs,
/// and loads the assemblies at runtime.
/// </summary>
[Collection("packaging")]
public sealed class ConsumerSmokeTests
{
    private readonly PackagingFixture _fx;

    public ConsumerSmokeTests(PackagingFixture fx) => _fx = fx;

    [Fact]
    public void Fresh_consumer_restores_produced_packages_and_runs()
    {
        var consumerDir = Path.Combine(Path.GetTempPath(), "andytui-consumer-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(consumerDir);
        try
        {
            // Restore exclusively from the produced local feed (plus nuget.org as a
            // fallback for framework reference assemblies). No project references.
            File.WriteAllText(Path.Combine(consumerDir, "nuget.config"),
                $"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <packageSources>
                    <clear />
                    <add key="local" value="{_fx.FeedDir}" />
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                  </packageSources>
                </configuration>
                """);

            File.WriteAllText(Path.Combine(consumerDir, "Consumer.csproj"),
                $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net8.0</TargetFramework>
                    <Nullable>enable</Nullable>
                    <ImplicitUsings>enable</ImplicitUsings>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Andy.Tui" Version="{_fx.Version}" />
                  </ItemGroup>
                </Project>
                """);

            // Documented public APIs reached through the single bundled package,
            // including the formerly separate CLI widgets assembly.
            File.WriteAllText(Path.Combine(consumerDir, "Program.cs"),
                """
                using Andy.Tui.DisplayList;
                using Andy.Tui.CliWidgets;

                var builder = new DisplayListBuilder();
                builder.DrawRect(new Rect(0, 0, 10, 2, new Rgb24(10, 20, 30)));
                var displayList = builder.Build();

                var counter = new TokenCounter();
                counter.AddTokens(3, 4);

                Console.WriteLine($"CONSUMER_OK ops={displayList.Ops.Count}");
                """);

            var (exit, stdout, stderr) = _fx.RunDotnet(
                "run --configuration Release", consumerDir);

            Assert.True(exit == 0,
                $"Consumer failed to restore/compile/run (exit {exit}).\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
            Assert.Contains("CONSUMER_OK ops=1", stdout);
        }
        finally
        {
            try { Directory.Delete(consumerDir, recursive: true); } catch { /* best effort */ }
        }
    }
}
