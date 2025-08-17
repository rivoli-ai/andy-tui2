using BenchmarkDotNet.Attributes;
using Andy.Tui.Text;

namespace Andy.Tui.Benchmarks;

[MemoryDiagnoser]
public class TextBenchmarks
{
    private readonly TextWrapper _wrapper = new();
    private string _text = string.Empty;

    [Params(10, 100, 1000, 10000)]
    public int Length;

    [GlobalSetup]
    public void Setup()
    {
        _text = new string('a', Length);
    }

    [Benchmark]
    public int Wrap_CharacterWrap()
    {
        var lines = _wrapper.Wrap(_text, new WrapOptions(80, WrapStrategy.CharacterWrap));
        return lines.Count;
    }
}
