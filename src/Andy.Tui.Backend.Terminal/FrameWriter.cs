using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Compositor;

namespace Andy.Tui.Backend.Terminal;

public sealed class FrameWriter
{
    private readonly IAnsiEncoder _encoder;

    public FrameWriter(IAnsiEncoder? encoder = null)
    {
        _encoder = encoder ?? new AnsiEncoder();
    }

    public Task RenderFrameAsync(IReadOnlyList<RowRun> runs, TerminalCapabilities caps, IPtyIo pty, CancellationToken cancellationToken = default)
    {
        var bytes = _encoder.Encode(runs, caps);
        return pty.WriteAsync(bytes, cancellationToken);
    }
}
