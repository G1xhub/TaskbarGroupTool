using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using TaskbarGroupTool.Windows;

namespace TaskbarGroupTool
{
    public partial class App : Application
    {
        // Import for setting AppUserModelID
        [DllImport("shell32.dll", SetLastError = true)]
        static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Get command line arguments
            var args = Environment.GetCommandLineArgs();
            
            // Set AppUserModelID based on arguments
            if (args.Length > 1 && !string.IsNullOrEmpty(args[1]))
            {
                // Running as taskbar group menu - DON'T show main window
                SetCurrentProcessExplicitAppUserModelID($"TaskbarGroupTool.menu.{args[1]}");
                
                try
                {
                    // Show group menu window ONLY
                    var groupMenuWindow = new GroupMenuWindow(args[1]);
                    groupMenuWindow.Show();
                    
                    // Shutdown application when group menu closes
                    groupMenuWindow.Closed += (s, e) => Current.Shutdown();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    Current.Shutdown();
                }
            }
            else
            {
                // Running as main application
                SetCurrentProcessExplicitAppUserModelID("TaskbarGroupTool.main");
                
                try
                {
                    // Show main window
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
        }
    }
}
