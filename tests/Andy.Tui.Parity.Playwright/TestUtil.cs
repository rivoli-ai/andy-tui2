using System;
using System.Threading.Tasks;
using PW = Microsoft.Playwright;
using System.Diagnostics.CodeAnalysis;

namespace Andy.Tui.Parity.Playwright;

internal static class TestUtil
{
    public static async Task<PW.IPlaywright?> TryCreatePlaywrightAsync()
    {
        try
        {
            return await PW.Playwright.CreateAsync();
        }
        catch (Exception ex)
        {
            // Not installed locally; return null so tests can early-return
            Console.WriteLine($"Playwright not available: {ex.Message}");
            return null;
        }
    }
}
