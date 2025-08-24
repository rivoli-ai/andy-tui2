using System;
using System.Collections.Generic;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

public sealed class CommandPalette
{
    private string[] _allCommands = Array.Empty<string>();
    private readonly List<FilterResult> _results = new();
    private string _query = string.Empty;
    private int _selectedFilteredIndex;
    private readonly HashSet<string> _pinned = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _recent = new();

    public DL.Rgb24 OverlayBg { get; private set; } = new(0, 0, 0);
    public DL.Rgb24 PanelBg { get; private set; } = new(20, 20, 20);
    public DL.Rgb24 PanelBorder { get; private set; } = new(90, 90, 90);
    public DL.Rgb24 TextFg { get; private set; } = new(220, 220, 220);
    public DL.Rgb24 Accent { get; private set; } = new(180, 180, 220);

    public void SetCommands(IEnumerable<string> commands)
    {
        _allCommands = (commands ?? Enumerable.Empty<string>()).Select(s => s ?? string.Empty).ToArray();
        ApplyFilter();
    }

    public void SetPinnedCommands(IEnumerable<string> commands)
    {
        _pinned.Clear();
        foreach (var c in (commands ?? Enumerable.Empty<string>())) if (!string.IsNullOrEmpty(c)) _pinned.Add(c);
        ApplyFilter();
    }

    public void SetRecentCommands(IEnumerable<string> commands)
    {
        _recent.Clear();
        foreach (var c in (commands ?? Enumerable.Empty<string>())) if (!string.IsNullOrEmpty(c)) _recent.Add(c);
        ApplyFilter();
    }

    public void SetQuery(string query)
    {
        _query = query ?? string.Empty;
        ApplyFilter();
    }

    public string GetQuery() => _query;

    public void MoveSelection(int delta)
    {
        if (_results.Count == 0) { _selectedFilteredIndex = 0; return; }
        int next = _selectedFilteredIndex + delta;
        _selectedFilteredIndex = Math.Max(0, Math.Min(next, _results.Count - 1));
    }

    public string? GetSelected()
    {
        if (_results.Count == 0) return null;
        int idx = Math.Max(0, Math.Min(_selectedFilteredIndex, _results.Count - 1));
        int cmdIdx = _results[idx].CommandIndex;
        return _allCommands[cmdIdx];
    }

