using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public readonly record struct ChatMessage(string Author, string Text, bool IsUser);

public sealed class ChatView
{
    private readonly List<ChatMessage> _messages = new();
    public int ScrollY { get; private set; }
    private int _offsetFromBottom; // 0 means tail, >0 means scrolled up by that many lines
    private bool _followTail = true;

    // Colors inspired by Bubble Tea demos
    private static readonly DL.Rgb24 UserName = new DL.Rgb24(95, 215, 255);   // cyan-ish
    private static readonly DL.Rgb24 BotName = new DL.Rgb24(241, 90, 90);    // coral-ish
    private static readonly DL.Rgb24 TextFg = new DL.Rgb24(220, 220, 220);
    private static readonly DL.Rgb24 Bg = new DL.Rgb24(0, 0, 0);

    public void SetMessages(IEnumerable<ChatMessage> messages)
    {
        _messages.Clear();
        _messages.AddRange(messages);
        if (_followTail) Tail();
    }

    public void Tail()
    {
        ScrollY = Math.Max(0, _messages.Count - 1);
    }

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X, y = (int)rect.Y, w = (int)rect.Width, h = (int)rect.Height;
        builder.PushClip(new DL.ClipPush(x, y, w, h));

        // Build wrapped visual lines with optional separation after assistant replies
        var lines = BuildVisualLines(w);
        // Determine start based on scroll offset from bottom
        int maxOffset = Math.Max(0, lines.Count - h);
        if (_offsetFromBottom > maxOffset) _offsetFromBottom = maxOffset;
        int start = Math.Max(0, lines.Count - h - (_followTail ? 0 : _offsetFromBottom));
        int yy = y;
        for (int i = start; i < lines.Count && yy < y + h; i++, yy++)
        {
            var vl = lines[i];
            // Clear full row to avoid overlap artifacts across frames
            builder.DrawRect(new DL.Rect(x, yy, w, 1, vl.Bg));
            if (vl.ShowName)
            {
                builder.DrawText(new DL.TextRun(x + 1, yy, vl.NameWithSpace, vl.NameColor, vl.Bg, DL.CellAttrFlags.Bold));
            }
            // Draw continuation marker in gutter for wrapped lines (even on empty visual lines)
            if (!vl.ShowName && vl.NameWidth > 0)
            {
                var marker = vl.IsLastOfMessage ? "└" : "│";
                int markerX = x + Math.Max(1, vl.NameWidth - 1);
                builder.DrawText(new DL.TextRun(markerX, yy, marker, vl.NameColor, vl.Bg, DL.CellAttrFlags.None));
            }
            if (vl.Text.Length > 0)
            {
                DrawInlineMarkdown(builder, x + 1 + vl.NameWidth, yy, vl.Text, vl.Fg, vl.Bg);
            }
        }

