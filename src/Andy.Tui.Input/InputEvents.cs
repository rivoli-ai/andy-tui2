namespace Andy.Tui.Input;

[System.Flags]
public enum KeyModifiers
{
    None = 0,
    Shift = 1 << 0,
    Alt = 1 << 1,
    Ctrl = 1 << 2,
    Meta = 1 << 3,
}

public interface IInputEvent { }

public readonly record struct KeyEvent(string Key, string Code, KeyModifiers Modifiers) : IInputEvent;

public enum MouseKind { Move, Down, Up, Wheel }
public enum MouseButton { None, Left, Middle, Right }

public readonly record struct MouseEvent(MouseKind Kind, int X, int Y, MouseButton Button, KeyModifiers Modifiers, int WheelDelta = 0) : IInputEvent;

public readonly record struct PasteEvent(string Text) : IInputEvent;

public readonly record struct ResizeEvent(int Cols, int Rows) : IInputEvent;

public enum ImeKind { Start, Update, End }
public readonly record struct ImeEvent(ImeKind Kind, string Text) : IInputEvent;
