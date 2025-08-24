using System;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets
{
    public enum ModalResult { None, Confirm, Cancel }

    public sealed class ModalDialog
    {
        private string _title = string.Empty;
        private string _message = string.Empty;
        private bool _showInput;
        private string _inputText = string.Empty;
        private bool _visible;
        private int _focusedIndex; // 0: input (if any), 1: OK, 2: Cancel
        private ModalResult _result = ModalResult.None;

        public void ShowConfirm(string title, string message)
        {
            _title = title ?? string.Empty;
            _message = message ?? string.Empty;
            _showInput = false; _inputText = string.Empty; _visible = true; _focusedIndex = 1; _result = ModalResult.None;
        }

        public void ShowPrompt(string title, string message, string defaultText = "")
        {
            _title = title ?? string.Empty;
            _message = message ?? string.Empty;
            _showInput = true; _inputText = defaultText ?? string.Empty; _visible = true; _focusedIndex = 0; _result = ModalResult.None;
        }

        public void Hide() => _visible = false;
        public bool IsVisible() => _visible;
        public string GetInputText() => _inputText;
        public ModalResult GetResult() => _result;

        public void MoveFocusNext()
        {
            if (!_visible) return;
            if (_showInput)
                _focusedIndex = (_focusedIndex + 1) % 3;
            else
                _focusedIndex = 1 + ((_focusedIndex - 1 + 1) % 2);
        }

        public void MoveFocusPrev()
        {
            if (!_visible) return;
            if (_showInput)
                _focusedIndex = (_focusedIndex - 1 + 3) % 3;
            else
                _focusedIndex = 1 + ((_focusedIndex - 1 - 1 + 2) % 2);
        }

        public void TypeChar(char ch)
        {
            if (!_visible || !_showInput) return;
            if (!char.IsControl(ch)) _inputText += ch;
        }

        public void Backspace()
        {
            if (!_visible || !_showInput) return;
            if (_inputText.Length > 0) _inputText = _inputText[..^1];
        }

        public void Confirm()
        {
            if (!_visible) return;
            _result = ModalResult.Confirm; _visible = false;
        }

        public void Cancel()
        {
            if (!_visible) return;
            _result = ModalResult.Cancel; _visible = false;
        }

        public (int Width, int Height) Measure(int viewportW, int viewportH)
        {
            int boxW = Math.Min(viewportW - 4, Math.Max(24, Math.Max(_title.Length + 4, _message.Length + 4)));
            int boxH = 6 + (_showInput ? 2 : 0);
            return (boxW, boxH);
        }

        public void Render(in L.Rect viewport, DL.DisplayList baseDl, DL.DisplayListBuilder b)
        {
            if (!_visible) return;
            int vw = (int)viewport.Width; int vh = (int)viewport.Height;
            // Backdrop dim
            b.PushClip(new DL.ClipPush(0, 0, vw, vh));
            b.DrawRect(new DL.Rect(0, 0, vw, vh, new DL.Rgb24(0, 0, 0))); // simple solid dim
            // Dialog box
            var (bw, bh) = Measure(vw, vh);
            int bx = Math.Max(0, (vw - bw) / 2);
            int by = Math.Max(0, (vh - bh) / 2);
            b.DrawRect(new DL.Rect(bx, by, bw, bh, new DL.Rgb24(20, 20, 20)));
            b.DrawBorder(new DL.Border(bx, by, bw, bh, "single", new DL.Rgb24(180, 180, 180)));
            // Title
            b.DrawText(new DL.TextRun(bx + 2, by, _title, new DL.Rgb24(220, 220, 220), null, DL.CellAttrFlags.Bold));
            // Message
            b.DrawText(new DL.TextRun(bx + 2, by + 2, _message, new DL.Rgb24(210, 210, 210), null, DL.CellAttrFlags.None));
            int y = by + 3;
            if (_showInput)
            {
                string inputVis = _inputText.PadRight(Math.Max(10, bw - 4));
                var fg = _focusedIndex == 0 ? new DL.Rgb24(255, 255, 255) : new DL.Rgb24(200, 200, 200);
                var bg = _focusedIndex == 0 ? new DL.Rgb24(60, 60, 90) : new DL.Rgb24(30, 30, 30);
                b.DrawRect(new DL.Rect(bx + 2, y, bw - 4, 1, bg));
                b.DrawText(new DL.TextRun(bx + 2, y, inputVis.Substring(0, Math.Min(inputVis.Length, bw - 4)), fg, null, DL.CellAttrFlags.None));
                y += 2;
            }
            // Buttons
            string ok = " OK "; string cancel = " Cancel ";
            int btnY = by + bh - 2;
            int cancelX = bx + bw - 2 - cancel.Length;
            int okX = cancelX - 2 - ok.Length;
            // OK
            var okFg = _focusedIndex == 1 ? new DL.Rgb24(0, 0, 0) : new DL.Rgb24(220, 220, 220);
            var okBg = _focusedIndex == 1 ? new DL.Rgb24(140, 210, 140) : new DL.Rgb24(40, 60, 40);
            b.DrawRect(new DL.Rect(okX, btnY, ok.Length, 1, okBg));
            b.DrawText(new DL.TextRun(okX, btnY, ok, okFg, null, DL.CellAttrFlags.Bold));
            // Cancel
            var caFg = _focusedIndex == 2 ? new DL.Rgb24(0, 0, 0) : new DL.Rgb24(220, 220, 220);
            var caBg = _focusedIndex == 2 ? new DL.Rgb24(210, 140, 140) : new DL.Rgb24(60, 40, 40);
            b.DrawRect(new DL.Rect(cancelX, btnY, cancel.Length, 1, caBg));
            b.DrawText(new DL.TextRun(cancelX, btnY, cancel, caFg, null, DL.CellAttrFlags.Bold));
            b.Pop();
        }
    }
}
