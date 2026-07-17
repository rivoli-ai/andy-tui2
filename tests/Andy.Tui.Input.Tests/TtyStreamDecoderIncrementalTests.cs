using System.Text;
using Andy.Tui.Input;

namespace Andy.Tui.Input.Tests;

public class TtyStreamDecoderIncrementalTests
{
    // Runs the decoder over a byte stream partitioned into the given chunk sizes and
    // returns every event, including anything reported by a final Flush().
    private static List<IInputEvent> Run(byte[] stream, IEnumerable<int> chunkSizes)
    {
        var dec = new TtyStreamDecoder();
        var all = new List<IInputEvent>();
        int i = 0;
        foreach (var size in chunkSizes)
        {
            int n = Math.Min(size, stream.Length - i);
            if (n <= 0) break;
            all.AddRange(dec.Push(stream[i..(i + n)]));
            i += n;
        }
        if (i < stream.Length) all.AddRange(dec.Push(stream[i..]));
        all.AddRange(dec.Flush());
        return all;
    }

    private static List<IInputEvent> RunWhole(byte[] stream)
        => Run(stream, new[] { stream.Length == 0 ? 0 : stream.Length });

    private static List<IInputEvent> RunPerByte(byte[] stream)
        => Run(stream, Enumerable.Repeat(1, Math.Max(1, stream.Length)));

    private static byte[] Bytes(string s) => Encoding.UTF8.GetBytes(s);

    [Fact]
    public void Partition_Independence_For_Composite_Stream()
    {
        // A mix of every supported category in one stream.
        var stream = Concat(
            Bytes("hi"),                         // printable ASCII
            Bytes("é"),                     // 2-byte UTF-8 scalar (é)
            Bytes("\U0001F600"),                 // 4-byte UTF-8 scalar (emoji)
            Bytes("[1;5A"),                // Ctrl+ArrowUp
            Bytes("[<64;10;5M"),           // SGR mouse wheel
            Bytes("a"),                    // Alt+a
            Bytes("[200~pasted[201~"), // bracketed paste
            Bytes("[8;24;80t"),            // resize
            Bytes("[15~"),                 // F5
            Bytes("[3~"));                 // Delete

        var whole = RunWhole(stream);
        var perByte = RunPerByte(stream);
        Assert.Equal(whole, perByte);

        // A handful of arbitrary partitions must all agree with the whole decode.
        var rng = new Random(1234);
        for (int t = 0; t < 25; t++)
        {
            var sizes = new List<int>();
            int remaining = stream.Length;
            while (remaining > 0)
            {
                int s = rng.Next(1, 7);
                sizes.Add(s);
                remaining -= s;
            }
            Assert.Equal(whole, Run(stream, sizes));
        }

        // Sanity: the composite decodes into the expected events.
        Assert.Collection(whole,
            e => Assert.Equal(new KeyEvent("h", "h", KeyModifiers.None), e),
            e => Assert.Equal(new KeyEvent("i", "i", KeyModifiers.None), e),
            e => Assert.Equal(new KeyEvent("é", "é", KeyModifiers.None), e),
            e => Assert.Equal(new KeyEvent("\U0001F600", "\U0001F600", KeyModifiers.None), e),
            e => Assert.Equal(new KeyEvent("ArrowUp", "ArrowUp", KeyModifiers.Ctrl), e),
            e => Assert.Equal(MouseKind.Wheel, Assert.IsType<MouseEvent>(e).Kind),
            e => Assert.Equal(new KeyEvent("a", "a", KeyModifiers.Alt), e),
            e => Assert.Equal("pasted", Assert.IsType<PasteEvent>(e).Text),
            e => Assert.Equal(new ResizeEvent(80, 24), e),
            e => Assert.Equal(new KeyEvent("F5", "F5", KeyModifiers.None), e),
            e => Assert.Equal(new KeyEvent("Delete", "Delete", KeyModifiers.None), e));
    }

    [Fact]
    public void Split_Utf8_Scalar_Is_Not_Corrupted()
    {
        var stream = Bytes("é"); // C3 A9
        Assert.Equal(2, stream.Length);

        // Split between the lead and continuation byte.
        var dec = new TtyStreamDecoder();
        var first = dec.Push(stream[..1]).ToList();
        Assert.Empty(first); // nothing yet: waiting for the continuation byte
        var second = dec.Push(stream[1..]).ToList();
        var ke = Assert.IsType<KeyEvent>(Assert.Single(second));
        Assert.Equal("é", ke.Key);
    }

