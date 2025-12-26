using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TaskbarGroupTool.Services
{
    public class SearchCacheService
    {
        private readonly ConcurrentDictionary<string, CachedSearchResult> _cache;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);
        
        public SearchCacheService()
        {
            _cache = new ConcurrentDictionary<string, CachedSearchResult>();
            
            // Start cleanup timer
            var cleanupTimer = new System.Timers.Timer(TimeSpan.FromMinutes(1).TotalMilliseconds);
            cleanupTimer.Elapsed += CleanupExpiredEntries;
            cleanupTimer.AutoReset = true;
            cleanupTimer.Start();
        }
        
        public async Task<List<SearchResult>> GetCachedSearchAsync(string searchTerm, Func<Task<List<SearchResult>>> searchFunction)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<SearchResult>();
                
            var cacheKey = searchTerm.ToLowerInvariant();
            
            // Check cache first
            if (_cache.TryGetValue(cacheKey, out var cached) && !cached.IsExpired)
            {
                return cached.Results;
            }
            
            // Perform search if not in cache or expired
            var results = await searchFunction();
            
            // Cache the results
            _cache.TryAdd(cacheKey, new CachedSearchResult
            {
                Results = results,
                CreatedAt = DateTime.UtcNow
            });
            
            return results;
        }
        
        public void InvalidateCache()
        {
            _cache.Clear();
        }
        
        public void InvalidateCache(string searchTerm)
        {
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var cacheKey = searchTerm.ToLowerInvariant();
                _cache.TryRemove(cacheKey, out _);
            }
        }
        
        private void CleanupExpiredEntries(object sender, System.Timers.ElapsedEventArgs e)
        {
            var expiredKeys = new List<string>();
            
            foreach (var kvp in _cache)
            {
                if (kvp.Value.IsExpired)
                {
                    expiredKeys.Add(kvp.Key);
                }
            }
            
            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }
        
        private class CachedSearchResult
        {
            public List<SearchResult> Results { get; set; }
            public DateTime CreatedAt { get; set; }
            
            public bool IsExpired => DateTime.UtcNow - CreatedAt > TimeSpan.FromMinutes(5);
        }
    }
    

}