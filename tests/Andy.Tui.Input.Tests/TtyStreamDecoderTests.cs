using Andy.Tui.Input;

namespace Andy.Tui.Input.Tests;

public class TtyStreamDecoderTests
{
    [Fact]
    public void Decodes_Bracketed_Paste()
    {
        var dec = new TtyStreamDecoder();
        var seq = "\u001b[200~hello world\u001b[201~"u8.ToArray();
        var events = dec.Push(seq).ToList();
        var pe = Assert.IsType<PasteEvent>(events.Single());
        Assert.Equal("hello world", pe.Text);
    }

    [Fact]
    public void Decodes_Sgr_Mouse_Wheel()
    {
        var dec = new TtyStreamDecoder();
        var seq = "\u001b[<64;10;5M"u8.ToArray();
        var events = dec.Push(seq).ToList();
        var me = Assert.IsType<MouseEvent>(events.Single());
        Assert.Equal(MouseKind.Wheel, me.Kind);
        Assert.Equal(9, me.X); // 1-based → 0-based
        Assert.Equal(4, me.Y);
    }

    [Fact]
    public void Decodes_Sgr_Mouse_Move_With_Modifiers()
    {
        var dec = new TtyStreamDecoder();
        // 32 indicates motion, plus 8 Alt, 4 Shift => b=44; x=3; y=2; trailing 'M' indicates press/move frame
        var seq = "\u001b[<44;3;2M"u8.ToArray();
        var events = dec.Push(seq).ToList();
        var me = Assert.IsType<MouseEvent>(events.Single());
        Assert.Equal(MouseKind.Move, me.Kind);
        Assert.Equal(KeyModifiers.Shift | KeyModifiers.Alt, me.Modifiers);
        Assert.Equal(2, me.X);
        Assert.Equal(1, me.Y);
    }

    [Fact]
    public void Decodes_Arrow_With_Modifiers()
    {
        var dec = new TtyStreamDecoder();
        // CSI 1;5A is Ctrl+Up in xterm encoding (1 + ctrl=4)
        var seq = "\u001b[1;5A"u8.ToArray();
        var evs = dec.Push(seq).ToList();
        var ke = Assert.IsType<KeyEvent>(evs.Single());
        Assert.Equal("ArrowUp", ke.Key);
        Assert.Equal(KeyModifiers.Ctrl, ke.Modifiers);
    }

    [Fact]
    public void Decodes_Alt_Modified_Printable()
    {
        var dec = new TtyStreamDecoder();
        var seq = "\u001ba"u8.ToArray();
        var evs = dec.Push(seq).ToList();
        var ke = Assert.IsType<KeyEvent>(evs.Single());
        Assert.Equal("a", ke.Key);
        Assert.Equal(KeyModifiers.Alt, ke.Modifiers);
    }
}
