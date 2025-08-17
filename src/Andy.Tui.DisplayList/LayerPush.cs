namespace Andy.Tui.DisplayList;

public readonly record struct LayerPush(Guid? Id = null, float? Opacity = null) : IDisplayOp;