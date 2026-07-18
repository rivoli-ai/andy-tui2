using Andy.Tui.Input;

namespace Andy.Tui.Input.Tests;

public class TtyDecoderTests
{
    [Fact]
    public void Decodes_Arrow_Keys()
    {
        var events = TtyDecoder.Decode("\u001b[A"u8.ToArray()).ToList();
        Assert.Contains(events, e => e is KeyEvent ke && ke.Key == "ArrowUp");
    }

    [Fact]
    public void Decodes_Printable_Chars()
    {
        var events = TtyDecoder.Decode("hi"u8.ToArray()).OfType<KeyEvent>().ToList();
        Assert.Equal(["h", "i"], events.Select(e => e.Key).ToArray());
    }

    [Fact]
    public void Decodes_Tab()
    {
        var events = TtyDecoder.Decode("\t"u8.ToArray()).OfType<KeyEvent>().ToList();
        var ke = Assert.Single(events);
        Assert.Equal("Tab", ke.Key);
        Assert.Equal(KeyModifiers.None, ke.Modifiers);
    }

    [Fact]
    public void Decodes_Shift_Tab()
    {
        // Back-tab: ESC [ Z
        var events = TtyDecoder.Decode("[Z"u8.ToArray()).OfType<KeyEvent>().ToList();
        var ke = Assert.Single(events);
        Assert.Equal("Tab", ke.Key);
        Assert.Equal(KeyModifiers.Shift, ke.Modifiers);
    }

    [Fact]
    public void Decodes_Resize_Sequence()
    {
        // ESC [ 8 ; rows ; cols t
        var seq = "\u001b[8;24;80t"u8.ToArray();
        var evs = TtyDecoder.Decode(seq).ToList();
        var re = Assert.IsType<ResizeEvent>(evs.Single());
        Assert.Equal(80, re.Cols);
        Assert.Equal(24, re.Rows);
    }
}
