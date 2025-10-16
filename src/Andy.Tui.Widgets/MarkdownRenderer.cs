using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    // Minimal markdown-ish: #, ##, ### headings; *italic*, **bold**, `code`; lists: - item
    public sealed class MarkdownRenderer
    {
        private string _md = string.Empty;
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);
        private DL.Rgb24 _fg = new DL.Rgb24(220,220,220);
        private DL.Rgb24 _accent = new DL.Rgb24(200,200,80);
        private DL.Rgb24 _h1Color = new DL.Rgb24(100,200,255);  // Bright blue for H1
        private DL.Rgb24 _h2Color = new DL.Rgb24(150,220,150);  // Green for H2
        private DL.Rgb24 _h3Color = new DL.Rgb24(255,180,100);  // Orange for H3
        private DL.Rgb24 _listColor = new DL.Rgb24(255,150,150); // Light red for list markers

        public void SetText(string md) => _md = md ?? string.Empty;
        public void SetColors(DL.Rgb24 fg, DL.Rgb24 bg, DL.Rgb24 accent) { _fg = fg; _bg = bg; _accent = accent; }
        public void SetHeaderColors(DL.Rgb24 h1, DL.Rgb24 h2, DL.Rgb24 h3) { _h1Color = h1; _h2Color = h2; _h3Color = h3; }
        public void SetListColor(DL.Rgb24 listColor) { _listColor = listColor; }

        private static readonly Regex Bold = new Regex(@"\*\*(.+?)\*\*", RegexOptions.Compiled);
        private static readonly Regex Italic = new Regex(@"\*(.+?)\*", RegexOptions.Compiled);
        private static readonly Regex Code = new Regex(@"`(.+?)`", RegexOptions.Compiled);

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            int cy = y;
            
            var lines = _md.Replace("\r\n","\n").Replace('\r','\n').Split('\n');
            var processedLines = ProcessUnderlinedHeaders(lines);
            var spacedLines = AddParagraphSpacing(processedLines);

            foreach (var line in spacedLines)
            {
                if (cy >= y + h) break;
                string text = line;
                DL.CellAttrFlags attrs = DL.CellAttrFlags.None;
                var color = _fg;
                int indent = 0;
                string listMarker = "";
                
                // Headers with distinct colors
                if (text.StartsWith("### ")) { text = text.Substring(4); attrs |= DL.CellAttrFlags.Bold; color = _h3Color; }
                else if (text.StartsWith("## ")) { text = text.Substring(3); attrs |= DL.CellAttrFlags.Bold; color = _h2Color; }
                else if (text.StartsWith("# ")) { text = text.Substring(2); attrs |= DL.CellAttrFlags.Bold; color = _h1Color; }
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
                // inline transforms: bold/italic/code
                string rendered = Code.Replace(Italic.Replace(Bold.Replace(text, "$1"), "$1"), "$1");
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
                
                // Render the main text content with proper wrapping
                if (string.IsNullOrEmpty(rendered))
                {
                    cy++;
                    continue;
                }
                
                // Handle text wrapping
                int availableWidth = w - indent;
                for (int pos = 0; pos < rendered.Length; )
                {
                    if (cy >= y + h) break;
                    
                    int segmentEnd = Math.Min(pos + availableWidth, rendered.Length);
                    string segment = rendered.Substring(pos, segmentEnd - pos);
                    
                    int segmentCx = cx;
                    foreach (char ch in segment)
                    {
                        if (segmentCx >= x + w) break;
                        if (ch == '\u0001') { attrs ^= DL.CellAttrFlags.Bold; continue; }
                        if (ch == '\u0002') { attrs ^= DL.CellAttrFlags.Underline; continue; }
                        if (ch == '\u0003') { /* code */ attrs ^= DL.CellAttrFlags.Underline; continue; }
                        b.DrawText(new DL.TextRun(segmentCx++, cy, ch.ToString(), color, _bg, attrs));
                    }
                    
                    pos = segmentEnd;
                    if (pos < rendered.Length)
                    {
                        cy++;
                        cx = x + indent; // Reset to indent for wrapped lines
                    }
                }
                cy++;
            }
            b.Pop();
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
