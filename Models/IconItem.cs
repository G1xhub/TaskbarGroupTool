using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace TaskbarGroupTool.Models
{
    public class IconItem
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public BitmapImage Icon { get; set; }

        public IconItem(string name, string path)
        {
            Name = name;
            Path = path;
            Icon = LoadIcon(path);
        }

        private BitmapImage LoadIcon(string iconPath)
        {
            try
            {
                if (File.Exists(iconPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(iconPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }
            catch
            {
                // Return default icon if loading fails
            }
            return null;
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
