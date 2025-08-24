using System;
using System.Collections.Generic;
using System.Linq;
using DL = Andy.Tui.DisplayList;
using L = Andy.Tui.Layout;

namespace Andy.Tui.Widgets;

/// <summary>
/// LargeText renders big glyphs (digits and separators) in multiple styles and sizes.
/// Intended for clocks, counters, and banners inside a TTY.
/// </summary>
public sealed class LargeText
{
    public enum LargeTextStyle { Block, SevenSegment, Outline }

    private string _text = string.Empty;
    private LargeTextStyle _style = LargeTextStyle.Block;
    private int _scale = 1;
    private int _spacing = 1; // columns between glyphs (unscaled)

    public DL.Rgb24 Background { get; set; } = new(0, 0, 0);
    public DL.Rgb24 Foreground { get; set; } = new(230, 230, 230);

    public void SetText(string? text) => _text = text ?? string.Empty;
    public void SetStyle(LargeTextStyle style) => _style = style;
    public void SetScale(int scale) => _scale = Math.Max(1, Math.Min(4, scale));
    public void SetSpacing(int columns) => _spacing = Math.Max(0, Math.Min(8, columns));

    /// <summary>
    /// Returns the unscaled glyph cell size (columns, rows) for the current style.
    /// </summary>
    private (int W, int H) GetBaseGlyphSize()
    {
        return _style switch
        {
            LargeTextStyle.SevenSegment => (4, 5), // compact 7-seg per character (including spacing)
            LargeTextStyle.Outline => (5, 7),
            _ => (5, 5), // Block
        };
    }

    /// <summary>
    /// Measure expected width x height for given rect constraints when rendering current text.
    /// </summary>
    public (int Width, int Height) Measure()
    {
        var (bw, bh) = GetBaseGlyphSize();
        int glyphs = _text.Length;
        int w = glyphs * bw * _scale + Math.Max(0, glyphs - 1) * _spacing;
        int h = bh * _scale;
        return (w, h);
    }

    public void Render(in L.Rect rect, DL.DisplayList baseDl, DL.DisplayListBuilder builder)
    {
        int x = (int)rect.X; int y = (int)rect.Y; int w = (int)rect.Width; int h = (int)rect.Height;
        if (w <= 0 || h <= 0) return;
        builder.PushClip(new DL.ClipPush(x, y, w, h));
        builder.DrawRect(new DL.Rect(x, y, w, h, Background));

        var (bw, bh) = GetBaseGlyphSize();
        int cursorX = x;
        foreach (char ch in _text)
        {
            var pattern = BuildPatternForChar(ch, _style);
            // pattern is bh rows of bw columns of booleans
            for (int row = 0; row < bh; row++)
            {
                for (int col = 0; col < bw; col++)
                {
                    if (!pattern[row, col]) continue;
                    int drawX = cursorX + col * _scale;
                    int drawY = y + row * _scale;
                    // Fill a scale x scale block
                    builder.DrawRect(new DL.Rect(drawX, drawY, _scale, _scale, Foreground));
                }
            }
            cursorX += bw * _scale + _spacing;
            if (cursorX >= x + w) break;
        }

        builder.Pop();
    }

    private static bool[,] BuildPatternForChar(char ch, LargeTextStyle style)
    {
        return style switch
        {
            LargeTextStyle.SevenSegment => BuildSevenSegment(ch),
            LargeTextStyle.Outline => BuildOutline(ch),
            _ => BuildBlock(ch),
        };
    }

    // 5x5 solid block font for digits and ':' and ' ' (space)
    private static bool[,] BuildBlock(char ch)
    {
        // Base size 5x5
        bool[,] empty = Make(false, 5, 5);
        return ch switch
        {
            '0' => FromRows("11111", "10001", "10001", "10001", "11111"),
            '1' => FromRows("00100", "00100", "00100", "00100", "11111"),
            '2' => FromRows("11111", "00001", "11111", "10000", "11111"),
            '3' => FromRows("11111", "00001", "11111", "00001", "11111"),
            '4' => FromRows("10001", "10001", "11111", "00001", "00001"),
            '5' => FromRows("11111", "10000", "11111", "00001", "11111"),
            '6' => FromRows("11111", "10000", "11111", "10001", "11111"),
            '7' => FromRows("11111", "00001", "00010", "00100", "01000"),
            '8' => FromRows("11111", "10001", "11111", "10001", "11111"),
            '9' => FromRows("11111", "10001", "11111", "00001", "11111"),
            ':' => FromRows("00000", "00100", "00000", "00100", "00000"),
            ' ' => empty,
            '-' => FromRows("00000", "00000", "11111", "00000", "00000"),
            _ => empty,
        };
    }

