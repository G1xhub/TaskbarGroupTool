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
            Width = 300;  // Initial width, will be adjusted
            Height = 150; // Initial height, will be adjusted
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
                Margin = new Thickness(5) // Reduced from 10
            };
            
            int appCount = 0;
            
            if (currentGroup != null && currentGroup.Applications.Any())
            {
                foreach (var appPath in currentGroup.Applications)
                {
                    var item = CreateMenuItem(appPath);
                    stackPanel.Children.Add(item);
                    appCount++;
                }
            }
            else
            {
                var noAppsText = new TextBlock
                {
                    Text = "No applications in this group",
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Margin = new Thickness(15), // Reduced from 20
                    Foreground = Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    TextWrapping = TextWrapping.Wrap
                };
                stackPanel.Children.Add(noAppsText);
            }
            
            // Use MainPanel instead of MainBorder
            MainPanel.Children.Add(stackPanel);
            
            // Simple size calculation based on app count
            CalculateSimpleSize(appCount);
        }
        
        private void CalculateSimpleSize(int appCount)
        {
            // Base size - no padding at all
            double baseWidth = 320;
            double baseHeight = 0; // No base height
            
            // Size per app - adjusted for larger icons/text
            double heightPerApp = 35;
            // Formula: Höhe = (Anzahl × 35)
            
            // Calculate final size using the exact formula
            double finalWidth = baseWidth;
            double finalHeight = appCount * heightPerApp;
            
            // Apply reasonable limits
            finalWidth = Math.Max(280, Math.Min(600, finalWidth));
            finalHeight = Math.Max(35, Math.Min(400, finalHeight)); // Min height for at least one app
            
            // Apply size
            Width = finalWidth;
            Height = finalHeight;
            
            // Debug output
            System.Diagnostics.Debug.WriteLine($"Apps: {appCount}, Window size: {Width}x{Height}");
        }
        
        private void CalculateOptimalSize()
        {
            // Force measure to get accurate content size
            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            UpdateLayout();
            
            // Get the actual content size
            var contentSize = MainPanel.DesiredSize;
            
            // Calculate minimum dimensions
            double minWidth = 300;
            double minHeight = 150;
            double maxWidth = 800;  // Increased max width
            double maxHeight = 700; // Increased max height
            
            // Calculate optimal width based on content
            double optimalWidth = Math.Max(minWidth, contentSize.Width + 60); // More padding
            optimalWidth = Math.Min(maxWidth, optimalWidth);
            
            // Calculate optimal height based on content
            double optimalHeight = Math.Max(minHeight, contentSize.Height + 60); // More padding
            optimalHeight = Math.Min(maxHeight, optimalHeight);
            
            // Apply the calculated size
            Width = optimalWidth;
            Height = optimalHeight;
            
            // Ensure window fits on screen
            EnsureWindowFitsScreen();
        }
        
        private void EnsureWindowFitsScreen()
        {
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            var workingArea = screen.WorkingArea;
            
            // Adjust width if needed
            if (Width > workingArea.Width)
            {
                Width = workingArea.Width - 20;
            }
            
            // Adjust height if needed
            if (Height > workingArea.Height)
            {
                Height = workingArea.Height - 20;
            }
            
            // Center window if it's too large
            if (Left + Width > workingArea.Right)
            {
                Left = workingArea.Left + (workingArea.Width - Width) / 2;
            }
            
            if (Top + Height > workingArea.Bottom)
            {
                Top = workingArea.Top + (workingArea.Height - Height) / 2;
            }
        }

        private Border CreateMenuItem(string appPath)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0)),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(0, 0, 0, 0), // No margin
                Padding = new Thickness(6, 3, 6, 3), // Slightly increased padding
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var stackPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };

            // Add icon - 25% larger
            var icon = GetApplicationIcon(appPath);
            var image = new Image
            {
                Source = icon,
                Width = 23, // Increased from 18 (18 * 1.25 = 22.5, rounded to 23)
                Height = 23, // Increased from 18
                Margin = new Thickness(0, 0, 6, 0) // Slightly increased margin
            };

            // Add text - 25% larger
            var textBlock = new TextBlock
            {
                Text = Path.GetFileNameWithoutExtension(appPath),
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14, // Increased from 11 (11 * 1.25 = 13.75, rounded to 14)
                FontWeight = FontWeights.Medium
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
