using Andy.Tui.Widgets;
using Xunit;

namespace Andy.Tui.Widgets.Tests;

/// <summary>
/// Pins ModalDialog's open/closed state to the unambiguous <c>IsOpen()</c> method,
/// which is deliberately independent of the inherited <see cref="IWidget.IsVisible"/>
/// widget-visibility property (they no longer shadow after the #78 review nit fix).
/// </summary>
public class ModalDialogStateTests
{
    [Fact]
    public void New_Dialog_Is_Closed_But_Widget_Visible()
    {
        var d = new ModalDialog();
        Assert.False(d.IsOpen());
        // Base widget visibility is a separate concept and defaults to true.
        Assert.True(((IWidget)d).IsVisible);
    }

    [Fact]
    public void ShowConfirm_Opens_And_Hide_Closes()
    {
        var d = new ModalDialog();
        d.ShowConfirm("t", "m");
        Assert.True(d.IsOpen());
        d.Hide();
        Assert.False(d.IsOpen());
    }

    [Fact]
    public void ShowPrompt_Opens_The_Dialog()
    {
        var d = new ModalDialog();
        d.ShowPrompt("t", "m", "x");
        Assert.True(d.IsOpen());
    }

    [Fact]
    public void Open_State_Is_Independent_Of_Widget_Visibility()
    {
        var d = new ModalDialog();
        d.ShowConfirm("t", "m");
        // Hiding the widget does not close the dialog, and vice versa.
        ((IWidget)d).SetVisible(false);
        Assert.True(d.IsOpen());
        Assert.False(((IWidget)d).IsVisible);
    }
}
