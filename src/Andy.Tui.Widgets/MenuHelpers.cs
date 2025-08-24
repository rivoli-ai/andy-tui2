using System;
using DL = Andy.Tui.DisplayList;

namespace Andy.Tui.Widgets;

public static class MenuHelpers
{
    public static string? GetSelectedItemPath(MenuBar menuBar, int activeHeaderIndex, int activeItemIndex)
    {
        if (menuBar is null) throw new ArgumentNullException(nameof(menuBar));
        if (activeHeaderIndex < 0 || activeHeaderIndex >= menuBar.Menus.Count) return null;
        var (title, menu) = menuBar.Menus[activeHeaderIndex];
        if (activeItemIndex < 0 || activeItemIndex >= menu.Items.Count) return null;
        var item = menu.Items[activeItemIndex];
        return $"{title} › {item.Text}";
    }

    public static void DrawStatusLine(DL.DisplayListBuilder builder, int y, int width, string text,
        DL.Rgb24? bg = null, DL.Rgb24? fg = null)
    {
        if (builder is null) throw new ArgumentNullException(nameof(builder));
        if (width <= 0 || y < 0) return;
        var bgColor = bg ?? new DL.Rgb24(35, 35, 35);
        var fgColor = fg ?? new DL.Rgb24(255, 255, 200);
        builder.PushClip(new DL.ClipPush(0, y, width, 1));
        builder.DrawRect(new DL.Rect(0, y, width, 1, bgColor));
        builder.DrawText(new DL.TextRun(2, y, text, fgColor, bgColor, DL.CellAttrFlags.Bold));
        builder.Pop();
    }

    public static (int X, int Y) ComputePopupPosition(int anchorX, int anchorY, int popupW, int popupH, int viewportW, int viewportH)
    {
        int x = Math.Max(0, Math.Min(anchorX, Math.Max(0, viewportW - popupW)));
        int y = Math.Max(0, Math.Min(anchorY, Math.Max(0, viewportH - popupH)));
        return (x, y);
    }

    public static (int X, int Y) ComputeSubmenuPosition(int parentX, int parentY, int parentW, int itemIndex, int popupW, int popupH, int viewportW, int viewportH)
    {
        int x = parentX + parentW; // to the right of parent
        int y = parentY + 1 + itemIndex; // align to selected item row inside parent
        // Clamp to viewport
        if (x + popupW > viewportW) x = Math.Max(0, parentX - popupW);
        if (y + popupH > viewportH) y = Math.Max(0, viewportH - popupH);
        return (x, y);
    }
}
