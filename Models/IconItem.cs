using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using TaskbarGroupTool.Services;

namespace TaskbarGroupTool.Models
{
    public class IconItem : IDisposable
    {
        private BitmapSource _icon;
        private bool _disposed = false;
        private readonly object _iconLock = new object();

        public string Name { get; set; }
        public string Path { get; set; }
        
        public BitmapSource Icon
        {
            get
            {
                if (_disposed)
                    return null;
                    
                lock (_iconLock)
                {
                    return _icon;
                }
            }
            private set
            {
                lock (_iconLock)
                {
                    // Dispose previous icon
                    if (_icon is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    
                    _icon = value;
                    
                    // Freeze for cross-thread access and memory efficiency
                    if (_icon != null && !_icon.CanFreeze)
                    {
                        _icon.Freeze();
                    }
                }
            }
        }

        public IconItem(string name, string path)
        {
            Name = name;
            Path = path;
            LoadIconAsync();
        }

        private async Task LoadIconAsync()
        {
            try
            {
                var icon = await IconCacheService.Instance.GetIconAsync(Path);
                if (icon != null)
                {
                    Icon = icon;
                }
            }
            catch
            {
                // Failed to load icon, keep it null
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_iconLock)
                {
                    if (_icon is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                    _icon = null;
                    _disposed = true;
                }
            }
        }
    }

    public class IconManager
    {
        public static ObservableCollection<IconItem> LoadPresetIcons()
        {
            var icons = new ObservableCollection<IconItem>();
            
            try
            {
                var iconsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons");
                
                if (Directory.Exists(iconsDirectory))
                {
                    var iconFiles = Directory.GetFiles(iconsDirectory, "*.ico")
                                           .OrderBy(f => Path.GetFileNameWithoutExtension(f))
                                           .ToArray();

                    foreach (var iconFile in iconFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(iconFile);
                        var displayName = fileName.Replace("_", " ").Replace("-", " ");
                        
                        // Capitalize first letter of each word
                        displayName = string.Join(" ", displayName.Split(' ')
                                                                   .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
                        
                        icons.Add(new IconItem(displayName, iconFile));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading preset icons: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return icons;
        }

        public static string BrowseForIcon()
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Icon File",
                Filter = "Icon Files (*.ico)|*.ico|All Files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            return null;
        }
    }
}
