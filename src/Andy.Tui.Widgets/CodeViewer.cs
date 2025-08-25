using System;
using System.Collections.Generic;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class CodeViewer
    {
        private readonly List<string> _lines = new();
        private int _scroll;
        public DL.Rgb24 Border = new DL.Rgb24(80,80,80);
        public DL.Rgb24 NumFg = new DL.Rgb24(120,120,120);
        public DL.Rgb24 CodeFg = new DL.Rgb24(220,220,220);
        public DL.Rgb24 Keyword = new DL.Rgb24(80,160,255);
        public DL.Rgb24 Comment = new DL.Rgb24(120,160,120);
        public DL.Rgb24 String = new DL.Rgb24(200,120,120);
        public DL.Rgb24 Number = new DL.Rgb24(180,180,100);
        public DL.Rgb24 Preproc = new DL.Rgb24(150,150,220);
        private static readonly HashSet<string> _keywords = new(StringComparer.Ordinal)
        {
            "using","namespace","class","struct","enum","interface",
            "public","private","internal","protected","void","int","string","char","double","float","decimal","long","short","byte","sbyte","uint","ulong","ushort",
            "return","new","static","sealed","abstract","readonly","const","ref","out","in","params","var","bool","true","false","null",
            "if","else","switch","case","default","break","continue","do","while","for","foreach","goto",
            "try","catch","finally","throw","checked","unchecked","fixed","unsafe","lock","this","base","get","set","init","value","nameof","async","await","yield"
        };

        public void SetText(string text)
        {
            _lines.Clear();
            if (text == null) return;
            _lines.AddRange(text.Replace("\r\n","\n").Replace('\r','\n').Split('\n'));
            _scroll = 0;
        }
        public void ScrollLines(int delta) { _scroll = Math.Max(0, Math.Min(Math.Max(0, _lines.Count - 1), _scroll + delta)); }
        public void Page(int delta, int pageSize) { ScrollLines(delta * Math.Max(1, pageSize - 1)); }
        public int GetScroll() => _scroll;

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawBorder(new DL.Border(x,y,w,h,"single", Border));
            int contentX = x + 1; int contentY = y + 1; int contentW = Math.Max(0, w - 2); int contentH = Math.Max(0, h - 2);
            int numW = Math.Max(3, (int)Math.Log10(Math.Max(1,_lines.Count)) + 1) + 2; // gutter width
            int codeX = contentX + numW;
            int maxIndex = Math.Min(_lines.Count, _scroll + contentH);
            for (int i = _scroll, row = 0; i < maxIndex; i++, row++)
            {
                string line = _lines[i];
                string num = (i+1).ToString().PadLeft(numW-1);
                b.DrawText(new DL.TextRun(contentX, contentY + row, num, NumFg, null, DL.CellAttrFlags.None));
                // preprocessor lines
                if (line.TrimStart().StartsWith("#"))
                {
                    b.DrawText(new DL.TextRun(codeX, contentY + row, Truncate(line, contentX + contentW - codeX), Preproc, null, DL.CellAttrFlags.None));
                    continue;
                }
                int cx = codeX;
                foreach (var (tok, kind) in TokenizeCs(line))
                {
                    if (cx >= contentX + contentW) break;
                    string t = tok;
                    int room = contentX + contentW - cx;
                    if (t.Length > room) t = t.Substring(0, room);
                    var color = kind switch
                    {
                        Tok.Comment => Comment,
                        Tok.String => String,
                        Tok.Number => Number,
                        Tok.Keyword => Keyword,
                        _ => CodeFg,
                    };
                    b.DrawText(new DL.TextRun(cx, contentY + row, t, color, null, DL.CellAttrFlags.None));
                    cx += t.Length;
                }
            }
            b.Pop();
        }

        private enum Tok { Plain, Keyword, String, Comment, Number }
        private static IEnumerable<(string tok, Tok kind)> TokenizeCs(string line)
        {
            if (string.IsNullOrEmpty(line)) yield break;
            int i = 0; bool inBlock = false; bool inLine = false;
            while (i < line.Length)
            {
                if (inLine) { yield return (line.Substring(i), Tok.Comment); yield break; }
                if (inBlock)
                {
                    int end = line.IndexOf("*/", i, StringComparison.Ordinal);
                    if (end == -1) { yield return (line.Substring(i), Tok.Comment); yield break; }
                    yield return (line.Substring(i, end + 2 - i), Tok.Comment); i = end + 2; inBlock = false; continue;
                }
                // line comment
                if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '/') { yield return (line.Substring(i), Tok.Comment); yield break; }
                // block comment start
                if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '*') { inBlock = true; i += 2; continue; }
                // strings
                if (line[i] == '"')
                {
                    int q = i; i++;
                    while (i < line.Length)
                    {
                        if (line[i] == '\\' && i + 1 < line.Length) { i += 2; continue; }
                        if (line[i] == '"') { i++; break; }
                        i++;
                    }
                    yield return (line.Substring(q, i - q), Tok.String);
                    continue;
                }
                if (line[i] == '\'')
                {
                    int q = i; i++;
                    while (i < line.Length)
                    {
                        if (line[i] == '\\' && i + 1 < line.Length) { i += 2; continue; }
                        if (line[i] == '\'') { i++; break; }
                        i++;
                    }
                    yield return (line.Substring(q, i - q), Tok.String);
                    continue;
                }
                // number
                if (char.IsDigit(line[i]))
                {
                    int j = i + 1; while (j < line.Length && (char.IsDigit(line[j]) || line[j]=='.' || char.IsLetter(line[j]))) j++;
                    yield return (line.Substring(i, j - i), Tok.Number); i = j; continue;
                }
                // identifier
                if (char.IsLetter(line[i]) || line[i] == '_')
                {
                    int j = i + 1; while (j < line.Length && (char.IsLetterOrDigit(line[j]) || line[j]=='_')) j++;
                    string ident = line.Substring(i, j - i);
                    yield return (ident, _keywords.Contains(ident) ? Tok.Keyword : Tok.Plain);
                    i = j; continue;
                }
                // single char
                yield return (line[i].ToString(), Tok.Plain); i++;
            }
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s.Substring(0, max);
    }
}
