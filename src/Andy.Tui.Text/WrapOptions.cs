namespace Andy.Tui.Text;

public enum WrapStrategy
{
    NoWrap,
    WordWrap,
    CharacterWrap
}

public sealed record WrapOptions(int MaxWidth, WrapStrategy Strategy);
