namespace Andy.Tui.Style;

public enum FontWeight
{
    Normal = 400,
    Bold = 700
}

public enum FontStyle
{
    Normal,
    Italic
}

[System.Flags]
public enum TextDecoration
{
    None = 0,
    Underline = 1,
    Strikethrough = 2
}