    [Fact]
    public void Invalid_Utf8_Recovers_To_Replacement_Char()
    {
        // 0xFF is never a valid UTF-8 byte; the following ASCII must still decode.
        var stream = new byte[] { 0xFF, (byte)'A' };
        var events = RunWhole(stream);
        Assert.Collection(events,
            e => Assert.Equal("�", Assert.IsType<KeyEvent>(e).Key),
            e => Assert.Equal("A", Assert.IsType<KeyEvent>(e).Key));

        // Same result regardless of partition.
        Assert.Equal(events, RunPerByte(stream));
    }

    [Fact]
    public void Overlong_Encoding_Is_Rejected()
    {
        // C0 80 is an overlong encoding of NUL; both bytes must be rejected.
        var events = RunWhole(new byte[] { 0xC0, 0x80, (byte)'x' });
        Assert.Collection(events,
            e => Assert.Equal("�", Assert.IsType<KeyEvent>(e).Key),
            e => Assert.Equal("�", Assert.IsType<KeyEvent>(e).Key),
            e => Assert.Equal("x", Assert.IsType<KeyEvent>(e).Key));
    }

    [Fact]
    public void Oversized_Escape_Sequence_Is_Dropped_And_Recovers()
    {
        var opts = new TtyStreamDecoderOptions { MaxEscapeLength = 16 };
        var dec = new TtyStreamDecoder(opts);
        var junk = new string('1', 200);
        // A CSI that never terminates within the limit, followed by a normal key.
        var stream = Bytes("[" + junk + "Z");
        var events = dec.Push(stream).ToList();
        events.AddRange(dec.Flush());
        // The malformed sequence produced no event; recovery leaves the decoder usable.
        Assert.DoesNotContain(events, e => e is PasteEvent);
        var next = dec.Push(Bytes("q")).ToList();
        Assert.Equal("q", Assert.IsType<KeyEvent>(Assert.Single(next)).Key);
    }

    [Fact]
    public void Unknown_Csi_Sequence_Is_Ignored_But_Stream_Continues()
    {
        // ESC [ 9 9 z : unknown final byte 'z'.
        var stream = Bytes("[99zok");
        var events = RunWhole(stream);
        Assert.Collection(events,
            e => Assert.Equal("o", Assert.IsType<KeyEvent>(e).Key),
            e => Assert.Equal("k", Assert.IsType<KeyEvent>(e).Key));
        Assert.Equal(events, RunPerByte(stream));
    }

    [Fact]
    public void Bracketed_Paste_Across_One_Byte_Chunks()
    {
        var stream = Bytes("[200~line1\nline2[201~");
        var perByte = RunPerByte(stream);
        var pe = Assert.IsType<PasteEvent>(Assert.Single(perByte));
        Assert.Equal("line1\nline2", pe.Text);
        Assert.Equal(RunWhole(stream), perByte);
    }

    [Fact]
    public void Unterminated_Paste_Is_Flushed_At_End_Of_Stream()
    {
        var dec = new TtyStreamDecoder();
        Assert.Empty(dec.Push(Bytes("[200~half")).ToList());
        var flushed = dec.Flush().ToList();
        var pe = Assert.IsType<PasteEvent>(Assert.Single(flushed));
        Assert.Equal("half", pe.Text);
    }

    [Fact]
    public void Incomplete_Scalar_At_End_Of_Stream_Yields_Replacement()
    {
        var dec = new TtyStreamDecoder();
        Assert.Empty(dec.Push(new byte[] { 0xC3 }).ToList()); // lead byte, no continuation
        var flushed = dec.Flush().ToList();
        Assert.Equal("�", Assert.IsType<KeyEvent>(Assert.Single(flushed)).Key);
    }

    [Fact]
    public void Focus_Events_Are_Decoded()
    {
        var dec = new TtyStreamDecoder();
        var evs = dec.Push(Bytes("[I[O")).ToList();
        Assert.Collection(evs,
            e => Assert.True(Assert.IsType<FocusEvent>(e).HasFocus),
            e => Assert.False(Assert.IsType<FocusEvent>(e).HasFocus));
    }

