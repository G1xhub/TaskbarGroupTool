using System;
using Microsoft.Win32;

namespace TaskbarGroupTool.Services
{
    public class AppRegistrationService
    {
        public static void RegisterApplication()
        {
            try
            {
                var appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var appDir = System.IO.Path.GetDirectoryName(appPath);

                // Register file association for .lnk files created by our app
                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\taskbargroup.lnk"))
                {
                    key.SetValue("", "Taskbar Group Shortcut");
                    key.SetValue("FriendlyTypeName", "Taskbar Group");
                }

                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\taskbargroup.lnk\shell\open\command"))
                {
                    key.SetValue("", $"\"{appPath}\" \"%1\"");
                }

                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\Applications\TaskbarGroupTool.exe"))
                {
                    key.SetValue("FriendlyAppName", "Taskbar Grouping Tool");
                }

                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Classes\Applications\TaskbarGroupTool.exe\shell\open\command"))
                {
                    key.SetValue("", $"\"{appPath}\" \"%1\"");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to register application: {ex.Message}", ex);
            }
        }

        public static void UnregisterApplication()
        {
            try
            {
                // Remove registry entries
                Registry.CurrentUser.DeleteSubKeyTree(@"SOFTWARE\Classes\taskbargroup.lnk", false);
                Registry.CurrentUser.DeleteSubKeyTree(@"SOFTWARE\Classes\Applications\TaskbarGroupTool.exe", false);
            }
            catch
            {
                // Keys might not exist, ignore errors
            }
        }
    }
}
