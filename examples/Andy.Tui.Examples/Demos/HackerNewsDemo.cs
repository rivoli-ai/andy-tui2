using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Examples.HackerNews;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Examples.Demos;

public static class HackerNewsDemo
{
    // HN theme colors
    private static readonly DL.Rgb24 HN_ORANGE = new(255, 102, 0);      // #ff6600
    private static readonly DL.Rgb24 HN_BEIGE = new(246, 246, 239);     // #f6f6ef
    private static readonly DL.Rgb24 HN_GRAY = new(130, 130, 130);      // #828282
    private static readonly DL.Rgb24 HN_BLACK = new(0, 0, 0);
    private static readonly DL.Rgb24 HN_WHITE = new(255, 255, 255);
    private static readonly DL.Rgb24 HN_DARK_BG = new(30, 30, 30);
    private static readonly DL.Rgb24 HN_SELECTION = new(255, 140, 60);

    private enum ViewMode { StoryList, StoryDetail, UserProfile, Search }
    private enum StoryFilter { Top, New, Best, Ask, Show, Jobs }

    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = false };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");

        var api = new HackerNewsApiClient();
        var cache = new HackerNewsCache();
        var state = new AppState();

        try
        {
            bool running = true;

            // Load initial stories
            _ = LoadStoriesAsync(api, cache, state, StoryFilter.Top);

            while (running)
            {
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);

                // Handle input
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);

                    if (k.Key == ConsoleKey.Escape)
                    {
                        if (state.CurrentView == ViewMode.StoryList && !state.CommandPaletteOpen)
                        {
                            running = false;
                            break;
                        }
                        else if (state.CommandPaletteOpen)
                        {
                            state.CommandPaletteOpen = false;
                            state.SearchQuery = string.Empty;
                        }
                        else
                        {
                            state.CurrentView = ViewMode.StoryList;
                            state.SelectedCommentIndex = 0;
                            // Position is already preserved in state.SelectedIndex
                        }
                    }
                    else if (k.Key == ConsoleKey.F2)
                    {
                        hud.Enabled = !hud.Enabled;
                    }
                    else if (!state.CommandPaletteOpen && (k.Key == ConsoleKey.Backspace || k.Key == ConsoleKey.Delete))
                    {
                        // Go back to story list from detail view
                        if (state.CurrentView == ViewMode.StoryDetail)
                        {
                            state.CurrentView = ViewMode.StoryList;
                            state.SelectedCommentIndex = 0;
                            // Position is already preserved in state.SelectedIndex
                        }
                    }
                    else if (!state.CommandPaletteOpen && k.KeyChar == '/')
                    {
                        state.CommandPaletteOpen = true;
                        state.SearchQuery = string.Empty;
                    }
                    else if (!state.CommandPaletteOpen && (k.Key == ConsoleKey.T || (k.Key == ConsoleKey.D1 && (k.Modifiers & ConsoleModifiers.Control) == 0)))
                    {
                        state.CurrentFilter = StoryFilter.Top;
                        state.SelectedIndex = 0;
                        _ = LoadStoriesAsync(api, cache, state, StoryFilter.Top);
                    }
                    else if (!state.CommandPaletteOpen && (k.Key == ConsoleKey.N || k.Key == ConsoleKey.D2))
                    {
                        state.CurrentFilter = StoryFilter.New;
                        state.SelectedIndex = 0;
                        _ = LoadStoriesAsync(api, cache, state, StoryFilter.New);
                    }
                    else if (!state.CommandPaletteOpen && (k.Key == ConsoleKey.B || k.Key == ConsoleKey.D3))
                    {
                        state.CurrentFilter = StoryFilter.Best;
                        state.SelectedIndex = 0;
                        _ = LoadStoriesAsync(api, cache, state, StoryFilter.Best);
                    }
                    else if (!state.CommandPaletteOpen && (k.Key == ConsoleKey.A || k.Key == ConsoleKey.D4))
                    {
                        state.CurrentFilter = StoryFilter.Ask;
                        state.SelectedIndex = 0;
                        _ = LoadStoriesAsync(api, cache, state, StoryFilter.Ask);
                    }
                    else if (!state.CommandPaletteOpen && (k.Key == ConsoleKey.S || k.Key == ConsoleKey.D5))
                    {
                        state.CurrentFilter = StoryFilter.Show;
                        state.SelectedIndex = 0;
                        _ = LoadStoriesAsync(api, cache, state, StoryFilter.Show);
                    }
                    else if (!state.CommandPaletteOpen && (k.Key == ConsoleKey.J || k.Key == ConsoleKey.D6))
                    {
                        state.CurrentFilter = StoryFilter.Jobs;
                        state.SelectedIndex = 0;
                        _ = LoadStoriesAsync(api, cache, state, StoryFilter.Jobs);
                    }
                    else if (state.CommandPaletteOpen)
                    {
                        HandleCommandPaletteInput(k, state);
                    }
                    else if (state.CurrentView == ViewMode.StoryList)
                    {
                        HandleStoryListInput(k, state, api, cache);
                    }
                    else if (state.CurrentView == ViewMode.StoryDetail)
                    {
                        HandleStoryDetailInput(k, state, viewport);
                    }
                }

                // Render
                var baseB = new DL.DisplayListBuilder();
                baseB.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                baseB.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, HN_DARK_BG));

                // Header
                RenderHeader(baseB, state, viewport.Width, cache);

                var baseDl = baseB.Build();
                var wb = new DL.DisplayListBuilder();

                // Content
                if (state.CurrentView == ViewMode.StoryList)
                {
                    RenderStoryList(wb, state, viewport, baseDl);
                }
                else if (state.CurrentView == ViewMode.StoryDetail)
                {
                    RenderStoryDetail(wb, state, viewport, baseDl);
                }

                // Command palette overlay
                if (state.CommandPaletteOpen)
                {
                    RenderCommandPalette(wb, state, viewport, baseDl);
                }

                var combined = Combine(baseDl, wb.Build());
                var overlay = new DL.DisplayListBuilder();
                hud.ViewportCols = viewport.Width;
                hud.ViewportRows = viewport.Height;
                hud.Contribute(combined, overlay);

                await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                await Task.Delay(16);
            }
        }
        finally
        {
            // Save cache to disk before exiting
            cache.SaveToDisk();
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    private static void RenderHeader(DL.DisplayListBuilder b, AppState state, int width, HackerNewsCache? cache = null)
    {
        // Top bar with HN orange
        b.DrawRect(new DL.Rect(0, 0, width, 1, HN_ORANGE));
        b.DrawText(new DL.TextRun(1, 0, "Y", HN_WHITE, null, DL.CellAttrFlags.Bold));
        b.DrawText(new DL.TextRun(3, 0, "Hacker News", HN_BLACK, null, DL.CellAttrFlags.Bold));

        // Navigation hints
        var filter = state.CurrentFilter.ToString();
        var hint = $"[{filter}]";
        b.DrawText(new DL.TextRun(width - hint.Length - 2, 0, hint, HN_BLACK, null, DL.CellAttrFlags.None));

        // Second line with navigation
        var nav = "[T]op [N]ew [B]est [A]sk [S]how [J]obs [/]Search [ESC]Quit";
        if (state.CurrentView != ViewMode.StoryList)
        {
            nav = "[ESC/Del]Back " + nav;
        }
        b.DrawText(new DL.TextRun(1, 1, nav, HN_GRAY, null, DL.CellAttrFlags.None));

        // Status line
        var status = state.Loading ? "Loading..." : $"{state.Stories.Count} stories";
        if (!string.IsNullOrEmpty(state.StatusMessage))
        {
            status = state.StatusMessage;
        }

        // Add cache stats if available
        if (cache != null)
        {
            var (items, sizeBytes, maxBytes) = cache.GetStats();
            var sizeMB = sizeBytes / (1024.0 * 1024.0);
            var cacheInfo = $" | Cache: {items} items ({sizeMB:F1}MB)";
            status += cacheInfo;
        }

        b.DrawText(new DL.TextRun(width - status.Length - 2, 1, status, HN_GRAY, null, DL.CellAttrFlags.None));
    }

    private static void RenderStoryList(DL.DisplayListBuilder wb, AppState state, (int Width, int Height) viewport, DL.DisplayList baseDl)
    {
        int startY = 3;
        int linesPerStory = 2;
        int visibleLines = viewport.Height - startY - 1;
        int visibleStories = visibleLines / linesPerStory;

        // Ensure selected item is in view
        if (state.SelectedIndex < state.ScrollOffset)
        {
            state.ScrollOffset = state.SelectedIndex;
        }
        else if (state.SelectedIndex >= state.ScrollOffset + visibleStories)
        {
            state.ScrollOffset = state.SelectedIndex - visibleStories + 1;
        }

        int y = startY;
        for (int i = state.ScrollOffset; i < state.Stories.Count && y < viewport.Height - 1; i++)
        {
            var story = state.Stories[i];
            bool selected = i == state.SelectedIndex;

            // Background for selection
            if (selected)
            {
                wb.DrawRect(new DL.Rect(0, y, viewport.Width, 2, new DL.Rgb24(50, 50, 50)));
            }

            // Rank and score
            var rank = $"{i + 1}.";
            var score = story.Score.HasValue ? $"▲{story.Score}" : "";
            wb.DrawText(new DL.TextRun(1, y, rank, HN_GRAY, null, DL.CellAttrFlags.None));
            wb.DrawText(new DL.TextRun(5, y, score, HN_ORANGE, null, selected ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));

            // Title
            var titleX = 13;
            var title = story.Title ?? "(no title)";
            if (title.Length > viewport.Width - titleX - 2)
            {
                title = title.Substring(0, viewport.Width - titleX - 5) + "...";
            }
            wb.DrawText(new DL.TextRun(titleX, y, title, HN_BEIGE, null, selected ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));

            // Domain/metadata
            var meta = "";
            if (!string.IsNullOrEmpty(story.Domain))
            {
                meta = $"({story.Domain})";
            }
            if (!string.IsNullOrEmpty(meta))
            {
                wb.DrawText(new DL.TextRun(titleX, y + 1, meta, HN_GRAY, null, DL.CellAttrFlags.None));
            }

            // Info line
            var timeAgo = GetTimeAgo(story.CreatedAt);
            var by = story.By ?? "unknown";
            var comments = story.Descendants.HasValue ? $"{story.Descendants} comments" : "discuss";
            var info = $"by {by} {timeAgo} | {comments}";
            var infoX = titleX + meta.Length + 2;
            if (string.IsNullOrEmpty(meta)) infoX = titleX;

            wb.DrawText(new DL.TextRun(infoX, y + 1, info, HN_GRAY, null, DL.CellAttrFlags.None));

            y += 2;
        }

        // Scrollbar indicator
        if (state.Stories.Count > visibleStories)
        {
            var scrollbarHeight = Math.Max(1, visibleLines * visibleStories / state.Stories.Count);
            var scrollbarPos = startY + (state.ScrollOffset * visibleLines / state.Stories.Count);
            for (int i = 0; i < scrollbarHeight; i++)
            {
                wb.DrawText(new DL.TextRun(viewport.Width - 1, scrollbarPos + i, "█", HN_ORANGE, null, DL.CellAttrFlags.None));
            }
        }
    }

    private static void RenderStoryDetail(DL.DisplayListBuilder wb, AppState state, (int Width, int Height) viewport, DL.DisplayList baseDl)
    {
        if (state.CurrentStory == null) return;

        int y = 3;
        var story = state.CurrentStory;

        // Title
        var title = story.Title ?? "(no title)";
        var wrappedTitle = WrapText(title, viewport.Width - 4);
        foreach (var line in wrappedTitle)
        {
            wb.DrawText(new DL.TextRun(2, y++, line, HN_BEIGE, null, DL.CellAttrFlags.Bold));
        }

        // Metadata
        var score = story.Score.HasValue ? $"▲{story.Score} points" : "";
        var by = story.By ?? "unknown";
        var timeAgo = GetTimeAgo(story.CreatedAt);
        var meta = $"{score} by {by} {timeAgo}";
        wb.DrawText(new DL.TextRun(2, y++, meta, HN_GRAY, null, DL.CellAttrFlags.None));

        if (!string.IsNullOrEmpty(story.Url))
        {
            wb.DrawText(new DL.TextRun(2, y++, $"URL: {story.Url}", HN_ORANGE, null, DL.CellAttrFlags.Underline));
        }

        y++; // blank line

        // Text content if available
        if (!string.IsNullOrEmpty(story.Text))
        {
            var text = StripHtml(story.Text);
            var wrappedText = WrapText(text, viewport.Width - 4);
            foreach (var line in wrappedText.Take(10))
            {
                wb.DrawText(new DL.TextRun(2, y++, line, HN_BEIGE, null, DL.CellAttrFlags.None));
            }
            y++; // blank line
        }

        // Comments section
        var commentCount = story.Descendants ?? story.Kids?.Count ?? 0;
        wb.DrawText(new DL.TextRun(2, y++, $"─── Comments ({commentCount}) ───", HN_ORANGE, null, DL.CellAttrFlags.None));
        y++;

        if (state.LoadingComments)
        {
            wb.DrawText(new DL.TextRun(2, y++, "Loading comments...", HN_GRAY, null, DL.CellAttrFlags.None));
        }
        else if (state.Comments.Count > 0)
        {
            RenderComments(wb, state, viewport, ref y);
        }
        else
        {
            wb.DrawText(new DL.TextRun(2, y++, "No comments yet", HN_GRAY, null, DL.CellAttrFlags.None));
        }
    }

    private static void RenderComments(DL.DisplayListBuilder wb, AppState state, (int Width, int Height) viewport, ref int y)
    {
        int startY = y;
        int visibleHeight = viewport.Height - startY;
        int startIdx = Math.Max(0, state.CommentScrollOffset);

        // Clear the comments area to prevent scrolling artifacts
        wb.DrawRect(new DL.Rect(0, startY, viewport.Width, visibleHeight, HN_DARK_BG));

        for (int i = startIdx; i < state.Comments.Count && y < viewport.Height; i++)
        {
            var comment = state.Comments[i];
            bool selected = i == state.SelectedCommentIndex;

            int baseIndent = 2 + comment.Depth * 2;

            // Stop if we're out of space
            if (y >= viewport.Height)
                break;

            if (selected)
            {
                wb.DrawRect(new DL.Rect(0, y, viewport.Width, 1, new DL.Rgb24(50, 50, 50)));
            }

            // Subtle hierarchy indicators - vertical bars for nested comments
            for (int d = 0; d < comment.Depth; d++)
            {
                var barColor = new DL.Rgb24(60, 60, 60); // Subtle gray
                wb.DrawText(new DL.TextRun(2 + d * 2, y, "│", barColor, HN_DARK_BG, DL.CellAttrFlags.None));
            }

            var by = comment.By ?? "unknown";
            var timeAgo = GetTimeAgo(comment.CreatedAt);
            var depthIndicator = comment.Depth > 0 ? "└ " : "";
            var header = $"{depthIndicator}{by} {timeAgo}";
            wb.DrawText(new DL.TextRun(baseIndent, y++, header, HN_ORANGE, HN_DARK_BG, selected ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));

            if (!string.IsNullOrEmpty(comment.Text) && y < viewport.Height)
            {
                var text = StripHtml(comment.Text);
                var textIndent = baseIndent + 2;
                // Add extra margin to prevent terminal auto-wrap at edge
                var maxWidth = viewport.Width - textIndent - 4;
                if (maxWidth < 20) maxWidth = 20; // Minimum readable width
                var wrappedText = WrapText(text, maxWidth);

                // Show as many lines as we have space for
                int maxLines = Math.Min(wrappedText.Count, viewport.Height - y);
                foreach (var line in wrappedText.Take(maxLines))
                {
                    if (y >= viewport.Height)
                        break;

                    // Draw hierarchy bars for text lines too
                    for (int d = 0; d < comment.Depth; d++)
                    {
                        var barColor = new DL.Rgb24(60, 60, 60);
                        wb.DrawText(new DL.TextRun(2 + d * 2, y, "│", barColor, HN_DARK_BG, DL.CellAttrFlags.None));
                    }

                    // CRITICAL: Verify line length doesn't exceed maxWidth
                    var actualLine = line.Length > maxWidth ? line.Substring(0, maxWidth) : line;
                    if (textIndent + actualLine.Length >= viewport.Width)
                    {
                        // This should never happen - truncate if it does
                        actualLine = actualLine.Substring(0, Math.Max(0, viewport.Width - textIndent - 1));
                    }

                    wb.DrawText(new DL.TextRun(textIndent, y++, actualLine, HN_BEIGE, HN_DARK_BG, DL.CellAttrFlags.None));
                }
            }

            // No blank line between comments - removed
        }
    }

    private static void RenderCommandPalette(DL.DisplayListBuilder wb, AppState state, (int Width, int Height) viewport, DL.DisplayList baseDl)
    {
        int paletteWidth = Math.Min(60, viewport.Width - 4);
        int paletteHeight = Math.Min(12, viewport.Height - 4);
        int paletteX = (viewport.Width - paletteWidth) / 2;
        int paletteY = (viewport.Height - paletteHeight) / 2;

        // Background overlay
        wb.DrawRect(new DL.Rect(paletteX, paletteY, paletteWidth, paletteHeight, new DL.Rgb24(40, 40, 40)));
        wb.DrawBorder(new DL.Border(paletteX, paletteY, paletteWidth, paletteHeight, "single", HN_ORANGE));

        // Title
        wb.DrawText(new DL.TextRun(paletteX + 2, paletteY, " Search ", HN_ORANGE, null, DL.CellAttrFlags.Bold));

        // Search input
        var query = state.SearchQuery;
        var prompt = $"> {query}█";
        wb.DrawText(new DL.TextRun(paletteX + 2, paletteY + 2, prompt, HN_BEIGE, null, DL.CellAttrFlags.None));

        // Filtered results
        var filtered = state.Stories
            .Where(s => s.Title != null && s.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(paletteHeight - 5)
            .ToList();

        int resultY = paletteY + 4;
        for (int i = 0; i < filtered.Count && resultY < paletteY + paletteHeight - 1; i++)
        {
            var story = filtered[i];
            var title = story.Title ?? "";
            if (title.Length > paletteWidth - 6)
            {
                title = title.Substring(0, paletteWidth - 9) + "...";
            }

            bool selected = i == state.SearchSelectedIndex;
            var fg = selected ? HN_BLACK : HN_BEIGE;
            var bg = selected ? HN_SELECTION : new DL.Rgb24(40, 40, 40);

            wb.DrawRect(new DL.Rect(paletteX + 1, resultY, paletteWidth - 2, 1, bg));
            wb.DrawText(new DL.TextRun(paletteX + 2, resultY, title, fg, null, selected ? DL.CellAttrFlags.Bold : DL.CellAttrFlags.None));
            resultY++;
        }

        state.SearchResults = filtered;
    }

    private static void HandleStoryListInput(ConsoleKeyInfo k, AppState state, HackerNewsApiClient api, HackerNewsCache cache)
    {
        if (k.Key == ConsoleKey.DownArrow || k.Key == ConsoleKey.J)
        {
            if (state.SelectedIndex < state.Stories.Count - 1)
            {
                state.SelectedIndex++;
            }
        }
        else if (k.Key == ConsoleKey.UpArrow || k.Key == ConsoleKey.K)
        {
            if (state.SelectedIndex > 0)
            {
                state.SelectedIndex--;
            }
        }
        else if (k.Key == ConsoleKey.PageDown)
        {
            state.SelectedIndex = Math.Min(state.Stories.Count - 1, state.SelectedIndex + 10);
        }
        else if (k.Key == ConsoleKey.PageUp)
        {
            state.SelectedIndex = Math.Max(0, state.SelectedIndex - 10);
        }
        else if (k.Key == ConsoleKey.Home)
        {
            state.SelectedIndex = 0;
        }
        else if (k.Key == ConsoleKey.End)
        {
            state.SelectedIndex = Math.Max(0, state.Stories.Count - 1);
        }
        else if (k.Key == ConsoleKey.Enter || k.Key == ConsoleKey.Spacebar)
        {
            if (state.SelectedIndex < state.Stories.Count)
            {
                state.CurrentStory = state.Stories[state.SelectedIndex];
                state.CurrentView = ViewMode.StoryDetail;
                state.Comments.Clear();
                state.SelectedCommentIndex = 0;
                state.CommentScrollOffset = 0;
                _ = LoadCommentsAsync(api, cache, state);
            }
        }
        else if (k.Key == ConsoleKey.R)
        {
            _ = LoadStoriesAsync(api, cache, state, state.CurrentFilter);
        }
    }

    private static void HandleStoryDetailInput(ConsoleKeyInfo k, AppState state, (int Width, int Height) viewport)
    {
        if (k.Key == ConsoleKey.DownArrow || k.Key == ConsoleKey.J)
        {
            if (state.SelectedCommentIndex < state.Comments.Count - 1)
            {
                state.SelectedCommentIndex++;
                // Simple scrolling - keep selected item visible
                if (state.SelectedCommentIndex > state.CommentScrollOffset + 5)
                {
                    state.CommentScrollOffset = state.SelectedCommentIndex - 5;
                }
            }
        }
        else if (k.Key == ConsoleKey.UpArrow || k.Key == ConsoleKey.K)
        {
            if (state.SelectedCommentIndex > 0)
            {
                state.SelectedCommentIndex--;
                if (state.SelectedCommentIndex < state.CommentScrollOffset)
                {
                    state.CommentScrollOffset = state.SelectedCommentIndex;
                }
            }
        }
        else if (k.Key == ConsoleKey.PageDown)
        {
            state.SelectedCommentIndex = Math.Min(state.Comments.Count - 1, state.SelectedCommentIndex + 10);
            state.CommentScrollOffset = Math.Max(0, state.SelectedCommentIndex - 5);
        }
        else if (k.Key == ConsoleKey.PageUp)
        {
            state.SelectedCommentIndex = Math.Max(0, state.SelectedCommentIndex - 10);
            state.CommentScrollOffset = Math.Max(0, state.SelectedCommentIndex - 5);
        }
        else if (k.Key == ConsoleKey.Home)
        {
            state.SelectedCommentIndex = 0;
            state.CommentScrollOffset = 0;
        }
        else if (k.Key == ConsoleKey.End)
        {
            state.SelectedCommentIndex = Math.Max(0, state.Comments.Count - 1);
            state.CommentScrollOffset = Math.Max(0, state.Comments.Count - 10);
        }
    }

    private static void HandleCommandPaletteInput(ConsoleKeyInfo k, AppState state)
    {
        if (k.Key == ConsoleKey.Backspace)
        {
            if (state.SearchQuery.Length > 0)
            {
                state.SearchQuery = state.SearchQuery[..^1];
                state.SearchSelectedIndex = 0;
            }
        }
        else if (k.Key == ConsoleKey.DownArrow)
        {
            if (state.SearchSelectedIndex < state.SearchResults.Count - 1)
            {
                state.SearchSelectedIndex++;
            }
        }
        else if (k.Key == ConsoleKey.UpArrow)
        {
            if (state.SearchSelectedIndex > 0)
            {
                state.SearchSelectedIndex--;
            }
        }
        else if (k.Key == ConsoleKey.Enter)
        {
            if (state.SearchSelectedIndex < state.SearchResults.Count)
            {
                var selected = state.SearchResults[state.SearchSelectedIndex];
                state.SelectedIndex = state.Stories.IndexOf(selected);
                state.CommandPaletteOpen = false;
                state.SearchQuery = string.Empty;
            }
        }
        else if (!char.IsControl(k.KeyChar))
        {
            state.SearchQuery += k.KeyChar;
            state.SearchSelectedIndex = 0;
        }
    }

    private static async Task LoadStoriesAsync(HackerNewsApiClient api, HackerNewsCache cache, AppState state, StoryFilter filter)
    {
        state.Loading = true;
        state.StatusMessage = "Loading stories...";

        try
        {
            var cacheKey = filter.ToString();

            // Try cache first
            List<int> ids;
            if (cache.TryGetStoryList(cacheKey, out var cachedIds) && cachedIds != null)
            {
                ids = cachedIds;
                state.StatusMessage = "Loading from cache...";
            }
            else
            {
                ids = filter switch
                {
                    StoryFilter.New => await api.GetNewStoriesAsync(50),
                    StoryFilter.Best => await api.GetBestStoriesAsync(50),
                    StoryFilter.Ask => await api.GetAskStoriesAsync(50),
                    StoryFilter.Show => await api.GetShowStoriesAsync(50),
                    StoryFilter.Jobs => await api.GetJobStoriesAsync(50),
                    _ => await api.GetTopStoriesAsync(50)
                };
                cache.SetStoryList(cacheKey, ids);
            }

            // Load items with caching - parallel for speed
            var stories = new List<HNItem>();
            var tasks = ids.Select(async id =>
            {
                if (cache.TryGetItem(id, out var cachedItem) && cachedItem != null)
                {
                    return cachedItem;
                }
                else
                {
                    var item = await api.GetItemAsync(id);
                    if (item != null)
                    {
                        cache.SetItem(id, item);
                    }
                    return item;
                }
            }).ToList();

            var results = await Task.WhenAll(tasks);
            stories = results.Where(item => item != null).Cast<HNItem>().ToList();

            state.Stories = stories;
            state.StatusMessage = $"Loaded {state.Stories.Count} stories";
            await Task.Delay(2000);
            state.StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            state.StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            state.Loading = false;
        }
    }

    private static async Task LoadCommentsAsync(HackerNewsApiClient api, HackerNewsCache cache, AppState state)
    {
        if (state.CurrentStory?.Kids == null || state.CurrentStory.Kids.Count == 0)
        {
            return;
        }

        state.LoadingComments = true;

        try
        {
            var allCommentIds = state.CurrentStory.Kids.Take(100).ToList();
            var flatComments = new List<CommentWithDepth>();

            // Load in batches for progressive display
            const int batchSize = 10;
            for (int i = 0; i < allCommentIds.Count; i += batchSize)
            {
                var batch = allCommentIds.Skip(i).Take(batchSize);

                // Load this batch in parallel
                var tasks = batch.Select(async id =>
                {
                    if (cache.TryGetItem(id, out var cachedItem) && cachedItem != null)
                    {
                        return cachedItem;
                    }
                    else
                    {
                        var item = await api.GetItemAsync(id);
                        if (item != null)
                        {
                            cache.SetItem(id, item);
                        }
                        return item;
                    }
                }).ToList();

                var results = await Task.WhenAll(tasks);
                var commentItems = results.Where(item => item != null).Cast<HNItem>().ToList();

                // Flatten this batch (with limited depth for first pass)
                foreach (var item in commentItems)
                {
                    await FlattenComments(api, cache, item, 0, flatComments, maxDepth: 2);
                }

                // Update UI immediately with this batch
                state.Comments = new List<CommentWithDepth>(flatComments);

                // Mark loading as false after first batch so UI shows content
                if (i == 0)
                {
                    state.LoadingComments = false;
                }
            }

            // Now load deeper comments in background
            _ = Task.Run(async () =>
            {
                try
                {
                    var existingIds = new HashSet<int>(flatComments.Select(c => c.Comment.Id));
                    var deepComments = new List<CommentWithDepth>();

                    foreach (var comment in flatComments.ToList())
                    {
                        if (comment.Comment.Kids != null && comment.Depth < 2)
                        {
                            await LoadDeeperComments(api, cache, comment.Comment, comment.Depth, deepComments, existingIds);
                        }
                    }

                    // Merge deep comments (only new ones)
                    if (deepComments.Count > 0)
                    {
                        var allComments = flatComments.Concat(deepComments).ToList();
                        state.Comments = allComments;
                    }
                }
                catch { /* Silently fail background loading */ }
            });
        }
        catch (Exception ex)
        {
            state.StatusMessage = $"Error loading comments: {ex.Message}";
        }
        finally
        {
            state.LoadingComments = false;
        }
    }

    private static async Task LoadDeeperComments(HackerNewsApiClient api, HackerNewsCache cache, HNItem comment, int depth, List<CommentWithDepth> result, HashSet<int> existingIds)
    {
        if (comment.Kids == null || comment.Kids.Count == 0 || depth >= 10)
            return;

        var tasks = comment.Kids.Take(50).Select(async id =>
        {
            if (cache.TryGetItem(id, out var cachedItem) && cachedItem != null)
            {
                return cachedItem;
            }
            else
            {
                var item = await api.GetItemAsync(id);
                if (item != null)
                {
                    cache.SetItem(id, item);
                }
                return item;
            }
        }).ToList();

        var results = await Task.WhenAll(tasks);
        var childItems = results.Where(item => item != null).Cast<HNItem>().ToList();

        foreach (var child in childItems)
        {
            // Only add if not already in the list
            if (!existingIds.Contains(child.Id))
            {
                result.Add(new CommentWithDepth(child, depth + 1));
                existingIds.Add(child.Id);
                await LoadDeeperComments(api, cache, child, depth + 1, result, existingIds);
            }
        }
    }

    private static async Task FlattenComments(HackerNewsApiClient api, HackerNewsCache cache, HNItem comment, int depth, List<CommentWithDepth> result, int maxDepth = 10)
    {
        if (depth > maxDepth || result.Count > 1000) return; // Limit depth and total comments

        result.Add(new CommentWithDepth(comment, depth));

        if (comment.Kids != null && comment.Kids.Count > 0 && depth < maxDepth)
        {
            // Load child items with caching - parallel for speed
            var tasks = comment.Kids.Take(50).Select(async id =>
            {
                if (cache.TryGetItem(id, out var cachedItem) && cachedItem != null)
                {
                    return cachedItem;
                }
                else
                {
                    var item = await api.GetItemAsync(id);
                    if (item != null)
                    {
                        cache.SetItem(id, item);
                    }
                    return item;
                }
            }).ToList();

            var results = await Task.WhenAll(tasks);
            var childItems = results.Where(item => item != null).Cast<HNItem>().ToList();

            foreach (var child in childItems)
            {
                await FlattenComments(api, cache, child, depth + 1, result, maxDepth);
            }
        }
    }

    private static string GetTimeAgo(DateTime time)
    {
        var span = DateTime.UtcNow - time.ToUniversalTime();

        if (span.TotalMinutes < 1) return "just now";
        if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
        if (span.TotalDays < 30) return $"{(int)span.TotalDays}d ago";
        if (span.TotalDays < 365) return $"{(int)(span.TotalDays / 30)}mo ago";
        return $"{(int)(span.TotalDays / 365)}y ago";
    }

    private static string StripHtml(string html)
    {
        var result = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "");
        result = System.Web.HttpUtility.HtmlDecode(result);
        return result;
    }

    private static List<string> WrapText(string text, int maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return new List<string>();
        if (maxWidth <= 0) return new List<string> { text };

        var lines = new List<string>();
        var words = text.Split(' ');
        var currentLine = "";

        foreach (var word in words)
        {
            // If the word itself is longer than maxWidth, we need to break it
            if (word.Length > maxWidth)
            {
                // First, add the current line if it has content
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                    currentLine = "";
                }

                // Break the long word into chunks of maxWidth
                for (int i = 0; i < word.Length; i += maxWidth)
                {
                    var chunk = word.Substring(i, Math.Min(maxWidth, word.Length - i));
                    lines.Add(chunk);
                }
                continue;
            }

            // Check if adding this word would exceed maxWidth
            var testLine = currentLine.Length > 0 ? currentLine + " " + word : word;
            if (testLine.Length > maxWidth)
            {
                // Current line is full, save it and start a new one
                if (!string.IsNullOrEmpty(currentLine))
                {
                    lines.Add(currentLine);
                }
                currentLine = word;
            }
            else
            {
                // Add word to current line
                currentLine = testLine;
            }
        }

        // Add the last line
        if (!string.IsNullOrEmpty(currentLine))
        {
            lines.Add(currentLine);
        }

        return lines;
    }

    private static DL.DisplayList Combine(DL.DisplayList a, DL.DisplayList b)
    {
        var builder = new DL.DisplayListBuilder();
        void Append(DL.DisplayList dl)
        {
            foreach (var op in dl.Ops)
            {
                switch (op)
                {
                    case DL.Rect r: builder.DrawRect(r); break;
                    case DL.Border br: builder.DrawBorder(br); break;
                    case DL.TextRun tr: builder.DrawText(tr); break;
                    case DL.ClipPush cp: builder.PushClip(cp); break;
                    case DL.LayerPush lp: builder.PushLayer(lp); break;
                    case DL.Pop: builder.Pop(); break;
                }
            }
        }
        Append(a);
        Append(b);
        return builder.Build();
    }

    private sealed class AppState
    {
        public ViewMode CurrentView { get; set; } = ViewMode.StoryList;
        public StoryFilter CurrentFilter { get; set; } = StoryFilter.Top;
        public List<HNItem> Stories { get; set; } = new();
        public int SelectedIndex { get; set; } = 0;
        public int ScrollOffset { get; set; } = 0;
        public bool Loading { get; set; } = false;
        public string StatusMessage { get; set; } = string.Empty;

        // Story detail
        public HNItem? CurrentStory { get; set; }
        public List<CommentWithDepth> Comments { get; set; } = new();
        public int SelectedCommentIndex { get; set; } = 0;
        public int CommentScrollOffset { get; set; } = 0;
        public bool LoadingComments { get; set; } = false;

        // Search
        public bool CommandPaletteOpen { get; set; } = false;
        public string SearchQuery { get; set; } = string.Empty;
        public int SearchSelectedIndex { get; set; } = 0;
        public List<HNItem> SearchResults { get; set; } = new();
    }

    private sealed record CommentWithDepth(HNItem Comment, int Depth)
    {
        public string? By => Comment.By;
        public string? Text => Comment.Text;
        public DateTime CreatedAt => Comment.CreatedAt;
    }
}