    // 4x5 seven-segment style occupancy (segments: a,b,c,d,e,f,g). We map digits accordingly.
    private static bool[,] BuildSevenSegment(char ch)
    {
        bool segA = false, segB = false, segC = false, segD = false, segE = false, segF = false, segG = false;
        switch (ch)
        {
            case '0': segA = segB = segC = segD = segE = segF = true; break;
            case '1': segB = segC = true; break;
            case '2': segA = segB = segD = segE = segG = true; break;
            case '3': segA = segB = segC = segD = segG = true; break;
            case '4': segF = segG = segB = segC = true; break;
            case '5': segA = segF = segG = segC = segD = true; break;
            case '6': segA = segF = segG = segC = segD = segE = true; break;
            case '7': segA = segB = segC = true; break;
            case '8': segA = segB = segC = segD = segE = segF = segG = true; break;
            case '9': segA = segB = segC = segD = segF = segG = true; break;
            case ':': return FromRows4("0000", "0100", "0000", "0100", "0000");
            case ' ': return Make(false, 4, 5);
            case '-': segG = true; break;
            default: return Make(false, 4, 5);
        }
        var g = Make(false, 4, 5);
        // a
        if (segA) for (int c = 0; c < 4; c++) g[0, c] = true;
        // b
        if (segB) for (int r = 1; r < 3; r++) g[r, 3] = true;
        // c
        if (segC) for (int r = 3; r < 5; r++) g[r, 3] = true;
        // d
        if (segD) for (int c = 0; c < 4; c++) g[4, c] = true;
        // e
        if (segE) for (int r = 3; r < 5; r++) g[r, 0] = true;
        // f
        if (segF) for (int r = 1; r < 3; r++) g[r, 0] = true;
        // g
        if (segG) for (int c = 0; c < 4; c++) g[2, c] = true;
        return g;
    }

    // 5x7 outline box style for digits
    private static bool[,] BuildOutline(char ch)
    {
        bool[,] empty = Make(false, 5, 7);
        return ch switch
        {
            '0' => Frame(5, 7, openTop: false),
            '1' => FromRows7(
                "00100",
                "01100",
                "00100",
                "00100",
                "00100",
                "00100",
                "11111"),
            '2' => FromRows7(
                "11111",
                "00001",
                "00001",
                "11111",
                "10000",
                "10000",
                "11111"),
            '3' => FromRows7(
                "11111", "00001", "00001", "11111", "00001", "00001", "11111"),
            '4' => FromRows7(
                "10001", "10001", "10001", "11111", "00001", "00001", "00001"),
            '5' => FromRows7(
                "11111", "10000", "10000", "11111", "00001", "00001", "11111"),
            '6' => FromRows7(
                "11111", "10000", "10000", "11111", "10001", "10001", "11111"),
            '7' => FromRows7(
                "11111", "00001", "00010", "00100", "01000", "01000", "01000"),
            '8' => FromRows7(
                "11111", "10001", "10001", "11111", "10001", "10001", "11111"),
            '9' => FromRows7(
                "11111", "10001", "10001", "11111", "00001", "00001", "11111"),
            ':' => FromRows7("00000", "00100", "00000", "00100", "00000", "00100", "00000"),
            ' ' => empty,
            '-' => FromRows7("00000", "00000", "00000", "11111", "00000", "00000", "00000"),
            _ => empty,
        };
    }

    private static bool[,] Frame(int w, int h, bool openTop)
    {
        var g = Make(false, w, h);
        for (int c = 0; c < w; c++) { if (!openTop) g[0, c] = true; g[h - 1, c] = true; }
        for (int r = 0; r < h; r++) { g[r, 0] = true; g[r, w - 1] = true; }
        return g;
    }

    private static bool[,] Make(bool value, int w, int h)
    {
        var a = new bool[h, w];
        if (value)
        {
            for (int r = 0; r < h; r++) for (int c = 0; c < w; c++) a[r, c] = true;
        }
        return a;
    }

    private static bool[,] FromRows(string r0, string r1, string r2, string r3, string r4)
    {
        var g = new bool[5, 5];
        string[] rows = new[] { r0, r1, r2, r3, r4 };
        for (int r = 0; r < 5; r++) for (int c = 0; c < 5; c++) g[r, c] = rows[r][c] == '1';
        return g;
    }

    private static bool[,] FromRows4(string r0, string r1, string r2, string r3, string r4)
    {
        var g = new bool[5, 4];
        string[] rows = new[] { r0, r1, r2, r3, r4 };
        for (int r = 0; r < 5; r++) for (int c = 0; c < 4; c++) g[r, c] = rows[r][c] == '1';
        return g;
    }

    private static bool[,] FromRows7(string r0, string r1, string r2, string r3, string r4, string r5, string r6)
    {
        var g = new bool[7, 5];
        string[] rows = new[] { r0, r1, r2, r3, r4, r5, r6 };
        for (int r = 0; r < 7; r++) for (int c = 0; c < 5; c++) g[r, c] = rows[r][c] == '1';
        return g;
    }
}
