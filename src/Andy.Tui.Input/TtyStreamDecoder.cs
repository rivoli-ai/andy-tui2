using System.Text;

namespace Andy.Tui.Input;

/// <summary>
/// Configuration for <see cref="TtyStreamDecoder"/>. All limits guard against
/// unbounded memory growth and hangs when fed malformed or hostile input.
/// </summary>
public sealed class TtyStreamDecoderOptions
{
    /// <summary>Maximum number of parameter/intermediate bytes allowed in a CSI/SS3
    /// escape sequence before it is treated as malformed and dropped.</summary>
    public int MaxEscapeLength { get; init; } = 128;

    /// <summary>Maximum number of bytes buffered for a single bracketed paste before
    /// it is force-flushed (recovery from a missing paste terminator).</summary>
    public int MaxPasteBytes { get; init; } = 8 * 1024 * 1024;

    /// <summary>Emit <see cref="FocusEvent"/> for terminal focus in/out reports.</summary>
    public bool EnableFocusEvents { get; init; } = true;

    /// <summary>Emit <see cref="MouseEvent"/> for SGR mouse reports.</summary>
    public bool EnableMouse { get; init; } = true;
}

/// <summary>
/// A persistent, incremental terminal input decoder. Bytes are pushed in arbitrary
/// chunks and the decoder maintains all state (partial UTF-8 scalars, partial escape
/// sequences, and in-progress bracketed paste) across <see cref="Push"/> calls so that
/// the produced event sequence is independent of how the byte stream is partitioned.
/// </summary>
public sealed class TtyStreamDecoder
{
    private static readonly byte[] PasteStart = { 0x1b, (byte)'[', (byte)'2', (byte)'0', (byte)'0', (byte)'~' };
    private static readonly byte[] PasteEnd = { 0x1b, (byte)'[', (byte)'2', (byte)'0', (byte)'1', (byte)'~' };

    private readonly TtyStreamDecoderOptions _options;

    // Undecoded bytes carried over from the previous Push (a partial scalar, a partial
    // escape sequence, or the trailing bytes of paste content that might begin a
    // paste terminator).
    private byte[] _pending = Array.Empty<byte>();

    private bool _inPaste;
    private readonly List<byte> _paste = new();

    // Set after an escape sequence overflows its length limit. While true, incoming bytes
    // are discarded until a safe resynchronization boundary (the next ESC, which starts a
    // fresh sequence, or a CSI final byte in 0x40..0x7e that ends the malformed one) so
    // that leftover parameter bytes are never re-emitted as phantom key events.
    private bool _discarding;

    public TtyStreamDecoder(TtyStreamDecoderOptions? options = null)
    {
        _options = options ?? new TtyStreamDecoderOptions();
    }

    private enum ScalarStatus { Done, NeedMore, Invalid }

    /// <summary>
    /// Feed a chunk of bytes and return the events that can be fully decoded from the
    /// data available so far. Any trailing incomplete token is retained internally and
    /// completed by a later <see cref="Push"/> or reported by <see cref="Flush"/>.
    /// </summary>
    public IEnumerable<IInputEvent> Push(byte[] chunk)
    {
        var events = new List<IInputEvent>();
        byte[] data = _pending.Length == 0
            ? (chunk ?? Array.Empty<byte>())
            : Concat(_pending, chunk ?? Array.Empty<byte>());

        int i = 0;
        int len = data.Length;

        while (i < len)
        {
            if (_discarding)
            {
                i = DiscardUntilBoundary(data, i);
                if (_discarding) break; // boundary not in this chunk: consumed all, keep discarding
                continue;
            }

            if (_inPaste)
            {
                int consumedTo = HandlePaste(data, i, events, out bool needMore);
                i = consumedTo;
                if (needMore) break;
                continue;
            }

            byte b = data[i];
            if (b == 0x1b)
            {
                int consumed = ParseEscape(data, i, events);
                if (consumed < 0) break;           // need more bytes
                if (consumed == 0) { i = len; break; } // safety: never stall
                i += consumed;
                continue;
            }

            var status = TryDecodeScalar(data, i, out int scalar, out int slen);
            if (status == ScalarStatus.NeedMore) break;
            if (status == ScalarStatus.Invalid)
            {
                EmitScalar(0xFFFD, events);
                i += 1;
                continue;
            }
            EmitScalar(scalar, events);
            i += slen;
        }

        _pending = i >= len ? Array.Empty<byte>() : data[i..];
        return events;
    }

