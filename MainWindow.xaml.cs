using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using TaskbarGroupTool.ViewModels;
using TaskbarGroupTool.Services;
using TaskbarGroupTool.Windows;
using TaskbarGroupTool.Models;

namespace TaskbarGroupTool
{
    public partial class MainWindow : Window
    {
        private MainViewModel viewModel;
        private ConfigurationService configService;
        private StatisticsService statisticsService;

        public MainWindow()
        {
            InitializeComponent();
            InitializeViewModel();
            SetupEventHandlers();
        }

        private void InitializeViewModel()
        {
            viewModel = new MainViewModel();
            configService = new ConfigurationService();
            statisticsService = new StatisticsService();
            DataContext = viewModel;
            
            // Bind search results
            SearchResultsListBox.ItemsSource = viewModel.SearchResults;
            
            // Initialize statistics data
            LoadStatisticsData();
        }

        private void LoadStatisticsData()
        {
            try
            {
                var topApps = statisticsService.GetTopApplications(10);
                var topGroups = statisticsService.GetTopGroups(5);
                
                // Create observable collections for binding
                viewModel.TopApplications = new ObservableCollection<UsageStatistics>(topApps);
                viewModel.TopGroups = new ObservableCollection<GroupUsageStatistics>(topGroups);
            }
            catch (Exception ex)
            {
                // Silently handle statistics loading errors
                System.Diagnostics.Debug.WriteLine($"Error loading statistics: {ex.Message}");
            }
        }

        private void SetupEventHandlers()
        {
            NewGroupButton.Click += NewGroupButton_Click;
            SaveGroupButton.Click += SaveGroupButton_Click;
            DeleteGroupButton.Click += DeleteGroupButton_Click;
            AddApplicationButton.Click += AddApplicationButton_Click;
            DeleteApplicationButton.Click += DeleteApplicationButton_Click;
            CreateShortcutButton.Click += CreateShortcutButton_Click;
            SearchButton.Click += SearchButton_Click;
            BrowseButton.Click += BrowseButton_Click;
            BrowseIconButton.Click += BrowseIconButton_Click;
            MoveUpButton.Click += MoveUpButton_Click;
            MoveDownButton.Click += MoveDownButton_Click;
            ExportButton.Click += ExportButton_Click;
            ImportButton.Click += ImportButton_Click;
            StatisticsButton.Click += StatisticsButton_Click;
            BackupButton.Click += BackupButton_Click;
            RestoreButton.Click += RestoreButton_Click;
            
            // Statistics buttons
            RefreshStatsButton.Click += RefreshStatsButton_Click;
            DetailedStatsButton.Click += DetailedStatsButton_Click;
            
            // Load preset icons
            IconComboBox.ItemsSource = IconManager.LoadPresetIcons();
            
            // Setup keyboard shortcuts
            this.KeyDown += MainWindow_KeyDown;
        }

        private void NewGroupButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddNewGroup();
        }

        private void SaveGroupButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SaveSelectedGroup();
            MessageBox.Show("Group saved!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteGroupButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.SelectedGroup != null)
            {
                var result = MessageBox.Show($"Are you sure you want to delete the group '{viewModel.SelectedGroup.Name}'?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    viewModel.DeleteSelectedGroup();
                }
            }
        }