        builder.Pop();
    }

    private readonly struct VisualLine
    {
        public readonly bool ShowName;
        public readonly string NameWithSpace; // includes trailing space when shown
        public readonly int NameWidth;        // visible width of name + space when shown, else indent spaces
        public readonly DL.Rgb24 NameColor;
        public readonly string Text;
        public readonly DL.Rgb24 Fg;
        public readonly DL.Rgb24 Bg;
        public readonly bool IsFirstOfMessage;
        public readonly bool IsLastOfMessage;
        public VisualLine(bool showName, string nameWithSpace, int nameWidth, DL.Rgb24 nameColor, string text, DL.Rgb24 fg, DL.Rgb24 bg, bool isFirstOfMessage = false, bool isLastOfMessage = false)
        {
            ShowName = showName;
            NameWithSpace = nameWithSpace;
            NameWidth = nameWidth;
            NameColor = nameColor;
            Text = text;
            Fg = fg;
            Bg = bg;
            IsFirstOfMessage = isFirstOfMessage;
            IsLastOfMessage = isLastOfMessage;
        }
    }

    private List<VisualLine> BuildVisualLines(int containerWidth)
    {
        var result = new List<VisualLine>(_messages.Count * 2);
        int contentMaxWidthGlobal;
        // 1 column padding on left and right like current rendering
        contentMaxWidthGlobal = Math.Max(0, containerWidth - 2);

        for (int mi = 0; mi < _messages.Count; mi++)
        {
            var m = _messages[mi];
            var nameColor = m.IsUser ? UserName : BotName;
            var name = m.IsUser ? "You:" : (string.IsNullOrWhiteSpace(m.Author) ? "Bot:" : m.Author + ":");
            var nameWithSpace = name + " ";
            int nameWidth = nameWithSpace.Length;
            int firstLineTextWidth = Math.Max(0, contentMaxWidthGlobal - nameWidth);
            if (firstLineTextWidth <= 0)
            {
                // Not enough space to show text after name; still render name-only line
                result.Add(new VisualLine(true, nameWithSpace, nameWidth, nameColor, string.Empty, TextFg, Bg, isFirstOfMessage: true, isLastOfMessage: true));
                continue;
            }

            // Parse markdown-like text with code fences, collect all visual lines for this message
            bool inCode = false; string? codeLang = null;
            var rawLines = m.Text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            var messageLines = new List<VisualLine>(8);
            for (int li = 0; li < rawLines.Length; li++)
            {
                var raw = rawLines[li];
                if (raw.StartsWith("```"))
                {
                    if (!inCode)
                    {
                        inCode = true;
                        codeLang = raw.Trim().TrimStart('`').Trim();
                        // Show the fence line dimmed
                        foreach (var (textPart, _) in WrapFixed($"```{codeLang}", firstLineTextWidth, contentMaxWidthGlobal))
                        {
                            messageLines.Add(new VisualLine(false, string.Empty, nameWidth, nameColor, textPart, new DL.Rgb24(160, 160, 160), new DL.Rgb24(20, 20, 20)));
                        }
                    }
                    else
                    {
                        // closing fence
                        foreach (var (textPart, _) in WrapFixed("```", firstLineTextWidth, contentMaxWidthGlobal))
                        {
                            messageLines.Add(new VisualLine(false, string.Empty, nameWidth, nameColor, textPart, new DL.Rgb24(160, 160, 160), new DL.Rgb24(20, 20, 20)));
                        }
                        inCode = false; codeLang = null;
                    }
                    continue;
                }

                if (inCode)
                {
                    // Preformatted lines; hard-wrap
                    foreach (var (textPart, _) in WrapFixed(raw, firstLineTextWidth, contentMaxWidthGlobal))
                    {
                        messageLines.Add(new VisualLine(false, string.Empty, nameWidth, nameColor, textPart, new DL.Rgb24(200, 220, 200), new DL.Rgb24(20, 20, 20)));
                    }
                }
                else
                {
                    // Paragraph lines; word-wrap
                    foreach (var (textPart, _) in WrapText(raw, firstLineTextWidth, contentMaxWidthGlobal, nameWidth))
                    {
                        messageLines.Add(new VisualLine(false, string.Empty, nameWidth, nameColor, textPart, TextFg, Bg));
                    }
                }
            }

            if (messageLines.Count == 0)
            {
                messageLines.Add(new VisualLine(false, string.Empty, nameWidth, nameColor, string.Empty, TextFg, Bg));
            }

            for (int i = 0; i < messageLines.Count; i++)
            {
                var ml = messageLines[i];
                bool isFirst = i == 0;
                bool isLast = i == messageLines.Count - 1;
                var nameToShow = isFirst ? nameWithSpace : string.Empty;
                result.Add(new VisualLine(isFirst, nameToShow, nameWidth, nameColor, ml.Text, ml.Fg, ml.Bg, isFirst, isLast));
            }

            // Optional separation after assistant replies to visually group Q&A
            if (!m.IsUser)
            {
                // Add a blank separator line without gutter marker by setting NameWidth=0
                result.Add(new VisualLine(false, string.Empty, 0, nameColor, string.Empty, TextFg, Bg, false, true));
            }
        }

        // Update ScrollY to the last line index for Tail()
        ScrollY = Math.Max(0, result.Count - 1);
        return result;
    }

    private static IEnumerable<(string lineText, bool isFirst)> WrapText(string text, int firstLineWidth, int subsequentWidth, int indentWidth)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield return (string.Empty, true);
            yield break;
        }

        // Split by whitespace but preserve words; fall back to hard-break for very long tokens
        var words = text.Split(' ');
        var sb = new System.Text.StringBuilder();
        int remaining = firstLineWidth;
        bool firstLine = true;

        for (int wi = 0; wi < words.Length; wi++)
        {
            var word = words[wi];
            // If word longer than remaining and longer than line, hard-break the word
            int idx = 0;
            while (idx < word.Length)
            {
                int chunkAvail = Math.Max(1, remaining);
                int chunkLen = Math.Min(chunkAvail, word.Length - idx);
                var chunk = word.Substring(idx, chunkLen);

                if (chunkLen < word.Length - idx)
                {
                    // fill line with chunk and break
                    if (sb.Length > 0)
                    {
                        // flush current buffer into a line before placing chunk if needed
                    }
                }

                if (chunkLen > remaining)
                {
                    // flush current buffer to line and reset remaining
                    if (sb.Length > 0)
                    {
                        yield return (sb.ToString(), firstLine);
                        sb.Clear();
                        firstLine = false;
                        remaining = subsequentWidth;
                    }
                    // recompute chunk for empty line
                    chunkAvail = Math.Max(1, remaining);
                    chunkLen = Math.Min(chunkAvail, word.Length - idx);
                    chunk = word.Substring(idx, chunkLen);
                }

                if (chunkLen <= remaining)
                {
                    if (sb.Length > 0)
                        sb.Append(chunk);
                    else
                        sb.Append(chunk);
                    remaining -= chunkLen;
                    idx += chunkLen;

                    if (idx < word.Length)
                    {
                        // line full; flush
                        yield return (sb.ToString(), firstLine);
                        sb.Clear();
                        firstLine = false;
                        remaining = subsequentWidth;
                    }
                }
            }

            // add space between words if not last, if fits; if not, break line
            if (wi < words.Length - 1)
            {
                if (remaining > 0)
                {
                    if (remaining == 0)
                    {
                        yield return (sb.ToString(), firstLine);
                        sb.Clear();
                        firstLine = false;
                        remaining = subsequentWidth;
                    }
                    else
                    {
                        if (remaining >= 1)
                        {
                            sb.Append(' ');
                            remaining -= 1;
                        }
                        else
                        {
                            yield return (sb.ToString(), firstLine);
                            sb.Clear();
                            firstLine = false;
                            remaining = subsequentWidth;
                        }
                    }
                }
                else
                {
                    yield return (sb.ToString(), firstLine);
                    sb.Clear();
                    firstLine = false;
                    remaining = subsequentWidth;
                }
            }
        }

        if (sb.Length > 0)
        {
            yield return (sb.ToString(), firstLine);
        }
        else
        {
            // ensure at least an empty line exists
            yield return (string.Empty, firstLine);
        }
    }

    private static IEnumerable<(string lineText, bool isFirst)> WrapFixed(string text, int firstLineWidth, int subsequentWidth)
    {
        if (text is null) { yield return (string.Empty, true); yield break; }
        bool first = true;
        int idx = 0;
        while (idx < text.Length)
        {
            int width = first ? firstLineWidth : subsequentWidth;
            width = Math.Max(0, width);
            int take = Math.Min(width, text.Length - idx);
            var slice = take > 0 ? text.Substring(idx, take) : string.Empty;
            yield return (slice, first);
            first = false;
            idx += take;
            if (take == 0) break;
        }
        if (idx >= text.Length && first)
        {
            // ensure at least one line emitted
            yield return (string.Empty, true);
        }
    }

    public void AdjustScroll(int delta, int viewportWidth, int viewportHeight)
    {
        // Positive delta scrolls up (older messages), negative delta scrolls down
        var lines = BuildVisualLines(viewportWidth);
        int maxOffset = Math.Max(0, lines.Count - viewportHeight);
        _offsetFromBottom = Math.Clamp(_offsetFromBottom + delta, 0, maxOffset);
        _followTail = _offsetFromBottom == 0;
    }

    public void FollowTail(bool enable)
    {
        _followTail = enable;
        if (enable) _offsetFromBottom = 0;
    }

    private static void DrawInlineMarkdown(DL.DisplayListBuilder builder, int x, int y, string text, DL.Rgb24 fg, DL.Rgb24 bg)
    {
        bool bold = false;
        int cursorX = x;
        for (int i = 0; i < text.Length;)
        {
            if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '*')
            {
                bold = !bold; i += 2; continue;
            }
            int start = i;
            while (i < text.Length)
            {
                if (i + 1 < text.Length && text[i] == '*' && text[i + 1] == '*') break;
                i++;
            }
            if (i > start)
            {
                var segment = text.Substring(start, i - start);
                var attrs = bold ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None;
                builder.DrawText(new DL.TextRun(cursorX, y, segment, fg, bg, attrs));
                cursorX += segment.Length;
            }
        }
    }
}
