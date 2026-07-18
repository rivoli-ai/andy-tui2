using System.Collections.Generic;

namespace Andy.Tui.Compositor;

/// <summary>
/// A description of how to turn the previously displayed frame into the next
/// one. It couples an optional vertical terminal scroll with the rows that must
/// still be painted afterwards, so a scroll optimisation can never be applied
/// without also emitting the matching scroll operation.
/// </summary>
/// <param name="ScrollDy">
/// The number of rows the existing on-screen content must be shifted before
/// painting. <c>0</c> means no scroll. A positive value shifts content down by
/// that many rows (new rows exposed at the top); a negative value shifts content
/// up (new rows exposed at the bottom). This mirrors the terminal SD (CSI T) and
/// SU (CSI S) operations respectively.
/// </param>
/// <param name="Dirty">
/// Rows to repaint <em>after</em> the scroll has been applied. When
/// <see cref="ScrollDy"/> is non-zero these cover exactly the newly exposed rows;
/// otherwise they are the full per-row dirty runs.
/// </param>
public readonly record struct DamagePlan(int ScrollDy, IReadOnlyList<DirtyRect> Dirty);
