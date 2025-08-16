using System;
using Andy.Tui.Core.Bindings;
using Xunit;

namespace Andy.Tui.Core.Tests;

public class BindingTests
{
    private sealed class DoubleConverter : IValueConverter<int, int>
    {
        public int Convert(int value) => value * 2;
        public int ConvertBack(int value) => value / 2;
    }

    private sealed class NonZeroValidator : IValidator<int>
    {
        public bool IsValid(int value, out string? error)
        {
            if (value == 0) { error = "zero not allowed"; return false; }
            error = null; return true;
        }
    }

    [Fact]
    public void Get_Uses_Converter_When_Present()
    {
        int source = 5;
        var binding = new Binding<int>(() => source, v => source = v, new DoubleConverter());
        Assert.Equal(10, binding.Get());
    }

    [Fact]
    public void TrySet_Uses_ConverterBack_And_Sets_Source()
    {
        int source = 5;
        var binding = new Binding<int>(() => source, v => source = v, new DoubleConverter());
        var ok = binding.TrySet(20, out var error);
        Assert.True(ok);
        Assert.Null(error);
        Assert.Equal(10, source);
    }

    [Fact]
    public void TrySet_Validates_And_Fails_When_Invalid()
    {
        int source = 5;
        var binding = new Binding<int>(() => source, v => source = v, validator: new NonZeroValidator());
        var ok = binding.TrySet(0, out var error);
        Assert.False(ok);
        Assert.Equal("zero not allowed", error);
        Assert.Equal(5, source);
    }

    [Fact]
    public void TrySet_ReadOnly_Fails()
    {
        int source = 5;
        var binding = new Binding<int>(() => source);
        var ok = binding.TrySet(42, out var error);
        Assert.False(ok);
        Assert.Equal("Binding is read-only", error);
        Assert.Equal(5, source);
    }
}
