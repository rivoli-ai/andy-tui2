using Andy.Tui.Backend.Terminal;

namespace Andy.Tui.Rendering.Tests;

public class CapabilityDetectionTests
{
    [Fact]
    public void Detects_Truecolor_From_COLORTERM()
    {
        try
        {
            Environment.SetEnvironmentVariable("COLORTERM", "truecolor");
            var caps = CapabilityDetector.DetectFromEnvironment();
            Assert.True(caps.TrueColor);
            Assert.True(caps.Palette256);
        }
        finally
        {
            Environment.SetEnvironmentVariable("COLORTERM", null);
        }
    }
}
