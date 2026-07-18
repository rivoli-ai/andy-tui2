namespace Andy.Tui.Virtualization;

/// <summary>
/// A cumulative-height (prefix-sum) index over a variable-height virtualized collection.
/// Once built it maps a content row to the item occupying it in O(log n) via binary search,
/// letting <see cref="ViewportComputer.ComputeLayout{T}(IVirtualizedCollection{T}, int, int, OverscanPolicy, System.Func{int, int}?, CumulativeHeightIndex?, int)"/>
/// locate the first visible item without walking every item from index 0 on each frame.
/// </summary>
/// <remarks>
/// The index caches per-item heights as prefix sums, so repeated layout passes at a deep scroll
/// offset perform no re-measurement: the measure callback is invoked only while (re)building.
/// Callers must invalidate the index whenever a height could change — item add/remove (a count
/// change is detected automatically), an in-place resize, or an available-width change that
/// affects wrapping — via <see cref="Invalidate"/> so the next layout pass rebuilds it.
/// </remarks>
public sealed class CumulativeHeightIndex
{
    // _prefix[i] = sum of heights of items [0, i). Length is _count + 1, so _prefix[_count]
    // is the total content height. The backing array may be larger than needed and is reused.
    private long[] _prefix = new long[] { 0 };
    private int _count;
    private bool _valid;

    /// <summary>True when the prefix sums reflect the current heights and can be queried.</summary>
    public bool IsValid => _valid;

    /// <summary>Number of items the index currently covers.</summary>
    public int Count => _count;

    /// <summary>Total content height in rows across all items (0 when not built).</summary>
    public long TotalHeight => _valid ? _prefix[_count] : 0;

    /// <summary>
    /// Mark the index stale so the next <see cref="EnsureBuilt"/> rebuilds it. Call on item
    /// add/remove, in-place resize, or a width change that affects measured heights.
    /// </summary>
    public void Invalidate() => _valid = false;

    /// <summary>
    /// Build the prefix sums for <paramref name="count"/> items using <paramref name="measure"/>
    /// if the index is stale or the item count changed. A no-op when already valid for the count,
    /// so it is safe (and cheap) to call every frame — measurement only happens on a rebuild.
    /// </summary>
    public void EnsureBuilt(int count, Func<int, int> measure)
    {
        if (_valid && _count == count) return;

        _count = count;
        if (_prefix.Length < count + 1)
            _prefix = new long[count + 1];

        _prefix[0] = 0;
        for (int i = 0; i < count; i++)
            _prefix[i + 1] = _prefix[i] + Math.Max(1, measure(i));

        _valid = true;
    }

    /// <summary>Content-row offset of the top of item <paramref name="index"/>.</summary>
    public long TopOf(int index) => _prefix[index];

    /// <summary>Measured height (in rows, at least 1) of item <paramref name="index"/>.</summary>
    public int HeightOf(int index) => (int)(_prefix[index + 1] - _prefix[index]);

    /// <summary>
    /// Return the index of the first item whose bottom edge lies strictly below <paramref name="row"/>
    /// (i.e. the first item that intersects or follows that content row), found by binary search in
    /// O(log n). Returns 0 for a row above the content and <see cref="Count"/> when every item ends
    /// at or above the row (nothing intersects at or below it).
    /// </summary>
    public int FirstIntersecting(long row)
    {
        if (row < 0) return 0;
        if (_count == 0 || _prefix[_count] <= row) return _count;

        // Smallest j in [1, _count] with _prefix[j] > row; the item index is j - 1 because item i
        // spans [_prefix[i], _prefix[i+1]) and we want the first i whose bottom (_prefix[i+1]) > row.
        int lo = 1, hi = _count;
        while (lo < hi)
        {
            int mid = lo + (hi - lo) / 2;
            if (_prefix[mid] > row) hi = mid;
            else lo = mid + 1;
        }
        return lo - 1;
    }
}
