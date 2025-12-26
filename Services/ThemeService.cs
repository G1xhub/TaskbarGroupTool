using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskbarGroupTool.Services
{
    public class ThemeService : INotifyPropertyChanged
    {
        private static ThemeService instance;
        public static ThemeService Instance => instance ??= new ThemeService();

        private bool isDarkMode;
        public bool IsDarkMode
        {
            get => isDarkMode;
            set
            {
                if (isDarkMode != value)
                {
                    isDarkMode = value;
                    OnPropertyChanged();
                    SaveThemePreference();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ToggleTheme()
        {
            IsDarkMode = !IsDarkMode;
        }

        private void SaveThemePreference()
        {
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var settingsPath = System.IO.Path.Combine(appDataPath, "TaskbarGroupTool", "settings.json");
                
                var settings = new { IsDarkMode = IsDarkMode };
                var json = System.Text.Json.JsonSerializer.Serialize(settings);
                
                var directory = System.IO.Path.GetDirectoryName(settingsPath);
                if (!System.IO.Directory.Exists(directory))
                    System.IO.Directory.CreateDirectory(directory);
                
                System.IO.File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving theme preference: {ex.Message}");
            }
        }

        public void LoadThemePreference()
        {
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var settingsPath = System.IO.Path.Combine(appDataPath, "TaskbarGroupTool", "settings.json");
                
                if (System.IO.File.Exists(settingsPath))
                {
                    var json = System.IO.File.ReadAllText(settingsPath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<ThemeSettings>(json);
                    IsDarkMode = settings?.IsDarkMode ?? false;
                }
                else
                {
                    // Check system theme
                    IsDarkMode = ShouldUseDarkMode();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading theme preference: {ex.Message}");
                IsDarkMode = false;
            }
        }

        private bool ShouldUseDarkMode()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("AppsUseLightTheme");
                        if (value != null && value is int intValue)
                        {
                            return intValue == 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking system theme: {ex.Message}");
            }
            return false;
        }

        private class ThemeSettings
        {
            public bool IsDarkMode { get; set; }
        }
    }
}
