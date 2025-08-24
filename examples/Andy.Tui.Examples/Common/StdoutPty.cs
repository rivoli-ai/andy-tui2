using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;

namespace Andy.Tui.Examples;

sealed class StdoutPty : IPtyIo
{
    public Task WriteAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken cancellationToken)
    {
        Console.Write(Encoding.UTF8.GetString(frameBytes.Span));
        return Task.CompletedTask;
    }
}
