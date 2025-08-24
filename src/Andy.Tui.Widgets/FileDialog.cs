using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public enum FileDialogMode { Open, Save }
    public sealed class FileDialog
    {
        private string _directory = Environment.CurrentDirectory;
        private FileDialogMode _mode = FileDialogMode.Open;
        private readonly List<string> _entries = new();
        private int _scroll;
        private int _cursor;
        private DL.Rgb24 _fg = new DL.Rgb24(220,220,220);
        private DL.Rgb24 _accent = new DL.Rgb24(200,200,80);
        public void SetDirectory(string path){ _directory = path; Refresh(); }
        public void SetMode(FileDialogMode mode){ _mode = mode; }
        public string? GetSelectedPath(){ if (_entries.Count==0) return null; return Path.Combine(_directory, _entries[_cursor]); }
        public void Refresh()
        {
            _entries.Clear();
            try
            {
                var dirs = Directory.EnumerateDirectories(_directory).Select(d=> (Path.GetFileName(d) ?? string.Empty) + "/");
                var files = Directory.EnumerateFiles(_directory).Select(f => Path.GetFileName(f) ?? string.Empty);
                _entries.AddRange(dirs.Concat(files));
            }
            catch { }
            _scroll = 0; _cursor = 0;
        }
        public void MoveCursor(int delta, int viewportRows)
        {
            _cursor = Math.Clamp(_cursor + delta, 0, Math.Max(0,_entries.Count-1));
            if (_cursor < _scroll) _scroll = _cursor;
            if (_cursor >= _scroll + viewportRows) _scroll = Math.Max(0, _cursor - viewportRows + 1);
        }
        public void Enter()
        {
            if (_entries.Count==0) return;
            string sel = _entries[_cursor];
            if (sel.EndsWith('/'))
            {
                var next = Path.Combine(_directory, sel.TrimEnd('/'));
                try { if (Directory.Exists(next)) { _directory = next; Refresh(); } }
                catch { }
            }
        }

        public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            int x=(int)rect.X, y=(int)rect.Y, w=(int)rect.Width, h=(int)rect.Height; if (w<=0||h<=0) return;
            b.PushClip(new DL.ClipPush(x,y,w,h));
            b.DrawRect(new DL.Rect(x,y,w,h,new DL.Rgb24(0,0,0)));
            b.DrawBorder(new DL.Border(x,y,w,h,"single", new DL.Rgb24(120,120,120)));
            string title = _mode==FileDialogMode.Open? "Open" : "Save";
            b.DrawText(new DL.TextRun(x+2, y, $"{title}: {_directory}", _accent, null, DL.CellAttrFlags.Bold));
            int listY = y + 1; int listH = Math.Max(0, h-2);
            int end = Math.Min(_entries.Count, _scroll + listH);
            for (int i=_scroll, row=0; i<end; i++,row++)
            {
                bool cur = i==_cursor;
                var bg = cur ? (DL.Rgb24?)_accent : null;
                var fg = cur ? new DL.Rgb24(0,0,0) : _fg;
                string line = _entries[i];
                if (line.Length > w-2) line = line.Substring(0,w-2);
                b.DrawRect(new DL.Rect(x+1, listY+row, w-2, 1, bg ?? new DL.Rgb24(0,0,0)));
                b.DrawText(new DL.TextRun(x+2, listY+row, line, fg, bg, cur? DL.CellAttrFlags.Bold: DL.CellAttrFlags.None));
            }
            b.Pop();
        }
    }
}