    [Fact]
    public void Navigation_And_Function_Keys()
    {
        var dec = new TtyStreamDecoder();
        var evs = dec.Push(Bytes("[H[F[5~[6~OP")).ToList();
        Assert.Collection(evs,
            e => Assert.Equal("Home", Assert.IsType<KeyEvent>(e).Key),
            e => Assert.Equal("End", Assert.IsType<KeyEvent>(e).Key),
            e => Assert.Equal("PageUp", Assert.IsType<KeyEvent>(e).Key),
            e => Assert.Equal("PageDown", Assert.IsType<KeyEvent>(e).Key),
            e => Assert.Equal("F1", Assert.IsType<KeyEvent>(e).Key));
    }

    [Fact]
    public void Ctrl_Keys_Are_Decoded()
    {
        var dec = new TtyStreamDecoder();
        // Ctrl+A (0x01), Enter (0x0d), Tab (0x09), Backspace (0x7f)
        var evs = dec.Push(new byte[] { 0x01, 0x0d, 0x09, 0x7f }).ToList();
        Assert.Collection(evs,
            e => Assert.Equal(new KeyEvent("A", "A", KeyModifiers.Ctrl), e),
            e => Assert.Equal("Enter", Assert.IsType<KeyEvent>(e).Key),
            e => Assert.Equal("Tab", Assert.IsType<KeyEvent>(e).Key),
            e => Assert.Equal("Backspace", Assert.IsType<KeyEvent>(e).Key));
    }

    [Fact]
    public void Combined_Events_In_A_Single_Push()
    {
        var dec = new TtyStreamDecoder();
        var evs = dec.Push(Bytes("a[Bb")).ToList();
        Assert.Collection(evs,
            e => Assert.Equal("a", Assert.IsType<KeyEvent>(e).Key),
            e => Assert.Equal("ArrowDown", Assert.IsType<KeyEvent>(e).Key),
            e => Assert.Equal("b", Assert.IsType<KeyEvent>(e).Key));
    }

    [Fact]
    public void Fuzz_Random_Bytes_Do_Not_Throw_Or_Hang()
    {
        var rng = new Random(20240717);
        for (int iter = 0; iter < 400; iter++)
        {
            var dec = new TtyStreamDecoder();
            int totalLen = rng.Next(0, 512);
            var stream = new byte[totalLen];
            rng.NextBytes(stream);
            // Bias some bytes toward ESC to exercise sequence parsing more often.
            for (int k = 0; k < stream.Length; k++)
                if (rng.Next(8) == 0) stream[k] = 0x1b;

            int i = 0;
            while (i < stream.Length)
            {
                int n = Math.Min(rng.Next(1, 9), stream.Length - i);
                // Must never throw.
                _ = dec.Push(stream[i..(i + n)]).ToList();
                i += n;
            }
            _ = dec.Flush().ToList();
        }
    }

    [Fact]
    public void Fuzz_Partition_Independence_On_Structured_Streams()
    {
        var fragments = new[]
        {
            "abc", "é", "\U0001F642", "[A", "[1;5D", "[<0;3;4M",
            "[<0;3;4m", "x", "[200~xy[201~", "[3~",
            "[8;10;20t", "OB", "[I", "\t", "\r", "",
        };
        var rng = new Random(99);
        for (int iter = 0; iter < 200; iter++)
        {
            var sb = new List<byte>();
            int count = rng.Next(1, 8);
            for (int c = 0; c < count; c++)
                sb.AddRange(Bytes(fragments[rng.Next(fragments.Length)]));
            var stream = sb.ToArray();

            var whole = RunWhole(stream);
            Assert.Equal(whole, RunPerByte(stream));

            var sizes = new List<int>();
            int rem = stream.Length;
            while (rem > 0) { int s = rng.Next(1, 5); sizes.Add(s); rem -= s; }
            Assert.Equal(whole, Run(stream, sizes));
        }
    }

    private static byte[] Concat(params byte[][] arrays)
    {
        int total = arrays.Sum(a => a.Length);
        var r = new byte[total];
        int o = 0;
        foreach (var a in arrays) { Array.Copy(a, 0, r, o, a.Length); o += a.Length; }
        return r;
    }
}
