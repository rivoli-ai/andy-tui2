using System.Runtime.CompilerServices;

namespace Andy.Tui.Style;

/// <summary>
/// Caches computed styles keyed by the full set of inputs the cascade depends on:
/// node identity, pseudo-class state, the stylesheet set, the parent style, and the
/// environment signature. A change in any dependency produces a cache miss and a fresh
/// computation, so a cached result can never be stale with respect to a changed
/// stylesheet, parent, theme, pseudo-state, or environment.
/// </summary>
/// <remarks>
/// <para>
/// <b>Identity.</b> Nodes are compared by reference identity, never by
/// <see cref="object.GetHashCode"/> alone. Two distinct nodes that happen to hash to the
/// same bucket cannot cross-contaminate: equality is decided by reference, so a hash
/// collision only costs a probe, never a wrong answer.
/// </para>
/// <para>
/// <b>Bounded growth.</b> The cache holds at most <see cref="Capacity"/> entries and
/// evicts the least-recently-used entry when that bound is exceeded. Repeated resize or
/// theme changes therefore cannot grow the cache without bound.
/// </para>
/// <para>
/// <b>Thread-safety.</b> All public members are safe for concurrent use; a single internal
/// lock guards the cache and its recency list. Callers do not need external synchronization.
/// </para>
/// </remarks>
public sealed class StyleCache
{
    /// <summary>Default maximum number of cached entries.</summary>
    public const int DefaultCapacity = 512;

    private readonly object _gate = new();
    private readonly Dictionary<CacheKey, LinkedListNode<Entry>> _cache;
    // Most-recently-used at the front, least-recently-used at the back.
    private readonly LinkedList<Entry> _lru = new();
    private readonly StyleResolver _resolver = new();
    private readonly int _capacity;

    /// <summary>Creates a cache with the <see cref="DefaultCapacity"/>.</summary>
    public StyleCache() : this(DefaultCapacity) { }

    /// <summary>Creates a cache bounded to <paramref name="maxEntries"/> entries.</summary>
    /// <param name="maxEntries">Maximum number of entries before least-recently-used eviction. Must be positive.</param>
    public StyleCache(int maxEntries)
    {
        if (maxEntries <= 0) throw new ArgumentOutOfRangeException(nameof(maxEntries), maxEntries, "Capacity must be positive.");
        _capacity = maxEntries;
        _cache = new Dictionary<CacheKey, LinkedListNode<Entry>>(new KeyComparer(collapseHashes: false));
    }

    // Test-only constructor: forces every key to the same hash bucket so that collision
    // handling (reference-identity equality) can be exercised deterministically.
    internal StyleCache(int maxEntries, bool forceHashCollisionsForTesting)
    {
        if (maxEntries <= 0) throw new ArgumentOutOfRangeException(nameof(maxEntries), maxEntries, "Capacity must be positive.");
        _capacity = maxEntries;
        _cache = new Dictionary<CacheKey, LinkedListNode<Entry>>(new KeyComparer(collapseHashes: forceHashCollisionsForTesting));
    }

    /// <summary>Maximum number of entries this cache retains before evicting.</summary>
    public int Capacity => _capacity;

    /// <summary>Current number of cached entries. Primarily useful for tests and diagnostics.</summary>
    public int Count
    {
        get { lock (_gate) { return _cache.Count; } }
    }

    /// <summary>
    /// Returns the computed style for <paramref name="node"/>, using a cached result only
    /// when every style dependency (node identity, pseudo-state, stylesheet set, parent, and
    /// environment) matches the cached entry exactly.
    /// </summary>
    public ResolvedStyle GetComputedStyle(Node node, IEnumerable<Stylesheet> stylesheets, EnvironmentContext env, ResolvedStyle? parent = null)
    {
        var sheets = stylesheets as Stylesheet[] ?? stylesheets.ToArray();
        var key = new CacheKey(node, EnvSignature.From(env), PseudoSignature.From(node), sheets, parent);

        lock (_gate)
        {
            if (_cache.TryGetValue(key, out var existing))
            {
                // Touch: promote to most-recently-used.
                _lru.Remove(existing);
                _lru.AddFirst(existing);
                return existing.Value.Style;
            }
        }

        // Compute outside the lock; the resolver is pure and does not touch cache state.
        var style = _resolver.Compute(node, sheets, env, parent);
        bool mediaDependent = IsMediaDependent(node, sheets);
        var entry = new Entry(key, style, mediaDependent, sheets, node);

        lock (_gate)
        {
            // Another thread may have inserted the same key while we computed; prefer the
            // existing entry to keep a single canonical result per key.
            if (_cache.TryGetValue(key, out var raced))
            {
                _lru.Remove(raced);
                _lru.AddFirst(raced);
                return raced.Value.Style;
            }

            var listNode = _lru.AddFirst(entry);
            _cache[key] = listNode;
            EvictIfNeeded();
            return style;
        }
    }

