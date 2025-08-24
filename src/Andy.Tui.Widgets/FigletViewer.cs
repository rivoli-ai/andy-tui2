using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public sealed class FigletViewer
    {
        private string _text = "HELLO";
        private DL.Rgb24 _fg = new DL.Rgb24(200,200,200);
        private DL.Rgb24 _bg = new DL.Rgb24(0,0,0);

        public void SetText(string text) => _text = text ?? string.Empty;
        public void SetColors(DL.Rgb24 fg, DL.Rgb24 bg) { _fg = fg; _bg = bg; }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height;
            if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,_bg));
            string[] lines = GenerateAscii(_text);
            for (int i=0;i<lines.Length && i<h;i++)
            {
                b.DrawText(new DL.TextRun(x+1, y+i, lines[i], _fg, _bg, DL.CellAttrFlags.Bold));
            }
            b.Pop();
        }

        private static string[] GenerateAscii(string s)
        {
            // Minimal banner font for A-Z, 0-9 (subset)
            string[] Map(char c) => c switch
            {
                'A' => new[]{"  /\\  "," /  \\ ","/ /\\ ","-----","/      ","/      "},
                'B' => new[]{"|\\__ ","|__/ ","|\\__ ","|__/ ","|\\__ ","|__/ "},
                'C' => new[]{" /\\\\ ","/    ","|     ","|     ","\\    "," \\\\  "},
                ':' => new[]{"  ","[]","  ","[]","  ","  "},
                ' ' => new[]{"  ","  ","  ","  ","  ","  "},
                _ => new[]{"?","?","?","?","?","?"}
            };
            var rows = new System.Collections.Generic.List<string> {"","","","","",""};
            foreach (var ch in s.ToUpperInvariant())
            {
                var glyph = Map(ch);
                for (int r=0;r<rows.Count;r++) rows[r] += glyph[r] + " ";
            }
            return rows.ToArray();
        }
    }
}
