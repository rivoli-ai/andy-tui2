using ResolvedStyle = Andy.Tui.Style.ResolvedStyle;

namespace Andy.Tui.Layout;

/// <summary>
/// Flexbox layout algorithm honoring the resolved style properties exposed by the public API.
/// Supports container padding, per-child margins, <c>display:none</c>, explicit width/height,
/// flex-basis, min/max constraints, grow/shrink distribution (row and column), wrap and
/// wrap-reverse, justify-content, align-items/align-self/align-content, and overflow clipping.
/// </summary>
public static class FlexLayout
{
    private const double Eps = 1e-6;

    public static void Layout(in Size containerSize, ResolvedStyle containerStyle, IReadOnlyList<(ILayoutNode Node, ResolvedStyle Style)> children)
    {
        // --- Container padding -> content box ---
        double padL = containerStyle.Padding.Left.Resolve(containerSize.Width);
        double padT = containerStyle.Padding.Top.Resolve(containerSize.Height);
        double padR = containerStyle.Padding.Right.Resolve(containerSize.Width);
        double padB = containerStyle.Padding.Bottom.Resolve(containerSize.Height);
        double originX = padL;
        double originY = padT;
        double contentW = Math.Max(0, containerSize.Width - padL - padR);
        double contentH = Math.Max(0, containerSize.Height - padT - padB);

        // --- Overflow clipping (to the content box) ---
        bool isOverflowHidden = containerStyle.Overflow == Andy.Tui.Style.Overflow.Hidden;
        double clipRight = originX + contentW;
        double clipBottom = originY + contentH;
        Rect ClipIfNeeded(Rect r)
        {
            if (!isOverflowHidden) return r;
            double w = Math.Min(r.Width, Math.Max(0, clipRight - r.X));
            double h = Math.Min(r.Height, Math.Max(0, clipBottom - r.Y));
            return new Rect(r.X, r.Y, w, h);
        }

        bool isRowDirection = containerStyle.FlexDirection == Andy.Tui.Style.FlexDirection.Row;

        // Sort by order (stable) and drop display:none items so they occupy no space.
        var ordered = children
            .Select((c, idx) => (c.Node, c.Style, idx))
            .Where(t => t.Style.Display != Andy.Tui.Style.Display.None)
            .OrderBy(t => t.Style.Order)
            .ThenBy(t => t.idx)
            .ToArray();

        // Measure against the content box, then apply explicit width/height as the base size.
        var availableForMeasure = new Size(contentW, contentH);
        var measured = new (ILayoutNode node, ResolvedStyle style, Size size)[ordered.Length];
        for (int mi = 0; mi < ordered.Length; mi++)
        {
            var it = ordered[mi];
            var m = it.Node.Measure(availableForMeasure);
            double w = m.Width;
            double h = m.Height;
            if (!it.Style.Width.IsAuto)
            {
                var v = it.Style.Width.Value!.Value;
                w = v.IsPercent ? v.Resolve(contentW) : v.Pixels;
            }
            if (!it.Style.Height.IsAuto)
            {
                var v = it.Style.Height.Value!.Value;
                h = v.IsPercent ? v.Resolve(contentH) : v.Pixels;
            }
            measured[mi] = (it.Node, it.Style, new Size(w, h));
        }

        // Per-item resolved margins (horizontal against content width, vertical against content height).
        double MarginLeft(int i) => measured[i].style.Margin.Left.Resolve(contentW);
        double MarginRight(int i) => measured[i].style.Margin.Right.Resolve(contentW);
        double MarginTop(int i) => measured[i].style.Margin.Top.Resolve(contentH);
        double MarginBottom(int i) => measured[i].style.Margin.Bottom.Resolve(contentH);

        // Outer extents including margins (used for line breaking and cross-size measurement).
        double OuterMain(int i) => isRowDirection
            ? MarginLeft(i) + measured[i].size.Width + MarginRight(i)
            : MarginTop(i) + measured[i].size.Height + MarginBottom(i);
        double OuterCross(int i) => isRowDirection
            ? MarginTop(i) + measured[i].size.Height + MarginBottom(i)
            : MarginLeft(i) + measured[i].size.Width + MarginRight(i);

        // Map align-self (with Auto falling back to the container's align-items).
        Andy.Tui.Style.AlignItems EffectiveAlign(Andy.Tui.Style.AlignSelf self) => self switch
        {
            Andy.Tui.Style.AlignSelf.FlexStart => Andy.Tui.Style.AlignItems.FlexStart,
            Andy.Tui.Style.AlignSelf.FlexEnd => Andy.Tui.Style.AlignItems.FlexEnd,
            Andy.Tui.Style.AlignSelf.Center => Andy.Tui.Style.AlignItems.Center,
            Andy.Tui.Style.AlignSelf.Stretch => Andy.Tui.Style.AlignItems.Stretch,
            Andy.Tui.Style.AlignSelf.Baseline => Andy.Tui.Style.AlignItems.Baseline,
            _ => containerStyle.AlignItems,
        };

        // --- Break into flex lines along the main axis (margins counted). ---
        var lines = new List<List<int>>();
        {
            var current = new List<int>();
            double running = 0;
            double mainGap = isRowDirection ? containerStyle.ColumnGap.Pixels : containerStyle.RowGap.Pixels;
            double mainContent = isRowDirection ? contentW : contentH;
            bool wraps = containerStyle.FlexWrap != Andy.Tui.Style.FlexWrap.Nowrap;
            for (int i = 0; i < measured.Length; i++)
            {
                double outer = OuterMain(i);
                double projected = running + (current.Count > 0 ? mainGap : 0) + outer;
                if (wraps && current.Count > 0 && projected > mainContent + Eps)
                {
                    lines.Add(current);
                    current = new List<int>();
                    running = 0;
                }
                if (current.Count > 0) running += mainGap;
                current.Add(i);
                running += outer;
            }
            if (current.Count > 0) lines.Add(current);
        }

        // Cross size of each line = max outer cross extent among its items.
        var lineSizes = new double[lines.Count];
        for (int li = 0; li < lines.Count; li++)
        {
            lineSizes[li] = lines[li].Count > 0 ? lines[li].Max(i => OuterCross(i)) : 0;
        }

        // Single-line cross base offset from align-items (line placed within the container cross size).
        double maxOuterCross = measured.Length > 0 ? Enumerable.Range(0, measured.Length).Max(i => OuterCross(i)) : 0;
        double containerCross = isRowDirection ? contentH : contentW;
        double singleLineCrossBaseOffset = containerStyle.AlignItems switch
        {
            Andy.Tui.Style.AlignItems.Center => Math.Max(0, (containerCross - maxOuterCross) / 2.0),
            Andy.Tui.Style.AlignItems.FlexEnd => Math.Max(0, containerCross - maxOuterCross),
            _ => 0,
        };

        // --- Cross-axis line placement (align-content) for multi-line. ---
        var lineOffsetsY = new double[lines.Count];
        var perLineExtraHeight = new double[lines.Count];
        var lineOffsetsX = new double[lines.Count];
        var perLineExtraWidth = new double[lines.Count];

        if (isRowDirection)
        {
            ComputeAlignContent(containerStyle, contentH, containerStyle.RowGap.Pixels, lineSizes, lineOffsetsY, perLineExtraHeight);
        }
        else
        {
            ComputeAlignContent(containerStyle, contentW, containerStyle.ColumnGap.Pixels, lineSizes, lineOffsetsX, perLineExtraWidth);
        }

        // --- Place each line ---
        for (int li = 0; li < lines.Count; li++)
        {
            var idxs = lines[li];
            int n = idxs.Count;
            if (n == 0) continue;

            if (isRowDirection)
            {
                double mainContent = contentW;
                double baseGap = containerStyle.ColumnGap.Pixels;
                double gapCount = Math.Max(0, n - 1);

                var mStart = new double[n];
                var mEnd = new double[n];
                var mCrossStart = new double[n];
                var mCrossEnd = new double[n];
                var baseMain = new double[n];
                var minMain = new double[n];
                var maxMain = new double[n];
                for (int k = 0; k < n; k++)
                {
                    int i = idxs[k];
                    var st = measured[i].style;
                    mStart[k] = MarginLeft(i);
                    mEnd[k] = MarginRight(i);
                    mCrossStart[k] = MarginTop(i);
                    mCrossEnd[k] = MarginBottom(i);
                    baseMain[k] = st.FlexBasis.IsAuto
                        ? measured[i].size.Width
                        : (st.FlexBasis.Value!.Value.IsPercent ? st.FlexBasis.Value.Value.Resolve(mainContent) : st.FlexBasis.Value.Value.Pixels);
                    minMain[k] = ResolveMin(st.MinWidth, mainContent);
                    maxMain[k] = ResolveMax(st.MaxWidth, mainContent);
                }

                var arranged = DistributeMain(idxs, measured, baseMain, minMain, maxMain, mStart, mEnd, gapCount, baseGap, mainContent);

                // Justify computed AFTER final flex sizes are known.
                double itemsExtent = 0;
                for (int k = 0; k < n; k++) itemsExtent += arranged[k] + mStart[k] + mEnd[k];
                ComputeJustify(containerStyle.JustifyContent, mainContent, itemsExtent, gapCount, baseGap, n, out double startX, out double dynamicGap);

                double lineHeight = lineSizes[li] + perLineExtraHeight[li];
                double baseLineTop = lines.Count == 1 ? singleLineCrossBaseOffset : lineOffsetsY[li];

                // Baselines for baseline alignment.
                var baselines = new double[n];
                double lineBaseline = 0;
                for (int k = 0; k < n; k++)
                {
                    var (node, _, size) = measured[idxs[k]];
                    baselines[k] = node is IBaselineProvider bp ? bp.GetFirstBaseline(in size) : size.Height;
                    if (baselines[k] > lineBaseline) lineBaseline = baselines[k];
                }

                double x = startX;
                for (int k = 0; k < n; k++)
                {
                    int i = idxs[k];
                    var st = measured[i].style;
                    double w = arranged[k];
                    double h = measured[i].size.Height;
                    double outerCross = mCrossStart[k] + h + mCrossEnd[k];
                    var eff = EffectiveAlign(st.AlignSelf);
                    double itemY;
                    switch (eff)
                    {
                        case Andy.Tui.Style.AlignItems.Center:
                            itemY = baseLineTop + (lineHeight - outerCross) / 2.0 + mCrossStart[k];
                            break;
                        case Andy.Tui.Style.AlignItems.FlexEnd:
                            itemY = baseLineTop + (lineHeight - outerCross) + mCrossStart[k];
                            break;
                        case Andy.Tui.Style.AlignItems.Baseline:
                            itemY = baseLineTop + (lineBaseline - baselines[k]) + mCrossStart[k];
                            break;
                        case Andy.Tui.Style.AlignItems.Stretch:
                            if (st.Height.IsAuto) h = Math.Max(h, lineHeight - mCrossStart[k] - mCrossEnd[k]);
                            itemY = baseLineTop + mCrossStart[k];
                            break;
                        default:
                            itemY = baseLineTop + mCrossStart[k];
                            break;
                    }
                    h = ClampCross(h, st.MinHeight, st.MaxHeight, contentH);
                    itemY = Math.Max(0, itemY);

                    double itemX = x + mStart[k];
                    var rect = ClipIfNeeded(new Rect(itemX + originX, itemY + originY, w, h));
                    measured[i].node.Arrange(rect);

                    x += mStart[k] + w + mEnd[k];
                    if (k < n - 1) x += dynamicGap;
                }
            }
            else
            {
                double mainContent = contentH;
                double baseGap = containerStyle.RowGap.Pixels;
                double gapCount = Math.Max(0, n - 1);

                var mStart = new double[n];
                var mEnd = new double[n];
                var mCrossStart = new double[n];
                var mCrossEnd = new double[n];
                var baseMain = new double[n];
                var minMain = new double[n];
                var maxMain = new double[n];
                for (int k = 0; k < n; k++)
                {
                    int i = idxs[k];
                    var st = measured[i].style;
                    mStart[k] = MarginTop(i);
                    mEnd[k] = MarginBottom(i);
                    mCrossStart[k] = MarginLeft(i);
                    mCrossEnd[k] = MarginRight(i);
                    baseMain[k] = st.FlexBasis.IsAuto
                        ? measured[i].size.Height
                        : (st.FlexBasis.Value!.Value.IsPercent ? st.FlexBasis.Value.Value.Resolve(mainContent) : st.FlexBasis.Value.Value.Pixels);
                    minMain[k] = ResolveMin(st.MinHeight, mainContent);
                    maxMain[k] = ResolveMax(st.MaxHeight, mainContent);
                }

                var arranged = DistributeMain(idxs, measured, baseMain, minMain, maxMain, mStart, mEnd, gapCount, baseGap, mainContent);

                double itemsExtent = 0;
                for (int k = 0; k < n; k++) itemsExtent += arranged[k] + mStart[k] + mEnd[k];
                ComputeJustify(containerStyle.JustifyContent, mainContent, itemsExtent, gapCount, baseGap, n, out double startY, out double dynamicGap);

                double columnWidth = lineSizes[li] + perLineExtraWidth[li];
                double crossBase = lines.Count == 1 ? singleLineCrossBaseOffset : lineOffsetsX[li];

                double y = startY;
                for (int k = 0; k < n; k++)
                {
                    int i = idxs[k];
                    var st = measured[i].style;
                    double h = arranged[k];
                    double w = measured[i].size.Width;
                    double outerCross = mCrossStart[k] + w + mCrossEnd[k];
                    var eff = EffectiveAlign(st.AlignSelf);
                    double itemX;
                    switch (eff)
                    {
                        case Andy.Tui.Style.AlignItems.Center:
                            itemX = crossBase + (columnWidth - outerCross) / 2.0 + mCrossStart[k];
                            break;
                        case Andy.Tui.Style.AlignItems.FlexEnd:
                            itemX = crossBase + (columnWidth - outerCross) + mCrossStart[k];
                            break;
                        case Andy.Tui.Style.AlignItems.Stretch:
                            if (st.Width.IsAuto) w = Math.Max(w, columnWidth - mCrossStart[k] - mCrossEnd[k]);
                            itemX = crossBase + mCrossStart[k];
                            break;
                        default:
                            itemX = crossBase + mCrossStart[k];
                            break;
                    }
                    w = ClampCross(w, st.MinWidth, st.MaxWidth, contentW);
                    h = ClampCross(h, st.MinHeight, st.MaxHeight, contentH);
                    itemX = Math.Max(0, itemX);

                    double itemY = y + mStart[k];
                    var rect = ClipIfNeeded(new Rect(itemX + originX, itemY + originY, w, h));
                    measured[i].node.Arrange(rect);

                    // Advance by the FINAL (constrained) main size plus margins so rects never overlap.
                    y += mStart[k] + h + mEnd[k];
                    if (k < n - 1) y += dynamicGap;
                }
            }
        }
    }

