using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using TaskbarGroupTool.Models;
using Newtonsoft.Json;

namespace TaskbarGroupTool.Services
{
    public class TaskbarManager
    {
        private const string REGISTRY_KEY = @"SOFTWARE\TaskbarGroupTool";
        private const string CONFIG_FILE = "groups.json";

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(long wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const long SHCNE_ASSOCCHANGED = 0x08000000L;

        public void SaveGroups(List<TaskbarGroup> groups)
        {
            try
            {
                var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskbarGroupTool", CONFIG_FILE);
                var directory = Path.GetDirectoryName(configPath);
                
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonConvert.SerializeObject(groups, Formatting.Indented);
                File.WriteAllText(configPath, json);

                // Windows über Änderungen benachrichtigen
                SHChangeNotify(SHCNE_ASSOCCHANGED, 0, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Speichern der Gruppen: {ex.Message}", ex);
            }
        }

        public List<TaskbarGroup> LoadGroups()
        {
            try
            {
                var configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskbarGroupTool", CONFIG_FILE);
                
                if (!File.Exists(configPath))
                {
                    return new List<TaskbarGroup>();
                }

                var json = File.ReadAllText(configPath);
                return JsonConvert.DeserializeObject<List<TaskbarGroup>>(json) ?? new List<TaskbarGroup>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Laden der Gruppen: {ex.Message}", ex);
            }
        }

        public void CreateTaskbarShortcut(TaskbarGroup group)
        {
            try
            {
                // Register application first
                AppRegistrationService.RegisterApplication();
                
                var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var shortcutsPath = Path.Combine(Path.GetDirectoryName(appPath), "Shortcuts");
                
                if (!Directory.Exists(shortcutsPath))
                {
                    Directory.CreateDirectory(shortcutsPath);
                }

                var shortcutPath = Path.Combine(shortcutsPath, $"{group.Name}.lnk");

                // Verwenden der COM-basierten ShellLink-Implementierung
                ShellLinkService.InstallShortcut(
                    appPath,
                    $"TaskbarGroupTool.menu.{group.Name}",
                    $"Taskbar Group: {group.Name}",
                    Path.GetDirectoryName(appPath),
                    appPath,
                    shortcutPath,
                    group.Name
                );

                // Windows über Änderungen benachrichtigen
                SHChangeNotify(SHCNE_ASSOCCHANGED, 0, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des Taskbar-Shortcuts: {ex.Message}", ex);
            }
        }

        public void RegisterStartup()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        key.SetValue("TaskbarGroupTool", $"\"{appPath}\" --startup");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Registrieren des Autostarts: {ex.Message}", ex);
            }
        }

        public void UnregisterStartup()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        key.DeleteValue("TaskbarGroupTool", false);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Entfernen des Autostarts: {ex.Message}", ex);
            }
        }

        public List<string> GetRunningApplications()
        {
            var applications = new List<string>();
            
            try
            {
                // Hier könnte eine Implementierung zum Abrufen laufender Anwendungen erfolgen
                // Für den Moment geben wir einige Beispiele zurück
                applications.AddRange(new[] { "Visual Studio", "Code", "Word", "Excel", "Chrome", "Firefox" });
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Abrufen der laufenden Anwendungen: {ex.Message}", ex);
            }

            return applications;
        }
    }
}
