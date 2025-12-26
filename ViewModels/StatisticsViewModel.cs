using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using TaskbarGroupTool.Models;
using TaskbarGroupTool.Services;

namespace TaskbarGroupTool.ViewModels
{
    public class StatisticsViewModel : INotifyPropertyChanged
    {
        private StatisticsService statisticsService;
        private ObservableCollection<UsageStatistics> topApplications;
        private ObservableCollection<GroupUsageStatistics> topGroups;
        private ObservableCollection<UsageStatistics> recentlyUsed;
        private int totalLaunches;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<UsageStatistics> TopApplications
        {
            get => topApplications;
            set
            {
                topApplications = value;
                OnPropertyChanged(nameof(TopApplications));
            }
        }

        public ObservableCollection<GroupUsageStatistics> TopGroups
        {
            get => topGroups;
            set
            {
                topGroups = value;
                OnPropertyChanged(nameof(TopGroups));
            }
        }

        public ObservableCollection<UsageStatistics> RecentlyUsed
        {
            get => recentlyUsed;
            set
            {
                recentlyUsed = value;
                OnPropertyChanged(nameof(RecentlyUsed));
            }
        }

        public int TotalLaunches
        {
            get => totalLaunches;
            set
            {
                totalLaunches = value;
                OnPropertyChanged(nameof(TotalLaunches));
            }
        }

        public ICommand RefreshCommand { get; private set; }
        public ICommand ClearStatisticsCommand { get; private set; }

        public StatisticsViewModel()
        {
            statisticsService = new StatisticsService();
            InitializeCommands();
            LoadStatistics();
        }

        private void InitializeCommands()
        {
            RefreshCommand = new RelayCommand(RefreshStatistics);
            ClearStatisticsCommand = new RelayCommand(ClearStatistics);
        }

        private void LoadStatistics()
        {
            try
            {
                var topApps = statisticsService.GetTopApplications(10);
                var topGroups = statisticsService.GetTopGroups(5);
                var recentApps = statisticsService.GetRecentlyUsedApplications(10);
                var total = statisticsService.GetTotalLaunches();

                TopApplications = new ObservableCollection<UsageStatistics>(topApps);
                TopGroups = new ObservableCollection<GroupUsageStatistics>(topGroups);
                RecentlyUsed = new ObservableCollection<UsageStatistics>(recentApps);
                TotalLaunches = total;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading statistics: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshStatistics()
        {
            LoadStatistics();
        }

        private void ClearStatistics()
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all statistics? This action cannot be undone.",
                "Clear Statistics", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                statisticsService.ClearStatistics();
                LoadStatistics();
                MessageBox.Show("Statistics cleared successfully!", "Success", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void RecordApplicationLaunch(string applicationPath, string applicationName)
        {
            statisticsService.RecordApplicationLaunch(applicationPath, applicationName);
        }

        public void RecordGroupLaunch(string groupName)
        {
            statisticsService.RecordGroupLaunch(groupName);
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        public void Execute(object parameter) => _execute();
    }
}