    private void EvictIfNeeded()
    {
        while (_cache.Count > _capacity)
        {
            var oldest = _lru.Last;
            if (oldest is null) break;
            _lru.RemoveLast();
            _cache.Remove(oldest.Value.Key);
        }
    }

    /// <summary>
    /// Coarse-grained invalidation for a viewport/environment transition: drops cached
    /// entries whose result could change because a media rule that matches the entry's node
    /// flips applicability between the old and new environment. Entries for other environment
    /// signatures remain valid because the environment is part of the cache key.
    /// </summary>
    public void InvalidateForEnvChange(EnvironmentContext oldEnv, EnvironmentContext newEnv)
    {
        if (EnvSignature.From(oldEnv).Equals(EnvSignature.From(newEnv))) return;

        lock (_gate)
        {
            var toRemove = new List<LinkedListNode<Entry>>();
            foreach (var kvp in _cache)
            {
                var entry = kvp.Value.Value;
                if (!entry.MediaDependent) continue;
                foreach (var sheet in entry.Stylesheets)
                {
                    foreach (var rule in sheet.Rules)
                    {
                        if (rule.MediaCondition is null) continue;
                        bool before = rule.MediaCondition(oldEnv);
                        bool after = rule.MediaCondition(newEnv);
                        if (before == after) continue;
                        if (rule.Selector.Matches(entry.Node))
                        {
                            toRemove.Add(kvp.Value);
                            goto NextEntry;
                        }
                    }
                }
            NextEntry:;
            }

            foreach (var listNode in toRemove)
            {
                _cache.Remove(listNode.Value.Key);
                _lru.Remove(listNode);
            }
        }
    }

    /// <summary>Removes every cached entry.</summary>
    public void Clear()
    {
        lock (_gate)
        {
            _cache.Clear();
            _lru.Clear();
        }
    }

    private readonly record struct EnvSignature(double W, double H, bool Terminal, bool Reduced)
    {
        public static EnvSignature From(EnvironmentContext env)
            => new(env.ViewportWidth, env.ViewportHeight, env.IsTerminal, env.PrefersReducedMotion);
    }

    private readonly record struct PseudoSignature(bool Hover, bool Focus, bool Active, bool Disabled)
    {
        public static PseudoSignature From(Node node)
            => new(node.IsHover, node.IsFocus, node.IsActive, node.IsDisabled);
    }

    private static bool IsMediaDependent(Node node, IEnumerable<Stylesheet> stylesheets)
    {
        foreach (var sheet in stylesheets)
        {
            foreach (var rule in sheet.Rules)
            {
                if (rule.MediaCondition is null) continue;
                if (rule.Selector.Matches(node)) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Composite cache key. Equality uses reference identity for the node and every
    /// stylesheet, value equality for the environment/pseudo signatures, and value equality
    /// for the parent style. This makes the key collision-safe: a shared hash bucket never
    /// implies equal keys.
    /// </summary>
    private readonly struct CacheKey : IEquatable<CacheKey>
    {
        public readonly Node Node;
        public readonly EnvSignature Env;
        public readonly PseudoSignature Pseudo;
        public readonly Stylesheet[] Sheets;
        public readonly ResolvedStyle? Parent;

        public CacheKey(Node node, EnvSignature env, PseudoSignature pseudo, Stylesheet[] sheets, ResolvedStyle? parent)
        {
            Node = node;
            Env = env;
            Pseudo = pseudo;
            Sheets = sheets;
            Parent = parent;
        }

        public bool Equals(CacheKey other)
        {
            if (!ReferenceEquals(Node, other.Node)) return false;
            if (!Env.Equals(other.Env)) return false;
            if (!Pseudo.Equals(other.Pseudo)) return false;
            if (!Nullable.Equals(Parent, other.Parent)) return false;
            if (Sheets.Length != other.Sheets.Length) return false;
            for (int i = 0; i < Sheets.Length; i++)
            {
                if (!ReferenceEquals(Sheets[i], other.Sheets[i])) return false;
            }
            return true;
        }

        public override bool Equals(object? obj) => obj is CacheKey k && Equals(k);

        public override int GetHashCode()
        {
            var hc = new HashCode();
            hc.Add(RuntimeHelpers.GetHashCode(Node));
            hc.Add(Env);
            hc.Add(Pseudo);
            hc.Add(Parent);
            foreach (var sheet in Sheets)
            {
                hc.Add(RuntimeHelpers.GetHashCode(sheet));
            }
            return hc.ToHashCode();
        }
    }

    private sealed class KeyComparer : IEqualityComparer<CacheKey>
    {
        private readonly bool _collapseHashes;
        public KeyComparer(bool collapseHashes) => _collapseHashes = collapseHashes;
        public bool Equals(CacheKey x, CacheKey y) => x.Equals(y);
        public int GetHashCode(CacheKey obj) => _collapseHashes ? 0 : obj.GetHashCode();
    }

    private readonly record struct Entry(CacheKey Key, ResolvedStyle Style, bool MediaDependent, Stylesheet[] Stylesheets, Node Node);
}
