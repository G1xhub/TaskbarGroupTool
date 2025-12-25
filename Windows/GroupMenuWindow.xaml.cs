using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using TaskbarGroupTool.Models;
using TaskbarGroupTool.Services;

namespace TaskbarGroupTool.Windows
{
    public partial class GroupMenuWindow : Window
    {
        private TaskbarGroup currentGroup;
        private List<UIElement> menuItems = new List<UIElement>();
        
        // Import for setting AppUserModelID
        [DllImport("shell32.dll", SetLastError = true)]
        static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        public GroupMenuWindow(string groupName)
        {
            InitializeComponent();
            
            // Load the group
            currentGroup = LoadGroup(groupName);
            
            // Set basic window properties
            Title = $"Group: {groupName}";
            Width = 300;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            // Create menu items for applications
            LoadMenuItems();
            
            // Close when deactivated
            Deactivated += GroupMenuWindow_Deactivated;
            KeyDown += GroupMenuWindow_KeyDown;
        }

        private TaskbarGroup LoadGroup(string groupName)
        {
            try
            {
                var taskbarManager = new TaskbarManager();
                var groups = taskbarManager.LoadGroups();
                var group = groups.FirstOrDefault(g => g.Name == groupName);
                
                if (group == null)
                {
                    // Create a new group if it doesn't exist
                    group = new TaskbarGroup(groupName);
                }
                
                return group;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading group: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return new TaskbarGroup(groupName);
            }
        }

        private void LoadMenuItems()
        {
            var stackPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Vertical,
                Margin = new Thickness(10)
            };
            
            if (currentGroup != null && currentGroup.Applications.Any())
            {
                foreach (var appPath in currentGroup.Applications)
                {
                    var item = CreateMenuItem(appPath);
                    stackPanel.Children.Add(item);
                }
            }
            else
            {
                var noAppsText = new TextBlock
                {
                    Text = "No applications in this group",
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Margin = new Thickness(20),
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.Wrap
                };
                stackPanel.Children.Add(noAppsText);
            }
            
            // Use MainPanel instead of MainBorder
            MainPanel.Children.Add(stackPanel);
            
            // Set window size based on content
            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Width = Math.Max(300, DesiredSize.Width + 40);
            Height = Math.Max(150, DesiredSize.Height + 40);
        }

