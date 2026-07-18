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
/// <para>
/// When built with a key function (see <see cref="EnsureBuilt(int, System.Func{int, string}, System.Func{int, int})"/>)
/// the index also captures the ordered sequence of stable item keys and rebuilds automatically
/// whenever that sequence changes — an insert, remove, or a same-count <em>reorder</em>. This keeps
/// the indexed layout byte-for-byte identical to the linear walk, which re-derived positions from
/// the current order every frame, so a reorder no longer requires an explicit
/// <see cref="Invalidate"/> call. Rebuilding only recomputes prefix sums (O(n) arithmetic); it does
/// not re-measure items whose key is unchanged provided the supplied measure callback is itself
/// keyed (as the widget's per-key measure cache is), so reused keys are served from that cache.
/// </para>
/// Callers must still invalidate the index for changes the key sequence cannot reveal — an
/// <em>in-place</em> resize under the same key, or an available-width change that affects wrapping —
/// via <see cref="Invalidate"/> so the next layout pass rebuilds it.
/// </remarks>
public sealed class CumulativeHeightIndex
{
    // _prefix[i] = sum of heights of items [0, i). Length is _count + 1, so _prefix[_count]
    // is the total content height. The backing array may be larger than needed and is reused.
    private long[] _prefix = new long[] { 0 };
    // Ordered stable keys captured on the last keyed build; used to detect a reorder/insert/remove
    // that leaves the count unchanged. Null/ignored for count-only builds. Reused across rebuilds.
    private string[]? _keys;
    private bool _keysValid;
    private int _count;
    private bool _valid;

    /// <summary>True when the prefix sums reflect the current heights and can be queried.</summary>
    public bool IsValid => _valid;

    /// <summary>Number of items the index currently covers.</summary>
    public int Count => _count;

    /// <summary>Total content height in rows across all items (0 when not built).</summary>
    public long TotalHeight => _valid ? _prefix[_count] : 0;

    /// <summary>
    /// Mark the index stale so the next <see cref="EnsureBuilt(int, System.Func{int, int})"/> rebuilds it. Call on item
    /// add/remove, in-place resize, or a width change that affects measured heights.
    /// </summary>
    public void Invalidate() => _valid = false;

    /// <summary>
    /// Build the prefix sums for <paramref name="count"/> items using <paramref name="measure"/>
    /// if the index is stale or the item count changed. A no-op when already valid for the count,
    /// so it is safe (and cheap) to call every frame — measurement only happens on a rebuild.
    /// This count-only overload does not detect a same-count reorder; prefer
    /// <see cref="EnsureBuilt(int, System.Func{int, string}, System.Func{int, int})"/> for
    /// content that can be reordered without an explicit <see cref="Invalidate"/>.
    /// </summary>
    public void EnsureBuilt(int count, Func<int, int> measure) => EnsureBuilt(count, null, measure);

    /// <summary>
    /// Build the prefix sums for <paramref name="count"/> items, rebuilding when the index is stale,
    /// the item count changed, or — when <paramref name="keyAt"/> is supplied — the ordered sequence
    /// of stable keys changed (an insert, remove, or same-count reorder). Detecting a reorder keeps
    /// the indexed layout consistent with the linear walk without an explicit <see cref="Invalidate"/>.
    /// A no-op when nothing changed, so it is safe (and cheap) to call every frame; measurement only
    /// happens on a rebuild, and a rebuild whose keys resolve through a per-key measure cache does not
    /// re-measure unchanged items.
    /// </summary>
    /// <param name="count">Number of items to index.</param>
    /// <param name="keyAt">
    /// Optional stable key per index (the same key a measure cache uses). When provided, a change in
    /// the ordered key sequence forces a rebuild even if the count is unchanged. Reading each key is
    /// O(n) per frame; only measurement is avoided when nothing changed.
    /// </param>
    /// <param name="measure">Per-index row height; values are clamped to a minimum of 1.</param>
    public void EnsureBuilt(int count, Func<int, string>? keyAt, Func<int, int> measure)
    {
        if (_valid && _count == count && KeysUnchanged(count, keyAt)) return;

        _count = count;
        if (_prefix.Length < count + 1)
            _prefix = new long[count + 1];

        _prefix[0] = 0;
        if (keyAt is not null)
        {
            if (_keys is null || _keys.Length < count)
                _keys = new string[count];
            for (int i = 0; i < count; i++)
            {
                _keys[i] = keyAt(i);
                _prefix[i + 1] = _prefix[i] + Math.Max(1, measure(i));
            }
            _keysValid = true;
        }
        else
        {
            // Count-only build: no key information to compare against on later frames.
            _keysValid = false;
            for (int i = 0; i < count; i++)
                _prefix[i + 1] = _prefix[i] + Math.Max(1, measure(i));
        }

        _valid = true;
    }

    // True when a keyed build's captured key sequence still matches the current one (or when the
    // caller supplied no key function, in which case count equality is the only signal we have).
    private bool KeysUnchanged(int count, Func<int, string>? keyAt)
    {
        if (keyAt is null) return true;
        if (!_keysValid || _keys is null) return false;
        for (int i = 0; i < count; i++)
            if (!string.Equals(_keys[i], keyAt(i), StringComparison.Ordinal))
                return false;
        return true;
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
