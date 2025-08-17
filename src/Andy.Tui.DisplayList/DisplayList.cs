using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Andy.Tui.DisplayList;

/// <summary>
/// Immutable display list for a single frame.
/// </summary>
public sealed class DisplayList
{
    public IReadOnlyList<IDisplayOp> Ops { get; }

    internal DisplayList(List<IDisplayOp> ops)
    {
        Ops = new ReadOnlyCollection<IDisplayOp>(ops.ToArray());
    }
}