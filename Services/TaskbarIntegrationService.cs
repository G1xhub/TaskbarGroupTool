using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using TaskbarGroupTool.Models;

namespace TaskbarGroupTool.Services
{
    public class TaskbarIntegrationService
    {
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(long wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        [DllImport("kernel32.dll")]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        private const long SHCNE_ASSOCCHANGED = 0x08000000L;
        private const uint SHCNF_IDLIST = 0x0000U;

        public void CreateTaskbarShortcut(TaskbarGroup group)
        {
            try
            {
                var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                var shortcutPath = Path.Combine(desktopPath, $"{group.Name}.lnk");

                // Echten Windows-Shortcut erstellen
                CreateWindowsShortcut(shortcutPath, group);

                // Windows über Änderungen benachrichtigen
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Erstellen des Taskbar-Shortcuts: {ex.Message}", ex);
            }
        }

        private void CreateWindowsShortcut(string shortcutPath, TaskbarGroup group)
        {
            // Verwenden einer COM-freien Methode zum Erstellen von Shortcuts
            var appPath = System.IO.Path.Combine(AppContext.BaseDirectory, "TaskbarGroupTool.exe");
            
            // Shortcut-Datei im Windows-Format erstellen
            using (var writer = new BinaryWriter(File.Create(shortcutPath)))
            {
                // Windows-Shortcut Header
                writer.Write((byte)0x4C); // L
                writer.Write((byte)0x00); // 
                writer.Write((byte)0x00); // 
                writer.Write((byte)0x01); // 
                writer.Write((byte)0x14); // 
                writer.Write((byte)0x02); // 
                writer.Write((byte)0x00); // 
                writer.Write((byte)0x00); // 
                writer.Write((byte)0x00); // 
                writer.Write((byte)0x00); // 
                writer.Write((byte)0x00); // 
                writer.Write((byte)0xC0); // 
                writer.Write((byte)0x00); // 
                writer.Write((byte)0x00); // 
                writer.Write((byte)0x00); // 
                writer.Write((byte)0x00); // 
                writer.Write((byte)0x00); // 
                writer.Write((byte)0x46); // F

                // Link-Flags
                writer.Write((uint)0x00000000);

                // File-Attributes
                writer.Write((uint)0x00000000);

                // Time-Daten
                writer.Write((long)0x0000000000000000);
                writer.Write((long)0x0000000000000000);
                writer.Write((long)0x0000000000000000);

                // Icon-Index
                writer.Write((int)0x00000000);

                // Show-Command
                writer.Write((int)0x00000001);

                // Hotkey
                writer.Write((short)0x0000);

                // Reserved
                writer.Write((short)0x0000);

                // Reserved
                writer.Write((int)0x00000000);

                // Reserved
                writer.Write((int)0x00000000);

                // Target-Information
                var targetPath = appPath;
                var targetBytes = Encoding.Unicode.GetBytes(targetPath + "\0");
                writer.Write(targetBytes);

                // Arguments
                var arguments = $"--group-id \"{group.Id}\"";
                var argBytes = Encoding.Unicode.GetBytes(arguments + "\0");
                writer.Write(argBytes);

                // Working Directory
                var workDir = Environment.CurrentDirectory;
                var workDirBytes = Encoding.Unicode.GetBytes(workDir + "\0");
                writer.Write(workDirBytes);

                // Description
                var description = $"Taskbar Group: {group.Name}";
                var descBytes = Encoding.Unicode.GetBytes(description + "\0");
                writer.Write(descBytes);

                // Icon-Location
                var iconLocation = appPath;
                var iconBytes = Encoding.Unicode.GetBytes(iconLocation + "\0");
                writer.Write(iconBytes);
            }

            // Alternative Methode: PowerShell verwenden
            CreateShortcutWithPowerShell(shortcutPath, appPath, $"--group-id \"{group.Id}\"", group.Name);
        }

        private void CreateShortcutWithPowerShell(string shortcutPath, string targetPath, string arguments, string description)
        {
            try
            {
                var script = $@"
$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut('{shortcutPath}')
$Shortcut.TargetPath = '{targetPath}'
$Shortcut.Arguments = '{arguments}'
$Shortcut.WorkingDirectory = '{Environment.CurrentDirectory}'
$Shortcut.Description = '{description}'
$Shortcut.IconLocation = '{targetPath}'
$Shortcut.Save()
";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    process.WaitForExit();
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine($"PowerShell Error: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PowerShell Shortcut Creation Error: {ex.Message}");
            }
        }

        public void PinToTaskbar(string shortcutPath)
        {
            try
            {
                var script = $@"
$shell = New-Object -ComObject Shell.Application
$folder = $shell.NameSpace((Get-Item '{shortcutPath}').DirectoryName)
$item = $folder.ParseName((Get-Item '{shortcutPath}').Name)
$item.InvokeVerb('P&in to Taskbar')
";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    process.WaitForExit();
                    var output = process.StandardOutput.ReadToEnd();
                    var error = process.StandardError.ReadToEnd();
                    
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine($"PowerShell Pin Error: {error}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Fehler beim Anheften an die Taskbar: {ex.Message}", ex);
            }
        }

        public bool IsPinnedToTaskbar(string shortcutPath)
        {
            try
            {
                var taskbarPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                    "Microsoft\\Internet Explorer\\Quick Launch\\User Pinned\\TaskBar");
                
                if (Directory.Exists(taskbarPath))
                {
                    var shortcutName = Path.GetFileName(shortcutPath);
                    return File.Exists(Path.Combine(taskbarPath, shortcutName));
                }
            }
            catch
            {
                // Fehler bei der Prüfung
            }
            return false;
        }
    }
}
