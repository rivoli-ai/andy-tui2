using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets.Tests;

public class FileFindPrefsColorTests
{
    [Fact]
    public void FileDialog_Renders_List()
    {
        var fd = new Andy.Tui.Widgets.FileDialog();
        fd.SetDirectory(System.Environment.CurrentDirectory);
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        fd.Render(new L.Rect(0,0,40,10), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.TextRun>().Any());
    }

    [Fact]
    public void FindReplacePanel_Renders_Text()
    {
        var fr = new Andy.Tui.Widgets.FindReplacePanel();
        fr.SetVisible(true);
        fr.SetText("foo","bar");
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        fr.Render(new L.Rect(0,0,40,3), baseDl, b);
        var dl = b.Build();
        var text = string.Join("", dl.Ops.OfType<DL.TextRun>().Select(t => t.Content));
        Assert.Contains("Find:", text);
        Assert.Contains("Replace:", text);
    }

    [Fact]
    public void Preferences_Renders_Items()
    {
        var p = new Andy.Tui.Widgets.PreferencesPanel();
        p.SetItems(new[]{("A","1")});
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        p.Render(new L.Rect(0,0,20,5), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.TextRun>().Any());
    }

    [Fact]
    public void ColorChooser_Renders_Segments()
    {
        var cc = new Andy.Tui.Widgets.ColorChooser();
        var baseDl = new DL.DisplayListBuilder().Build();
        var b = new DL.DisplayListBuilder();
        cc.Render(new L.Rect(0,0,24,3), baseDl, b);
        var dl = b.Build();
        Assert.True(dl.Ops.OfType<DL.Rect>().Any());
    }
}
