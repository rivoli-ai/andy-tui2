using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    // Minimal markdown-ish: #, ##, ### headings; *italic*, **bold**, `code`; lists: - item
    public sealed class MarkdownRenderer : WidgetBase
    {
        private string _md = string.Empty;
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);
        private DL.Rgb24 _fg = new DL.Rgb24(220,220,220);
        private DL.Rgb24 _accent = new DL.Rgb24(200,200,80);
        private DL.Rgb24 _h1Color = new DL.Rgb24(100,200,255);  // Bright blue for H1
        private DL.Rgb24 _h2Color = new DL.Rgb24(150,220,150);  // Green for H2
        private DL.Rgb24 _h3Color = new DL.Rgb24(255,180,100);  // Orange for H3
        private DL.Rgb24 _listColor = new DL.Rgb24(255,150,150); // Light red for list markers
        private DL.Rgb24 _inlineCodeColor = new DL.Rgb24(220,180,120); // inline `code`

        public void SetText(string md) => _md = md ?? string.Empty;
        public void SetColors(DL.Rgb24 fg, DL.Rgb24 bg, DL.Rgb24 accent) { _fg = fg; _bg = bg; _accent = accent; }
        public void SetHeaderColors(DL.Rgb24 h1, DL.Rgb24 h2, DL.Rgb24 h3) { _h1Color = h1; _h2Color = h2; _h3Color = h3; }
        public void SetListColor(DL.Rgb24 listColor) { _listColor = listColor; }
        /// <summary>Color used for inline `code` spans (a distinct color, never an underline).</summary>
        public void SetInlineCodeColor(DL.Rgb24 c) { _inlineCodeColor = c; }

        /// <summary>
        /// A contiguous run of text that shares a single set of cell attributes and a
        /// single foreground decision (normal vs inline-code color). Inline parsing
        /// produces a fresh list of these per line, so emphasis state is physically
        /// unable to leak past the end of a line or across a wrapped segment.
        /// </summary>
        private readonly struct Span
        {
            public readonly string Text;
            public readonly DL.CellAttrFlags Attrs;
            public readonly bool Code;
            public Span(string text, DL.CellAttrFlags attrs, bool code) { Text = text; Attrs = attrs; Code = code; }
        }

        protected override void RenderCore(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            int cy = y;

            var lines = _md.Replace("\r\n","\n").Replace('\r','\n').Split('\n');
            var processedLines = ProcessUnderlinedHeaders(lines);
            var spacedLines = AddParagraphSpacing(processedLines);

            bool inCodeFence = false;
            foreach (var line in spacedLines)
            {
                if (cy >= y + h) break;
                string text = line;
                DL.CellAttrFlags attrs = DL.CellAttrFlags.None;
                bool isHeader = false; // header lines are uniformly styled, not inline-parsed
                var color = _fg;
                int indent = 0;
                string listMarker = "";

                // Check for code fence toggle
                if (text.TrimStart().StartsWith("```"))
                {
                    inCodeFence = !inCodeFence;
                    cy++; // Skip the ``` line itself
                    continue;
                }

                // If inside code fence, render as plain code
                if (inCodeFence)
                {
                    // Render code with monospace color and no markdown processing
                    int codeCx = x + 2; // Small indent for code blocks
                    foreach (char ch in text)
                    {
                        if (codeCx >= x + w) break;
                        b.DrawText(new DL.TextRun(codeCx++, cy, ch.ToString(), _accent, _bg, DL.CellAttrFlags.None));
                    }
                    cy++;
                    continue;
                }

                // Headers with distinct colors
                if (text.StartsWith("### ")) { text = text.Substring(4); attrs |= DL.CellAttrFlags.Bold; color = _h3Color; isHeader = true; }
                else if (text.StartsWith("## ")) { text = text.Substring(3); attrs |= DL.CellAttrFlags.Bold; color = _h2Color; isHeader = true; }
                else if (text.StartsWith("# ")) { text = text.Substring(2); attrs |= DL.CellAttrFlags.Bold; color = _h1Color; isHeader = true; }
                // Unordered lists (- or * bullet points)
                else if (text.StartsWith("- ")) { text = text.Substring(2); listMarker = "• "; indent = 2; }
                else if (text.StartsWith("* ")) { text = text.Substring(2); listMarker = "★ "; indent = 2; }
                // Numbered lists (1. 2. 3. etc.)
                else if (System.Text.RegularExpressions.Regex.IsMatch(text, @"^\d+\.\s"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(text, @"^(\d+\.\s)(.*)");
                    if (match.Success)
                    {
                        listMarker = match.Groups[1].Value;
                        text = match.Groups[2].Value;
                        indent = listMarker.Length;
                    }
                }

                // Parse inline emphasis/strong/code into styled spans. All emphasis state
                // lives in this list and is rebuilt for every line, so a span can never
                // leak onto a later line. An unclosed marker is emitted as literal text.
                List<Span> spans;
                if (isHeader)
                {
                    // A heading is uniformly styled; do not re-parse inline markers inside it.
                    spans = new List<Span> { new Span(text, attrs, false) };
                }
                else
                {
                    spans = ParseInline(text, attrs);
                }

                int cx = x;

                // Render list marker first if present (with list color and bold)
                if (!string.IsNullOrEmpty(listMarker))
                {
                    foreach (char ch in listMarker)
                    {
                        if (cx >= x + w) break;
                        b.DrawText(new DL.TextRun(cx++, cy, ch.ToString(), _listColor, _bg, DL.CellAttrFlags.Bold));
                    }
                }

                // Render the main text content with proper wrapping.
                int totalChars = spans.Sum(s => s.Text.Length);
                if (totalChars == 0)
                {
                    cy++;
                    continue;
                }

                int availableWidth = w - indent;
                if (availableWidth < 1) availableWidth = 1;
                int colInLine = 0; // visual column within the current (possibly wrapped) row
                foreach (var span in spans)
                {
                    var spanColor = span.Code ? _inlineCodeColor : color;
                    foreach (char ch in span.Text)
                    {
                        if (cy >= y + h) goto doneLine;
                        if (colInLine >= availableWidth)
                        {
                            // Wrap to next visual row, re-applying the list indent.
                            cy++;
                            if (cy >= y + h) goto doneLine;
                            cx = x + indent;
                            colInLine = 0;
                        }
                        if (cx < x + w)
                            b.DrawText(new DL.TextRun(cx, cy, ch.ToString(), spanColor, _bg, span.Attrs));
                        cx++;
                        colInLine++;
                    }
                }
                doneLine:
                cy++;
            }
            b.Pop();
        }

        /// <summary>
        /// Tokenizes a single line of text into styled spans, honoring `**`/`__` (strong),
        /// `*`/`_` (emphasis) and `` ` `` (inline code). Only the text strictly between a
        /// matched pair of markers receives the corresponding attribute; surrounding text
        /// keeps <paramref name="baseAttrs"/>. A marker with no matching close on the same
        /// line is treated as literal text, so emphasis can never spill onto the rest of the
        /// line or onto following lines.
        /// </summary>
        private static List<Span> ParseInline(string text, DL.CellAttrFlags baseAttrs)
        {
            var spans = new List<Span>();
            if (string.IsNullOrEmpty(text))
                return spans;

            var buf = new System.Text.StringBuilder();
            bool bold = false;
            bool italic = false;
            // Current foreground/attr context (excluding inline code, which is a separate flag).
            DL.CellAttrFlags CurrentAttrs() => baseAttrs | (bold ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None);

            void Flush(bool code)
            {
                if (buf.Length == 0) return;
                spans.Add(new Span(buf.ToString(), CurrentAttrs(), code));
                buf.Clear();
            }

            int i = 0;
            int n = text.Length;
            while (i < n)
            {
                char c = text[i];

                // Inline code: spans from the next backtick to the following backtick.
                if (c == '`')
                {
                    int close = text.IndexOf('`', i + 1);
                    if (close > i)
                    {
                        Flush(false);
                        string inner = text.Substring(i + 1, close - i - 1);
                        // Inline code is rendered verbatim; markers inside are literal.
                        spans.Add(new Span(inner, baseAttrs, true));
                        i = close + 1;
                        continue;
                    }
                    // Unmatched backtick: literal.
                    buf.Append(c);
                    i++;
                    continue;
                }

                // Strong: ** or __ . Requires a matching close on this line.
                if ((c == '*' || c == '_') && i + 1 < n && text[i + 1] == c)
                {
                    char marker = c;
                    if (!bold)
                    {
                        if (HasClosingDouble(text, i + 2, marker))
                        {
                            Flush(false);
                            bold = true;
                            i += 2;
                            continue;
                        }
                    }
                    else
                    {
                        // Close the open strong span.
                        Flush(false);
                        bold = false;
                        i += 2;
                        continue;
                    }
                    // No close available: literal.
                    buf.Append(marker);
                    buf.Append(marker);
                    i += 2;
                    continue;
                }

                // Emphasis: single * or _ . Requires a matching close on this line.
                if (c == '*' || c == '_')
                {
                    char marker = c;
                    if (!italic)
                    {
                        if (HasClosingSingle(text, i + 1, marker))
                        {
                            Flush(false);
                            italic = true;
                            i++;
                            continue;
                        }
                    }
                    else
                    {
                        Flush(false);
                        italic = false;
                        i++;
                        continue;
                    }
                    // No close available: literal.
                    buf.Append(marker);
                    i++;
                    continue;
                }

                buf.Append(c);
                i++;
            }

            // End of line: emit whatever is buffered. Any still-open emphasis simply ends
            // here; because state is local it cannot affect the next line.
            Flush(false);
            return spans;
        }

        // True if a matching double marker (e.g. "**") exists at or after start, with at
        // least one character of content between the open and close.
        private static bool HasClosingDouble(string text, int start, char marker)
        {
            for (int i = start; i + 1 < text.Length; i++)
            {
                if (text[i] == marker && text[i + 1] == marker)
                {
                    if (i > start) return true; // non-empty content found
                    // Empty content (e.g. "****"): not a valid close here. Skip this
                    // adjacent marker pair and keep looking for a real, non-empty close.
                    i++; // consume the second marker char; loop's i++ skips the first
                    continue;
                }
            }
            return false;
        }

        // True if a matching single marker exists at or after start (not part of a double
        // marker), with at least one character of content between the open and close.
        private static bool HasClosingSingle(string text, int start, char marker)
        {
            for (int i = start; i < text.Length; i++)
            {
                if (text[i] == marker)
                    return i > start; // require non-empty content
            }
            return false;
        }

        private static List<string> ProcessUnderlinedHeaders(string[] lines)
        {
            var result = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                // Check for underlined headers (next line is all === or ---)
                if (i < lines.Length - 1)
                {
                    string nextLine = lines[i + 1];
                    if (nextLine.Length > 0)
                    {
                        if (nextLine.All(c => c == '=') && nextLine.Length >= 3)
                        {
                            // H1 header
                            result.Add("# " + line);
                            i++; // Skip the underline
                            continue;
                        }
                        else if (nextLine.All(c => c == '-') && nextLine.Length >= 3)
                        {
                            // H2 header
                            result.Add("## " + line);
                            i++; // Skip the underline
                            continue;
                        }
                    }
                }

                result.Add(line);
            }

            return result;
        }

        private static List<string> AddParagraphSpacing(List<string> lines)
        {
            if (lines.Count == 0) return lines;

            var result = new List<string>();

            for (int i = 0; i < lines.Count; i++)
            {
                string current = lines[i];
                string next = i < lines.Count - 1 ? lines[i + 1] : "";

                result.Add(current);

                // Don't add spacing if current or next line is already blank
                if (string.IsNullOrWhiteSpace(current) || string.IsNullOrWhiteSpace(next))
                    continue;

                // Don't add spacing at the end
                if (i == lines.Count - 1)
                    continue;

                var currentType = GetLineType(current);
                var nextType = GetLineType(next);

                // Add blank line when transitioning between different content types
                bool needsSpacing = false;

                // After a list item, before a heading or non-list text
                if (currentType == LineType.List && nextType != LineType.List)
                    needsSpacing = true;

                // Before a heading (except after another heading or list)
                if (nextType == LineType.Heading && currentType != LineType.Heading && currentType != LineType.List)
                    needsSpacing = true;

                // After a heading, before text or list
                if (currentType == LineType.Heading && (nextType == LineType.Text || nextType == LineType.List))
                    needsSpacing = false; // Don't add spacing after headings for now

                if (needsSpacing)
                    result.Add("");
            }

            // Trim trailing blank lines
            while (result.Count > 0 && string.IsNullOrWhiteSpace(result[^1]))
            {
                result.RemoveAt(result.Count - 1);
            }

            return result;
        }

        private enum LineType { Heading, List, Text }

        private static LineType GetLineType(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return LineType.Text;

            var trimmed = line.TrimStart();

            // Check for headings
            if (trimmed.StartsWith("# ") || trimmed.StartsWith("## ") || trimmed.StartsWith("### "))
                return LineType.Heading;

            // Check for list items
            if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") ||
                trimmed.StartsWith("• ") || trimmed.StartsWith("★ ") ||
                System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+\.\s"))
                return LineType.List;

            return LineType.Text;
        }
    }
}
