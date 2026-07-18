using System.Text;
using Andy.Tui.Input;

namespace Andy.Tui.Input.Tests;

/// <summary>
/// Adversarial input harness: byte-partition invariance and fuzzing. Real terminals deliver input
/// in arbitrarily fragmented reads — a single keystroke or paste can straddle several OS read()
/// boundaries. These tests feed the decoder every partition of a stream and random hostile byte
/// streams, asserting deterministic, crash-free decoding. Failures report the exact partition so
/// the offending split is reproducible.
/// </summary>
public class InputAdversarialTests
{
    private static List<IInputEvent> DecodeChunks(IEnumerable<byte[]> chunks)
    {
        var dec = new TtyStreamDecoder();
        var acc = new List<IInputEvent>();
        foreach (var chunk in chunks)
            acc.AddRange(dec.Push(chunk));
        return acc;
    }

    private static IEnumerable<byte[]> PartitionAt(byte[] data, int[] cuts)
    {
        int start = 0;
        foreach (var cut in cuts)
        {
            yield return data[start..cut];
            start = cut;
        }
        yield return data[start..];
    }

    private static string Describe(int[] cuts) => "cuts=[" + string.Join(",", cuts) + "]";

    [Fact]
    public void Printable_Stream_Is_Invariant_To_Every_Single_Split()
    {
        var data = Encoding.ASCII.GetBytes("hello world 123");
        var whole = DecodeChunks(new[] { data });
        var expected = whole.OfType<KeyEvent>().Select(k => k.Key).ToList();
        Assert.Equal(data.Length, expected.Count);

        for (int cut = 1; cut < data.Length; cut++)
        {
            var parts = PartitionAt(data, new[] { cut }).ToList();
            var got = DecodeChunks(parts).OfType<KeyEvent>().Select(k => k.Key).ToList();
            Assert.True(expected.SequenceEqual(got),
                $"printable stream diverged under split. {Describe(new[] { cut })} expected [{string.Join("", expected)}] got [{string.Join("", got)}]");
        }
    }

    [Fact]
    public void Printable_Stream_Is_Invariant_To_Every_Two_Cut_Partition()
    {
        var data = Encoding.ASCII.GetBytes("abcdef");
        var expected = new string(data.Select(b => (char)b).ToArray());

        for (int a = 1; a < data.Length; a++)
        {
            for (int b = a + 1; b < data.Length; b++)
            {
                var parts = PartitionAt(data, new[] { a, b }).ToList();
                var keys = DecodeChunks(parts).OfType<KeyEvent>().Select(k => k.Key);
                Assert.True(expected == string.Join("", keys),
                    $"two-cut partition diverged. {Describe(new[] { a, b })}");
            }
        }
    }

    [Fact]
    public void Bracketed_Paste_Content_Fragmentation_Yields_One_Paste_Event()
    {
        // The start/end markers arrive intact; the PASTE CONTENT is split at every interior
        // boundary. The decoder's paste buffer must accumulate across Push() calls.
        const string start = "[200~";
        const string end = "[201~";
        const string content = "the quick brown fox";
        var startBytes = Encoding.UTF8.GetBytes(start);
        var endBytes = Encoding.UTF8.GetBytes(end);
        var contentBytes = Encoding.UTF8.GetBytes(content);

        for (int cut = 0; cut <= contentBytes.Length; cut++)
        {
            var chunks = new List<byte[]>
            {
                startBytes.Concat(contentBytes[..cut]).ToArray(),
                contentBytes[cut..].Concat(endBytes).ToArray(),
            };
            var events = DecodeChunks(chunks);
            var paste = Assert.IsType<PasteEvent>(Assert.Single(events));
            Assert.True(content == paste.Text,
                $"paste content corrupted at content cut {cut}: got '{paste.Text}'");
        }
    }

    [Fact]
    public void Fuzz_Random_Bytes_Never_Throw_And_Terminate()
    {
        // Deterministic seed so CI failures are reproducible.
        var rng = new Random(0xC0FFEE);
        for (int iteration = 0; iteration < 2000; iteration++)
        {
            int len = rng.Next(0, 48);
            var buf = new byte[len];
            rng.NextBytes(buf);

            // Feed as one chunk and as a random fragmentation; neither may throw.
            var dec1 = new TtyStreamDecoder();
            _ = dec1.Push(buf).ToList();

            var dec2 = new TtyStreamDecoder();
            int i = 0;
            while (i < buf.Length)
            {
                int take = rng.Next(1, Math.Max(2, buf.Length - i + 1));
                take = Math.Min(take, buf.Length - i);
                _ = dec2.Push(buf[i..(i + take)]).ToList();
                i += take;
            }
        }
    }

    [Fact]
    public void Fuzz_Printable_Ascii_Produces_One_Key_Per_Char()
    {
        var rng = new Random(1234);
        for (int iteration = 0; iteration < 500; iteration++)
        {
            int len = rng.Next(0, 40);
            var sb = new StringBuilder();
            for (int i = 0; i < len; i++) sb.Append((char)rng.Next(0x20, 0x7F)); // printable ASCII
            var text = sb.ToString();
            var events = new TtyStreamDecoder().Push(Encoding.ASCII.GetBytes(text)).ToList();
            var keys = string.Join("", events.OfType<KeyEvent>().Select(k => k.Key));
            Assert.True(text == keys, $"iteration {iteration}: '{text}' decoded to '{keys}'");
        }
    }

    [Fact]
    public void Hostile_Control_Bytes_Do_Not_Crash_Decoder()
    {
        // A grab-bag of partial escape sequences, lone ESCs, C1 bytes, and NULs.
        var payloads = new[]
        {
            "", "[", "[<", "[<0;", "[200~unterminated",
            "[999999999999999999;5A", "\0\0\0", "31m", "[;;;;;m",
        };
        foreach (var p in payloads)
        {
            var dec = new TtyStreamDecoder();
            var ex = Record.Exception(() => dec.Push(Encoding.UTF8.GetBytes(p)).ToList());
            Assert.True(ex is null, $"decoder threw on payload '{p.Replace("", "<ESC>")}' : {ex}");
        }
    }
}
