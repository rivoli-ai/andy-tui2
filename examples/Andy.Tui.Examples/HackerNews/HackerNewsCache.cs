using System.Collections.Concurrent;
using System.Text.Json;

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
}
