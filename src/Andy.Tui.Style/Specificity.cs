using System;
using System.Collections.Generic;
using System.Linq;

namespace Andy.Tui.Style;

/// <summary>
/// Represents CSS specificity as a tuple (A, B, C) and supports comparisons.
/// </summary>
public readonly record struct Specificity(int A, int B, int C) : IComparable<Specificity>
{
    public int CompareTo(Specificity other)
    {
        if (A != other.A) return A.CompareTo(other.A);
        if (B != other.B) return B.CompareTo(other.B);
        return C.CompareTo(other.C);
    }

    public static bool operator >(Specificity left, Specificity right) => left.CompareTo(right) > 0;
    public static bool operator <(Specificity left, Specificity right) => left.CompareTo(right) < 0;

    public static Specificity operator +(Specificity x, Specificity y)
        => new(x.A + y.A, x.B + y.B, x.C + y.C);
}