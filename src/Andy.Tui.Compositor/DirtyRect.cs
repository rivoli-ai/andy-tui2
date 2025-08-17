using System;
using System.Collections.Generic;

namespace Andy.Tui.Compositor;

public readonly record struct DirtyRect(int X, int Y, int Width, int Height);