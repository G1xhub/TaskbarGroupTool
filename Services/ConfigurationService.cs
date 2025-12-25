using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using TaskbarGroupTool.Models;

namespace TaskbarGroupTool.Services
{
    public class ConfigurationService
    {
        private const string EXPORT_EXTENSION = ".tbg";
        private const string FILTER = "Taskbar Group Files (*.tbg)|*.tbg|All Files (*.*)|*.*";

        public void ExportSingleGroup(TaskbarGroup group)
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = FILTER,
                    DefaultExt = EXPORT_EXTENSION,
                    Title = "Export Group Configuration",
                    FileName = $"{group.Name}_{DateTime.Now:yyyyMMdd_HHmmss}{EXPORT_EXTENSION}"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportData = new ExportData
                    {
                        Version = "1.0",
                        ExportDate = DateTime.Now,
                        Groups = new List<TaskbarGroup> { group }
                    };

                    var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
                    File.WriteAllText(saveFileDialog.FileName, json);

                    System.Windows.MessageBox.Show($"Group '{group.Name}' exported successfully to:\n{saveFileDialog.FileName}", 
                        "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error exporting group: {ex.Message}", "Export Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public TaskbarGroup ImportSingleGroup()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = FILTER,
                    Title = "Import Group Configuration"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(openFileDialog.FileName);
                    var exportData = JsonConvert.DeserializeObject<ExportData>(json);

                    if (exportData != null && exportData.Groups.Any())
                    {
                        var importedGroup = exportData.Groups.First();
                        
                        var result = System.Windows.MessageBox.Show(
                            $"Import group '{importedGroup.Name}' from:\n{openFileDialog.FileName}?\n\n" +
                            "This will add the group to your existing groups.",
                            "Import Confirmation", 
                            MessageBoxButton.YesNo, 
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            return importedGroup;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error importing group: {ex.Message}", "Import Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        public void CreateBackup(List<TaskbarGroup> groups)
        {
            try
            {
                var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskbarGroupTool", "Backups");
                
                if (!Directory.Exists(backupDir))
                {
                    Directory.CreateDirectory(backupDir);
                }

                var backupFile = Path.Combine(backupDir, $"Backup_{DateTime.Now:yyyyMMdd_HHmmss}.tbg");
                
                var exportData = new ExportData
                {
                    Version = "1.0",
                    ExportDate = DateTime.Now,
                    Groups = groups
                };

                var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
                File.WriteAllText(backupFile, json);

                // Keep only last 10 backups
                var backupFiles = Directory.GetFiles(backupDir, "Backup_*.tbg")
                    .OrderByDescending(f => f)
                    .Skip(10);

                foreach (var oldBackup in backupFiles)
                {
                    File.Delete(oldBackup);
                }

                System.Windows.MessageBox.Show($"Backup created successfully:\n{backupFile}", 
                    "Backup Created", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error creating backup: {ex.Message}", "Backup Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public List<string> GetAvailableBackups()
        {
            try
            {
                var backupDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskbarGroupTool", "Backups");
                
                if (!Directory.Exists(backupDir))
                {
                    return new List<string>();
                }

                return Directory.GetFiles(backupDir, "Backup_*.tbg")
                    .OrderByDescending(f => f)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public List<TaskbarGroup> RestoreBackup(string backupFile)
        {
            try
            {
                var json = File.ReadAllText(backupFile);
                var exportData = JsonConvert.DeserializeObject<ExportData>(json);

                if (exportData != null)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"Restore backup from {Path.GetFileName(backupFile)}?\n\n" +
                        $"This will replace all existing groups. Continue?",
                        "Restore Confirmation", 
                        MessageBoxButton.YesNo, 
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        return exportData.Groups;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error restoring backup: {ex.Message}", "Restore Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return null;
        }

        private class ExportData
        {
            public string Version { get; set; }
            public DateTime ExportDate { get; set; }
            public List<TaskbarGroup> Groups { get; set; }
        }
    }
}
