namespace Andy.Tui.DisplayList;

/// <summary>
/// Text cell attribute bit flags.
/// </summary>
[Flags]
public enum CellAttrFlags
{
    None = 0,
    Bold = 1 << 0,
    Faint = 1 << 1,
    Italic = 1 << 2,
    Underline = 1 << 3,
    DoubleUnderline = 1 << 4,
    Strikethrough = 1 << 5,
    Reverse = 1 << 6,
    Dim = 1 << 7,
    Blink = 1 << 8,
}