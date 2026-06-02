namespace Andy.Tui.Virtualization;

public readonly record struct ViewportState(int FirstRow, int RowCount, int Cols, int Rows, int PixelWidth, int PixelHeight);

public readonly record struct OverscanPolicy(int Before, int After, bool Adaptive);
