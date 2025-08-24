using System;
using System.Threading;
using System.Threading.Tasks;
using Andy.Tui.Backend.Terminal;
using DL = Andy.Tui.DisplayList;
using Andy.Tui.Examples;

namespace Andy.Tui.Examples.Demos;

public static class ChatInteractiveDemo
{
    public static async Task Run((int Width, int Height) viewport, TerminalCapabilities caps)
    {
        var scheduler = new Andy.Tui.Core.FrameScheduler();
        var hud = new Andy.Tui.Observability.HudOverlay { Enabled = true };
        scheduler.SetMetricsSink(hud);
        var pty = new StdoutPty();
        Console.Write("\u001b[?1049h\u001b[?25l\u001b[?7l\u001b[?1000h\u001b[?1006h");
        try
        {
            bool running = true;
            string input = string.Empty;
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

                viewport = TerminalHelpers.PollResize(viewport, scheduler);
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
                    if (k.Key == ConsoleKey.UpArrow) { pendingLineDelta += 1; }
                    else if (k.Key == ConsoleKey.DownArrow) { pendingLineDelta -= 1; }
                    else if (k.Key == ConsoleKey.PageUp) { pendingPageDelta += 1; }
                    else if (k.Key == ConsoleKey.PageDown) { pendingPageDelta -= 1; }
                    else if (k.Key == ConsoleKey.Home) { goTop = true; }
                    else if (k.Key == ConsoleKey.End) { goBottom = true; }
                }

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

                int headerH = 2;
                int inputH = 3;
                int chatX = 2;
                int chatY = headerH + 1;
                int chatW = Math.Max(30, viewport.Width - 4);
                int chatH = Math.Max(5, viewport.Height - (chatY + inputH) - 2);

                var baseB = new DL.DisplayListBuilder();
                baseB.PushClip(new DL.ClipPush(0, 0, viewport.Width, viewport.Height));
                baseB.DrawRect(new DL.Rect(0, 0, viewport.Width, viewport.Height, new DL.Rgb24(0, 0, 0)));
                baseB.DrawText(new DL.TextRun(2, 1, "Chat — type and Enter to send; ESC/Q back; F2 HUD" + (string.IsNullOrEmpty(status) ? "" : "  — " + status), new DL.Rgb24(200, 200, 50), null, DL.CellAttrFlags.Bold));
                var baseDl = baseB.Build();

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

                var widgets = new DL.DisplayListBuilder();
                chatView.SetMessages(viewMessages);
                chatView.Render(new Andy.Tui.Layout.Rect(chatX, chatY, chatW, chatH), baseDl, widgets);

                var ti = new Andy.Tui.Widgets.TextInput();
                ti.SetFocused(true);
                ti.SetText(input);
                ti.Render(new Andy.Tui.Layout.Rect(chatX, chatY + chatH + 1, chatW, 1), baseDl, widgets);

                var composed = Combine(baseDl, widgets.Build());
                var overlay = new DL.DisplayListBuilder();
                hud.ViewportCols = viewport.Width; hud.ViewportRows = viewport.Height;
                hud.Contribute(composed, overlay);
                await scheduler.RenderOnceAsync(Combine(composed, overlay.Build()), viewport, caps, pty, CancellationToken.None);
                await Task.Delay(16);
            }
        }
        finally
        {
            Console.Write("\u001b[?1006l\u001b[?1000l\u001b[?7h\u001b[?25h\u001b[?1049l");
        }
    }

    private static DL.DisplayList Combine(DL.DisplayList a, DL.DisplayList b)
    {
        var builder = new DL.DisplayListBuilder();
        Append(a); Append(b);
        return builder.Build();
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
    }
}
