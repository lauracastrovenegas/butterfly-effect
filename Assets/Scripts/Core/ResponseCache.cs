using UnityEngine;
using System.Collections.Generic;
using System;

public class ResponseCache
{
    private class CachedResponse
    {
        public AudioClip AudioClip { get; set; }
        public DateTime Timestamp { get; set; }
    }

    private readonly Dictionary<string, CachedResponse> cache = new Dictionary<string, CachedResponse>();
    private readonly int maxCacheSize = 50;
    private readonly TimeSpan cacheExpiration = TimeSpan.FromHours(1);

    public bool TryGetCachedResponse(string input, out AudioClip audioClip)
    {
        audioClip = null;
        string key = GenerateCacheKey(input);

        if (cache.TryGetValue(key, out CachedResponse cachedResponse))
        {
            if (DateTime.Now - cachedResponse.Timestamp < cacheExpiration)
            {
                audioClip = cachedResponse.AudioClip;
                return true;
            }
            else
            {
                // Remove expired cache entry
                cache.Remove(key);
            }
        }

        return false;
    }

    public void CacheResponse(string input, AudioClip audioClip)
    {
        if (string.IsNullOrEmpty(input) || audioClip == null) return;

        string key = GenerateCacheKey(input);
        
        // Manage cache size
        if (cache.Count >= maxCacheSize)
        {
            RemoveOldestEntry();
        }

        cache[key] = new CachedResponse
        {
            AudioClip = audioClip,
            Timestamp = DateTime.Now
        };
    }

    private string GenerateCacheKey(string input)
    {
        // Normalize input to create consistent cache keys
        return input.Trim().ToLowerInvariant();
    }

    private void RemoveOldestEntry()
    {
        DateTime oldestTime = DateTime.Now;
        string oldestKey = null;

        foreach (var entry in cache)
        {
            if (entry.Value.Timestamp < oldestTime)
            {
                oldestTime = entry.Value.Timestamp;
                oldestKey = entry.Key;
            }
        }

        if (oldestKey != null)
        {
            cache.Remove(oldestKey);
        }
    }

    public void ClearCache()
    {
        cache.Clear();
    }

    public void RemoveExpiredEntries()
    {
        var expiredKeys = new List<string>();
        var now = DateTime.Now;

        foreach (var entry in cache)
        {
            if (now - entry.Value.Timestamp >= cacheExpiration)
            {
                expiredKeys.Add(entry.Key);
            }
        }

        foreach (var key in expiredKeys)
        {
            cache.Remove(key);
        }
    }
}
