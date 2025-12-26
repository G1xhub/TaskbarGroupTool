using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace TaskbarGroupTool.Services
{
    public class IconCacheService
    {
        private readonly ConcurrentDictionary<string, BitmapSource> _iconCache;
        private readonly SemaphoreSlim _loadingSemaphore;
        private static readonly Lazy<IconCacheService> _instance = new Lazy<IconCacheService>(() => new IconCacheService());
        
        public static IconCacheService Instance => _instance.Value;
        
        private IconCacheService()
        {
            _iconCache = new ConcurrentDictionary<string, BitmapSource>();
            _loadingSemaphore = new SemaphoreSlim(3, 3); // Limit concurrent icon loading
        }
        
        public async Task<BitmapSource> GetIconAsync(string iconPath)
        {
            if (string.IsNullOrEmpty(iconPath) || !File.Exists(iconPath))
                return null;
                
            // Check cache first
            if (_iconCache.TryGetValue(iconPath, out var cachedIcon))
                return cachedIcon;
                
            // Load icon with semaphore limiting
            await _loadingSemaphore.WaitAsync();
            try
            {
                return await LoadIconAsync(iconPath);
            }
            finally
            {
                _loadingSemaphore.Release();
            }
        }
        
        private async Task<BitmapSource> LoadIconAsync(string iconPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(iconPath))
                        return null;
                        
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(iconPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    
                    // Freeze for cross-thread access and memory efficiency
                    bitmap.Freeze();
                    
                    // Cache the loaded icon
                    _iconCache.TryAdd(iconPath, bitmap);
                    
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            });
        }
        
        public void ClearCache()
        {
            _iconCache.Clear();
        }
        
        public void InvalidateIcon(string iconPath)
        {
            if (!string.IsNullOrEmpty(iconPath))
            {
                _iconCache.TryRemove(iconPath, out _);
            }
        }
    }
}