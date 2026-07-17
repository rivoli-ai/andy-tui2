namespace Andy.Tui.Virtualization;

/// <summary>
/// Placement of a single virtualized item, expressed in terminal rows relative to the
/// viewport's top row (row 0 == the first visible viewport row).
/// </summary>
/// <remarks>
/// <see cref="Top"/> may be negative when the item begins above the viewport — this happens
/// for before-overscan items and for the first item when the scroll offset lands partway
/// through it. Likewise <c>Top + Height</c> may exceed the viewport height for the last
/// (after-overscan / partially clipped) item. Consumers must clip to the viewport rather than
/// assume every slot fits inside it.
/// </remarks>
public readonly record struct ItemSlot(int Index, int Top, int Height);

/// <summary>
/// The resolved set of virtualized items intersecting a viewport (plus overscan), along with
/// their per-item row placements. Never contains indices outside the collection.
/// </summary>
public sealed class VirtualLayout
{
    /// <summary>An empty layout — no items intersect the viewport (e.g. an empty collection).</summary>
    public static readonly VirtualLayout Empty = new(Array.Empty<ItemSlot>(), -1, -1);

    /// <summary>Placements for every item that intersects the viewport window, in index order.</summary>
    public IReadOnlyList<ItemSlot> Slots { get; }

    /// <summary>First item index in <see cref="Slots"/>, or -1 when empty.</summary>
    public int FirstIndex { get; }

    /// <summary>Last item index in <see cref="Slots"/>, or -1 when empty.</summary>
    public int LastIndex { get; }

    /// <summary>True when no items intersect the viewport.</summary>
    public bool IsEmpty => Slots.Count == 0;

    public VirtualLayout(IReadOnlyList<ItemSlot> slots, int firstIndex, int lastIndex)
    {
        Slots = slots;
        FirstIndex = firstIndex;
        LastIndex = lastIndex;
    }
}
