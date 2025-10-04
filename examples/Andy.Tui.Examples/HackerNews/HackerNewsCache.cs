using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Andy.Tui.Examples.HackerNews;

public sealed class HackerNewsCache
{
    private readonly ConcurrentDictionary<string, CacheEntry<List<int>>> _storyListCache = new();
    private readonly ConcurrentDictionary<int, CacheEntry<HNItem>> _itemCache = new();
    private readonly LinkedList<int> _lruList = new();
    private readonly object _lruLock = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);
    private const long MaxCacheSizeBytes = 20 * 1024 * 1024; // 20MB
    private long _currentCacheSize = 0;
    private readonly string _cacheFilePath;

    public HackerNewsCache()
    {
        // Use temp directory for cache file
        var cacheDir = Path.Combine(Path.GetTempPath(), "andy-tui-hn-cache");
        Directory.CreateDirectory(cacheDir);
        _cacheFilePath = Path.Combine(cacheDir, "cache.json");
        LoadFromDisk();
    }

    public bool TryGetStoryList(string key, out List<int>? value)
    {
        if (_storyListCache.TryGetValue(key, out var entry))
        {
            if (DateTime.UtcNow - entry.Timestamp < _cacheExpiration)
            {
                value = entry.Value;
                return true;
            }
            else
            {
                // Remove expired entry
                _storyListCache.TryRemove(key, out _);
            }
        }

        value = null;
        return false;
    }

    public void SetStoryList(string key, List<int> value)
    {
        _storyListCache[key] = new CacheEntry<List<int>>(value, DateTime.UtcNow);
    }

    public bool TryGetItem(int id, out HNItem? value)
    {
        if (_itemCache.TryGetValue(id, out var entry))
        {
            if (DateTime.UtcNow - entry.Timestamp < _cacheExpiration)
            {
                value = entry.Value;

                // Update LRU - move to front
                lock (_lruLock)
                {
                    if (entry.LruNode != null)
                    {
                        _lruList.Remove(entry.LruNode);
                        _lruList.AddFirst(entry.LruNode);
                    }
                }

                return true;
            }
            else
            {
                // Remove expired entry
                RemoveItem(id);
            }
        }

        value = null;
        return false;
    }

    public void SetItem(int id, HNItem value)
    {
        // Estimate size of the item (rough approximation)
        long itemSize = EstimateSize(value);

        lock (_lruLock)
        {
            // Evict old items if needed to stay under size limit
            while (_currentCacheSize + itemSize > MaxCacheSizeBytes && _lruList.Count > 0)
            {
                var oldestId = _lruList.Last!.Value;
                RemoveItemInternal(oldestId);
            }

            // Add new item
            var lruNode = _lruList.AddFirst(id);
            _itemCache[id] = new CacheEntry<HNItem>(value, DateTime.UtcNow, lruNode);
            _currentCacheSize += itemSize;
        }
    }

    private void RemoveItem(int id)
    {
        lock (_lruLock)
        {
            RemoveItemInternal(id);
        }
    }

    private void RemoveItemInternal(int id)
    {
        if (_itemCache.TryRemove(id, out var entry))
        {
            if (entry.LruNode != null)
            {
                _lruList.Remove(entry.LruNode);
            }
            _currentCacheSize -= EstimateSize(entry.Value);
        }
    }

    private long EstimateSize(HNItem item)
    {
        // Rough size estimation
        long size = 100; // Base object overhead

        if (item.Title != null)
            size += item.Title.Length * 2; // UTF-16

        if (item.Text != null)
            size += item.Text.Length * 2;

        if (item.By != null)
            size += item.By.Length * 2;

        if (item.Url != null)
            size += item.Url.Length * 2;

        if (item.Kids != null)
            size += item.Kids.Count * 4;

        return size;
    }

    public void Clear()
    {
        lock (_lruLock)
        {
            _storyListCache.Clear();
            _itemCache.Clear();
            _lruList.Clear();
            _currentCacheSize = 0;
        }
    }

    public (int Items, long SizeBytes, long MaxBytes) GetStats()
    {
        lock (_lruLock)
        {
            return (_itemCache.Count, _currentCacheSize, MaxCacheSizeBytes);
        }
    }

    public void SaveToDisk()
    {
        try
        {
            lock (_lruLock)
            {
                var data = new PersistedCache
                {
                    StoryLists = _storyListCache.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new PersistedCacheEntry<List<int>>
                        {
                            Value = kvp.Value.Value,
                            Timestamp = kvp.Value.Timestamp
                        }),
                    Items = _itemCache.ToDictionary(
                        kvp => kvp.Key,
                        kvp => new PersistedCacheEntry<HNItem>
                        {
                            Value = kvp.Value.Value,
                            Timestamp = kvp.Value.Timestamp
                        })
                };

                var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                File.WriteAllText(_cacheFilePath, json);
            }
        }
        catch
        {
            // Silently fail - cache persistence is not critical
        }
    }

    private void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(_cacheFilePath))
                return;

            var json = File.ReadAllText(_cacheFilePath);
            var data = JsonSerializer.Deserialize<PersistedCache>(json);

            if (data == null)
                return;

            lock (_lruLock)
            {
                // Load story lists
                foreach (var kvp in data.StoryLists ?? new())
                {
                    if (DateTime.UtcNow - kvp.Value.Timestamp < _cacheExpiration)
                    {
                        _storyListCache[kvp.Key] = new CacheEntry<List<int>>(kvp.Value.Value, kvp.Value.Timestamp);
                    }
                }

                // Load items
                foreach (var kvp in data.Items ?? new())
                {
                    if (DateTime.UtcNow - kvp.Value.Timestamp < _cacheExpiration)
                    {
                        var lruNode = _lruList.AddLast(kvp.Key);
                        _itemCache[kvp.Key] = new CacheEntry<HNItem>(kvp.Value.Value, kvp.Value.Timestamp, lruNode);
                        _currentCacheSize += EstimateSize(kvp.Value.Value);
                    }
                }

                // Enforce size limit after loading
                while (_currentCacheSize > MaxCacheSizeBytes && _lruList.Count > 0)
                {
                    var oldestId = _lruList.First!.Value;
                    RemoveItemInternal(oldestId);
                }
            }
        }
        catch
        {
            // Silently fail - start with empty cache if load fails
        }
    }

    private sealed class CacheEntry<T>
    {
        public T Value { get; }
        public DateTime Timestamp { get; }
        public LinkedListNode<int>? LruNode { get; }

        public CacheEntry(T value, DateTime timestamp, LinkedListNode<int>? lruNode = null)
        {
            Value = value;
            Timestamp = timestamp;
            LruNode = lruNode;
        }
    }

    // Serialization classes for disk persistence
    private sealed class PersistedCache
    {
        public Dictionary<string, PersistedCacheEntry<List<int>>> StoryLists { get; set; } = new();
        public Dictionary<int, PersistedCacheEntry<HNItem>> Items { get; set; } = new();
    }

    private sealed class PersistedCacheEntry<T>
    {
        public T Value { get; set; } = default!;
        public DateTime Timestamp { get; set; }
    }
}