    private static double ResolveMin(Andy.Tui.Style.LengthOrAuto v, double reference)
        => v.IsAuto ? double.NegativeInfinity : (v.Value!.Value.IsPercent ? v.Value.Value.Resolve(reference) : v.Value.Value.Pixels);

    private static double ResolveMax(Andy.Tui.Style.LengthOrAuto v, double reference)
        => v.IsAuto ? double.PositiveInfinity : (v.Value!.Value.IsPercent ? v.Value.Value.Resolve(reference) : v.Value.Value.Pixels);

    private static double ClampCross(double value, Andy.Tui.Style.LengthOrAuto min, Andy.Tui.Style.LengthOrAuto max, double reference)
    {
        if (!min.IsAuto)
        {
            var v = min.Value!.Value;
            value = Math.Max(value, v.IsPercent ? v.Resolve(reference) : v.Pixels);
        }
        if (!max.IsAuto)
        {
            var v = max.Value!.Value;
            value = Math.Min(value, v.IsPercent ? v.Resolve(reference) : v.Pixels);
        }
        return value;
    }

    /// <summary>
    /// Distributes free (or negative) space along the main axis using grow/shrink factors,
    /// freezing items that hit their min/max and re-distributing the remaining space.
    /// </summary>
    private static double[] DistributeMain(
        IReadOnlyList<int> idxs,
        (ILayoutNode node, ResolvedStyle style, Size size)[] measured,
        double[] baseMain,
        double[] minMain,
        double[] maxMain,
        double[] mStart,
        double[] mEnd,
        double gapCount,
        double baseGap,
        double mainContent)
    {
        int n = idxs.Count;
        var arranged = new double[n];
        for (int k = 0; k < n; k++) arranged[k] = baseMain[k];

        double MarginsAndGaps()
        {
            double s = gapCount * baseGap;
            for (int k = 0; k < n; k++) s += mStart[k] + mEnd[k];
            return s;
        }
        double SumArranged()
        {
            double s = 0;
            for (int k = 0; k < n; k++) s += arranged[k];
            return s;
        }

        double marginsAndGaps = MarginsAndGaps();
        double freeSpace = mainContent - (SumArranged() + marginsAndGaps);

        if (freeSpace > Eps)
        {
            for (int iter = 0; iter < n; iter++)
            {
                double growSum = 0;
                var canGrow = new bool[n];
                for (int k = 0; k < n; k++)
                {
                    double g = measured[idxs[k]].style.FlexGrow;
                    canGrow[k] = g > 0 && arranged[k] < maxMain[k] - Eps;
                    if (canGrow[k]) growSum += g;
                }
                if (growSum <= 0) break;
                for (int k = 0; k < n; k++)
                {
                    if (!canGrow[k]) continue;
                    double g = measured[idxs[k]].style.FlexGrow;
                    arranged[k] = Math.Min(maxMain[k], arranged[k] + freeSpace * (g / growSum));
                }
                double newFree = mainContent - (SumArranged() + marginsAndGaps);
                if (Math.Abs(newFree - freeSpace) < Eps || newFree <= Eps) { break; }
                freeSpace = newFree;
            }
        }
        else if (freeSpace < -Eps)
        {
            for (int iter = 0; iter < n; iter++)
            {
                double weightSum = 0;
                var canShrink = new bool[n];
                for (int k = 0; k < n; k++)
                {
                    double w = measured[idxs[k]].style.FlexShrink * baseMain[k];
                    canShrink[k] = w > 0 && arranged[k] > minMain[k] + Eps;
                    if (canShrink[k]) weightSum += w;
                }
                if (weightSum <= 0) break;
                double deficit = -freeSpace;
                for (int k = 0; k < n; k++)
                {
                    if (!canShrink[k]) continue;
                    double w = measured[idxs[k]].style.FlexShrink * baseMain[k];
                    arranged[k] = Math.Max(minMain[k], arranged[k] - deficit * (w / weightSum));
                }
                double newFree = mainContent - (SumArranged() + marginsAndGaps);
                if (Math.Abs(newFree - freeSpace) < Eps || newFree >= -Eps) { break; }
                freeSpace = newFree;
            }
        }

        return arranged;
    }