    /// <summary>
    /// Signal end-of-stream. Emits a best-effort event for any buffered data: an
    /// in-progress paste is emitted with the content gathered so far, an incomplete
    /// UTF-8 scalar becomes a single replacement character, and an incomplete escape
    /// sequence is discarded.
    /// </summary>
    public IEnumerable<IInputEvent> Flush()
    {
        var events = new List<IInputEvent>();

        if (_inPaste)
        {
            if (_pending.Length > 0) { AppendPaste(_pending); _pending = Array.Empty<byte>(); }
            events.Add(new PasteEvent(Encoding.UTF8.GetString(_paste.ToArray())));
            _paste.Clear();
            _inPaste = false;
            return events;
        }

        if (_pending.Length > 0)
        {
            // A leftover ESC-prefixed run is an incomplete escape sequence: drop it.
            // Anything else can only be an incomplete UTF-8 scalar: one replacement char.
            if (_pending[0] != 0x1b)
            {
                EmitScalar(0xFFFD, events);
            }
            _pending = Array.Empty<byte>();
        }

        return events;
    }

    // Skip bytes of an overflowed escape sequence until a resynchronization boundary.
    // Stops before the next ESC (which begins a fresh sequence) or after a CSI final byte
    // in 0x40..0x7e (which ends the malformed one). Returns the resume index; clears
    // _discarding once a boundary is found, otherwise consumes the whole chunk.
    private int DiscardUntilBoundary(byte[] data, int i)
    {
        int len = data.Length;
        while (i < len)
        {
            byte b = data[i];
            if (b == 0x1b) { _discarding = false; return i; }        // ESC: start a new sequence here
            if (b >= 0x40 && b <= 0x7e) { _discarding = false; return i + 1; } // final byte: consume it
            i++;
        }
        return i; // no boundary in this chunk; remain in discard mode
    }

    // Returns the index up to which bytes were consumed. needMore==true means the
    // remaining bytes (from the returned index) must be retained for the next Push.
    private int HandlePaste(byte[] data, int i, List<IInputEvent> events, out bool needMore)
    {
        needMore = false;
        int len = data.Length;
        int k = IndexOf(data, i, PasteEnd);
        if (k >= 0)
        {
            AppendPaste(data, i, k - i);
            events.Add(new PasteEvent(Encoding.UTF8.GetString(_paste.ToArray())));
            _paste.Clear();
            _inPaste = false;
            return k + PasteEnd.Length;
        }

        // No terminator yet. Commit everything except a trailing window that could be
        // the start of the terminator, so buffered state stays bounded.
        int available = len - i;
        int keep = Math.Min(PasteEnd.Length - 1, available);
        int commit = available - keep;
        if (commit > 0)
        {
            AppendPaste(data, i, commit);
            i += commit;
        }

        if (_paste.Count > _options.MaxPasteBytes)
        {
            // Recovery: force-flush an oversized/unterminated paste and resume normally.
            events.Add(new PasteEvent(Encoding.UTF8.GetString(_paste.ToArray())));
            _paste.Clear();
            _inPaste = false;
            return i; // reprocess the retained tail as normal input
        }

        needMore = true;
        return i;
    }

    // Parse an escape-introduced token starting at data[i] (== ESC).
    // Returns bytes consumed, or -1 if more input is required.
    private int ParseEscape(byte[] data, int i, List<IInputEvent> events)
    {
        int len = data.Length;
        if (i + 1 >= len) return -1;
        byte c1 = data[i + 1];

        if (c1 == (byte)'[') return ParseCsi(data, i, events);
        if (c1 == (byte)'O') return ParseSs3(data, i, events);
        if (c1 == 0x1b) return 1; // ESC ESC: consume one ESC, reprocess the second

        var status = TryDecodeScalar(data, i + 1, out int scalar, out int slen);
        if (status == ScalarStatus.NeedMore) return -1;
        if (status == ScalarStatus.Invalid) return 1; // drop ESC, reprocess bad byte

        if (scalar >= 0x20 && scalar != 0x7f)
        {
            string s = char.ConvertFromUtf32(scalar);
            events.Add(new KeyEvent(s, s, KeyModifiers.Alt));
            return 1 + slen;
        }
        // ESC + control byte: drop the ESC and let the control byte decode normally.
        return 1;
    }

