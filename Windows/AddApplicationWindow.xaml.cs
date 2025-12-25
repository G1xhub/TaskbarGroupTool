using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using TaskbarGroupTool.Models;
using TaskbarGroupTool.Services;

namespace TaskbarGroupTool.Windows
{
    public partial class AddApplicationWindow : Window
    {
        private readonly ApplicationSearchService searchService;
        private TaskbarGroup currentGroup;
        
        public ObservableCollection<SearchResult> SearchResults { get; set; }
        public string SearchTerm { get; set; }

        public AddApplicationWindow(TaskbarGroup group)
        {
            InitializeComponent();
            searchService = new ApplicationSearchService();
            currentGroup = group;
            SearchResults = new ObservableCollection<SearchResult>();
            
            DataContext = this;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
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
                    // Create a SearchResult from the selected file
                    var searchResult = new SearchResult
                    {
                        Name = System.IO.Path.GetFileNameWithoutExtension(fileName),
                        Path = fileName,
                        Type = GetFileType(fileName)
                    };
                    
                    SearchResults.Add(searchResult);
                }
            }
        }

        private SearchResultType GetFileType(string filePath)
        {
            var extension = System.IO.Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".exe" => SearchResultType.Application,
                ".lnk" => SearchResultType.Shortcut,
                _ => SearchResultType.Folder
            };
        }

        private void SearchTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformSearch();
            }
        }

        private void PerformSearch()
        {
            if (!string.IsNullOrEmpty(SearchTerm) && SearchTerm.Length > 2)
            {
                try
                {
                    var results = searchService.SearchApplications(SearchTerm);
                    SearchResults.Clear();
                    
                    foreach (var result in results.Take(20))
                    {
                        SearchResults.Add(result);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Search error: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                SearchResults.Clear();
            }
        }

        private void SearchResultsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AddSelectedApplication();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddSelectedApplication();
        }

        private void AddSelectedApplication()
        {
            var selectedResult = SearchResultsListBox.SelectedItem as SearchResult;
            if (selectedResult != null)
            {
                // Add to the current group
                if (!currentGroup.Applications.Contains(selectedResult.Path))
                {
                    currentGroup.Applications.Add(selectedResult.Path);
                    MessageBox.Show($"'{selectedResult.Name}' has been added to the group.", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("This application is already in the group.", 
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Please select an application from the search results.", 
                    "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
