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
        private StatisticsService statisticsService;
        private readonly ThemeService themeService;

        // Theme colors
        private Color bgColor;
        private Color borderColor;
        private Color textColor;
        private Color textSecondaryColor;
        private Color hoverColor;
        private Color headerColor;

        [DllImport("shell32.dll", SetLastError = true)]
        static extern void SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

        public GroupMenuWindow(string groupName)
        {
            InitializeComponent();

            themeService = ThemeService.Instance;
            statisticsService = new StatisticsService();

            // Load theme
            try { themeService.LoadThemePreference(); } catch { }
            ApplyPopupTheme(themeService.IsDarkMode);

            // Load the group
            currentGroup = LoadGroup(groupName);

            Title = $"Group: {groupName}";
            Width = 300;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            LoadMenuItems();

            Deactivated += GroupMenuWindow_Deactivated;
            KeyDown += GroupMenuWindow_KeyDown;
        }

        private void ApplyPopupTheme(bool isDarkMode)
        {
            if (isDarkMode)
            {
                bgColor = ColorFromHex("#1E1E28");
                borderColor = ColorFromHex("#2E2E3A");
                textColor = ColorFromHex("#D4D2CC");
                textSecondaryColor = ColorFromHex("#7A7872");
                hoverColor = ColorFromHex("#282834");
                headerColor = ColorFromHex("#8B7D6B");
            }
            else
            {
                bgColor = ColorFromHex("#F5F3EF");
                borderColor = ColorFromHex("#C8C4BC");
                textColor = ColorFromHex("#2A2A2A");
                textSecondaryColor = ColorFromHex("#6B6860");
                hoverColor = ColorFromHex("#DDD9D0");
                headerColor = ColorFromHex("#8B7D6B");
            }

            MainBorder.Background = new SolidColorBrush(bgColor);
            MainBorder.BorderBrush = new SolidColorBrush(borderColor);
        }

        private static Color ColorFromHex(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
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
                Margin = new Thickness(2)
            };

            // Add group name header
            var headerText = new TextBlock
            {
                Text = currentGroup.Name.ToUpper(),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Margin = new Thickness(8, 4, 8, 6),
                Foreground = new SolidColorBrush(headerColor),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 10,
                FontWeight = FontWeights.Bold
            };
            stackPanel.Children.Add(headerText);

            // Add separator
            var separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(borderColor),
                Margin = new Thickness(4, 0, 4, 4)
            };
            stackPanel.Children.Add(separator);

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
                    Text = "[ NO APPLICATIONS ]",
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                    VerticalAlignment = System.Windows.VerticalAlignment.Center,
                    Margin = new Thickness(12, 8, 12, 8),
                    Foreground = new SolidColorBrush(textSecondaryColor),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    FontWeight = FontWeights.Normal
                };
                stackPanel.Children.Add(noAppsText);
            }

            MainPanel.Children.Add(stackPanel);
            CalculateSimpleSize(appCount);
        }

        private void CalculateSimpleSize(int appCount)
        {
            double baseWidth = 300;
            // Header (20) + separator (5) + apps
            double headerHeight = 28;
            double heightPerApp = 34;

            double finalWidth = baseWidth;
            double finalHeight = headerHeight + (appCount * heightPerApp) + 16; // 16 for padding

            finalWidth = Math.Max(260, Math.Min(500, finalWidth));
            finalHeight = Math.Max(60, Math.Min(400, finalHeight));

            Width = finalWidth;
            Height = finalHeight;
        }

        private void CalculateOptimalSize()
        {
            Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            UpdateLayout();

            var contentSize = MainPanel.DesiredSize;

            double minWidth = 300;
            double minHeight = 150;
            double maxWidth = 800;
            double maxHeight = 700;

            double optimalWidth = Math.Max(minWidth, contentSize.Width + 60);
            optimalWidth = Math.Min(maxWidth, optimalWidth);

            double optimalHeight = Math.Max(minHeight, contentSize.Height + 60);
            optimalHeight = Math.Min(maxHeight, optimalHeight);

            Width = optimalWidth;
            Height = optimalHeight;

            EnsureWindowFitsScreen();
        }

        private void EnsureWindowFitsScreen()
        {
            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            var workingArea = screen.WorkingArea;

            if (Width > workingArea.Width)
                Width = workingArea.Width - 20;

            if (Height > workingArea.Height)
                Height = workingArea.Height - 20;

            if (Left + Width > workingArea.Right)
                Left = workingArea.Left + (workingArea.Width - Width) / 2;

            if (Top + Height > workingArea.Bottom)
                Top = workingArea.Top + (workingArea.Height - Height) / 2;
        }

        private Border CreateMenuItem(string appPath)
        {
            var border = new Border
            {
                Background = Brushes.Transparent,
                Margin = new Thickness(2, 0, 2, 1),
                Padding = new Thickness(8, 5, 8, 5),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var stackPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };

            var icon = GetApplicationIcon(appPath);
            var image = new Image
            {
                Source = icon,
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 10, 0),
                RenderOptions =
                {
                    BitmapScalingMode = BitmapScalingMode.HighQuality
                }
            };

            var textBlock = new TextBlock
            {
                Text = Path.GetFileNameWithoutExtension(appPath),
                Foreground = new SolidColorBrush(textColor),
                VerticalAlignment = VerticalAlignment.Center,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                FontWeight = FontWeights.Normal
            };

            stackPanel.Children.Add(image);
            stackPanel.Children.Add(textBlock);
            border.Child = stackPanel;

            var hoverBrush = new SolidColorBrush(hoverColor);
            border.MouseEnter += (s, e) => border.Background = hoverBrush;
            border.MouseLeave += (s, e) => border.Background = Brushes.Transparent;
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

                var appName = Path.GetFileNameWithoutExtension(appPath);
                statisticsService.RecordApplicationLaunch(appPath, appName);
                statisticsService.RecordGroupLaunch(currentGroup.Name);

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
            var mousePos = GetCursorPosition();

            var screen = System.Windows.Forms.Screen.PrimaryScreen;
            var taskbarRect = GetTaskbarRect();

            double left, top;

            if (taskbarRect.Contains(mousePos.X, mousePos.Y))
            {
                if (taskbarRect.Top == screen.Bounds.Top && taskbarRect.Width == screen.Bounds.Width)
                {
                    top = screen.Bounds.Y + taskbarRect.Height + 10;
                    left = mousePos.X - (Width / 2);
                }
                else if (taskbarRect.Bottom == screen.Bounds.Bottom && taskbarRect.Width == screen.Bounds.Width)
                {
                    top = screen.Bounds.Y + screen.Bounds.Height - Height - taskbarRect.Height - 10;
                    left = mousePos.X - (Width / 2);
                }
                else if (taskbarRect.Left == screen.Bounds.Left)
                {
                    top = mousePos.Y - (Height / 2);
                    left = screen.Bounds.X + taskbarRect.Width + 10;
                }
                else
                {
                    top = mousePos.Y - (Height / 2);
                    left = screen.Bounds.X + screen.Bounds.Width - Width - taskbarRect.Width - 10;
                }
            }
            else
            {
                top = mousePos.Y - Height - 20;
                left = mousePos.X - (Width / 2);
            }

            if (left < screen.Bounds.Left)
                left = screen.Bounds.Left + 10;
            if (top < screen.Bounds.Top)
                top = screen.Bounds.Top + 10;
            if (left + Width > screen.Bounds.Right)
                left = screen.Bounds.Right - Width - 10;

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
