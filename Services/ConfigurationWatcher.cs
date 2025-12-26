using System;
using System.IO;
using System.Threading.Tasks;
using TaskbarGroupTool.Models;

namespace TaskbarGroupTool.Services
{
    public class ConfigurationWatcher : IDisposable
    {
        private FileSystemWatcher _groupsWatcher;
        private FileSystemWatcher _backupsWatcher;
        private readonly Action _onConfigurationChanged;
        private readonly Action _onBackupChanged;
        private readonly object _lockObject = new object();
        private bool _isDisposed = false;

        public ConfigurationWatcher(Action onConfigurationChanged, Action onBackupChanged = null)
        {
            _onConfigurationChanged = onConfigurationChanged ?? throw new ArgumentNullException(nameof(onConfigurationChanged));
            _onBackupChanged = onBackupChanged;
            
            InitializeWatchers();
        }

        private void InitializeWatchers()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                "TaskbarGroupTool"
            );

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            // Watch for changes to groups configuration
            _groupsWatcher = new FileSystemWatcher
            {
                Path = appDataPath,
                Filter = "groups.json",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = false
            };

            _groupsWatcher.Changed += OnGroupsConfigurationChanged;

            // Watch for backup changes
            var backupsPath = Path.Combine(appDataPath, "Backups");
            if (Directory.Exists(backupsPath))
            {
                _backupsWatcher = new FileSystemWatcher
                {
                    Path = backupsPath,
                    Filter = "Backup_*.tbg",
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
                    EnableRaisingEvents = false
                };

                _backupsWatcher.Created += OnBackupChanged;
                _backupsWatcher.Changed += OnBackupChanged;
                _backupsWatcher.Deleted += OnBackupChanged;
            }

            StartWatching();
        }

        public void StartWatching()
        {
            lock (_lockObject)
            {
                if (!_isDisposed)
                {
                    _groupsWatcher.EnableRaisingEvents = true;
                    if (_backupsWatcher != null)
                        _backupsWatcher.EnableRaisingEvents = true;
                }
            }
        }

        public void StopWatching()
        {
            lock (_lockObject)
            {
                _groupsWatcher.EnableRaisingEvents = false;
                if (_backupsWatcher != null)
                    _backupsWatcher.EnableRaisingEvents = false;
            }
        }

        private async void OnGroupsConfigurationChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Debounce rapid file changes
                await Task.Delay(500);

                lock (_lockObject)
                {
                    if (!_isDisposed)
                    {
                        _onConfigurationChanged?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling groups configuration change: {ex.Message}");
            }
        }

        private async void OnBackupChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Debounce rapid backup changes
                await Task.Delay(500);

                lock (_lockObject)
                {
                    if (!_isDisposed)
                    {
                        _onBackupChanged?.Invoke();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling backup change: {ex.Message}");
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                if (!_isDisposed)
                {
                    _isDisposed = true;
                    
                    _groupsWatcher?.Dispose();
                    _backupsWatcher?.Dispose();
                }
            }
        }

        ~ConfigurationWatcher()
        {
            Dispose();
        }
    }
}