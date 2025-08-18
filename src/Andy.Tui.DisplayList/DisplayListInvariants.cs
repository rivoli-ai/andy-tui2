using System;
using System.Collections.Generic;

namespace Andy.Tui.DisplayList;

/// <summary>
/// Validates invariants of a <see cref="DisplayList"/> similar to v1, adapted to v2 ops.
/// </summary>
public static class DisplayListInvariants
{
    public static void Validate(DisplayList dl)
    {
        if (dl is null) throw new DisplayListInvariantViolationException("Display list is null");
        var clipDepth = 0;
        var lastLayer = int.MinValue;
        var clipStack = new Stack<(int X, int Y, int W, int H)>();

        foreach (var op in dl.Ops)
        {
            switch (op)
            {
                case ClipPush cp:
                    // compute intersection with current clip (if any); must be non-empty
                    if (clipStack.Count == 0)
                    {
                        clipStack.Push((cp.X, cp.Y, cp.Width, cp.Height));
                    }
                    else
                    {
                        var top = clipStack.Peek();
                        var x1 = Math.Max(top.X, cp.X);
                        var y1 = Math.Max(top.Y, cp.Y);
                        var x2 = Math.Min(top.X + top.W, cp.X + cp.Width);
                        var y2 = Math.Min(top.Y + top.H, cp.Y + cp.Height);
                        var w = Math.Max(0, x2 - x1);
                        var h = Math.Max(0, y2 - y1);
                        if (w <= 0 || h <= 0)
                            throw new DisplayListInvariantViolationException("Pushed clip has no intersection with current clip");
                        clipStack.Push((x1, y1, w, h));
                    }
                    clipDepth++;
                    break;
                case Pop:
                    if (clipDepth <= 0)
                        throw new DisplayListInvariantViolationException("Pop without matching ClipPush/LayerPush");
                    clipDepth--;
                    if (clipStack.Count > 0) clipStack.Pop();
                    break;
                case LayerPush:
                    // Model layer push/pop using the same Pop token; ensure monotonic non-decreasing layer count
                    lastLayer = Math.Max(lastLayer, clipDepth);
                    clipDepth++; // enter layer scope
                    break;
                case Rect:
                case Border:
                case TextRun:
                    // For now enforce monotonic layer by sequence within frame using stack depth as a proxy
                    if (clipDepth < lastLayer)
                        throw new DisplayListInvariantViolationException("Layer order decreased within frame");
                    break;
            }
        }

        if (clipDepth != 0)
            throw new DisplayListInvariantViolationException($"Unbalanced push/pop count. Remaining depth={clipDepth}");
    }
}

public sealed class DisplayListInvariantViolationException : Exception
{
    public DisplayListInvariantViolationException(string message) : base(message) { }
}
