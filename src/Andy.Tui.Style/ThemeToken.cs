namespace Andy.Tui.Style;

/// <summary>
/// Semantic color roles a <see cref="Theme"/> defines. Widgets and CSS reference
/// these roles rather than raw colors, so swapping the active <see cref="Theme"/>
/// restyles the whole UI without touching call sites.
/// </summary>
public enum ThemeToken
{
    // Surfaces (back-to-front depth)
    Background,        // app / container backdrop
    Surface,           // raised control face (buttons, checkboxes)
    SurfaceSunken,     // recessed fields (text inputs, lists)
    SurfaceHover,      // control face while hovered
    SurfaceActive,     // control face while pressed
    SurfaceDisabled,   // control face while disabled
    SurfaceSelected,   // selected row / item highlight

    // Foregrounds
    Foreground,        // primary text
    ForegroundMuted,   // secondary / dimmed text
    ForegroundDisabled,

    // Accent
    Accent,            // primary brand / emphasis color
    AccentForeground,  // text drawn on top of Accent

    // Lines
    Border,
    BorderFocus,

    // Status
    Success,
    Warning,
    Error,
    Info,

    // Syntax highlighting (CodeViewer, DiffViewer, ...)
    SyntaxKeyword,
    SyntaxComment,
    SyntaxString,
    SyntaxNumber,
    SyntaxPreproc,
}
