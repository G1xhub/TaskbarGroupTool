using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using TaskbarGroupTool.ViewModels;
using TaskbarGroupTool.Services;
using TaskbarGroupTool.Windows;

namespace TaskbarGroupTool
{
    public partial class MainWindow : Window
    {
        private MainViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            InitializeViewModel();
            SetupEventHandlers();
        }

        private void InitializeViewModel()
        {
            viewModel = new MainViewModel();
            DataContext = viewModel;
            
            // Bind search results
            SearchResultsListBox.ItemsSource = viewModel.SearchResults;
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

        private void CreateShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            viewModel.CreateTaskbarShortcut();
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
    }
}