        private void AddApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.SelectedGroup != null)
            {
                var addWindow = new AddApplicationWindow(viewModel.SelectedGroup);
                addWindow.Owner = this;
                addWindow.ShowDialog();
                
                // The applications list will update automatically through data binding
            }
            else
            {
                MessageBox.Show("Please select a group first.", "Warning", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteApplicationButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedApp = ApplicationsListBox.SelectedItem as string;
            if (selectedApp != null)
            {
                viewModel.RemoveApplicationFromGroup(selectedApp);
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedApp = ApplicationsListBox.SelectedItem as string;
            if (selectedApp != null)
            {
                viewModel.MoveApplicationUp(selectedApp);
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedApp = ApplicationsListBox.SelectedItem as string;
            if (selectedApp != null)
            {
                viewModel.MoveApplicationDown(selectedApp);
            }
        }

        private void CreateShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.SelectedGroup != null && viewModel.SelectedGroup.Applications.Any())
            {
                var selectedIcon = IconComboBox.SelectedItem as IconItem;
                string iconPath = selectedIcon?.Path;
                viewModel.CreateTaskbarShortcut(iconPath);
                
                // Record statistics
                statisticsService.RecordGroupLaunch(viewModel.SelectedGroup.Name);
                
                MessageBox.Show($"Taskbar shortcut created for group '{viewModel.SelectedGroup.Name}'!", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Please select a group with at least one application.", 
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BrowseIconButton_Click(object sender, RoutedEventArgs e)
        {
            var iconPath = IconManager.BrowseForIcon();
            if (!string.IsNullOrEmpty(iconPath))
            {
                // Add custom icon to the combo box
                var customIcon = new IconItem("Custom", iconPath);
                var icons = IconComboBox.ItemsSource as System.Collections.ObjectModel.ObservableCollection<IconItem>;
                if (icons != null)
                {
                    icons.Add(customIcon);
                    IconComboBox.SelectedItem = customIcon;
                }
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SearchTerm = SearchTextBox.Text;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Application or Shortcut",
                Filter = "Applications (*.exe)|*.exe|Shortcuts (*.lnk)|*.lnk|All Files (*.*)|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    if (viewModel.SelectedGroup != null)
                    {
                        viewModel.AddApplicationToGroup(fileName);
                    }
                }
                
                MessageBox.Show($"Added {openFileDialog.FileNames.Length} file(s) to the group.", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                viewModel.SearchTerm = SearchTextBox.Text;
            }
        }

        private void SearchResultsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedResult = SearchResultsListBox.SelectedItem as SearchResult;
            if (selectedResult != null && viewModel.SelectedGroup != null)
            {
                viewModel.AddApplicationToGroup(selectedResult.Path);
                MessageBox.Show($"'{selectedResult.Name}' has been added to the group '{viewModel.SelectedGroup.Name}'.", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle keyboard shortcuts
            if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Ctrl+N - New Group
                NewGroupButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Ctrl+S - Save Group
                SaveGroupButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Ctrl+D - Delete Group
                DeleteGroupButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                // Delete - Remove selected application
                var focusedElement = Keyboard.FocusedElement as FrameworkElement;
                if (focusedElement != null && focusedElement.Name == "ApplicationsListBox")
                {
                    DeleteApplicationButton_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
        }

        private void SearchResultsListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Start drag operation
            var listBox = sender as ListBox;
            var item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            
            if (item != null && item.Content is SearchResult searchResult)
            {
                var data = new DataObject(DataFormats.FileDrop, new[] { searchResult.Path });
                data.SetData("SearchResult", searchResult);
                
                DragDrop.DoDragDrop(item, data, DragDropEffects.Copy);
            }
        }

        private void ApplicationsListBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) || e.Data.GetDataPresent("SearchResult"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void ApplicationsListBox_Drop(object sender, DragEventArgs e)
        {
            if (viewModel.SelectedGroup == null)
            {
                MessageBox.Show("Please select a group first.", "Warning", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Handle file drop
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        if (file.EndsWith(".exe") || file.EndsWith(".lnk"))
                        {
                            viewModel.AddApplicationToGroup(file);
                        }
                    }
                }
            }

            // Handle search result drop
            if (e.Data.GetDataPresent("SearchResult"))
            {
                var searchResult = e.Data.GetData("SearchResult") as SearchResult;
                if (searchResult != null)
                {
                    viewModel.AddApplicationToGroup(searchResult.Path);
                }
            }
            e.Handled = true;
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (viewModel.SelectedGroup != null)
            {
                configService.ExportSingleGroup(viewModel.SelectedGroup);
            }
            else
            {
                MessageBox.Show("Please select a group to export.", "No Group Selected", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var importedGroup = configService.ImportSingleGroup();
            if (importedGroup != null)
            {
                // Check if group with same name already exists
                var existingGroup = viewModel.Groups.FirstOrDefault(g => g.Name.Equals(importedGroup.Name, StringComparison.OrdinalIgnoreCase));
                
                if (existingGroup != null)
                {
                    var result = MessageBox.Show(
                        $"A group named '{importedGroup.Name}' already exists.\n\n" +
                        "Do you want to replace it?",
                        "Group Exists", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Replace existing group
                        var index = viewModel.Groups.IndexOf(existingGroup);
                        viewModel.Groups.RemoveAt(index);
                        viewModel.Groups.Insert(index, importedGroup);
                        viewModel.SelectedGroup = importedGroup;
                    }
                }
                else
                {
                    // Add new group
                    viewModel.Groups.Add(importedGroup);
                    viewModel.SelectedGroup = importedGroup;
                }
                
                // Save the updated groups
                viewModel.SaveGroups();
                
                MessageBox.Show($"Group '{importedGroup.Name}' imported successfully!", 
                    "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            var groupsList = new List<TaskbarGroup>(viewModel.Groups);
            configService.CreateBackup(groupsList);
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            var availableBackups = configService.GetAvailableBackups();
            
            if (!availableBackups.Any())
            {
                MessageBox.Show("No backups available.", "No Backups", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Simple restore from latest backup
            var latestBackup = availableBackups.First();
            var restoredGroups = configService.RestoreBackup(latestBackup);
            
            if (restoredGroups != null)
            {
                // Clear existing groups and add restored ones
                viewModel.Groups.Clear();
                foreach (var group in restoredGroups)
                {
                    viewModel.Groups.Add(group);
                }
                
                // Save the restored groups
                viewModel.SaveGroups();
                
                MessageBox.Show($"Successfully restored {restoredGroups.Count} groups from backup!", 
                    "Restore Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void StatisticsButton_Click(object sender, RoutedEventArgs e)
        {
            var statsWindow = new StatisticsWindow();
            statsWindow.Owner = this;
            statsWindow.ShowDialog();
        }

        private void RefreshStatsButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStatisticsData();
        }

        private void DetailedStatsButton_Click(object sender, RoutedEventArgs e)
        {
            var statsWindow = new StatisticsWindow();
            statsWindow.Owner = this;
            statsWindow.ShowDialog();
        }
    }
}
