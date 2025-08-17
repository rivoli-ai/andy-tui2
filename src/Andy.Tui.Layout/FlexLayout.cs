using ResolvedStyle = Andy.Tui.Style.ResolvedStyle;

namespace Andy.Tui.Layout;

/// <summary>
/// Placeholder flex layout algorithm entry. Will be expanded in Phase 1.
/// </summary>
public static class FlexLayout
{
    public static void Layout(in Size containerSize, ResolvedStyle containerStyle, IReadOnlyList<(ILayoutNode Node, ResolvedStyle Style)> children)
    {
        // Helper: clip a rect to container when overflow hidden (avoid capturing 'in' parameter)
        var containerWidth = containerSize.Width;
        var containerHeight = containerSize.Height;
        bool isOverflowHidden = containerStyle.Overflow == Andy.Tui.Style.Overflow.Hidden;
        Rect ClipIfNeeded(Rect r)
        {
            if (!isOverflowHidden) return r;
            double maxW = Math.Max(0, containerWidth - r.X);
            double maxH = Math.Max(0, containerHeight - r.Y);
            double w = Math.Min(r.Width, maxW);
            double h = Math.Min(r.Height, maxH);
            return new Rect(r.X, r.Y, w, h);
        }
        // Minimal row-direction layout that respects 'order' and places items sequentially with gap and basic justify center.
        // Future work: wrapping, alignments (full), grow/shrink, column direction, constraints, etc.

        bool isRowDirection = containerStyle.FlexDirection == Andy.Tui.Style.FlexDirection.Row;

        // Sort by order, stable to preserve original order among equals
        var ordered = children
            .Select((c, idx) => (c.Node, c.Style, idx))
            .OrderBy(t => t.Style.Order)
            .ThenBy(t => t.idx)
            .ToArray();

        // Measure
        var availableForMeasure = new Size(containerSize.Width, containerSize.Height);
        var measured = new (ILayoutNode node, ResolvedStyle style, Size size)[ordered.Length];
        for (int mi = 0; mi < ordered.Length; mi++)
        {
            var it = ordered[mi];
            measured[mi] = (it.Node, it.Style, it.Node.Measure(availableForMeasure));
        }

        // Build lines when wrap is enabled (row direction). For column direction (MVP), treat as single line (no wrap yet).
        var lines = new List<List<int>>();
        if (isRowDirection)
        {
            var current = new List<int>();
            double currentWidth = 0;
            for (int i = 0; i < measured.Length; i++)
            {
                var w = measured[i].size.Width;
                var projected = currentWidth + (current.Count > 0 ? containerStyle.ColumnGap.Pixels : 0) + w;
                if (containerStyle.FlexWrap == Andy.Tui.Style.FlexWrap.Wrap && current.Count > 0 && projected > containerSize.Width)
                {
                    lines.Add(current);
                    current = new List<int>();
                    currentWidth = 0;
                }
                if (current.Count > 0)
                {
                    currentWidth += containerStyle.ColumnGap.Pixels;
                }
                current.Add(i);
                currentWidth += w;
            }
            if (current.Count > 0) lines.Add(current);
        }
        else
        {
            // Build columns when wrap is enabled for column direction
            var current = new List<int>();
            double currentHeight = 0;
            for (int i = 0; i < measured.Length; i++)
            {
                var h = measured[i].size.Height;
                var projected = currentHeight + (current.Count > 0 ? containerStyle.RowGap.Pixels : 0) + h;
                if (containerStyle.FlexWrap == Andy.Tui.Style.FlexWrap.Wrap && current.Count > 0 && projected > containerSize.Height)
                {
                    lines.Add(current);
                    current = new List<int>();
                    currentHeight = 0;
                }
                if (current.Count > 0)
                {
                    currentHeight += containerStyle.RowGap.Pixels;
                }
                current.Add(i);
                currentHeight += h;
            }
            if (current.Count > 0) lines.Add(current);
        }

        // Align-items base offset for single-line case only; for multi-line, align-content governs cross-axis placement
        double singleLineCrossBaseOffset = 0;
        if (isRowDirection)
        {
            double maxItemHeight = measured.Length > 0 ? measured.Max(m => m.size.Height) : 0;
            switch (containerStyle.AlignItems)
            {
                case Andy.Tui.Style.AlignItems.Center:
                    singleLineCrossBaseOffset = Math.Max(0, (containerSize.Height - maxItemHeight) / 2.0);
                    break;
                case Andy.Tui.Style.AlignItems.FlexEnd:
                    singleLineCrossBaseOffset = Math.Max(0, containerSize.Height - maxItemHeight);
                    break;
                case Andy.Tui.Style.AlignItems.Baseline:
                    singleLineCrossBaseOffset = 0; // TODO: implement true baseline; approximate as flex-start
                    break;
                case Andy.Tui.Style.AlignItems.Stretch:
                case Andy.Tui.Style.AlignItems.FlexStart:
                default:
                    singleLineCrossBaseOffset = 0;
                    break;
            }
        }
        else
        {
            double maxItemWidth = measured.Length > 0 ? measured.Max(m => m.size.Width) : 0;
            switch (containerStyle.AlignItems)
            {
                case Andy.Tui.Style.AlignItems.Center:
                    singleLineCrossBaseOffset = Math.Max(0, (containerSize.Width - maxItemWidth) / 2.0);
                    break;
                case Andy.Tui.Style.AlignItems.FlexEnd:
                    singleLineCrossBaseOffset = Math.Max(0, containerSize.Width - maxItemWidth);
                    break;
                case Andy.Tui.Style.AlignItems.Baseline:
                    singleLineCrossBaseOffset = 0; // N/A in column cross-axis; approximate flex-start
                    break;
                case Andy.Tui.Style.AlignItems.Stretch:
                case Andy.Tui.Style.AlignItems.FlexStart:
                default:
                    singleLineCrossBaseOffset = 0;
                    break;
            }
        }

        // Prepare per-line heights (row) or widths (column) and compute align-content distribution for multi-line
        var lineSizes = new double[lines.Count];
        if (isRowDirection)
        {
            for (int li = 0; li < lines.Count; li++)
            {
                lineSizes[li] = lines[li].Count > 0 ? lines[li].Max(i => measured[i].size.Height) : 0;
            }
        }
        else
        {
            // Column direction: size along cross-axis is column width (max item width per column)
            for (int li = 0; li < lines.Count; li++)
            {
                lineSizes[li] = lines[li].Count > 0 ? lines[li].Max(i => measured[i].size.Width) : 0;
            }
        }

        // Compute line offsets along cross-axis for multi-lines (align-content)
        var lineOffsetsY = new double[lines.Count];
        var perLineExtraHeight = new double[lines.Count];
        var lineOffsetsX = new double[lines.Count];
        var perLineExtraWidth = new double[lines.Count];
        if (isRowDirection)
        {
            double baseRowGap = containerStyle.RowGap.Pixels;
            double sumHeights = lineSizes.Sum();
            double yStart = 0;
            double betweenGap = baseRowGap;
            if (lines.Count == 1)
            {
                yStart = 0;
                betweenGap = baseRowGap;
            }
            else
            {
                double baseTotal = sumHeights + baseRowGap * (lines.Count - 1);
                switch (containerStyle.AlignContent)
                {
                    case Andy.Tui.Style.AlignContent.FlexStart:
                        yStart = 0;
                        betweenGap = baseRowGap;
                        break;
                    case Andy.Tui.Style.AlignContent.Center:
                        yStart = Math.Max(0, (containerSize.Height - baseTotal) / 2.0);
                        betweenGap = baseRowGap;
                        break;
                    case Andy.Tui.Style.AlignContent.FlexEnd:
                        yStart = Math.Max(0, containerSize.Height - baseTotal);
                        betweenGap = baseRowGap;
                        break;
                    case Andy.Tui.Style.AlignContent.SpaceBetween:
                        if (lines.Count > 1)
                        {
                            betweenGap = Math.Max(0, (containerSize.Height - sumHeights) / (lines.Count - 1));
                        }
                        else
                        {
                            betweenGap = 0;
                        }
                        yStart = 0;
                        break;
                    case Andy.Tui.Style.AlignContent.SpaceAround:
                        {
                            double spacing = Math.Max(0, (containerSize.Height - sumHeights) / (lines.Count));
                            yStart = spacing / 2.0;
                            betweenGap = spacing;
                            break;
                        }
                    case Andy.Tui.Style.AlignContent.SpaceEvenly:
                        {
                            double spacing = Math.Max(0, (containerSize.Height - sumHeights) / (lines.Count + 1));
                            yStart = spacing;
                            betweenGap = spacing;
                            break;
                        }
                    case Andy.Tui.Style.AlignContent.Stretch:
                    default:
                        {
                            // Distribute extra space equally to line heights
                            double extra = Math.Max(0, containerSize.Height - baseTotal);
                            double add = lines.Count > 0 ? extra / lines.Count : 0;
                            for (int li = 0; li < lines.Count; li++) perLineExtraHeight[li] = add;
                            yStart = 0;
                            betweenGap = baseRowGap;
                            break;
                        }
                }
            }

            if (containerStyle.FlexWrap == Andy.Tui.Style.FlexWrap.WrapReverse && lines.Count > 0)
            {
                double total = lineSizes.Sum() + perLineExtraHeight.Sum() + betweenGap * Math.Max(0, lines.Count - 1);
                double yAccum = Math.Max(0, containerSize.Height - total + yStart);
                for (int li = lines.Count - 1; li >= 0; li--)
                {
                    lineOffsetsY[li] = yAccum;
                    yAccum += lineSizes[li] + perLineExtraHeight[li];
                    if (li > 0) yAccum += betweenGap;
                }
            }
            else
            {
                double yAccum = yStart;
                for (int li = 0; li < lines.Count; li++)
                {
                    lineOffsetsY[li] = yAccum;
                    yAccum += lineSizes[li] + perLineExtraHeight[li];
                    if (li < lines.Count - 1) yAccum += betweenGap;
                }
            }
        }
        else
        {
            double baseColGap = containerStyle.ColumnGap.Pixels;
            double sumWidths = lineSizes.Sum();
            double xStart = 0;
            double betweenGap = baseColGap;
            if (lines.Count == 1)
            {
                xStart = 0;
                betweenGap = baseColGap;
            }
            else
            {
                double baseTotal = sumWidths + baseColGap * (lines.Count - 1);
                switch (containerStyle.AlignContent)
                {
                    case Andy.Tui.Style.AlignContent.FlexStart:
                        xStart = 0;
                        betweenGap = baseColGap;
                        break;
                    case Andy.Tui.Style.AlignContent.Center:
                        xStart = Math.Max(0, (containerSize.Width - baseTotal) / 2.0);
                        betweenGap = baseColGap;
                        break;
                    case Andy.Tui.Style.AlignContent.FlexEnd:
                        xStart = Math.Max(0, containerSize.Width - baseTotal);
                        betweenGap = baseColGap;
                        break;
                    case Andy.Tui.Style.AlignContent.SpaceBetween:
                        if (lines.Count > 1)
                        {
                            betweenGap = Math.Max(0, (containerSize.Width - sumWidths) / (lines.Count - 1));
                        }
                        else
                        {
                            betweenGap = 0;
                        }
                        xStart = 0;
                        break;
                    case Andy.Tui.Style.AlignContent.SpaceAround:
                        {
                            double spacing = Math.Max(0, (containerSize.Width - sumWidths) / (lines.Count));
                            xStart = spacing / 2.0;
                            betweenGap = spacing;
                            break;
                        }
                    case Andy.Tui.Style.AlignContent.SpaceEvenly:
                        {
                            double spacing = Math.Max(0, (containerSize.Width - sumWidths) / (lines.Count + 1));
                            xStart = spacing;
                            betweenGap = spacing;
                            break;
                        }
                    case Andy.Tui.Style.AlignContent.Stretch:
                    default:
                        {
                            double extra = Math.Max(0, containerSize.Width - baseTotal);
                            double add = lines.Count > 0 ? extra / lines.Count : 0;
                            for (int li = 0; li < lines.Count; li++) perLineExtraWidth[li] = add;
                            xStart = 0;
                            betweenGap = baseColGap;
                            break;
                        }
                }
            }

            if (containerStyle.FlexWrap == Andy.Tui.Style.FlexWrap.WrapReverse && lines.Count > 0)
            {
                double total = lineSizes.Sum() + perLineExtraWidth.Sum() + betweenGap * Math.Max(0, lines.Count - 1);
                double xAccum = Math.Max(0, containerSize.Width - total + xStart);
                for (int li = lines.Count - 1; li >= 0; li--)
                {
                    lineOffsetsX[li] = xAccum;
                    xAccum += lineSizes[li] + perLineExtraWidth[li];
                    if (li > 0) xAccum += betweenGap;
                }
            }
            else
            {
                double xAccum = xStart;
                for (int li = 0; li < lines.Count; li++)
                {
                    lineOffsetsX[li] = xAccum;
                    xAccum += lineSizes[li] + perLineExtraWidth[li];
                    if (li < lines.Count - 1) xAccum += betweenGap;
                }
            }
        }

        for (int li = 0; li < lines.Count; li++)
        {
            var idxs = lines[li];
            if (isRowDirection)
            {
                // Establish base widths using flex-basis when provided
                var baseWidths = new double[idxs.Count];
                for (int k = 0; k < idxs.Count; k++)
                {
                    int i = idxs[k];
                    if (measured[i].style.FlexBasis.IsAuto)
                    {
                        baseWidths[k] = measured[i].size.Width;
                    }
                    else
                    {
                        var v = measured[i].style.FlexBasis.Value!.Value;
                        baseWidths[k] = v.IsPercent ? v.Resolve(containerSize.Width) : v.Pixels;
                    }
                }
                double lineItemsWidth = baseWidths.Sum();
                double gapCount = Math.Max(0, idxs.Count - 1);
                double baseGap = containerStyle.ColumnGap.Pixels;
                double startX = 0;
                double dynamicGap = baseGap;
                double totalBaseWidth = lineItemsWidth + gapCount * baseGap;

                switch (containerStyle.JustifyContent)
                {
                    case Andy.Tui.Style.JustifyContent.FlexStart:
                        startX = 0;
                        dynamicGap = baseGap;
                        break;
                    case Andy.Tui.Style.JustifyContent.Center:
                        startX = Math.Max(0, (containerSize.Width - totalBaseWidth) / 2.0);
                        dynamicGap = baseGap;
                        break;
                    case Andy.Tui.Style.JustifyContent.FlexEnd:
                        startX = Math.Max(0, containerSize.Width - totalBaseWidth);
                        dynamicGap = baseGap;
                        break;
                    case Andy.Tui.Style.JustifyContent.SpaceBetween:
                        if (gapCount > 0)
                            dynamicGap = Math.Max(0, (containerSize.Width - lineItemsWidth) / gapCount);
                        else
                            dynamicGap = 0;
                        startX = 0;
                        break;
                    case Andy.Tui.Style.JustifyContent.SpaceAround:
                        {
                            double spacing = Math.Max(0, (containerSize.Width - lineItemsWidth) / (idxs.Count));
                            startX = spacing / 2.0;
                            dynamicGap = spacing;
                            break;
                        }
                    case Andy.Tui.Style.JustifyContent.SpaceEvenly:
                        {
                            double spacing = Math.Max(0, (containerSize.Width - lineItemsWidth) / (idxs.Count + 1));
                            startX = spacing;
                            dynamicGap = spacing;
                            break;
                        }
                    default:
                        startX = 0;
                        dynamicGap = baseGap;
                        break;
                }

                // Flex grow/shrink distribution along main axis (row) with simple freeze/reflow on constraints
                var arrangedWidths = new double[idxs.Count];
                for (int k = 0; k < idxs.Count; k++) arrangedWidths[k] = baseWidths[k];
                double freeSpace = containerSize.Width - totalBaseWidth;
                double[] minW = new double[idxs.Count];
                double[] maxW = new double[idxs.Count];
                var containerWidthLocal = containerSize.Width;
                for (int k = 0; k < idxs.Count; k++)
                {
                    var st = measured[idxs[k]].style;
                    minW[k] = st.MinWidth.IsAuto ? double.NegativeInfinity : (st.MinWidth.Value!.Value.IsPercent ? st.MinWidth.Value.Value.Resolve(containerWidthLocal) : st.MinWidth.Value.Value.Pixels);
                    maxW[k] = st.MaxWidth.IsAuto ? double.PositiveInfinity : (st.MaxWidth.Value!.Value.IsPercent ? st.MaxWidth.Value.Value.Resolve(containerWidthLocal) : st.MaxWidth.Value.Value.Pixels);
                }
                double SumArranged() { double s = 0; for (int k = 0; k < idxs.Count; k++) s += arrangedWidths[k]; return s; }
                void DistributeGrow()
                {
                    for (int iter = 0; iter < idxs.Count; iter++)
                    {
                        double growSum = 0;
                        var canGrow = new bool[idxs.Count];
                        for (int k = 0; k < idxs.Count; k++)
                        {
                            double g = measured[idxs[k]].style.FlexGrow;
                            canGrow[k] = g > 0 && arrangedWidths[k] < maxW[k] - 1e-6;
                            if (canGrow[k]) growSum += g;
                        }
                        if (growSum <= 0) break;
                        for (int k = 0; k < idxs.Count; k++)
                        {
                            if (!canGrow[k]) continue;
                            double g = measured[idxs[k]].style.FlexGrow;
                            double add = freeSpace * (g / growSum);
                            arrangedWidths[k] = Math.Min(maxW[k], arrangedWidths[k] + add);
                        }
                        double used = SumArranged();
                        double baseGaps = gapCount * baseGap; // gapCount/baseGap in scope from above
                        double newFree = containerWidthLocal - (used + baseGaps);
                        if (Math.Abs(newFree - freeSpace) < 1e-6 || newFree <= 1e-6) { freeSpace = Math.Max(0, newFree); break; }
                        freeSpace = newFree;
                    }
                }
                void DistributeShrink()
                {
                    for (int iter = 0; iter < idxs.Count; iter++)
                    {
                        double weightSum = 0;
                        var canShrink = new bool[idxs.Count];
                        for (int k = 0; k < idxs.Count; k++)
                        {
                            var st = measured[idxs[k]].style;
                            double w = st.FlexShrink * baseWidths[k];
                            canShrink[k] = w > 0 && arrangedWidths[k] > minW[k] + 1e-6;
                            if (canShrink[k]) weightSum += w;
                        }
                        if (weightSum <= 0) break;
                        double deficit = -freeSpace;
                        for (int k = 0; k < idxs.Count; k++)
                        {
                            if (!canShrink[k]) continue;
                            var st = measured[idxs[k]].style;
                            double w = st.FlexShrink * baseWidths[k];
                            double reduce = deficit * (w / weightSum);
                            arrangedWidths[k] = Math.Max(minW[k], arrangedWidths[k] - reduce);
                        }
                        double used = SumArranged();
                        double baseGaps = gapCount * baseGap;
                        double newFree = containerWidthLocal - (used + baseGaps);
                        if (Math.Abs(newFree - freeSpace) < 1e-6 || newFree >= -1e-6) { freeSpace = Math.Min(0, newFree); break; }
                        freeSpace = newFree;
                    }
                }
                if (freeSpace > 1e-6)
                {
                    DistributeGrow();
                }
                else if (freeSpace < -1e-6)
                {
                    DistributeShrink();
                }

                double x = startX;
                double lineHeight = lineSizes[li] + perLineExtraHeight[li];
                // Compute line baseline: max baseline among items in this line
                double ComputeBaselineForIndex(int idx)
                {
                    var (node, _, size) = measured[idx];
                    if (node is IBaselineProvider bp)
                    {
                        return bp.GetFirstBaseline(in size);
                    }
                    // Fallback: use bottom (textless block) as baseline
                    return size.Height;
                }
                double[] baselines = new double[idxs.Count];
                double lineBaseline = 0;
                for (int k = 0; k < idxs.Count; k++)
                {
                    baselines[k] = ComputeBaselineForIndex(idxs[k]);
                    if (baselines[k] > lineBaseline) lineBaseline = baselines[k];
                }
                for (int k = 0; k < idxs.Count; k++)
                {
                    int i = idxs[k];
                    var sz = measured[i].size;
                    // Align-self overrides align-items when not Auto
                    var self = measured[i].style.AlignSelf;
                    double baseLineTop = (lines.Count == 1 ? singleLineCrossBaseOffset : lineOffsetsY[li]);
                    double itemY = baseLineTop;
                    if (self != Andy.Tui.Style.AlignSelf.Auto)
                    {
                        switch (self)
                        {
                            case Andy.Tui.Style.AlignSelf.Center:
                                itemY = Math.Max(0, baseLineTop + (lineHeight - sz.Height) / 2.0);
                                break;
                            case Andy.Tui.Style.AlignSelf.FlexEnd:
                                itemY = Math.Max(0, baseLineTop + (lineHeight - sz.Height));
                                break;
                            case Andy.Tui.Style.AlignSelf.Baseline:
                                // Align typographic baselines when available
                                double selfBaseline = baselines[k];
                                itemY = Math.Max(0, baseLineTop + (lineBaseline - selfBaseline));
                                break;
                            case Andy.Tui.Style.AlignSelf.Stretch:
                                itemY = baseLineTop;
                                break;
                            case Andy.Tui.Style.AlignSelf.FlexStart:
                            default:
                                itemY = baseLineTop;
                                break;
                        }
                    }
                    double itemH = sz.Height;

                    if ((self == Andy.Tui.Style.AlignSelf.Stretch && isRowDirection) || (self == Andy.Tui.Style.AlignSelf.Auto && containerStyle.AlignItems == Andy.Tui.Style.AlignItems.Stretch))
                    {
                        itemH = Math.Max(itemH, lineHeight);
                    }

                    // If align-items is Baseline and self is Auto, align baselines
                    if (self == Andy.Tui.Style.AlignSelf.Auto && containerStyle.AlignItems == Andy.Tui.Style.AlignItems.Baseline)
                    {
                        double selfBaseline = baselines[k];
                        itemY = Math.Max(0, baseLineTop + (lineBaseline - selfBaseline));
                    }

                    // Final width from arranged (already clamped/grown)
                    double usedWidth = arrangedWidths[k];
                    var stItem = measured[i].style;
                    // heights clamping below
                    if (!stItem.MinHeight.IsAuto)
                    {
                        var v = stItem.MinHeight.Value!.Value;
                        var minH = v.IsPercent ? v.Resolve(containerSize.Height) : v.Pixels;
                        itemH = Math.Max(itemH, minH);
                    }
                    if (!stItem.MaxHeight.IsAuto)
                    {
                        var v = stItem.MaxHeight.Value!.Value;
                        var maxH = v.IsPercent ? v.Resolve(containerSize.Height) : v.Pixels;
                        itemH = Math.Min(itemH, maxH);
                    }

                    var rect = new Rect(x, itemY, usedWidth, itemH);
                    rect = ClipIfNeeded(rect);
                    measured[i].node.Arrange(rect);
                    x += usedWidth;
                    if (k < idxs.Count - 1) x += dynamicGap;
                }
            }
            else
            {
                // Column direction: multiple columns possible
                double columnWidth = lineSizes[li] + perLineExtraWidth[li];
                double x = (lines.Count == 1 ? singleLineCrossBaseOffset : lineOffsetsX[li]);

                // Vertical main axis distribution via justify-content using RowGap
                double itemsHeight = idxs.Sum(i => measured[i].size.Height);
                double gapCount = Math.Max(0, idxs.Count - 1);
                double baseGap = containerStyle.RowGap.Pixels;
                double startY = 0;
                double dynamicGap = baseGap;
                double totalBaseHeight = itemsHeight + gapCount * baseGap;
                switch (containerStyle.JustifyContent)
                {
                    case Andy.Tui.Style.JustifyContent.FlexStart:
                        startY = 0; dynamicGap = baseGap; break;
                    case Andy.Tui.Style.JustifyContent.Center:
                        startY = Math.Max(0, (containerSize.Height - totalBaseHeight) / 2.0); dynamicGap = baseGap; break;
                    case Andy.Tui.Style.JustifyContent.FlexEnd:
                        startY = Math.Max(0, containerSize.Height - totalBaseHeight); dynamicGap = baseGap; break;
                    case Andy.Tui.Style.JustifyContent.SpaceBetween:
                        dynamicGap = gapCount > 0 ? Math.Max(0, (containerSize.Height - itemsHeight) / gapCount) : 0; startY = 0; break;
                    case Andy.Tui.Style.JustifyContent.SpaceAround:
                        {
                            double spacing = Math.Max(0, (containerSize.Height - itemsHeight) / (idxs.Count));
                            startY = spacing / 2.0; dynamicGap = spacing; break;
                        }
                    case Andy.Tui.Style.JustifyContent.SpaceEvenly:
                        {
                            double spacing = Math.Max(0, (containerSize.Height - itemsHeight) / (idxs.Count + 1));
                            startY = spacing; dynamicGap = spacing; break;
                        }
                    default:
                        startY = 0; dynamicGap = baseGap; break;
                }

                double y = startY;
                // Align-items horizontally within this column
                for (int k = 0; k < idxs.Count; k++)
                {
                    int i = idxs[k];
                    var sz = measured[i].size;
                    var self = measured[i].style.AlignSelf;
                    double baseLineLeft = singleLineCrossBaseOffset;
                    double itemX = x; // default flex-start within column bounds
                    if (self != Andy.Tui.Style.AlignSelf.Auto)
                    {
                        switch (self)
                        {
                            case Andy.Tui.Style.AlignSelf.Center:
                                itemX = Math.Max(0, x + (columnWidth - sz.Width) / 2.0);
                                break;
                            case Andy.Tui.Style.AlignSelf.FlexEnd:
                                itemX = Math.Max(0, x + (columnWidth - sz.Width));
                                break;
                            case Andy.Tui.Style.AlignSelf.Stretch:
                                itemX = x;
                                break;
                            case Andy.Tui.Style.AlignSelf.FlexStart:
                            case Andy.Tui.Style.AlignSelf.Baseline:
                            default:
                                itemX = x;
                                break;
                        }
                    }
                    double itemW = sz.Width;
                    if ((self == Andy.Tui.Style.AlignSelf.Stretch && !isRowDirection) || (self == Andy.Tui.Style.AlignSelf.Auto && containerStyle.AlignItems == Andy.Tui.Style.AlignItems.Stretch))
                    {
                        itemW = Math.Max(itemW, columnWidth);
                    }

                    // Clamp with min/max
                    var stItem = measured[i].style;
                    if (!stItem.MinWidth.IsAuto)
                    {
                        var v = stItem.MinWidth.Value!.Value;
                        var minW = v.IsPercent ? v.Resolve(containerSize.Width) : v.Pixels;
                        itemW = Math.Max(itemW, minW);
                    }
                    if (!stItem.MaxWidth.IsAuto)
                    {
                        var v = stItem.MaxWidth.Value!.Value;
                        var maxW = v.IsPercent ? v.Resolve(containerSize.Width) : v.Pixels;
                        itemW = Math.Min(itemW, maxW);
                    }
                    double itemH = sz.Height;
                    if (!stItem.MinHeight.IsAuto)
                    {
                        var vH = stItem.MinHeight.Value!.Value;
                        var minH = vH.IsPercent ? vH.Resolve(containerSize.Height) : vH.Pixels;
                        itemH = Math.Max(itemH, minH);
                    }
                    if (!stItem.MaxHeight.IsAuto)
                    {
                        var vH = stItem.MaxHeight.Value!.Value;
                        var maxH = vH.IsPercent ? vH.Resolve(containerSize.Height) : vH.Pixels;
                        itemH = Math.Min(itemH, maxH);
                    }

                    var rect = new Rect(itemX, y, itemW, itemH);
                    rect = ClipIfNeeded(rect);
                    measured[i].node.Arrange(rect);
                    y += sz.Height;
                    if (k < idxs.Count - 1) y += dynamicGap;
                }
            }
        }
    }
}