using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        private readonly ThemeService themeService;

        public MainWindow()
        {
            InitializeComponent();
            themeService = ThemeService.Instance;
            InitializeTheme();
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

            UpdateStatusBar("> SYSTEM INITIALIZED...", "> Waiting for input...");
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

        private void UpdateStatusBar(string line1, string line2)
        {
            if (StatusText1 != null) StatusText1.Text = line1;
            if (StatusText2 != null) StatusText2.Text = line2;
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

            // Theme toggle
            ThemeToggleButton.Click += ThemeToggleButton_Click;

            // Load preset icons
            IconComboBox.ItemsSource = IconManager.LoadPresetIcons();

            // Setup keyboard shortcuts
            this.KeyDown += MainWindow_KeyDown;
        }

        private void NewGroupButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.AddNewGroup();
            UpdateStatusBar($"[{DateTime.Now:HH:mm:ss}] New group created", "> Ready");
        }

        private void SaveGroupButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.SaveSelectedGroup();
            UpdateStatusBar($"[{DateTime.Now:HH:mm:ss}] Group saved successfully", "> Ready");
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
                    var name = viewModel.SelectedGroup.Name;
                    viewModel.DeleteSelectedGroup();
                    UpdateStatusBar($"[{DateTime.Now:HH:mm:ss}] Group '{name}' deleted", "> Ready");
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

                UpdateStatusBar($"[{DateTime.Now:HH:mm:ss}] Shortcut created for '{viewModel.SelectedGroup.Name}'", "> Pin to taskbar via right-click");

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
            UpdateStatusBar($"[{DateTime.Now:HH:mm:ss}] Searching for '{SearchTextBox.Text}'...", "> Results updated");
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

                UpdateStatusBar($"[{DateTime.Now:HH:mm:ss}] Added {openFileDialog.FileNames.Length} file(s)", "> Ready");
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
                UpdateStatusBar($"[{DateTime.Now:HH:mm:ss}] Added '{selectedResult.Name}'", "> Ready");
                MessageBox.Show($"'{selectedResult.Name}' has been added to the group '{viewModel.SelectedGroup.Name}'.",
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                NewGroupButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SaveGroupButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                DeleteGroupButton_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
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
                UpdateStatusBar($"[{DateTime.Now:HH:mm:ss}] Group exported", "> Ready");
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
                        var index = viewModel.Groups.IndexOf(existingGroup);
                        viewModel.Groups.RemoveAt(index);
                        viewModel.Groups.Insert(index, importedGroup);
                        viewModel.SelectedGroup = importedGroup;
                    }
                }
                else
                {
                    viewModel.Groups.Add(importedGroup);
                    viewModel.SelectedGroup = importedGroup;
                }

                viewModel.SaveGroups();
                UpdateStatusBar($"[{DateTime.Now:HH:mm:ss}] Imported '{importedGroup.Name}'", "> Ready");

                MessageBox.Show($"Group '{importedGroup.Name}' imported successfully!",
                    "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            var groupsList = new List<TaskbarGroup>(viewModel.Groups);
            configService.CreateBackup(groupsList);
            UpdateStatusBar($"[{DateTime.Now:HH:mm:ss}] Backup created", "> Ready");
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

            var latestBackup = availableBackups.First();
            var restoredGroups = configService.RestoreBackup(latestBackup);

            if (restoredGroups != null)
            {
                viewModel.Groups.Clear();
                foreach (var group in restoredGroups)
                {
                    viewModel.Groups.Add(group);
                }

                viewModel.SaveGroups();
                UpdateStatusBar($"[{DateTime.Now:HH:mm:ss}] Restored {restoredGroups.Count} groups", "> Ready");

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

        // ═══════════════ THEME MANAGEMENT ═══════════════

        private void InitializeTheme()
        {
            try
            {
                themeService.LoadThemePreference();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading theme preference: {ex.Message}");
            }

            ApplyTheme(themeService.IsDarkMode);
            themeService.PropertyChanged += ThemeService_PropertyChanged;
        }

        private void ThemeService_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ThemeService.IsDarkMode))
            {
                ApplyTheme(themeService.IsDarkMode);
            }
        }

        private static Color HexColor(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }

        private void ApplyTheme(bool isDarkMode)
        {
            if (isDarkMode)
            {
                // Dark theme - deep, elegant dark
                Resources["PrimaryBackground"]  = new SolidColorBrush(HexColor("#141419"));
                Resources["SecondaryBackground"] = new SolidColorBrush(HexColor("#1C1C24"));
                Resources["CardBackground"]      = new SolidColorBrush(HexColor("#1E1E28"));
                Resources["BorderBrush"]         = new SolidColorBrush(HexColor("#2E2E3A"));
                Resources["TextPrimary"]         = new SolidColorBrush(HexColor("#D4D2CC"));
                Resources["TextSecondary"]       = new SolidColorBrush(HexColor("#7A7872"));
                Resources["AccentColor"]         = new SolidColorBrush(HexColor("#8B7D6B"));
                Resources["HeaderBackground"]    = new SolidColorBrush(HexColor("#0E0E12"));
                Resources["HeaderText"]          = new SolidColorBrush(HexColor("#C8C4BC"));
                Resources["InputBackground"]     = new SolidColorBrush(HexColor("#16161C"));
                Resources["StatusBackground"]    = new SolidColorBrush(HexColor("#0A0A0E"));
                Resources["StatusText"]          = new SolidColorBrush(HexColor("#5A9E5A"));
                Resources["ActionButtonBg"]      = new SolidColorBrush(HexColor("#252530"));
                Resources["ActionButtonText"]    = new SolidColorBrush(HexColor("#8A8780"));
                Resources["InactiveTabText"]     = new SolidColorBrush(HexColor("#555560"));
                Resources["ListItemHover"]       = new SolidColorBrush(HexColor("#282834"));
                Resources["ListItemSelected"]    = new SolidColorBrush(HexColor("#323240"));
                Resources["DangerColor"]         = new SolidColorBrush(HexColor("#8B3A3A"));
                Resources["DangerHover"]         = new SolidColorBrush(HexColor("#A04545"));
                Resources["SuccessColor"]        = new SolidColorBrush(HexColor("#5A9E5A"));
            }
            else
            {
                // Light theme - warm industrial
                Resources["PrimaryBackground"]  = new SolidColorBrush(HexColor("#EDEBE6"));
                Resources["SecondaryBackground"] = new SolidColorBrush(HexColor("#E3E0D9"));
                Resources["CardBackground"]      = new SolidColorBrush(HexColor("#F5F3EF"));
                Resources["BorderBrush"]         = new SolidColorBrush(HexColor("#C8C4BC"));
                Resources["TextPrimary"]         = new SolidColorBrush(HexColor("#2A2A2A"));
                Resources["TextSecondary"]       = new SolidColorBrush(HexColor("#6B6860"));
                Resources["AccentColor"]         = new SolidColorBrush(HexColor("#8B7D6B"));
                Resources["HeaderBackground"]    = new SolidColorBrush(HexColor("#2A2A2A"));
                Resources["HeaderText"]          = new SolidColorBrush(HexColor("#E8E6E1"));
                Resources["InputBackground"]     = new SolidColorBrush(HexColor("#F9F8F5"));
                Resources["StatusBackground"]    = new SolidColorBrush(HexColor("#1E1E1E"));
                Resources["StatusText"]          = new SolidColorBrush(HexColor("#7CB87C"));
                Resources["ActionButtonBg"]      = new SolidColorBrush(HexColor("#3A3A3A"));
                Resources["ActionButtonText"]    = new SolidColorBrush(HexColor("#C8C4BC"));
                Resources["InactiveTabText"]     = new SolidColorBrush(HexColor("#8A8780"));
                Resources["ListItemHover"]       = new SolidColorBrush(HexColor("#DDD9D0"));
                Resources["ListItemSelected"]    = new SolidColorBrush(HexColor("#C8C0B4"));
                Resources["DangerColor"]         = new SolidColorBrush(HexColor("#A04040"));
                Resources["DangerHover"]         = new SolidColorBrush(HexColor("#8A3535"));
                Resources["SuccessColor"]        = new SolidColorBrush(HexColor("#4A7C59"));
            }

            Background = (Brush)Resources["PrimaryBackground"];
            UpdateThemeToggleContent(isDarkMode);
            UpdateStatusBar($"[{DateTime.Now:HH:mm:ss}] Theme switched to {(isDarkMode ? "DARK" : "LIGHT")}", "> Ready");
        }

        private void UpdateThemeToggleContent(bool isDarkMode)
        {
            if (ThemeToggleButton != null)
            {
                ThemeToggleButton.Content = isDarkMode ? "/mnt" : "/dark";

                if (isDarkMode)
                {
                    ThemeToggleButton.Background = new SolidColorBrush(HexColor("#2E2E3A"));
                    ThemeToggleButton.Foreground = new SolidColorBrush(HexColor("#C8C4BC"));
                }
                else
                {
                    ThemeToggleButton.Background = new SolidColorBrush(HexColor("#2A2A2A"));
                    ThemeToggleButton.Foreground = new SolidColorBrush(HexColor("#E8E6E1"));
                }
            }
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            themeService.ToggleTheme();
        }
    }
}
