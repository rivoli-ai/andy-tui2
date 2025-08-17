using System;
using System.Threading;
using System.Threading.Tasks;

namespace Andy.Tui.Backend.Terminal;

public interface IPtyIo
{
    Task WriteAsync(ReadOnlyMemory<byte> frameBytes, CancellationToken cancellationToken);
}
