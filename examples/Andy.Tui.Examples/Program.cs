using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.Examples.Demos;
using Andy.Tui.DisplayList;

class Program
{
    static async Task Main()
    {
        var caps = CapabilityDetector.DetectFromEnvironment();
        var viewport = (Width: Console.WindowWidth, Height: Console.WindowHeight);
        await RunMainMenu(viewport, caps);
    }

    static async Task RenderAsync(DisplayListBuilder builder, (int Width, int Height) viewport, TerminalCapabilities caps, bool showHud = false)
    {
        var dl = builder.Build();
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = showHud };
        scheduler.SetMetricsSink(hud);
        var pty = new Andy.Tui.Examples.StdoutPty();
        // Combine base + overlay for a single frame
        var overlayBuilder = new DisplayListBuilder();
        hud.ViewportCols = viewport.Width;
        hud.ViewportRows = viewport.Height;
        hud.Contribute(dl, overlayBuilder);
        var combined = Combine(dl, overlayBuilder.Build());
        await scheduler.RenderOnceAsync(combined, viewport, caps, pty, CancellationToken.None);
    }

    static async Task RunSingleWidgetExample(string choice, (int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
        b.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
        switch (choice)
        {
            case "2": // buttons handled in main loop
                break;
            case "3": // Toggle
                {
                    b.DrawText(new TextRun(2, 1, "Toggle", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
                    var toggle = new Andy.Tui.Widgets.Toggle(true, "WiFi");
                    var baseDl = b.Build();
                    var bb = new DisplayListBuilder();
                    toggle.SetFocused(true);
                    toggle.Render(new Andy.Tui.Layout.Rect(2, 3, 16, 1), baseDl, bb);
                    b = CombineBuilders(b, bb);
                    break;
                }
            case "4": // Checkbox
                {
                    b.DrawText(new TextRun(2, 1, "Checkbox", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
                    var cb = new Andy.Tui.Widgets.Checkbox("Receive updates", true);
                    var baseDl = b.Build();
                    var bb = new DisplayListBuilder();
                    cb.Render(new Andy.Tui.Layout.Rect(2, 3, viewport.Width - 4, 1), baseDl, bb);
                    var combined = Combine(Combine(baseDl, b.Build()), bb.Build());
                    await RenderAndWaitForEsc(new PrebuiltDisplayListBuilder(combined), viewport, caps);
                    break;
                }
            case "5": // RadioGroup
                {
                    b.DrawText(new TextRun(2, 1, "RadioGroup", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
                    var rg = new Andy.Tui.Widgets.RadioGroup();
                    rg.SetItems(new[] { "Red", "Green", "Blue" });
                    rg.SetSelectedIndex(1);
                    var baseDl = b.Build();
                    var bb = new DisplayListBuilder();
                    rg.Render(new Andy.Tui.Layout.Rect(2, 3, 20, 5), baseDl, bb);
                    var combined = Combine(Combine(baseDl, b.Build()), bb.Build());
                    await RenderAndWaitForEsc(new PrebuiltDisplayListBuilder(combined), viewport, caps);
                    break;
                }
            case "6": // TextInput
                {
                    b.DrawText(new TextRun(2, 1, "TextInput", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
                    var ti = new Andy.Tui.Widgets.TextInput();
                    ti.SetText("Hello world");
                    ti.SetFocused(true);
                    var baseDl = b.Build();
                    var bb = new DisplayListBuilder();
                    ti.Render(new Andy.Tui.Layout.Rect(2, 3, 24, 1), baseDl, bb);
                    var combined = Combine(Combine(baseDl, b.Build()), bb.Build());
                    await RenderAndWaitForEsc(new PrebuiltDisplayListBuilder(combined), viewport, caps);
                    break;
                }
            case "7": // ScrollView
                {
                    b.DrawText(new TextRun(2, 1, "ScrollView", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
                    var sv = new Andy.Tui.Widgets.ScrollView();
                    sv.SetContent(string.Join("\n", Enumerable.Range(1, 50).Select(i => $"Line {i}")));
                    sv.SetScrollY(10);
                    var baseDl = b.Build();
                    var bb = new DisplayListBuilder();
                    sv.Render(new Andy.Tui.Layout.Rect(2, 3, 30, 10), baseDl, bb);
                    b = CombineBuilders(b, bb);
                    break;
                }
            case "8": // ProgressBar + Slider
                {
                    b.DrawText(new TextRun(2, 1, "ProgressBar + Slider", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
                    var pb = new Andy.Tui.Widgets.ProgressBar { Value = 0.6 };
                    var sl = new Andy.Tui.Widgets.Slider { Value = 0.3 };
                    var baseDl = b.Build();
                    var bb = new DisplayListBuilder();
                    pb.Render(new Andy.Tui.Layout.Rect(2, 3, 30, 1), baseDl, bb);
                    sl.Render(new Andy.Tui.Layout.Rect(2, 5, 30, 1), baseDl, bb);
                    b = CombineBuilders(b, bb);
                    break;
                }
        }
        await RenderAsync(b, viewport, caps, showHud: true);
    }

    static DisplayListBuilder CombineBuilders(DisplayListBuilder a, DisplayListBuilder b)
    {
        var dl = Combine(a.Build(), b.Build());
        var nb = new DisplayListBuilder();
        foreach (var op in dl.Ops)
        {
            switch (op)
            {
                case Rect r: nb.DrawRect(r); break;
                case Border br: nb.DrawBorder(br); break;
                case TextRun tr: nb.DrawText(tr); break;
                case ClipPush cp: nb.PushClip(cp); break;
                case LayerPush lp: nb.PushLayer(lp); break;
                case Pop: nb.Pop(); break;
            }
        }
        return nb;
    }

    static DisplayList Combine(DisplayList a, DisplayList b)
    {
        var builder = new DisplayListBuilder();
        void Append(DisplayList dl)
        {
            foreach (var op in dl.Ops)
            {
                switch (op)
                {
                    case Rect r: builder.DrawRect(r); break;
                    case Border br: builder.DrawBorder(br); break;
                    case TextRun tr: builder.DrawText(tr); break;
                    case ClipPush cp: builder.PushClip(cp); break;
                    case LayerPush lp: builder.PushLayer(lp); break;
                    case Pop: builder.Pop(); break;
                }
            }
        }
        Append(a);
        Append(b);
        return builder.Build();
    }

    // Adapter to reuse RenderAndWaitForEsc signature without deriving
    readonly struct PrebuiltDisplayListBuilder
    {
        private readonly DisplayList _dl;
        public PrebuiltDisplayListBuilder(DisplayList dl) { _dl = dl; }
        public DisplayList Build() => _dl;
    }

    static async Task RunMainMenu((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        bool running = true;
        while (running)
        {
            var b = new DisplayListBuilder();
            b.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
            b.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
            b.DrawText(new TextRun(2, 1, "Andy.Tui Examples — Type number then Enter (ESC/Q to quit)", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
            int y = 3;
            string[] items = new[]
        {
                "1) Hello + HUD (static)",
                "2) Buttons (interactive)",
                "3) Toggle (interactive)",
                "4) Checkbox (interactive)",
                "5) RadioGroup (interactive)",
                "6) TextInput (interactive)",
                "7) ScrollView (interactive)",
                "8) ProgressBar + Slider (animated)",
                "9) Real-time Log (auto-append)",
                "10) Chat (interactive)",
                "11) ListBox (interactive)",
                "12) MenuBar + Menus (interactive)",
                    "13) TreeView (interactive)",
                "14) Table (real-time)",
                "15) Virtualized Grid (scroll)",
                "16) International text (CJK/RTL/Emoji)",
                "17) EditorView (interactive)",
                "18) Context Menu (interactive)",
                "19) Select / Dropdown (interactive)",
                "20) Command Palette (interactive)",
                "21) LargeText Clock (world time)",
                "22) ASCII Art (Napoleon image)",
                "23) Pager/Toast/Spinner (basics)",
                "24) Sparklines (ASCII)",
                "25) Bar Chart (ASCII)",
                "26) Link (underlined; OSC8 TBD)",
                "27) Layers (stack/overlay)",
                "28) Data Grid (virtualized)",
                "29) Modal Dialog (Confirm/Prompt)",
                "30) Splitter/Resizer",
                "31) Tabs / TabView",
                "32) Accordion / Collapsible",
                "33) GroupBox / Fieldset",
                "34) Align / Spacer / Expander",
                "35) Dock / Sidebar / Drawer",
                "36) Status/Title bar",
                "37) Breadcrumbs",
                "38) Router / Navigator",
                "39) ListView (multi-select)",
                "40) Carousel / Stepper",
                "41) Focus Ring / Manager",
                "42) Rich Text / Markup",
                "43) Code Viewer",
                "44) Markdown Renderer",
                "45) Diff Viewer",
                "46) Tooltip / Popover",
                "47) Badge / Pill",
                "48) Hint / Help Panel",
                "49) Tree Table",
                "50) Key–Value / Description List",
                "51) Card",
                "52) Timeline",
                "53) Line & Area Charts",
                "54) Scatter / Histogram / Box",
                "55) More Charts (Heatmap/Bullet/Gauge/Candles)",
                "56) Gantt / Graph",
                "57) Map / Panels",
                "58) File / Find / Prefs / Color",
                "59) About Dialog",
                "60) Title + Badge",
                "61) Notifications / Bell",
                "62) Resize Handle",
                "63) FIGlet Viewer"
            };
            foreach (var line in items)
            {
                b.DrawText(new TextRun(4, y++, line, new Rgb24(220, 220, 220), null, CellAttrFlags.None));
            }
            b.DrawText(new TextRun(2, y + 1, "Press ESC/Q in any demo to return here. ESC/Q here quits.", new Rgb24(160, 160, 160), null, CellAttrFlags.None));
            await RenderAsync(b, viewport, caps, showHud: true);

            var sel = ReadMenuSelectionInteractive(items);
            if (sel is null) return; // quit
            switch (sel)
            {
                case "1": await HelloDemo.Run(viewport, caps); break;
                case "2": await ButtonsInteractiveDemo.Run(viewport, caps); break;
                case "3": await ToggleInteractiveDemo.Run(viewport, caps); break;
                case "4": await CheckboxInteractiveDemo.Run(viewport, caps); break;
                case "5": await RadioGroupInteractiveDemo.Run(viewport, caps); break;
                case "6": await TextInputInteractiveDemo.Run(viewport, caps); break;
                case "7": await ScrollViewInteractiveDemo.Run(viewport, caps); break;
                case "8": await ProgressAnimatedDemo.Run(viewport, caps); break;
                case "9": await LogAutoAppendDemo.Run(viewport, caps); break;
                case "10": await ChatInteractiveDemo.Run(viewport, caps); break;
                case "0": await ChatInteractiveDemo.Run(viewport, caps); break;
                case "11": await ListBoxInteractiveDemo.Run(viewport, caps); break;
                case "12": await MenuBarInteractiveDemo.Run(viewport, caps); break;
                case "13": await TreeViewInteractiveDemo.Run(viewport, caps); break;
                case "14": await TableRealtimeDemo.Run(viewport, caps); break;
                case "15": await VirtualizedGridDemo.Run(viewport, caps); break;
                case "16": await InternationalTextDemo.Run(viewport, caps); break;
                case "17": await EditorViewDemo.Run(viewport, caps); break;
                case "18": await ContextMenuDemo.Run(viewport, caps); break;
                case "19": await SelectDemo.Run(viewport, caps); break;
                case "20": await CommandPaletteDemo.Run(viewport, caps); break;
                case "21": await LargeTextClockDemo.Run(viewport, caps); break;
                case "22": await AsciiArtDemo.Run(viewport, caps); break;
                case "23": await PagerToastSpinnerDemo.Run(viewport, caps); break;
                case "24": await SparklinesDemo.Run(viewport, caps); break;
                case "25": await BarChartDemo.Run(viewport, caps); break;
                case "26": await LinkDemo.Run(viewport, caps); break;
                case "27": await LayersDemo.Run(viewport, caps); break;
                case "28": await DataGridDemo.Run(viewport, caps); break;
                case "29": await ModalDialogDemo.Run(viewport, caps); break;
                case "30": await SplitterDemo.Run(viewport, caps); break;
                case "31": await TabsDemo.Run(viewport, caps); break;
                case "32": await AccordionDemo.Run(viewport, caps); break;
                case "33": await GroupBoxDemo.Run(viewport, caps); break;
                case "34": await AlignDemo.Run(viewport, caps); break;
                case "35": await DockDemo.Run(viewport, caps); break;
                case "36": await StatusBarDemo.Run(viewport, caps); break;
                case "37": await BreadcrumbsDemo.Run(viewport, caps); break;
                case "38": await RouterDemo.Run(viewport, caps); break;
                case "39": await ListViewDemo2.Run(viewport, caps); break;
                case "40": await CarouselDemo.Run(viewport, caps); break;
                case "41": await FocusRingDemo.Run(viewport, caps); break;
                case "42": await RichTextDemo.Run(viewport, caps); break;
                case "43": await CodeViewerDemo.Run(viewport, caps); break;
                case "44": await MarkdownDemo.Run(viewport, caps); break;
                case "45": await DiffViewerDemo.Run(viewport, caps); break;
                case "46": await TooltipDemo.Run(viewport, caps); break;
                case "47": await BadgeDemo.Run(viewport, caps); break;
                case "48": await HintPanelDemo.Run(viewport, caps); break;
                case "49": await TreeTableDemo.Run(viewport, caps); break;
                case "50": await KeyValueListDemo.Run(viewport, caps); break;
                case "51": await CardDemo.Run(viewport, caps); break;
                case "52": await TimelineDemo.Run(viewport, caps); break;
                case "53": await LineAreaChartsDemo.Run(viewport, caps); break;
                case "54": await ScatterHistogramBoxDemo.Run(viewport, caps); break;
                case "55": await MoreChartsDemo.Run(viewport, caps); break;
                case "56": await GanttGraphMapDemo.Run(viewport, caps); break;
                case "57": await MapAndPanelsDemo.Run(viewport, caps); break;
                case "58": await FileFindPrefsColorDemo.Run(viewport, caps); break;
                case "59": await AboutDialogDemo.Run(viewport, caps); break;
                case "60": await TitleBadgeDemo.Run(viewport, caps); break;
                case "61": await BellDemo.Run(viewport, caps); break;
                case "62": await ResizeHandleDemo.Run(viewport, caps); break;
                case "63": await FigletViewerDemo.Run(viewport, caps); break;
            }
        }
    }

    static async Task RunAllWidgetsDashboard((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new Andy.Tui.Examples.StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            bool running = true;
            int focus = 1; // skip menubar (0)
            // States
            bool btnActive = false, cbChecked = true, tgOn = true;
            int radioIndex = 0, listSel = 0; double slider = 0.4, progress = 0.6;
            string input = "Type here";
            // Tree
            var tv = new Andy.Tui.Widgets.TreeView();
            var mammals = new RenderNode("mammals", "Mammals", false, new[] { new RenderNode("human", "Homo sapiens", true), new RenderNode("wolf", "Canis lupus", true), new RenderNode("whale", "Balaenoptera musculus", true) });
            var birds = new RenderNode("birds", "Birds", false, new[] { new RenderNode("eagle", "Aquila chrysaetos", true), new RenderNode("sparrow", "Passer domesticus", true) });
            var reptiles = new RenderNode("reptiles", "Reptiles", false, new[] { new RenderNode("cobra", "Naja naja", true), new RenderNode("tortoise", "Testudo graeca", true) });
            tv.SetRoots(new[] { new RenderNode("life", "Tree of Life", false, new[] { mammals, birds, reptiles }) }); tv.Expand("life"); tv.Select("mammals");
            // Table
            var table = new Andy.Tui.Widgets.Table();
            table.SetColumns(new[] { "Ticker", "Price", "Change" });
            var tableRows = new List<string[]>{
                new[]{"AAPL","196.48","+0.84%"}, new[]{"MSFT","423.12","-0.31%"}, new[]{"GOOGL","174.77","+1.12%"}, new[]{"AMZN","182.14","+0.45%"}, new[]{"NVDA","130.15","+2.15%"}, new[]{"TSLA","232.65","-1.02%"}
            };
            table.SetRows(tableRows);
            // List items
            var listItems = Enumerable.Range(1, 50).Select(i => $"Item {i:D2}").ToArray();
            var scrollbar = new Andy.Tui.Widgets.ScrollView(); scrollbar.SetContent(string.Join("\n", Enumerable.Range(1, 200).Select(i => $"Line {i}")));

            while (running)
            {
                // Handle resize consistently with demos
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, scheduler);
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    if (k.Key == ConsoleKey.Tab)
                    {
                        if ((k.Modifiers & ConsoleModifiers.Shift) != 0) focus = (focus - 1 + 10) % 10; else focus = (focus + 1) % 10;
                    }
                    else
                    {
                        switch (focus)
                        {
                            case 1: // Button
                                if (k.Key is ConsoleKey.Spacebar or ConsoleKey.Enter) btnActive = !btnActive; break;
                            case 2: // Checkbox
                                if (k.Key is ConsoleKey.Spacebar or ConsoleKey.Enter) cbChecked = !cbChecked; break;
                            case 3: // Radio
                                if (k.Key == ConsoleKey.UpArrow) radioIndex = Math.Max(0, radioIndex - 1);
                                else if (k.Key == ConsoleKey.DownArrow) radioIndex = Math.Min(2, radioIndex + 1); break;
                            case 4: // Toggle
                                if (k.Key is ConsoleKey.Spacebar or ConsoleKey.Enter) tgOn = !tgOn; break;
                            case 5: // TextInput
                                if (k.Key == ConsoleKey.Backspace) { if (input.Length > 0) input = input[..^1]; }
                                else if (!char.IsControl(k.KeyChar)) input += k.KeyChar; break;
                            case 6: // ListBox
                                if (k.Key == ConsoleKey.UpArrow) listSel = Math.Max(0, listSel - 1);
                                else if (k.Key == ConsoleKey.DownArrow) listSel = Math.Min(listItems.Length - 1, listSel + 1); break;
                            case 7: // ScrollView
                                if (k.Key == ConsoleKey.UpArrow) scrollbar.AdjustScroll(-1, viewport.Height / 3);
                                else if (k.Key == ConsoleKey.DownArrow) scrollbar.AdjustScroll(1, viewport.Height / 3); break;
                            case 8: // Progress/Slider
                                if (k.Key == ConsoleKey.LeftArrow) slider = Math.Max(0, slider - 0.05);
                                else if (k.Key == ConsoleKey.RightArrow) slider = Math.Min(1, slider + 0.05);
                                progress = slider; break;
                            case 9: // TreeView
                                if (k.Key == ConsoleKey.UpArrow) tv.SelectPrevious();
                                else if (k.Key == ConsoleKey.DownArrow) tv.SelectNext();
                                else if (k.Key is ConsoleKey.LeftArrow or ConsoleKey.RightArrow) tv.ToggleExpandSelected();
                                break;
                            case 10: // Table
                                if (k.Key == ConsoleKey.S) { table.SortBy(0, asc: true); }
                                break;
                        }
                    }
                }

                var b = new DisplayListBuilder();
                b.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
                // Header
                b.DrawText(new TextRun(2, 1, "All Widgets — Tab/Shift+Tab to navigate; arrows/Enter/Space interact; ESC/Q back; F2 HUD", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
                var baseDl = b.Build();
                var wb = new DisplayListBuilder();
                // MenuBar
                new Andy.Tui.Widgets.MenuBar().Add("File", new Andy.Tui.Widgets.Menu()).Add("Edit", new Andy.Tui.Widgets.Menu()).Add("View", new Andy.Tui.Widgets.Menu())
                    .Render(new Andy.Tui.Layout.Rect(0, 0, viewport.Width, 1), baseDl, wb);
                // Layout positions
                int colW = viewport.Width / 2 - 3; int rowH = viewport.Height / 2 - 4;
                var leftTop = new Andy.Tui.Layout.Rect(2, 3, colW, rowH);
                var rightTop = new Andy.Tui.Layout.Rect(4 + colW, 3, colW, rowH);
                var leftBottom = new Andy.Tui.Layout.Rect(2, 4 + rowH, colW, rowH);
                var rightBottom = new Andy.Tui.Layout.Rect(4 + colW, 4 + rowH, colW, rowH);
                // LeftTop controls panel
                var panel = new Andy.Tui.Widgets.Panel(); panel.SetTitle("Controls"); panel.Render(leftTop, baseDl, wb);
                int y0 = (int)leftTop.Y + 1;
                var btn = new Andy.Tui.Widgets.Button("Run"); btn.SetActive(btnActive); btn.SetFocused(focus == 1); btn.Render(new Andy.Tui.Layout.Rect((int)leftTop.X + 2, y0, 10, 1), baseDl, wb);
                var cb = new Andy.Tui.Widgets.Checkbox("Enable feature", cbChecked); cb.Render(new Andy.Tui.Layout.Rect((int)leftTop.X + 2, y0 + 2, (int)leftTop.Width - 4, 1), baseDl, wb);
                var tg = new Andy.Tui.Widgets.Toggle(tgOn, "Power"); tg.SetFocused(focus == 4); tg.Render(new Andy.Tui.Layout.Rect((int)leftTop.X + 2, y0 + 4, 14, 1), baseDl, wb);
                // Radio group (simple render)
                var rg = new Andy.Tui.Widgets.RadioGroup(); rg.SetItems(new[] { "Option A", "Option B", "Option C" }); rg.SetSelectedIndex(radioIndex);
                rg.Render(new Andy.Tui.Layout.Rect((int)leftTop.X + 2, y0 + 6, (int)leftTop.Width - 4, 3), baseDl, wb);
                // TextInput
                var ti = new Andy.Tui.Widgets.TextInput(); ti.SetFocused(focus == 5); ti.SetText(input);
                ti.Render(new Andy.Tui.Layout.Rect((int)leftTop.X + 2, y0 + 10, (int)leftTop.Width - 4, 1), baseDl, wb);
                // ListBox
                var lb = new Andy.Tui.Widgets.ListBox(); lb.SetItems(listItems); lb.SetSelectedIndex(listSel);
                lb.Render(new Andy.Tui.Layout.Rect((int)leftTop.X + 2, y0 + 12, (int)leftTop.Width - 4, (int)leftTop.Height - 14), baseDl, wb);
                // RightTop: TreeView
                var panel2 = new Andy.Tui.Widgets.Panel(); panel2.SetTitle("Tree of Life"); panel2.Render(rightTop, baseDl, wb);
                tv.Render(new Andy.Tui.Layout.Rect((int)rightTop.X + 1, (int)rightTop.Y + 1, (int)rightTop.Width - 2, (int)rightTop.Height - 2), baseDl, wb);
                // LeftBottom: ScrollView + Progress/Slider
                var panel3 = new Andy.Tui.Widgets.Panel(); panel3.SetTitle("Scroll & Progress"); panel3.Render(leftBottom, baseDl, wb);
                scrollbar.Render(new Andy.Tui.Layout.Rect((int)leftBottom.X + 1, (int)leftBottom.Y + 1, (int)leftBottom.Width - 2, (int)leftBottom.Height - 4), baseDl, wb);
                new Andy.Tui.Widgets.ProgressBar { Value = progress }.Render(new Andy.Tui.Layout.Rect((int)leftBottom.X + 1, (int)leftBottom.Y + (int)leftBottom.Height - 3, (int)leftBottom.Width - 2, 1), baseDl, wb);
                new Andy.Tui.Widgets.Slider { Value = slider }.Render(new Andy.Tui.Layout.Rect((int)leftBottom.X + 1, (int)leftBottom.Y + (int)leftBottom.Height - 1, (int)leftBottom.Width - 2, 1), baseDl, wb);
                // RightBottom: Table
                var panel4 = new Andy.Tui.Widgets.Panel(); panel4.SetTitle("Stocks"); panel4.Render(rightBottom, baseDl, wb);
                table.Render(new Andy.Tui.Layout.Rect((int)rightBottom.X + 1, (int)rightBottom.Y + 1, (int)rightBottom.Width - 2, (int)rightBottom.Height - 2), baseDl, wb);

                // Focus ring: draw bright border around focused panel
                void DrawFocusBorder(Andy.Tui.Layout.Rect r)
                { wb.DrawBorder(new Border((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height, "single", new Rgb24(200, 200, 80))); }
                if (focus == 1 || focus == 2 || focus == 3 || focus == 4 || focus == 5 || focus == 6) DrawFocusBorder(leftTop);
                else if (focus == 9) DrawFocusBorder(rightTop);
                else if (focus == 7 || focus == 8) DrawFocusBorder(leftBottom);
                else if (focus == 10) DrawFocusBorder(rightBottom);

                var combined = Combine(baseDl, wb.Build());
                var overlay = new DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(combined, overlay);
                await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                await Task.Delay(16);
            }
        }
        finally
        {
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    static string? ReadMenuSelectionInteractive(string[] items)
    {
        int index = 0;
        while (true)
        {
            // Render selection cursor line
            var b = new DisplayListBuilder();
            int yStart = 3;
            int contentX = 2;
            int contentW = Math.Max(0, Console.WindowWidth - 4);
            // Clear content area
            b.PushClip(new ClipPush(0, 0, Console.WindowWidth, Console.WindowHeight));
            b.DrawRect(new Rect(0, 0, Console.WindowWidth, Console.WindowHeight, new Rgb24(0, 0, 0)));
            for (int i = 0; i < items.Length; i++)
            {
                bool sel = i == index;
                var fg = sel ? new Rgb24(0, 0, 0) : new Rgb24(220, 220, 220);
                var bg = sel ? new Rgb24(200, 200, 80) : new Rgb24(0, 0, 0);
                b.DrawRect(new Rect(contentX, yStart + i, contentW, 1, bg));
                b.DrawText(new TextRun(4, yStart + i, items[i], fg, null, sel ? CellAttrFlags.Bold : CellAttrFlags.None));
            }
            b.Pop();
            RenderAsync(b, (Console.WindowWidth, Console.WindowHeight), CapabilityDetector.DetectFromEnvironment(), showHud: true).GetAwaiter().GetResult();

            var k = Console.ReadKey(true);
            if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Q) return null;
            if (k.Key == ConsoleKey.DownArrow) index = (index + 1) % items.Length;
            else if (k.Key == ConsoleKey.UpArrow) index = (index - 1 + items.Length) % items.Length;
            else if (k.Key == ConsoleKey.PageDown) index = Math.Min(items.Length - 1, index + Math.Max(1, Console.WindowHeight - 6));
            else if (k.Key == ConsoleKey.PageUp) index = Math.Max(0, index - Math.Max(1, Console.WindowHeight - 6));
            else if (k.Key == ConsoleKey.Home) index = 0;
            else if (k.Key == ConsoleKey.End) index = items.Length - 1;
            else if (k.Key == ConsoleKey.Enter || k.Key == ConsoleKey.Spacebar)
            {
                // Map index to menu number (1-based string)
                return (index + 1).ToString();
            }
        }
    }

    static async Task RunMenuBarRender((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
        b.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
        var mb = new Andy.Tui.Widgets.MenuBar()
            .Add("File", new Andy.Tui.Widgets.Menu())
            .Add("Edit", new Andy.Tui.Widgets.Menu())
            .Add("View", new Andy.Tui.Widgets.Menu());
        var baseDl = b.Build();
        var wb = new DisplayListBuilder();
        mb.Render(new Andy.Tui.Layout.Rect(0, 0, viewport.Width, 1), baseDl, wb);
        var combined = Combine(baseDl, wb.Build());
        await RenderAndWaitForEsc(new PrebuiltDisplayListBuilder(combined), viewport, caps);
    }

    static async Task RunMenuBarInteractive((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        await MenuBarInteractiveDemo.Run(viewport, caps);
    }

    static async Task RunTreeViewRender((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
        b.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
        var baseDl = b.Build();
        var wb = new DisplayListBuilder();
        var tv = new Andy.Tui.Widgets.TreeView();
        // Simple “tree of life” sample
        var mammals = new RenderNode("mammals", "Mammals", false, new[]{
            new RenderNode("human","Homo sapiens", true),
            new RenderNode("wolf","Canis lupus", true),
            new RenderNode("whale","Balaenoptera musculus", true)
        });
        var birds = new RenderNode("birds", "Birds", false, new[]{
            new RenderNode("eagle","Aquila chrysaetos", true),
            new RenderNode("sparrow","Passer domesticus", true)
        });
        var reptiles = new RenderNode("reptiles", "Reptiles", false, new[]{
            new RenderNode("cobra","Naja naja", true),
            new RenderNode("tortoise","Testudo graeca", true)
        });
        tv.SetRoots(new[] { new RenderNode("life", "Tree of Life", false, new[] { mammals, birds, reptiles }) });
        tv.Expand("life"); tv.Expand("mammals"); tv.Select("human");
        tv.Render(new Andy.Tui.Layout.Rect(2, 2, viewport.Width - 4, viewport.Height - 4), baseDl, wb);
        var combined = Combine(baseDl, wb.Build());
        await RenderAndWaitForEsc(new PrebuiltDisplayListBuilder(combined), viewport, caps);
    }

    static async Task RunTreeViewInteractive((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        await TreeViewInteractiveDemo.Run(viewport, caps);
    }

    static async Task RunTableRender((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var b = new DisplayListBuilder();
        b.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
        b.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
        var baseDl = b.Build();
        var wb = new DisplayListBuilder();
        var t = new Andy.Tui.Widgets.Table();
        t.SetColumns(new[] { "Ticker", "Price", "Change" });
        t.SetRows(new[]{
            new[]{"AAPL","196.48","+0.84%"},
            new[]{"MSFT","423.12","-0.31%"},
            new[]{"GOOGL","174.77","+1.12%"},
            new[]{"AMZN","182.14","+0.45%"}
        });
        t.Render(new Andy.Tui.Layout.Rect(2, 2, viewport.Width - 4, viewport.Height - 4), baseDl, wb);
        var combined = Combine(baseDl, wb.Build());
        await RenderAndWaitForEsc(new PrebuiltDisplayListBuilder(combined), viewport, caps);
    }

    static async Task RunTableRealtime((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        await TableRealtimeDemo.Run(viewport, caps);
    }

    // Minimal render node for TreeView example
    sealed class RenderNode : Andy.Tui.Widgets.ITreeNode
    {
        public string Id { get; }
        public string Label { get; }
        public bool IsLeaf { get; }
        public IEnumerable<Andy.Tui.Widgets.ITreeNode> Children { get; }
        public RenderNode(string id, string label, bool leaf = false, IEnumerable<RenderNode>? children = null)
        { Id = id; Label = label; IsLeaf = leaf; Children = children ?? Enumerable.Empty<RenderNode>(); }
    }

    // moved to Demos/TextInputInteractiveDemo.cs
    static async Task RunTextInputInteractive((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new Andy.Tui.Examples.StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            bool running = true;
            string input = string.Empty;
            while (running)
            {
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    else if (k.Key == ConsoleKey.Backspace) { if (input.Length > 0) input = input[..^1]; }
                    else if (!char.IsControl(k.KeyChar)) input += k.KeyChar;
                }
                var b = new DisplayListBuilder();
                b.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
                b.DrawText(new TextRun(2, 1, "TextInput — type; Backspace; ESC/Q back; F2 HUD", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
                var baseDl = b.Build();
                var wb = new DisplayListBuilder();
                var ti = new Andy.Tui.Widgets.TextInput();
                ti.SetFocused(true);
                ti.SetText(input);
                ti.Render(new Andy.Tui.Layout.Rect(2, 3, Math.Max(20, viewport.Width - 4), 1), baseDl, wb);
                var combined = Combine(baseDl, wb.Build());
                var overlay = new DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(combined, overlay);
                await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                await Task.Delay(16);
            }
        }
        finally
        {
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    // moved to Demos/ListBoxInteractiveDemo.cs
    static async Task RunListBoxInteractive((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new Andy.Tui.Examples.StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            bool running = true;
            int selected = 0;
            var items = Enumerable.Range(1, 20).Select(i => $"Item {i:D2}").ToArray();
            while (running)
            {
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    if (k.Key == ConsoleKey.UpArrow) selected = Math.Max(0, selected - 1);
                    if (k.Key == ConsoleKey.DownArrow) selected = Math.Min(items.Length - 1, selected + 1);
                }
                var b = new DisplayListBuilder();
                b.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
                b.DrawText(new TextRun(2, 1, "ListBox — Up/Down; ESC/Q back; F2 HUD", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
                var baseDl = b.Build();
                var wb = new DisplayListBuilder();
                var lb = new Andy.Tui.Widgets.ListBox();
                lb.SetItems(items);
                lb.SetSelectedIndex(selected);
                lb.Render(new Andy.Tui.Layout.Rect(2, 3, 20, Math.Max(5, viewport.Height - 6)), baseDl, wb);
                var combined = Combine(baseDl, wb.Build());
                var overlay = new DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(combined, overlay);
                await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                await Task.Delay(16);
            }
        }
        finally
        {
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    static string? ReadMenuSelection()
    {
        var sb = new System.Text.StringBuilder();
        Console.Write("Select: ");
        while (true)
        {
            var k = Console.ReadKey(true);
            if (k.Key == ConsoleKey.Escape || k.Key == ConsoleKey.Q) return null;
            if (k.Key == ConsoleKey.Enter)
            {
                var s = sb.ToString().Trim();
                Console.WriteLine();
                return s.Length == 0 ? null : s;
            }
            if (k.Key == ConsoleKey.Backspace)
            {
                if (sb.Length > 0) sb.Length -= 1;
                Console.Write("\rSelect: " + sb.ToString() + "    ");
                continue;
            }
            if (!char.IsControl(k.KeyChar) && char.IsDigit(k.KeyChar))
            {
                sb.Append(k.KeyChar);
                Console.Write("\rSelect: " + sb.ToString());
            }
        }
    }

    // moved to Demos/CheckboxInteractiveDemo.cs
    static async Task RunCheckboxInteractive((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new Andy.Tui.Examples.StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            bool running = true;
            bool state = true;
            while (running)
            {
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) running = false;
                    if (k.Key == ConsoleKey.H) hud.Enabled = !hud.Enabled;
                    if (k.Key == ConsoleKey.Spacebar || k.Key == ConsoleKey.Enter) state = !state;
                }
                var b = new DisplayListBuilder();
                b.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
                b.DrawText(new TextRun(2, 1, "Checkbox — Space/Enter toggle; ESC/Q back; h HUD", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
                var baseDl = b.Build();
                var wb = new DisplayListBuilder();
                var cb = new Andy.Tui.Widgets.Checkbox("Receive updates", state);
                cb.Render(new Andy.Tui.Layout.Rect(2, 3, viewport.Width - 4, 1), baseDl, wb);
                var combined = Combine(baseDl, wb.Build());
                var overlay = new DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(combined, overlay);
                await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                await Task.Delay(16);
            }
        }
        finally
        {
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    // moved to Demos/RadioGroupInteractiveDemo.cs
    static async Task RunRadioGroupInteractive((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new Andy.Tui.Examples.StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            bool running = true;
            int selected = 0;
            string[] options = new[] { "Red", "Green", "Blue", "Yellow" };
            while (running)
            {
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) running = false;
                    if (k.Key == ConsoleKey.H) hud.Enabled = !hud.Enabled;
                    if (k.Key == ConsoleKey.UpArrow) selected = Math.Max(0, selected - 1);
                    if (k.Key == ConsoleKey.DownArrow) selected = Math.Min(options.Length - 1, selected + 1);
                    if (k.Key == ConsoleKey.Spacebar || k.Key == ConsoleKey.Enter) { /* keep selected, no-op */ }
                }
                var b = new DisplayListBuilder();
                b.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
                b.DrawText(new TextRun(2, 1, "RadioGroup — Up/Down to select; ESC/Q back; h HUD", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
                var baseDl = b.Build();
                var wb = new DisplayListBuilder();
                var rg = new Andy.Tui.Widgets.RadioGroup();
                rg.SetItems(options);
                rg.SetSelectedIndex(selected);
                rg.Render(new Andy.Tui.Layout.Rect(2, 3, 20, options.Length + 2), baseDl, wb);
                var combined = Combine(baseDl, wb.Build());
                var overlay = new DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(combined, overlay);
                await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                await Task.Delay(16);
            }
        }
        finally
        {
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    // moved to Demos/ChatInteractiveDemo.cs
    static async Task RunChatInteractive((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new Andy.Tui.Examples.StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            bool running = true;
            string input = string.Empty;
            // Chat state: display messages and LLM conversation
            var viewMessages = new System.Collections.Generic.List<Andy.Tui.Widgets.ChatMessage>();
            var conversation = new System.Collections.Generic.List<Andy.Tui.Examples.Chat.CerebrasHttpChatClient.ChatMessage>();
            Andy.Tui.Examples.Chat.CerebrasHttpChatClient? client = null;
            string status = string.Empty;
            try
            {
                var cfg = Andy.Tui.Examples.Chat.ChatConfiguration.Load();
                if (!string.IsNullOrWhiteSpace(cfg.ApiKey))
                {
                    client = new Andy.Tui.Examples.Chat.CerebrasHttpChatClient(cfg);
                    status = $"Model: {client.Model}";
                }
                else
                {
                    status = "Set CEREBRAS_API_KEY to enable Cerebras";
                }
            }
            catch (Exception ex)
            {
                status = $"[error] {ex.Message}";
            }

            Task<string>? pendingReply = null;
            string? inflightUser = null;
            var chatView = new Andy.Tui.Widgets.ChatView();

            while (running)
            {
                int pendingLineDelta = 0; // + up, - down
                int pendingPageDelta = 0; // + page up, - page down
                bool goTop = false, goBottom = false;
                // Handle input
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    else if (k.Key == ConsoleKey.Enter)
                    {
                        var candidate = Andy.Tui.Widgets.ChatInputSanitizer.SanitizeForSend(input);
                        if (candidate.Length > 0 && pendingReply is null)
                        {
                            // Add user message and kick off LLM call or fallback
                            viewMessages.Add(new Andy.Tui.Widgets.ChatMessage("You", candidate, true));
                            conversation.Add(new Andy.Tui.Examples.Chat.CerebrasHttpChatClient.ChatMessage("user", candidate));
                            inflightUser = candidate;
                            input = string.Empty;
                            if (client is not null)
                            {
                                pendingReply = client.CreateCompletionAsync(conversation);
                            }
                            else
                            {
                                // Fallback: local echo (slight delay)
                                pendingReply = Task.Run(async () => { await Task.Delay(250); return inflightUser!.ToUpperInvariant(); });
                            }
                        }
                    }
                    else if (k.Key == ConsoleKey.Backspace)
                    {
                        if (input.Length > 0) input = input.Substring(0, input.Length - 1);
                    }
                    else if (!char.IsControl(k.KeyChar))
                    {
                        input += k.KeyChar;
                    }
                    // Scrolling keys for chat history
                    if (k.Key == ConsoleKey.UpArrow) { pendingLineDelta += 1; }
                    else if (k.Key == ConsoleKey.DownArrow) { pendingLineDelta -= 1; }
                    else if (k.Key == ConsoleKey.PageUp) { pendingPageDelta += 1; }
                    else if (k.Key == ConsoleKey.PageDown) { pendingPageDelta -= 1; }
                    else if (k.Key == ConsoleKey.Home) { goTop = true; }
                    else if (k.Key == ConsoleKey.End) { goBottom = true; }
                }
                // Process pending LLM reply
                if (pendingReply is not null && pendingReply.IsCompleted)
                {
                    try
                    {
                        var reply = pendingReply.Result;
                        if (!string.IsNullOrEmpty(reply))
                        {
                            viewMessages.Add(new Andy.Tui.Widgets.ChatMessage("Bot", reply, false));
                            conversation.Add(new Andy.Tui.Examples.Chat.CerebrasHttpChatClient.ChatMessage("assistant", reply));
                        }
                    }
                    catch (Exception ex)
                    {
                        viewMessages.Add(new Andy.Tui.Widgets.ChatMessage("Bot", $"[error] {ex.Message}", false));
                    }
                    finally
                    {
                        pendingReply = null;
                        inflightUser = null;
                    }
                }

                // Handle resize
                viewport = (Console.WindowWidth, Console.WindowHeight);

                // Layout
                int headerH = 2;
                int inputH = 3; // border + 1 line
                int chatX = 2;
                // Start chat directly below header
                int chatY = headerH + 1;
                int chatW = Math.Max(30, viewport.Width - 4);
                int chatH = Math.Max(5, viewport.Height - (chatY + inputH) - 2);

                // Build frame
                var baseB = new DisplayListBuilder();
                baseB.PushClip(new ClipPush(0, 0, viewport.Width, viewport.Height));
                baseB.DrawRect(new Rect(0, 0, viewport.Width, viewport.Height, new Rgb24(0, 0, 0)));
                baseB.DrawText(new TextRun(2, 1, "Chat — type and Enter to send; ESC/Q back; F2 HUD", new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
                var baseDl = baseB.Build();

                // Apply any pending scroll actions now that we know chat viewport size
                if (goTop)
                {
                    chatView.FollowTail(false);
                    chatView.AdjustScroll(int.MaxValue / 2, chatW, chatH);
                }
                else if (goBottom)
                {
                    chatView.FollowTail(true);
                }
                if (pendingLineDelta != 0)
                {
                    chatView.FollowTail(false);
                    chatView.AdjustScroll(pendingLineDelta, chatW, chatH);
                }
                if (pendingPageDelta != 0)
                {
                    int page = Math.Max(1, chatH - 2);
                    chatView.FollowTail(false);
                    chatView.AdjustScroll(pendingPageDelta * page, chatW, chatH);
                }

                // Chat content via ChatView
                var widgets = new DisplayListBuilder();
                chatView.SetMessages(viewMessages);
                chatView.Render(new Andy.Tui.Layout.Rect(chatX, chatY, chatW, chatH), baseDl, widgets);

                // Input box
                var ti = new Andy.Tui.Widgets.TextInput();
                ti.SetFocused(true);
                ti.SetText(input);
                ti.Render(new Andy.Tui.Layout.Rect(chatX, chatY + chatH + 1, chatW, 1), baseDl, widgets);

                // Header/status line
                var statusBuilder = new DisplayListBuilder();
                var statusText = string.IsNullOrEmpty(status) ? "" : $"  — {status}";
                statusBuilder.DrawText(new TextRun(2, 1, "Chat — type and Enter to send; ESC/Q back; F2 HUD" + statusText, new Rgb24(200, 200, 50), null, CellAttrFlags.Bold));
                var composed = Combine(Combine(baseDl, widgets.Build()), statusBuilder.Build());
                var overlay = new DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(composed, overlay);
                await scheduler.RenderOnceAsync(Combine(composed, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                await Task.Delay(16);
            }
        }
        finally
        {
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    static async Task RunHello((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        await HelloDemo.Run(viewport, caps);
    }

    static async Task RenderAndWaitForEsc(object builderLike, (int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var sched = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        sched.SetMetricsSink(hud);
        var pty = new Andy.Tui.Examples.StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            bool running = true;
            while (running)
            {
                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                }
                // Detect terminal resize each frame
                viewport = Andy.Tui.Examples.TerminalHelpers.PollResize(viewport, sched);
                DisplayList baseDl;
                if (builderLike is DisplayListBuilder b)
                {
                    baseDl = b.Build();
                }
                else if (builderLike is PrebuiltDisplayListBuilder pre)
                {
                    baseDl = pre.Build();
                }
                else baseDl = new DisplayListBuilder().Build();

                // draw footer instruction
                var footer = new DisplayListBuilder();
                var msg = "ESC/Q to return";
                footer.PushClip(new ClipPush(0, viewport.Height - 1, viewport.Width, 1));
                footer.DrawRect(new Rect(0, viewport.Height - 1, viewport.Width, 1, new Rgb24(15, 15, 15)));
                footer.DrawText(new TextRun(2, viewport.Height - 1, msg, new Rgb24(160, 160, 160), null, CellAttrFlags.None));
                footer.Pop();
                var combined = Combine(baseDl, footer.Build());
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                var overlay = new DisplayListBuilder();
                hud.Contribute(combined, overlay);
                await sched.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                await Task.Delay(16);
            }
        }
        finally
        {
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    static async Task RunButtonsInteractive((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        await ButtonsInteractiveDemo.Run(viewport, caps);
    }

    static async Task RunToggleInteractive((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        await ToggleInteractiveDemo.Run(viewport, caps);
    }

    static async Task RunProgressAnimated((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        await ProgressAnimatedDemo.Run(viewport, caps);
    }

    static async Task RunLogAutoAppend((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        await LogAutoAppendDemo.Run(viewport, caps);
    }

    static async Task RunScrollViewInteractive((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        await ScrollViewInteractiveDemo.Run(viewport, caps);
    }
}

// StdoutPty moved to Common/StdoutPty.cs
