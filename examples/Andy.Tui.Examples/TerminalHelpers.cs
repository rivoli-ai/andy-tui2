using System;

namespace Andy.Tui.Examples;

public static class TerminalHelpers
{
    public static (int Width, int Height) PollResize((int Width, int Height) viewport, Andy.Tui.Core.FrameScheduler scheduler)
    {
        int cw = Console.WindowWidth;
        int ch = Console.WindowHeight;
        if (cw != viewport.Width || ch != viewport.Height)
        {
            scheduler.SetForceFullClear(true);
            return (cw, ch);
        }
        scheduler.SetForceFullClear(false);
        return viewport;
    }
}
