using System.Text;

namespace Andy.Tui.Style;

/// <summary>
/// Bridges a <see cref="Theme"/> into the CSS cascade by emitting its tokens as
/// CSS custom properties on the universal selector, e.g. <c>* { --accent: rgb(..) }</c>.
/// App stylesheets then reference <c>var(--accent)</c> and the <see cref="StyleResolver"/>
/// resolves them. Prepend the returned sheet (lowest source order) so app rules win.
/// </summary>
public static class ThemeCss
{
    /// <summary>Variable name for a token, e.g. <see cref="ThemeToken.SurfaceHover"/> → <c>--surface-hover</c>.</summary>
    public static string VarName(ThemeToken token) => "--" + ToKebab(token.ToString());

    /// <summary>
    /// Build a stylesheet of <c>--token: rgb(r,g,b)</c> declarations on <c>*</c>.
    /// Transparent tokens are emitted as <c>transparent</c> so <c>var()</c> consumers
    /// fall back to the terminal default.
    /// </summary>
    public static Stylesheet ToVariableSheet(this Theme theme, int baseSourceOrder = 0)
    {
        var sb = new StringBuilder("* {");
        foreach (ThemeToken tok in Enum.GetValues(typeof(ThemeToken)))
        {
            var c = theme.Get(tok);
            sb.Append(VarName(tok)).Append(':');
            sb.Append(c.IsTransparent ? "transparent" : $"rgb({c.R},{c.G},{c.B})");
            sb.Append(';');
        }
        sb.Append('}');
        return CssParser.Parse(sb.ToString(), baseSourceOrder);
    }

    private static string ToKebab(string pascal)
    {
        var sb = new StringBuilder(pascal.Length + 4);
        for (int i = 0; i < pascal.Length; i++)
        {
            char ch = pascal[i];
            if (char.IsUpper(ch))
            {
                if (i > 0) sb.Append('-');
                sb.Append(char.ToLowerInvariant(ch));
            }
            else
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }
}
