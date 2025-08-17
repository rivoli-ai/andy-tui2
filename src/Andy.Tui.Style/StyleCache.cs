namespace Andy.Tui.Style;

/// <summary>
/// Very small style cache with coarse invalidation for environment changes.
/// Clears all cached entries when environment changes. Not thread-safe.
/// </summary>
public sealed class StyleCache
{
    private readonly Dictionary<(int NodeId, EnvSignature Env), Entry> _cache = new();
    private readonly StyleResolver _resolver = new();

    public ResolvedStyle GetComputedStyle(Node node, IEnumerable<Stylesheet> stylesheets, EnvironmentContext env, ResolvedStyle? parent = null)
    {
        var key = (NodeId: node.GetHashCode(), Env: EnvSignature.From(env));
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached.Style;
        }
        var sheets = stylesheets as Stylesheet[] ?? stylesheets.ToArray();
        var style = _resolver.Compute(node, sheets, env, parent);
        bool mediaDependent = IsMediaDependent(node, sheets);
        _cache[key] = new Entry(style, mediaDependent, sheets, node);
        return style;
    }

    /// <summary>
    /// Coarse-grained invalidation: clears the entire cache on any relevant environment change.
    /// </summary>
    public void InvalidateForEnvChange(EnvironmentContext oldEnv, EnvironmentContext newEnv)
    {
        if (EnvSignature.From(oldEnv).Equals(EnvSignature.From(newEnv))) return;
        // Remove only entries where at least one rule changes applicability AND selector matches node
        var keysToRemove = new List<(int NodeId, EnvSignature Env)>();
        foreach (var kvp in _cache)
        {
            var entry = kvp.Value;
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
                        keysToRemove.Add(kvp.Key);
                        goto NextEntry;
                    }
                }
            }
        NextEntry:;
        }
        foreach (var k in keysToRemove) _cache.Remove(k);
    }

    private readonly record struct EnvSignature(double W, double H, bool Terminal, bool Reduced)
    {
        public static EnvSignature From(EnvironmentContext env)
            => new(env.ViewportWidth, env.ViewportHeight, env.IsTerminal, env.PrefersReducedMotion);
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

    private readonly record struct Entry(ResolvedStyle Style, bool MediaDependent, Stylesheet[] Stylesheets, Node Node);
}