    private int ParseCsi(byte[] data, int i, List<IInputEvent> events)
    {
        int len = data.Length;
        int j = i + 2; // first byte after "ESC ["

        // SGR mouse: ESC [ < ... (M|m)
        if (j < len && data[j] == (byte)'<')
        {
            int scan = j + 1;
            while (scan < len && data[scan] != (byte)'M' && data[scan] != (byte)'m')
            {
                if (scan - (j + 1) >= _options.MaxEscapeLength) { _discarding = true; return scan - i; } // overflow: discard to boundary
                scan++;
            }
            if (scan >= len) return -1; // need the final byte
            byte finalB = data[scan];
            string payload = AsciiString(data, j + 1, scan - (j + 1));
            ParseSgrMouse(payload, finalB, events);
            return scan + 1 - i;
        }

        // Generic CSI: parameter/intermediate bytes then a final byte in 0x40..0x7e.
        int p = j;
        while (p < len)
        {
            byte c = data[p];
            if (c >= 0x40 && c <= 0x7e) break; // final byte
            if (p - j >= _options.MaxEscapeLength) { _discarding = true; return p - i; } // overflow: discard to boundary
            p++;
        }
        if (p >= len) return -1; // need the final byte
        byte fin = data[p];
        string paramStr = AsciiString(data, j, p - j);
        InterpretCsi(paramStr, fin, events);
        return p + 1 - i;
    }

    private int ParseSs3(byte[] data, int i, List<IInputEvent> events)
    {
        int len = data.Length;
        if (i + 2 >= len) return -1;
        int c2 = data[i + 2];
        string? name = c2 switch
        {
            'A' => "ArrowUp",
            'B' => "ArrowDown",
            'C' => "ArrowRight",
            'D' => "ArrowLeft",
            'H' => "Home",
            'F' => "End",
            'P' => "F1",
            'Q' => "F2",
            'R' => "F3",
            'S' => "F4",
            _ => null,
        };
        if (name != null) events.Add(new KeyEvent(name, name, KeyModifiers.None));
        return 3;
    }

    private void InterpretCsi(string paramStr, byte fin, List<IInputEvent> events)
    {
        var parts = paramStr.Split(';');
        int First()
        {
            return parts.Length > 0 && int.TryParse(parts[0], out var v) ? v : -1;
        }
        KeyModifiers ModAt(int idx)
        {
            return parts.Length > idx && int.TryParse(parts[idx], out var m) ? DecodeMods(m) : KeyModifiers.None;
        }

        switch ((char)fin)
        {
            case 'A':
            case 'B':
            case 'C':
            case 'D':
            {
                string name = ArrowName((char)fin);
                events.Add(new KeyEvent(name, name, ModAt(1)));
                break;
            }
            case 'H':
                events.Add(new KeyEvent("Home", "Home", ModAt(1)));
                break;
            case 'F':
                events.Add(new KeyEvent("End", "End", ModAt(1)));
                break;
            case 'I':
                if (_options.EnableFocusEvents) events.Add(new FocusEvent(true));
                break;
            case 'O':
                if (_options.EnableFocusEvents) events.Add(new FocusEvent(false));
                break;
            case 't':
                // Resize report: ESC [ 8 ; rows ; cols t
                if (First() == 8 && parts.Length >= 3
                    && int.TryParse(parts[1], out var rows) && int.TryParse(parts[2], out var cols))
                {
                    events.Add(new ResizeEvent(cols, rows));
                }
                break;
            case '~':
            {
                int code = First();
                if (code == 200) { _inPaste = true; break; }
                if (code == 201) break; // stray terminator outside paste
                string? name = TildeName(code);
                if (name != null) events.Add(new KeyEvent(name, name, ModAt(1)));
                break;
            }
            default:
                break; // unknown final byte: drop the sequence
        }
    }

    private void ParseSgrMouse(string payload, byte finalB, List<IInputEvent> events)
    {
        if (!_options.EnableMouse) return;
        var parts = payload.Split(';');
        if (parts.Length < 3
            || !int.TryParse(parts[0], out var b)
            || !int.TryParse(parts[1], out var x)
            || !int.TryParse(parts[2], out var y))
        {
            return;
        }

        var kind = finalB == (byte)'M' ? MouseKind.Down : MouseKind.Up;
        var button = (b & 3) switch
        {
            0 => MouseButton.Left,
            1 => MouseButton.Middle,
            2 => MouseButton.Right,
            _ => MouseButton.None,
        };
        var mods = KeyModifiers.None;
        if ((b & 4) != 0) mods |= KeyModifiers.Shift;
        if ((b & 8) != 0) mods |= KeyModifiers.Alt;
        if ((b & 16) != 0) mods |= KeyModifiers.Ctrl;

        if ((b & 32) != 0)
        {
            kind = MouseKind.Move;
            button = MouseButton.None;
        }
        if ((b & 64) != 0)
        {
            kind = MouseKind.Wheel;
            button = MouseButton.None;
            int delta = (b & 1) == 0 ? 1 : -1;
            events.Add(new MouseEvent(kind, x - 1, y - 1, button, mods, delta));
            return;
        }
        events.Add(new MouseEvent(kind, x - 1, y - 1, button, mods));
    }

