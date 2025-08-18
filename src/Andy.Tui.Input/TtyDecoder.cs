using System.Text;

namespace Andy.Tui.Input;

public static class TtyDecoder
{
    // Decode a stream of bytes into zero or more input events (MVP: keys + resize tokens + SGR mouse)
    public static IEnumerable<IInputEvent> Decode(byte[] buffer)
    {
        // Simplified MVP; real implementation should maintain decoder state across calls
        var s = Encoding.UTF8.GetString(buffer);
        if (s.StartsWith("\u001b["))
        {
            // Resize: ESC [ 8 ; rows ; cols t
            if (s.StartsWith("\u001b[8;") && s.EndsWith("t"))
            {
                var body = s.Substring(3, s.Length - 4);
                var parts = body.Split(';');
                if (parts.Length >= 3 && int.TryParse(parts[1], out var rows) && int.TryParse(parts[2], out var cols))
                {
                    yield return new ResizeEvent(cols, rows);
                    yield break;
                }
            }
            // Very simplified: arrow keys CSI A/B/C/D
            if (s.EndsWith("A")) yield return new KeyEvent("ArrowUp", "ArrowUp", KeyModifiers.None);
            if (s.EndsWith("B")) yield return new KeyEvent("ArrowDown", "ArrowDown", KeyModifiers.None);
            if (s.EndsWith("C")) yield return new KeyEvent("ArrowRight", "ArrowRight", KeyModifiers.None);
            if (s.EndsWith("D")) yield return new KeyEvent("ArrowLeft", "ArrowLeft", KeyModifiers.None);
        }
        else
        {
            // Plain printable keys
            foreach (var ch in s)
            {
                if (!char.IsControl(ch))
                    yield return new KeyEvent(ch.ToString(), ch.ToString(), KeyModifiers.None);
            }
        }
    }
}
