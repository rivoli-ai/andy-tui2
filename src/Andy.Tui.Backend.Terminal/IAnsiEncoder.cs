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
        // Color tracking must distinguish three states:
        //   - not yet emitted          (xEmitted == false)
        //   - explicit RGB color        (xEmitted == true, currentX has value)
        //   - transparent / default     (xEmitted == true, currentX == null)
        // so we cannot overload "currentX == null" to mean "unset".
        bool fgEmitted = false;
        Rgb24? currentFg = null;
        bool bgEmitted = false;
        Rgb24? currentBg = null;
        CellAttrFlags currentAttrs = CellAttrFlags.None;
        // The encoder is created fresh per frame and writes absolute SGR state, so
        // it must not assume the terminal starts in any particular state: emit a
        // single baseline reset for the first run so leftover attributes/colors
        // from a previous frame cannot leak in. (No runs => no output.)
        bool first = true;

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

            // Reset on the first run (establish a known baseline) or whenever the
            // attributes change. ESC[0m clears attributes and resets colors to the
            // terminal defaults, so force the colors to be re-emitted afterwards.
            if (first || run.Attrs != currentAttrs)
            {
                sb.Append("\x1b[0m");
                currentAttrs = run.Attrs;
                currentFg = null; currentBg = null;
                fgEmitted = false; bgEmitted = false;
                ApplyAttrs(sb, run.Attrs);
                first = false;
            }

            // Foreground
            if (!fgEmitted || currentFg != run.Fg)
            {
                if (run.Fg is null)
                {
                    // Transparent: reset to the terminal's default foreground.
                    sb.Append("\x1b[39m");
                }
                else if (caps.TrueColor)
                {
                    var fg = run.Fg.Value;
                    sb.Append($"\x1b[38;2;{fg.R};{fg.G};{fg.B}m");
                }
                else if (caps.Palette256)
                {
                    var fg = run.Fg.Value;
                    var idx = AnsiColorMapping.RgbTo256Color(fg.R, fg.G, fg.B);
                    sb.Append($"\x1b[38;5;{idx}m");
                }
                else
                {
                    var fg = run.Fg.Value;
                    var idx = AnsiColorMapping.RgbTo16Color(fg.R, fg.G, fg.B);
                    sb.Append(GetBasicColorCode(idx, isForeground: true));
                }
                currentFg = run.Fg;
                fgEmitted = true;
            }
            if (!bgEmitted || currentBg != run.Bg)
            {
                if (run.Bg is null)
                {
                    // Transparent: reset to the terminal's default background so the
                    // terminal's own background (including transparency) shows through.
                    sb.Append("\x1b[49m");
                }
                else if (caps.TrueColor)
                {
                    var bg = run.Bg.Value;
                    sb.Append($"\x1b[48;2;{bg.R};{bg.G};{bg.B}m");
                }
                else if (caps.Palette256)
                {
                    var bg = run.Bg.Value;
                    var idx = AnsiColorMapping.RgbTo256Color(bg.R, bg.G, bg.B);
                    sb.Append($"\x1b[48;5;{idx}m");
                }
                else
                {
                    var bg = run.Bg.Value;
                    var idx = AnsiColorMapping.RgbTo16Color(bg.R, bg.G, bg.B);
                    sb.Append(GetBasicColorCode(idx, isForeground: false));
                }
                currentBg = run.Bg;
                bgEmitted = true;
            }

            // Defense in depth at the terminal boundary: run text is written verbatim to
            // the terminal, so any control character it still carries would execute as a
            // command. The compositor already rewrites controls to inert placeholders, but
            // a RowRun can be constructed directly (bypassing the compositor), so sanitize
            // here as well. Sanitize returns the same instance when there is nothing to fix.
            sb.Append(TerminalText.Sanitize(run.Text));
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