    private static void ComputeJustify(
        Andy.Tui.Style.JustifyContent jc,
        double mainContent,
        double itemsExtent,
        double gapCount,
        double baseGap,
        int n,
        out double start,
        out double dynamicGap)
    {
        double totalBase = itemsExtent + gapCount * baseGap;
        switch (jc)
        {
            case Andy.Tui.Style.JustifyContent.Center:
                start = Math.Max(0, (mainContent - totalBase) / 2.0);
                dynamicGap = baseGap;
                break;
            case Andy.Tui.Style.JustifyContent.FlexEnd:
                start = Math.Max(0, mainContent - totalBase);
                dynamicGap = baseGap;
                break;
            case Andy.Tui.Style.JustifyContent.SpaceBetween:
                dynamicGap = gapCount > 0 ? Math.Max(0, (mainContent - itemsExtent) / gapCount) : 0;
                start = 0;
                break;
            case Andy.Tui.Style.JustifyContent.SpaceAround:
                {
                    double spacing = Math.Max(0, (mainContent - itemsExtent) / n);
                    start = spacing / 2.0;
                    dynamicGap = spacing;
                    break;
                }
            case Andy.Tui.Style.JustifyContent.SpaceEvenly:
                {
                    double spacing = Math.Max(0, (mainContent - itemsExtent) / (n + 1));
                    start = spacing;
                    dynamicGap = spacing;
                    break;
                }
            case Andy.Tui.Style.JustifyContent.FlexStart:
            default:
                start = 0;
                dynamicGap = baseGap;
                break;
        }
    }