        private Border CreateMenuItem(string appPath)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(2, 2, 2, 2),
                Padding = new Thickness(8, 4, 8, 4),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var stackPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };

            // Add icon
            var icon = GetApplicationIcon(appPath);
            var image = new Image
            {
                Source = icon,
                Width = 24,
                Height = 24,
                Margin = new Thickness(0, 0, 8, 0)
            };

            // Add text
            var textBlock = new TextBlock
            {
                Text = Path.GetFileNameWithoutExtension(appPath),
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12
            };

            stackPanel.Children.Add(image);
            stackPanel.Children.Add(textBlock);
            border.Child = stackPanel;

            // Add hover effects
            border.MouseEnter += (s, e) => border.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            border.MouseLeave += (s, e) => border.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
            border.MouseUp += (s, e) => LaunchApplication(appPath);

            return border;
        }

        private ImageSource GetApplicationIcon(string appPath)
        {
            try
            {
                if (File.Exists(appPath))
                {
                    var icon = System.Drawing.Icon.ExtractAssociatedIcon(appPath);
                    if (icon != null)
                    {
                        return System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                            icon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                    }
                }
            }
            catch
            {
                // Return default icon if extraction fails
            }

            // Return default application icon
            return new BitmapImage(new Uri("pack://application:,,,/Resources/default_app.png"));
        }

        private void LaunchApplication(string appPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = appPath,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
                
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to launch application: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PositionWindow()
        {
            // Get mouse position from command line args or current position
            var mousePos = GetCursorPosition();
            
            // Get screen dimensions and taskbar info
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            var taskbarRect = GetTaskbarRect();
            
            // Calculate window position based on taskbar location
            double left, top;
            
            if (taskbarRect.Contains(mousePos.X, mousePos.Y)) // Click on taskbar
            {
                if (taskbarRect.Top == screen.Bounds.Top && taskbarRect.Width == screen.Bounds.Width)
                {
                    // TOP taskbar
                    top = screen.Bounds.Y + taskbarRect.Height + 10;
                    left = mousePos.X - (Width / 2);
                }
                else if (taskbarRect.Bottom == screen.Bounds.Bottom && taskbarRect.Width == screen.Bounds.Width)
                {
                    // BOTTOM taskbar
                    top = screen.Bounds.Y + screen.Bounds.Height - Height - taskbarRect.Height - 10;
                    left = mousePos.X - (Width / 2);
                }
                else if (taskbarRect.Left == screen.Bounds.Left)
                {
                    // LEFT taskbar
                    top = mousePos.Y - (Height / 2);
                    left = screen.Bounds.X + taskbarRect.Width + 10;
                }
                else
                {
                    // RIGHT taskbar
                    top = mousePos.Y - (Height / 2);
                    left = screen.Bounds.X + screen.Bounds.Width - Width - taskbarRect.Width - 10;
                }
            }
            else // Not click on taskbar
            {
                top = mousePos.Y - Height - 20;
                left = mousePos.X - (Width / 2);
            }
            
            // Adjust if window goes off screen
            if (left < screen.Bounds.Left)
                left = screen.Bounds.Left + 10;
            if (top < screen.Bounds.Top)
                top = screen.Bounds.Top + 10;
            if (left + Width > screen.Bounds.Right)
                left = screen.Bounds.Right - Width - 10;
            
            // If window goes over taskbar
            if (taskbarRect.Contains((int)left, (int)top) && taskbarRect.Contains((int)(left + Width), (int)top))
                top = screen.Bounds.Top + 10 + taskbarRect.Height;
            if (taskbarRect.Contains((int)left, (int)top))
                left = screen.Bounds.Left + 10 + taskbarRect.Width;
            if (taskbarRect.Contains((int)(left + Width), (int)top))
                left = screen.Bounds.Right - Width - 10 - taskbarRect.Width;
            
            Left = left;
            Top = top;
        }

        private System.Drawing.Rectangle GetTaskbarRect()
        {
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            var bounds = screen.Bounds;
            var workingArea = screen.WorkingArea;
            
            var leftDockedWidth = Math.Abs(bounds.Left - workingArea.Left);
            var topDockedHeight = Math.Abs(bounds.Top - workingArea.Top);
            var rightDockedWidth = (bounds.Width - leftDockedWidth) - workingArea.Width;
            var bottomDockedHeight = (bounds.Height - topDockedHeight) - workingArea.Height;
            
            var taskbarRect = new System.Drawing.Rectangle();
            
            if (leftDockedWidth > 0)
            {
                taskbarRect.X = bounds.Left;
                taskbarRect.Y = bounds.Top;
                taskbarRect.Width = leftDockedWidth;
                taskbarRect.Height = bounds.Height;
            }
            else if (rightDockedWidth > 0)
            {
                taskbarRect.X = workingArea.Right;
                taskbarRect.Y = bounds.Top;
                taskbarRect.Width = rightDockedWidth;
                taskbarRect.Height = bounds.Height;
            }
            else if (topDockedHeight > 0)
            {
                taskbarRect.X = workingArea.Left;
                taskbarRect.Y = bounds.Top;
                taskbarRect.Width = workingArea.Width;
                taskbarRect.Height = topDockedHeight;
            }
            else if (bottomDockedHeight > 0)
            {
                taskbarRect.X = workingArea.Left;
                taskbarRect.Y = workingArea.Bottom;
                taskbarRect.Width = workingArea.Width;
                taskbarRect.Height = bottomDockedHeight;
            }
            else
            {
                // Auto-hide taskbar - use default bottom position
                taskbarRect.X = workingArea.Left;
                taskbarRect.Y = screen.Bounds.Height - 40;
                taskbarRect.Width = workingArea.Width;
                taskbarRect.Height = 40;
            }
            
            return taskbarRect;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private POINT GetCursorPosition()
        {
            POINT point;
            GetCursorPos(out point);
            return point;
        }

        private void GroupMenuWindow_Deactivated(object sender, EventArgs e)
        {
            Close();
        }

        private void GroupMenuWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                Close();
            }
        }
    }
}
