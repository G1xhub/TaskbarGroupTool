using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using TaskbarGroupTool.Models;
using TaskbarGroupTool.Services;

namespace TaskbarGroupTool.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly TaskbarManager taskbarManager;
        private readonly ApplicationSearchService searchService;
        private TaskbarGroup selectedGroup;
        private string groupName;
        private string searchTerm;
        private bool isSearchInProgress;
        private CancellationTokenSource searchCancellationTokenSource;
        private System.Timers.Timer searchDebounceTimer;

        public ObservableCollection<TaskbarGroup> Groups { get; set; }
        public ObservableCollection<SearchResult> SearchResults { get; set; }
        public ObservableCollection<UsageStatistics> TopApplications { get; set; }
        public ObservableCollection<GroupUsageStatistics> TopGroups { get; set; }

        public bool IsSearchInProgress
        {
            get => isSearchInProgress;
            set
            {
                isSearchInProgress = value;
                OnPropertyChanged();
            }
        }

        public TaskbarGroup SelectedGroup
        {
            get => selectedGroup;
            set
            {
                selectedGroup = value;
                OnPropertyChanged();
                
                if (selectedGroup != null)
                {
                    GroupName = selectedGroup.Name;
                }
            }
        }

        public string GroupName
        {
            get => groupName;
            set
            {
                groupName = value;
                OnPropertyChanged();
            }
        }

        public string SearchTerm
        {
            get => searchTerm;
            set
            {
                searchTerm = value;
                OnPropertyChanged();
                
                // Debounced search - reset timer
                searchDebounceTimer?.Stop();
                searchDebounceTimer?.Start();
            }
        }

        public MainViewModel()
        {
            taskbarManager = new TaskbarManager();
            searchService = new ApplicationSearchService();
            Groups = new ObservableCollection<TaskbarGroup>();
            SearchResults = new ObservableCollection<SearchResult>();
            TopApplications = new ObservableCollection<UsageStatistics>();
            TopGroups = new ObservableCollection<GroupUsageStatistics>();
            
            // Initialize debounce timer for search
            searchDebounceTimer = new System.Timers.Timer(300); // 300ms delay
            searchDebounceTimer.Elapsed += SearchDebounceTimer_Elapsed;
            searchDebounceTimer.AutoReset = false;
            
            LoadGroups();
        }

        private void LoadGroups()
        {
            try
            {
                var groups = taskbarManager.LoadGroups();
                Groups.Clear();
                
                foreach (var group in groups)
                {
                    Groups.Add(group);
                }
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading groups: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private async void SearchDebounceTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (string.IsNullOrEmpty(searchTerm) || searchTerm.Length < 3)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchResults.Clear();
                    IsSearchInProgress = false;
                });
                return;
            }

            await PerformSearchAsync();
        }

        private async Task PerformSearchAsync()
        {
            // Cancel any ongoing search
            searchCancellationTokenSource?.Cancel();
            searchCancellationTokenSource?.Dispose();
            searchCancellationTokenSource = new CancellationTokenSource();

            try
            {
                IsSearchInProgress = true;
                
                var results = await searchService.SearchApplicationsAsync(searchTerm, searchCancellationTokenSource.Token);
                
                // Update UI on main thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchResults.Clear();
                    
                    foreach (var result in results.Take(20)) // Virtualize results
                    {
                        SearchResults.Add(result);
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Search was cancelled, ignore
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
            }
            finally
            {
                IsSearchInProgress = false;
            }
        }

        public void SaveGroups()
        {
            try
            {
                var groupsList = new System.Collections.Generic.List<TaskbarGroup>(Groups);
                taskbarManager.SaveGroups(groupsList);
            }
            catch (System.Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving groups: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void AddNewGroup()
        {
            var newGroup = new TaskbarGroup("New Group");
            Groups.Add(newGroup);
            SelectedGroup = newGroup;
        }

        public void DeleteSelectedGroup()
        {
            if (SelectedGroup != null)
            {
                Groups.Remove(SelectedGroup);
                SelectedGroup = null;
                SaveGroups();
            }
        }

        public void SaveSelectedGroup()
        {
            if (SelectedGroup != null)
            {
                SelectedGroup.Name = GroupName;
                SaveGroups();
                
                // Update the display name in the Groups collection
                var index = Groups.IndexOf(SelectedGroup);
                if (index >= 0)
                {
                    Groups.RemoveAt(index);
                    Groups.Insert(index, SelectedGroup);
                    SelectedGroup = Groups[index]; // Re-select to maintain selection
                }
            }
        }

        public void AddApplicationToGroup(string applicationPath)
        {
            if (SelectedGroup != null && !string.IsNullOrEmpty(applicationPath))
            {
                if (!SelectedGroup.Applications.Contains(applicationPath))
                {
                    SelectedGroup.Applications.Add(applicationPath);
                    SaveGroups();
                }
            }
        }

        public void RemoveApplicationFromGroup(string applicationPath)
        {
            if (SelectedGroup != null && SelectedGroup.Applications.Contains(applicationPath))
            {
                SelectedGroup.Applications.Remove(applicationPath);
                SaveGroups();
            }
        }

        public void MoveApplicationUp(string applicationPath)
        {
            if (SelectedGroup != null && SelectedGroup.Applications.Contains(applicationPath))
            {
                var index = SelectedGroup.Applications.IndexOf(applicationPath);
                if (index > 0)
                {
                    SelectedGroup.Applications.RemoveAt(index);
                    SelectedGroup.Applications.Insert(index - 1, applicationPath);
                    SaveGroups();
                    
                    // Update selection in UI
                    var appsList = SelectedGroup.Applications.ToList();
                    var newIndex = appsList.IndexOf(applicationPath);
                    // The UI will update automatically through data binding
                }
            }
        }

        public void MoveApplicationDown(string applicationPath)
        {
            if (SelectedGroup != null && SelectedGroup.Applications.Contains(applicationPath))
            {
                var index = SelectedGroup.Applications.IndexOf(applicationPath);
                if (index < SelectedGroup.Applications.Count - 1)
                {
                    SelectedGroup.Applications.RemoveAt(index);
                    SelectedGroup.Applications.Insert(index + 1, applicationPath);
                    SaveGroups();
                    
                    // Update selection in UI
                    var appsList = SelectedGroup.Applications.ToList();
                    var newIndex = appsList.IndexOf(applicationPath);
                    // The UI will update automatically through data binding
                }
            }
        }

        public void CreateTaskbarShortcut(string iconPath = null)
        {
            if (SelectedGroup != null)
            {
                try
                {
                    taskbarManager.CreateTaskbarShortcut(SelectedGroup, iconPath);
                    
                    var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    var shortcutsPath = Path.Combine(Path.GetDirectoryName(appPath), "Shortcuts");
                    var shortcutPath = Path.Combine(shortcutsPath, $"{SelectedGroup.Name}.lnk");
                    
                    var message = $"Taskbar shortcut for '{SelectedGroup.Name}' has been created.\n\n" +
                                 $"Shortcut location: {shortcutPath}\n\n" +
                                 "To pin to taskbar:\n" +
                                 "1. Navigate to the Shortcuts folder\n" +
                                 "2. Right-click on the shortcut\n" +
                                 "3. Select 'Pin to taskbar'\n\n" +
                                 $"Would you like to open the Shortcuts folder now?";
                    
                    var result = System.Windows.MessageBox.Show(message, 
                        "Success", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Information);
                    
                    if (result == System.Windows.MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start("explorer.exe", shortcutsPath);
                    }
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error creating shortcut: {ex.Message}", "Error", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