    private static void ComputeAlignContent(
        ResolvedStyle containerStyle,
        double containerCross,
        double baseGap,
        double[] lineSizes,
        double[] lineOffsets,
        double[] perLineExtra)
    {
        int count = lineSizes.Length;
        double sumSizes = lineSizes.Sum();
        double start = 0;
        double betweenGap = baseGap;

        if (count > 1)
        {
            double baseTotal = sumSizes + baseGap * (count - 1);
            switch (containerStyle.AlignContent)
            {
                case Andy.Tui.Style.AlignContent.FlexStart:
                    start = 0; betweenGap = baseGap; break;
                case Andy.Tui.Style.AlignContent.Center:
                    start = Math.Max(0, (containerCross - baseTotal) / 2.0); betweenGap = baseGap; break;
                case Andy.Tui.Style.AlignContent.FlexEnd:
                    start = Math.Max(0, containerCross - baseTotal); betweenGap = baseGap; break;
                case Andy.Tui.Style.AlignContent.SpaceBetween:
                    betweenGap = count > 1 ? Math.Max(0, (containerCross - sumSizes) / (count - 1)) : 0;
                    start = 0; break;
                case Andy.Tui.Style.AlignContent.SpaceAround:
                    {
                        double spacing = Math.Max(0, (containerCross - sumSizes) / count);
                        start = spacing / 2.0; betweenGap = spacing; break;
                    }
                case Andy.Tui.Style.AlignContent.SpaceEvenly:
                    {
                        double spacing = Math.Max(0, (containerCross - sumSizes) / (count + 1));
                        start = spacing; betweenGap = spacing; break;
                    }
                case Andy.Tui.Style.AlignContent.Stretch:
                default:
                    {
                        double extra = Math.Max(0, containerCross - baseTotal);
                        double add = count > 0 ? extra / count : 0;
                        for (int li = 0; li < count; li++) perLineExtra[li] = add;
                        start = 0; betweenGap = baseGap; break;
                    }
            }
        }

        if (containerStyle.FlexWrap == Andy.Tui.Style.FlexWrap.WrapReverse && count > 0)
        {
            double total = lineSizes.Sum() + perLineExtra.Sum() + betweenGap * Math.Max(0, count - 1);
            double accum = Math.Max(0, containerCross - total + start);
            for (int li = count - 1; li >= 0; li--)
            {
                lineOffsets[li] = accum;
                accum += lineSizes[li] + perLineExtra[li];
                if (li > 0) accum += betweenGap;
            }
        }
        else
        {
            double accum = start;
            for (int li = 0; li < count; li++)
            {
                lineOffsets[li] = accum;
                accum += lineSizes[li] + perLineExtra[li];
                if (li < count - 1) accum += betweenGap;
            }
        }
    }
}
