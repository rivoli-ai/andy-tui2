using System.Text;

namespace Andy.Tui.Input;

public sealed class TtyStreamDecoder
{
    private bool _inPaste;
    private readonly StringBuilder _paste = new();

    public IEnumerable<IInputEvent> Push(byte[] chunk)
    {
        var s = Encoding.UTF8.GetString(chunk);
        int i = 0;
        while (i < s.Length)
        {
            if (_inPaste)
            {
                // Look for end: ESC[201~
                int end = s.IndexOf("\u001b[201~", i, StringComparison.Ordinal);
                if (end >= 0)
                {
                    _paste.Append(s.AsSpan(i, end - i));
                    _inPaste = false;
                    i = end + "\u001b[201~".Length;
                    yield return new PasteEvent(_paste.ToString());
                    _paste.Clear();
                }
                else
                {
                    _paste.Append(s.AsSpan(i));
                    yield break;
                }
            }
            else if (s.AsSpan(i).StartsWith("\u001b[200~"))
            {
                _inPaste = true;
                i += "\u001b[200~".Length;
            }
            else if (s.AsSpan(i).StartsWith("\u001b[<"))
            {
                // SGR mouse: ESC[<b;x;yM or m
                int mEnd = s.IndexOfAny(new[] { 'M', 'm' }, i);
                if (mEnd > i)
                {
                    var payload = s.Substring(i + 3, mEnd - (i + 3)); // after "\x1b[<"
                    var parts = payload.Split(';');
                    if (parts.Length >= 3 && int.TryParse(parts[0], out var b) && int.TryParse(parts[1], out var x) && int.TryParse(parts[2], out var y))
                    {
                        var kind = s[mEnd] == 'M' ? MouseKind.Down : MouseKind.Up;
                        var button = (b & 3) switch { 0 => MouseButton.Left, 1 => MouseButton.Middle, 2 => MouseButton.Right, _ => MouseButton.None };
                        var mods = KeyModifiers.None;
                        if ((b & 4) != 0) mods |= KeyModifiers.Shift;
                        if ((b & 8) != 0) mods |= KeyModifiers.Alt;
                        if ((b & 16) != 0) mods |= KeyModifiers.Ctrl;
                        if ((b & 32) != 0)
                        {
                            // motion event: encode as Move regardless of button
                            kind = MouseKind.Move;
                            button = MouseButton.None;
                        }
                        if ((b & 64) != 0) // wheel
                        {
                            kind = MouseKind.Wheel;
                            button = MouseButton.None;
                            int delta = ((b & 1) == 0) ? 1 : -1; // up/down
                            yield return new MouseEvent(kind, x - 1, y - 1, button, mods, delta);
                        }
                        else
                        {
                            yield return new MouseEvent(kind, x - 1, y - 1, button, mods);
                        }
                    }
                    i = mEnd + 1;
                }
                else
                {
                    // incomplete
                    yield break;
                }
            }
            else if (s.AsSpan(i).StartsWith("\u001b["))
            {
                // CSI ... final
                int finalIdx = i + 2;
                while (finalIdx < s.Length && !(s[finalIdx] >= '@' && s[finalIdx] <= '~')) finalIdx++;
                if (finalIdx >= s.Length) yield break;
                char final = s[finalIdx];
                var paramsStr = s.Substring(i + 2, finalIdx - (i + 2));
                if (final is 'A' or 'B' or 'C' or 'D')
                {
                    var mods = KeyModifiers.None;
                    if (paramsStr.Contains(';'))
                    {
                        var parts = paramsStr.Split(';');
                        if (int.TryParse(parts[^1], out var modCode))
                        {
                            // xterm encodes as 1 + (shift=1, alt=2, ctrl=4)
                            int flags = Math.Max(0, modCode - 1);
                            if ((flags & 1) != 0) mods |= KeyModifiers.Shift;
                            if ((flags & 2) != 0) mods |= KeyModifiers.Alt;
                            if ((flags & 4) != 0) mods |= KeyModifiers.Ctrl;
                        }
                    }
                    string key = final switch { 'A' => "ArrowUp", 'B' => "ArrowDown", 'C' => "ArrowRight", 'D' => "ArrowLeft", _ => "" };
                    yield return new KeyEvent(key, key, mods);
                }
                i = finalIdx + 1;
            }
            else if (s[i] == '\u001b')
            {
                // Alt-modified printable: ESC followed by char
                if (i + 1 < s.Length)
                {
                    char ch = s[i + 1];
                    if (!char.IsControl(ch))
                    {
                        yield return new KeyEvent(ch.ToString(), ch.ToString(), KeyModifiers.Alt);
                        i += 2;
                        continue;
                    }
                }
                // If not printable or incomplete, stop to await more input
                yield break;
            }
            else
            {
                // printable
                var ch = s[i];
                if (!char.IsControl(ch))
                {
                    yield return new KeyEvent(ch.ToString(), ch.ToString(), KeyModifiers.None);
                }
                else
                {
                    // Map common Ctrl combinations (ASCII 1-26) to letters with Ctrl
                    int code = ch;
                    if (code >= 1 && code <= 26)
                    {
                        char letter = (char)('A' + (code - 1));
                        yield return new KeyEvent(letter.ToString(), letter.ToString(), KeyModifiers.Ctrl);
                    }
                }
                i++;
            }
        }
    }
}
