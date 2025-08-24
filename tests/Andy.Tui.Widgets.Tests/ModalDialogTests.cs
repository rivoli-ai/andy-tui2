using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class ModalDialogTests
{
    [Fact]
    public void Confirm_Dialog_Renders_Title_Message_And_Buttons()
    {
        var dlg = new Andy.Tui.Widgets.ModalDialog();
        dlg.ShowConfirm("Title", "Message");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        dlg.Render(new L.Rect(0, 0, 80, 24), baseDl, b);
        var dl = b.Build();
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("Title"));
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("Message"));
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("OK"));
        Assert.Contains(dl.Ops.OfType<DL.TextRun>(), t => t.Content.Contains("Cancel"));
    }

    [Fact]
    public void Prompt_Dialog_Accepts_Input_And_Confirm_Returns_Result()
    {
        var dlg = new Andy.Tui.Widgets.ModalDialog();
        dlg.ShowPrompt("Ask", "Your name:", "Bob");
        dlg.TypeChar('b');
        dlg.Backspace();
        dlg.Confirm();
        Assert.Equal(Andy.Tui.Widgets.ModalResult.Confirm, dlg.GetResult());
        Assert.StartsWith("Bob", dlg.GetInputText());
    }
}