    private void EmitScalar(int scalar, List<IInputEvent> events)
    {
        if (scalar == 0x0d || scalar == 0x0a)
        {
            events.Add(new KeyEvent("Enter", "Enter", KeyModifiers.None));
        }
        else if (scalar == 0x09)
        {
            events.Add(new KeyEvent("Tab", "Tab", KeyModifiers.None));
        }
        else if (scalar == 0x08 || scalar == 0x7f)
        {
            events.Add(new KeyEvent("Backspace", "Backspace", KeyModifiers.None));
        }
        else if (scalar < 0x20)
        {
            if (scalar >= 1 && scalar <= 26)
            {
                char letter = (char)('A' + (scalar - 1));
                string s = letter.ToString();
                events.Add(new KeyEvent(s, s, KeyModifiers.Ctrl));
            }
            // other C0 controls are dropped
        }
        else
        {
            string s = char.ConvertFromUtf32(scalar);
            events.Add(new KeyEvent(s, s, KeyModifiers.None));
        }
    }

    // ---- helpers ----

    private static ScalarStatus TryDecodeScalar(byte[] d, int i, out int scalar, out int length)
    {
        scalar = 0;
        length = 0;
        byte b0 = d[i];
        int need;
        int val;
        if (b0 < 0x80) { scalar = b0; length = 1; return ScalarStatus.Done; }
        else if (b0 >= 0xC2 && b0 <= 0xDF) { need = 2; val = b0 & 0x1F; }
        else if (b0 >= 0xE0 && b0 <= 0xEF) { need = 3; val = b0 & 0x0F; }
        else if (b0 >= 0xF0 && b0 <= 0xF4) { need = 4; val = b0 & 0x07; }
        else { return ScalarStatus.Invalid; } // continuation byte as lead, 0xC0/0xC1, 0xF5..0xFF

        for (int k = 1; k < need; k++)
        {
            if (i + k >= d.Length) return ScalarStatus.NeedMore; // valid prefix so far
            byte bc = d[i + k];
            if (bc < 0x80 || bc > 0xBF) return ScalarStatus.Invalid; // bad continuation
            val = (val << 6) | (bc & 0x3F);
        }

        // Reject overlong encodings, surrogates, and out-of-range scalars.
        int min = need switch { 2 => 0x80, 3 => 0x800, _ => 0x10000 };
        if (val < min) return ScalarStatus.Invalid;
        if (val > 0x10FFFF || (val >= 0xD800 && val <= 0xDFFF)) return ScalarStatus.Invalid;

        scalar = val;
        length = need;
        return ScalarStatus.Done;
    }

    private void AppendPaste(byte[] data, int start, int count)
    {
        for (int k = 0; k < count; k++) _paste.Add(data[start + k]);
    }

    private void AppendPaste(byte[] data) => AppendPaste(data, 0, data.Length);

    private static int IndexOf(byte[] data, int start, byte[] pattern)
    {
        int last = data.Length - pattern.Length;
        for (int p = start; p <= last; p++)
        {
            bool match = true;
            for (int q = 0; q < pattern.Length; q++)
            {
                if (data[p + q] != pattern[q]) { match = false; break; }
            }
            if (match) return p;
        }
        return -1;
    }

    private static string AsciiString(byte[] data, int start, int count)
        => count <= 0 ? string.Empty : Encoding.ASCII.GetString(data, start, count);

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var r = new byte[a.Length + b.Length];
        Array.Copy(a, 0, r, 0, a.Length);
        Array.Copy(b, 0, r, a.Length, b.Length);
        return r;
    }

    private static KeyModifiers DecodeMods(int code)
    {
        int flags = Math.Max(0, code - 1);
        var mods = KeyModifiers.None;
        if ((flags & 1) != 0) mods |= KeyModifiers.Shift;
        if ((flags & 2) != 0) mods |= KeyModifiers.Alt;
        if ((flags & 4) != 0) mods |= KeyModifiers.Ctrl;
        if ((flags & 8) != 0) mods |= KeyModifiers.Meta;
        return mods;
    }

    private static string ArrowName(char final) => final switch
    {
        'A' => "ArrowUp",
        'B' => "ArrowDown",
        'C' => "ArrowRight",
        'D' => "ArrowLeft",
        _ => "",
    };

    private static string? TildeName(int code) => code switch
    {
        1 or 7 => "Home",
        2 => "Insert",
        3 => "Delete",
        4 or 8 => "End",
        5 => "PageUp",
        6 => "PageDown",
        15 => "F5",
        17 => "F6",
        18 => "F7",
        19 => "F8",
        20 => "F9",
        21 => "F10",
        23 => "F11",
        24 => "F12",
        _ => null,
    };
}
