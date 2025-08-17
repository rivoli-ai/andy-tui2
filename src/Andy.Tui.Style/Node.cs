using System.Collections.ObjectModel;

namespace Andy.Tui.Style;

/// <summary>
/// Simple Node model for selector matching and style resolution.
/// </summary>
public sealed class Node
{
    public string Type { get; }
    public string? Id { get; }
    public IReadOnlyCollection<string> Classes { get; }
    // Pseudo-class state (wired externally by UI state machine)
    public bool IsHover { get; init; }
    public bool IsFocus { get; init; }
    public bool IsActive { get; init; }
    public bool IsDisabled { get; init; }

    public Node(string type, string? id = null, IEnumerable<string>? classes = null)
    {
        Type = type;
        Id = id;
        Classes = new ReadOnlyCollection<string>((classes ?? Array.Empty<string>()).ToArray());
    }
}