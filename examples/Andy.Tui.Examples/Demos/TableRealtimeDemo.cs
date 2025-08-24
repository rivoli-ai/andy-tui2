using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using Andy.Tui.Examples;

namespace Andy.Tui.Examples.Demos;

public static class TableRealtimeDemo
{
    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l");
        try
        {
            bool running = true;
            var rng = new Random(42);
            var stocks = SeedUniverse();

            var table = new Andy.Tui.Widgets.Table();
            table.SetColumns(new[] { "Ticker", "Price", "Change", "Bid", "Ask", "Volume", "Sector" });
            table.SetMinColumnWidths(8, 8, 8, 8, 8, 14, 12);
            table.SetRightAlignedColumns(1, 2, 3, 4, 5);

            void RebuildRows()
            {
                var rows = new List<string[]>(stocks.Count);
                foreach (var s in stocks)
                {
                    double pct = (s.Price - s.PrevClose) / Math.Max(0.01, s.PrevClose) * 100.0;
                    string change = (pct >= 0 ? "+" : "") + pct.ToString("0.00") + "%";
                    double spread = Math.Max(0.01, s.Price * 0.0005);
                    string bid = (s.Price - spread).ToString("0.00");
                    string ask = (s.Price + spread).ToString("0.00");
                    string vol = s.Volume.ToString("N0");
                    rows.Add(new[] { s.Ticker, s.Price.ToString("0.00"), change, bid, ask, vol, s.Sector });
                }
                table.SetRows(rows);
            }

            RebuildRows();
            int sortCol = 0; bool sortAsc = true;

            double NextGaussianBps(double meanBps, double stddevBps)
            {
                double u1 = 1.0 - rng.NextDouble();
                double u2 = 1.0 - rng.NextDouble();
                double z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                return meanBps + stddevBps * z0;
            }

            while (running)
            {
                viewport = TerminalHelpers.PollResize(viewport, scheduler);

                while (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Escape) { running = false; break; }
                    if (k.Key == ConsoleKey.F2) hud.Enabled = !hud.Enabled;
                    if (k.Key == ConsoleKey.S) { sortAsc = !sortAsc; table.SortBy(2, asc: sortAsc); }
                    if (k.Key == ConsoleKey.T) { sortCol = (sortCol + 1) % 7; table.SortBy(sortCol, asc: sortAsc); }
                }

                for (int i = 0; i < 8; i++)
                {
                    int idx = rng.Next(stocks.Count);
                    var s = stocks[idx];
                    double currentPct = (s.Price - s.PrevClose) / Math.Max(0.01, s.PrevClose) * 100.0;
                    double noiseBps = NextGaussianBps(0.0, 6.0);
                    double reversionBps = -0.02 * currentPct * 100.0;
                    double newPct = currentPct + (noiseBps + reversionBps) / 100.0;
                    newPct = Math.Max(-10.0, Math.Min(10.0, newPct));
                    double newPrice = Math.Max(0.01, s.PrevClose * (1.0 + newPct / 100.0));
                    long newVol = s.Volume + rng.Next(1000, 100000);
                    stocks[idx] = (s.Ticker, s.Name, s.Sector, newPrice, s.PrevClose, newVol);
                }
                RebuildRows();
                if (sortCol != 0 || !sortAsc) table.SortBy(sortCol, asc: sortAsc);

                var b = new DL.DisplayListBuilder();
                b.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                b.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                b.DrawText(new DL.TextRun(2, 1, "Market — 30 FPS; S sort dir; T cycle column; F2 HUD; ESC back", new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                var baseDl = b.Build();
                var wb = new DL.DisplayListBuilder();
                var panel = new Andy.Tui.Widgets.Panel(); panel.SetTitle("Stocks — simulated real-time");
                var rect = new Andy.Tui.Layout.Rect(2, 3, viewport.Width - 4, viewport.Height - 4);
                panel.Render(rect, baseDl, wb);
                ApplySectorColors(table);
                table.Render(new Andy.Tui.Layout.Rect((int)rect.X + 1, (int)rect.Y + 1, (int)rect.Width - 2, (int)rect.Height - 2), baseDl, wb);

                var combined = Combine(baseDl, wb.Build());
                var overlay = new DL.DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(combined, overlay);
                await scheduler.RenderOnceAsync(Combine(combined, overlay.Build()), viewport, caps, pty, CancellationToken.None);
            }
        }
        finally
        {
            Console.Write("\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    private static void ApplySectorColors(Andy.Tui.Widgets.Table table)
    {
        var sectorColors = new Dictionary<string, (DL.Rgb24 NameColor, DL.Rgb24 SectorColor)>
        {
            ["Tech"] = (new DL.Rgb24(100, 200, 255), new DL.Rgb24(60, 120, 180)),
            ["Consumer"] = (new DL.Rgb24(255, 220, 120), new DL.Rgb24(180, 120, 60)),
            ["E-Commerce"] = (new DL.Rgb24(255, 200, 150), new DL.Rgb24(200, 120, 80)),
            ["FinTech"] = (new DL.Rgb24(200, 255, 180), new DL.Rgb24(120, 180, 100)),
            ["Banking"] = (new DL.Rgb24(200, 255, 200), new DL.Rgb24(100, 160, 100)),
            ["Health"] = (new DL.Rgb24(255, 170, 170), new DL.Rgb24(180, 80, 80)),
            ["Media"] = (new DL.Rgb24(220, 200, 255), new DL.Rgb24(140, 120, 200)),
            ["Aero"] = (new DL.Rgb24(200, 230, 255), new DL.Rgb24(120, 150, 200)),
            ["Industrial"] = (new DL.Rgb24(210, 210, 210), new DL.Rgb24(130, 130, 130)),
            ["Energy"] = (new DL.Rgb24(255, 220, 180), new DL.Rgb24(200, 150, 100)),
            ["Transport"] = (new DL.Rgb24(200, 255, 255), new DL.Rgb24(120, 180, 180)),
            ["Retail"] = (new DL.Rgb24(255, 240, 200), new DL.Rgb24(200, 170, 120)),
            ["Telecom"] = (new DL.Rgb24(200, 200, 255), new DL.Rgb24(120, 120, 200)),
        };
        table.SetCellColorProvider((colIndex, cell, row) =>
        {
            if (colIndex == 0 || colIndex == 6)
            {
                string sector = row[Math.Min(6, row.Count - 1)];
                if (sectorColors.TryGetValue(sector, out var colors))
                {
                    var fg = colIndex == 0 ? colors.NameColor : colors.SectorColor;
                    return (fg, null);
                }
            }
            return (null, null);
        });
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

    private static List<(string Ticker, string Name, string Sector, double Price, double PrevClose, long Volume)> SeedUniverse()
    {
        return new()
        {
            ("AAPL","Apple Inc.","Tech",196.48,195.00, 100_000_000),
            ("MSFT","Microsoft Corp.","Tech",423.12,420.00, 80_000_000),
            ("GOOGL","Alphabet Inc.","Tech",174.77,172.00, 60_000_000),
            ("AMZN","Amazon.com Inc.","Consumer",182.14,180.00, 70_000_000),
            ("NVDA","NVIDIA Corp.","Tech",130.15,129.00, 90_000_000),
            ("META","Meta Platforms", "Tech", 508.30, 505.00, 50_000_000),
            ("TSLA","Tesla Inc.","Auto",232.65,230.00, 120_000_000),
            ("NFLX","Netflix Inc.","Media",655.40,650.00, 15_000_000),
            ("ORCL","Oracle Corp.","Tech",142.10,141.00, 12_000_000),
            ("INTC","Intel Corp.","Tech",33.85,33.50, 40_000_000),
            ("AMD","Advanced Micro","Tech",162.70,161.00, 55_000_000),
            ("IBM","IBM Corp.","Tech",185.20,184.00, 6_000_000),
            ("SAP","SAP SE","Tech",190.40,189.00, 3_000_000),
            ("UBER","Uber Tech","Transport",70.15,69.50, 25_000_000),
            ("LYFT","Lyft Inc.","Transport",16.10,16.00, 10_000_000),
            ("SHOP","Shopify","E-Commerce",90.55,90.00, 8_000_000),
            ("SQ","Block Inc.","FinTech",78.30,78.00, 7_000_000),
            ("PYPL","PayPal","FinTech",63.40,63.00, 9_000_000),
            ("JPM","JPMorgan","Banking",207.60,207.00, 11_000_000),
            ("BAC","Bank of America","Banking",40.20,40.00, 18_000_000),
            ("GS","Goldman Sachs","Banking",502.50,501.00, 3_500_000),
            ("NVAX","Novavax","Health",16.80,16.50, 5_000_000),
            ("PFE","Pfizer","Health",28.40,28.20, 20_000_000),
            ("MRNA","Moderna","Health",125.10,124.00, 7_500_000),
            ("NKE","Nike","Consumer",90.20,90.00, 9_000_000),
            ("DIS","Disney","Media",92.10,92.00, 14_000_000),
            ("BA","Boeing","Aero",187.40,186.80, 8_500_000),
            ("GE","GE Aerospace","Aero",162.30,161.50, 6_700_000),
            ("CAT","Caterpillar","Industrial",341.10,340.00, 2_200_000),
            ("XOM","ExxonMobil","Energy",116.50,116.00, 13_000_000),
            ("CVX","Chevron","Energy",159.80,159.00, 9_000_000),
            ("BP","BP plc","Energy",36.40,36.20, 4_500_000),
            ("T","AT&T","Telecom",18.30,18.20, 22_000_000),
            ("VZ","Verizon","Telecom",40.60,40.50, 16_000_000),
            ("KO","Coca-Cola","Consumer",64.30,64.10, 12_000_000),
            ("PEP","PepsiCo","Consumer",179.50,179.00, 7_800_000),
            ("MCD","McDonald's","Consumer",260.20,260.00, 5_400_000),
            ("WMT","Walmart","Retail",70.90,70.80, 10_200_000),
            ("COST","Costco","Retail",854.60,853.00, 3_100_000),
        };
    }
}

// StdoutPty defined in Common/StdoutPty.cs
