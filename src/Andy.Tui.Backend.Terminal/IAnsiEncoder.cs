using System;
using System.Collections.Generic;
using System.Text;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

namespace Andy.Tui.Backend.Terminal;

public interface IAnsiEncoder
{
    ReadOnlyMemory<byte> Encode(IReadOnlyList<RowRun> runs, TerminalCapabilities caps);
}

public sealed class AnsiEncoder : IAnsiEncoder
{
    public ReadOnlyMemory<byte> Encode(IReadOnlyList<RowRun> runs, TerminalCapabilities caps)
    {
        var sb = new StringBuilder(1024);
        int currentRow = -1;
        Rgb24? currentFg = null;
        Rgb24? currentBg = null;
        CellAttrFlags currentAttrs = CellAttrFlags.None;

        foreach (var run in runs)
        {
            if (run.Row != currentRow)
            {
                // move cursor to row start (1-based)
                sb.Append($"\x1b[{run.Row + 1};{run.ColStart + 1}H");
                currentRow = run.Row;
            }
            else
            {
                // move to column start
                sb.Append($"\x1b[{run.Row + 1};{run.ColStart + 1}H");
            }

            // Reset if attrs changed
            if (run.Attrs != currentAttrs)
            {
                sb.Append("\x1b[0m");
                currentAttrs = run.Attrs;
                currentFg = null; currentBg = null;
                ApplyAttrs(sb, run.Attrs);
            }

            // Colors
            if (currentFg is null || currentFg.Value != run.Fg)
            {
                if (caps.TrueColor)
                {
                    sb.Append($"\x1b[38;2;{run.Fg.R};{run.Fg.G};{run.Fg.B}m");
                }
                else if (caps.Palette256)
                {
                    var idx = AnsiColorMapping.RgbTo256Color(run.Fg.R, run.Fg.G, run.Fg.B);
                    sb.Append($"\x1b[38;5;{idx}m");
                }
                else
                {
                    var idx = AnsiColorMapping.RgbTo16Color(run.Fg.R, run.Fg.G, run.Fg.B);
                    sb.Append(GetBasicColorCode(idx, isForeground: true));
                }
                currentFg = run.Fg;
            }
            if (currentBg is null || currentBg.Value != run.Bg)
            {
                if (caps.TrueColor)
                {
                    sb.Append($"\x1b[48;2;{run.Bg.R};{run.Bg.G};{run.Bg.B}m");
                }
                else if (caps.Palette256)
                {
                    var idx = AnsiColorMapping.RgbTo256Color(run.Bg.R, run.Bg.G, run.Bg.B);
                    sb.Append($"\x1b[48;5;{idx}m");
                }
                else
                {
                    var idx = AnsiColorMapping.RgbTo16Color(run.Bg.R, run.Bg.G, run.Bg.B);
                    sb.Append(GetBasicColorCode(idx, isForeground: false));
                }
                currentBg = run.Bg;
            }

            // We cannot determine exact fg/bg per run without carrying them; encode just text with attrs.
            sb.Append(run.Text);
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void ApplyAttrs(StringBuilder sb, CellAttrFlags attrs)
    {
        if ((attrs & CellAttrFlags.Bold) != 0) sb.Append("\x1b[1m");
        if ((attrs & CellAttrFlags.Italic) != 0) sb.Append("\x1b[3m");
        if ((attrs & CellAttrFlags.Underline) != 0) sb.Append("\x1b[4m");
        if ((attrs & CellAttrFlags.DoubleUnderline) != 0) sb.Append("\x1b[21m");
        if ((attrs & CellAttrFlags.Strikethrough) != 0) sb.Append("\x1b[9m");
        if ((attrs & CellAttrFlags.Dim) != 0) sb.Append("\x1b[2m");
        if ((attrs & CellAttrFlags.Blink) != 0) sb.Append("\x1b[5m");
        if ((attrs & CellAttrFlags.Reverse) != 0) sb.Append("\x1b[7m");
    }
    private static string GetBasicColorCode(int colorIndex, bool isForeground)
    {
        if (colorIndex < 8)
        {
            var code = (isForeground ? 30 : 40) + colorIndex;
            return $"\x1b[{code}m";
        }
        else
        {
            var code = (isForeground ? 90 : 100) + (colorIndex - 8);
            return $"\x1b[{code}m";
        }
    }
}