    private void ApplyFilter()
    {
        _results.Clear();
        string q = _query.Trim();
        for (int i = 0; i < _allCommands.Length; i++)
        {
            var title = _allCommands[i];
            if (string.IsNullOrEmpty(q))
            {
                int s = 0;
                s += _pinned.Contains(title) ? 1000 : 0;
                s += GetRecentBonus(title);
                _results.Add(new FilterResult(i, s, Array.Empty<int>()));
                continue;
            }
            if (TryFuzzyScore(title, q, out int score, out List<int> positions))
            {
                score += _pinned.Contains(title) ? 1000 : 0;
                score += GetRecentBonus(title);
                _results.Add(new FilterResult(i, score, positions.ToArray()));
            }
        }
        // sort by score desc then by title
        _results.Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : string.Compare(_allCommands[a.CommandIndex], _allCommands[b.CommandIndex], StringComparison.OrdinalIgnoreCase));
        _selectedFilteredIndex = 0;
    }

    private int GetRecentBonus(string title)
    {
        // Most recent gets higher bonus
        for (int idx = 0; idx < _recent.Count; idx++)
        {
            if (string.Equals(_recent[idx], title, StringComparison.OrdinalIgnoreCase))
            {
                return Math.Max(0, (_recent.Count - idx));
            }
        }
        return 0;
    }

    private static bool TryFuzzyScore(string text, string query, out int score, out List<int> matchPositions)
    {
        score = 0; matchPositions = new List<int>();
        if (string.IsNullOrEmpty(query)) { return true; }
        int ti = 0; int qi = 0; int streak = 0;
        while (ti < text.Length && qi < query.Length)
        {
            char tc = char.ToLowerInvariant(text[ti]);
            char qc = char.ToLowerInvariant(query[qi]);
            if (tc == qc)
            {
                matchPositions.Add(ti);
                // scoring: base + boundary + streak
                score += 5; // base match
                if (ti == 0 || text[ti - 1] == ' ' || text[ti - 1] == '-' || text[ti - 1] == '_') score += 3;
                streak += 1; score += Math.Max(0, streak - 1) * 2;
                qi++; ti++;
            }
            else { streak = 0; ti++; }
        }
        return qi == query.Length;
    }

    public (int Width, int Height, int X, int Y) MeasurePanel(int viewportW, int viewportH)
    {
        int w = Math.Max(24, Math.Min(viewportW - 4, (int)Math.Round(viewportW * 0.6)));
        int h = Math.Max(6, Math.Min(viewportH - 4, 10));
        int x = (viewportW - w) / 2;
        int y = (viewportH - h) / 3; // slightly upper third
        return (w, h, x, y);
    }

    public void Render(in L.Rect viewport, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int vw = (int)viewport.Width; int vh = (int)viewport.Height;
        builder.PushClip(new DL.ClipPush(0, 0, vw, vh));
        builder.DrawRect(new DL.Rect(0, 0, vw, vh, OverlayBg));

        var (w, h, x, y) = MeasurePanel(vw, vh);
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, PanelBg));
        builder.DrawBorder(new DL.Border(x, y, w, h, "single", PanelBorder));
        // Input line
        string prompt = "> ";
        string input = prompt + _query;
        int inputW = Math.Max(0, w - 2);
        builder.DrawText(new DL.TextRun(x + 1, y + 1, (input.Length > inputW ? input[..inputW] : input).PadRight(inputW), TextFg, PanelBg, DL.CellAttrFlags.Bold));
        // Results with sections: Pinned, Recent, All Commands
        int listY = y + 3;
        int visible = Math.Max(0, h - 4);
        // Build grouped lists
        var pinnedIdxs = new List<int>();
        var recentIdxs = new List<int>();
        var otherIdxs = new List<int>();
        var seen = new HashSet<int>();
        foreach (var res in _results)
        {
            int ci = res.CommandIndex;
            var title = _allCommands[ci];
            if (_pinned.Contains(title)) { pinnedIdxs.Add(ci); seen.Add(ci); continue; }
            if (!seen.Contains(ci) && _recent.Any(r => string.Equals(r, title, StringComparison.OrdinalIgnoreCase))) { recentIdxs.Add(ci); seen.Add(ci); continue; }
            if (!seen.Contains(ci)) otherIdxs.Add(ci);
        }
        // Flatten rows: headers + items
        var rows = new List<(bool IsHeader, string Header, int CmdIdx)>();
        if (pinnedIdxs.Count > 0) { rows.Add((true, "Pinned", -1)); rows.AddRange(pinnedIdxs.Select(ci => (false, string.Empty, ci))); }
        if (recentIdxs.Count > 0) { rows.Add((true, "Recent", -1)); rows.AddRange(recentIdxs.Select(ci => (false, string.Empty, ci))); }
        if (otherIdxs.Count > 0) { rows.Add((true, "All Commands", -1)); rows.AddRange(otherIdxs.Select(ci => (false, string.Empty, ci))); }
        // Map selection item index to its row index
        var combinedItems = pinnedIdxs.Concat(recentIdxs).Concat(otherIdxs).ToList();
        int selItemIdx = Math.Max(0, Math.Min(_selectedFilteredIndex, combinedItems.Count - 1));
        int selCmdIdx = combinedItems.Count > 0 ? combinedItems[selItemIdx] : -1;
        int selRowIndex = 0;
        for (int i = 0, itemCount = 0; i < rows.Count; i++)
        {
            if (!rows[i].IsHeader)
            {
                if (rows[i].CmdIdx == selCmdIdx) { selRowIndex = i; break; }
                itemCount++;
            }
        }
        // Compute start to keep selection visible
        int start = 0;
        if (selRowIndex >= visible) start = selRowIndex - (visible - 1);
        for (int i = 0; i < visible && start + i < rows.Count; i++)
        {
            int rowY = listY + i;
            var row = rows[start + i];
            if (row.IsHeader)
            {
                // Header line styling
                builder.DrawRect(new DL.Rect(x + 1, rowY, w - 2, 1, PanelBg));
                builder.DrawText(new DL.TextRun(x + 2, rowY, row.Header, Accent, PanelBg, DL.CellAttrFlags.Bold | DL.CellAttrFlags.Underline));
                continue;
            }
            // Find match positions for highlighting
            var res = _results.First(r => r.CommandIndex == row.CmdIdx);
            string title = _allCommands[row.CmdIdx];
            bool isSel = (start + i) == selRowIndex;
            var rowBg = isSel ? new DL.Rgb24(60, 60, 90) : PanelBg;
            builder.DrawRect(new DL.Rect(x + 1, rowY, w - 2, 1, rowBg));
            int maxW = Math.Max(0, w - 3);
            int drawX = x + 2;
            int nextClip = 0;
            var matched = res.MatchPositions;
            for (int mi = 0; mi < matched.Length && nextClip < maxW; mi++)
            {
                int pos = matched[mi];
                if (pos > nextClip)
                {
                    string seg = title.Substring(nextClip, Math.Min(pos - nextClip, maxW - (drawX - (x + 2))));
                    builder.DrawText(new DL.TextRun(drawX, rowY, seg, isSel ? Accent : TextFg, rowBg, isSel ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));
                    drawX += seg.Length;
                }
                if (pos < title.Length && (drawX - (x + 2)) < maxW)
                {
                    string seg = title.Substring(pos, 1);
                    builder.DrawText(new DL.TextRun(drawX, rowY, seg, isSel ? Accent : TextFg, rowBg, (isSel ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None) | DL.CellAttrFlags.Underline));
                    drawX += 1;
                }
                nextClip = pos + 1;
            }
            if (nextClip < title.Length && (drawX - (x + 2)) < maxW)
            {
                string seg = title.Substring(nextClip, Math.Min(title.Length - nextClip, maxW - (drawX - (x + 2))));
                builder.DrawText(new DL.TextRun(drawX, rowY, seg, isSel ? Accent : TextFg, rowBg, isSel ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));
            }
        }
        builder.Pop();
        builder.Pop();
    }

    private readonly record struct FilterResult(int CommandIndex, int Score, int[] MatchPositions)
    {
        public int CommandIndex { get; } = CommandIndex;
        public int Score { get; } = Score;
        public int[] MatchPositions { get; } = MatchPositions;
    }

    // For testing: return current filtered titles in order
    public string[] GetFilteredForTesting()
    {
        return _results.Select(r => _allCommands[r.CommandIndex]).ToArray();
    }
}
