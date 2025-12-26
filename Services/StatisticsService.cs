using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using TaskbarGroupTool.Models;

namespace TaskbarGroupTool.Services
{
    public class StatisticsService
    {
        private const string STATISTICS_FILE = "statistics.json";
        private List<UsageStatistics> applicationStats;
        private List<GroupUsageStatistics> groupStats;

        public StatisticsService()
        {
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskbarGroupTool", STATISTICS_FILE);
                
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var data = JsonConvert.DeserializeObject<StatisticsData>(json);
                    
                    if (data != null)
                    {
                        applicationStats = data.ApplicationStats ?? new List<UsageStatistics>();
                        groupStats = data.GroupStats ?? new List<GroupUsageStatistics>();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading statistics: {ex.Message}", "Statistics Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }

            // Initialize with empty lists if loading fails
            applicationStats = new List<UsageStatistics>();
            groupStats = new List<GroupUsageStatistics>();
        }

        private void SaveStatistics()
        {
            try
            {
                var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskbarGroupTool", STATISTICS_FILE);
                var directory = Path.GetDirectoryName(configPath);
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var data = new StatisticsData
                {
                    ApplicationStats = applicationStats,
                    GroupStats = groupStats
                };

                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error saving statistics: {ex.Message}", "Statistics Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        public void RecordApplicationLaunch(string applicationPath, string applicationName)
        {
            var stats = applicationStats.FirstOrDefault(s => s.ApplicationPath == applicationPath);
            
            if (stats == null)
            {
                stats = new UsageStatistics
                {
                    ApplicationPath = applicationPath,
                    ApplicationName = applicationName,
                    LaunchCount = 0,
                    UsageHistory = new List<DateTime>()
                };
                applicationStats.Add(stats);
            }

            stats.LaunchCount++;
            stats.LastUsed = DateTime.Now;
            stats.UsageHistory.Add(DateTime.Now);

            // Keep only last 100 entries per application
            if (stats.UsageHistory.Count > 100)
            {
                stats.UsageHistory = stats.UsageHistory.Skip(stats.UsageHistory.Count - 100).ToList();
            }

            SaveStatistics();
        }

        public void RecordGroupLaunch(string groupName)
        {
            var stats = groupStats.FirstOrDefault(s => s.GroupName == groupName);
            
            if (stats == null)
            {
                stats = new GroupUsageStatistics
                {
                    GroupName = groupName,
                    LaunchCount = 0,
                    UsageHistory = new List<DateTime>()
                };
                groupStats.Add(stats);
            }

            stats.LaunchCount++;
            stats.LastUsed = DateTime.Now;
            stats.UsageHistory.Add(DateTime.Now);

            // Keep only last 100 entries per group
            if (stats.UsageHistory.Count > 100)
            {
                stats.UsageHistory = stats.UsageHistory.Skip(stats.UsageHistory.Count - 100).ToList();
            }

            SaveStatistics();
        }

        public List<UsageStatistics> GetTopApplications(int count = 10)
        {
            return applicationStats
                .OrderByDescending(s => s.LaunchCount)
                .Take(count)
                .ToList();
        }

        public List<GroupUsageStatistics> GetTopGroups(int count = 5)
        {
            return groupStats
                .OrderByDescending(s => s.LaunchCount)
                .Take(count)
                .ToList();
        }

        public List<UsageStatistics> GetRecentlyUsedApplications(int count = 10)
        {
            return applicationStats
                .Where(s => s.UsageHistory.Any())
                .OrderByDescending(s => s.LastUsed)
                .Take(count)
                .ToList();
        }

        public int GetTotalLaunches()
        {
            return applicationStats.Sum(s => s.LaunchCount);
        }

        public void ClearStatistics()
        {
            applicationStats.Clear();
            groupStats.Clear();
            SaveStatistics();
        }

        private class StatisticsData
        {
            public List<UsageStatistics> ApplicationStats { get; set; }
            public List<GroupUsageStatistics> GroupStats { get; set; }
        }
    }
}
