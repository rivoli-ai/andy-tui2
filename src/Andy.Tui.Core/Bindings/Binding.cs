using System;

namespace Andy.Tui.Core.Bindings;

/// <summary>
/// Converts values between a source type and a target type.
/// </summary>
public interface IValueConverter<TSource, TTarget>
{
    /// <summary>Converts a source value to target.</summary>
    TTarget Convert(TSource value);
    /// <summary>Converts a target value back to source.</summary>
    TSource ConvertBack(TTarget value);
}

/// <summary>
/// Validates a value, returning an error message when invalid.
/// </summary>
public interface IValidator<T>
{
    bool IsValid(T value, out string? error);
}

/// <summary>
/// A binding pairs a getter and optional setter with optional conversion and validation logic.
/// </summary>
public sealed class Binding<T>
{
    private readonly Func<T> _getter;
    private readonly Action<T>? _setter;
    private readonly IValueConverter<T, T>? _converter;
    private readonly IValidator<T>? _validator;

    /// <summary>
    /// Creates a new binding.
    /// </summary>
    public Binding(Func<T> getter, Action<T>? setter = null, IValueConverter<T, T>? converter = null, IValidator<T>? validator = null)
    {
        _getter = getter;
        _setter = setter;
        _converter = converter;
        _validator = validator;
    }

    /// <summary>Reads the current value, applying conversion if configured.</summary>
    public T Get() => _converter is null ? _getter() : _converter.Convert(_getter());

    /// <summary>
    /// Attempts to set a value, applying validation and conversion-back if configured.
    /// </summary>
    public bool TrySet(T value, out string? error)
    {
        error = null;
        if (_setter is null) { error = "Binding is read-only"; return false; }
        var toSet = _converter is null ? value : _converter.ConvertBack(value);
        if (_validator is not null && !_validator.IsValid(toSet, out error))
        {
            return false;
        }
        _setter(toSet);
        return true;
    }
}
