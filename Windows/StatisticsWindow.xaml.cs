using System.Windows;
using System.Windows.Media;
using TaskbarGroupTool.ViewModels;
using TaskbarGroupTool.Services;

namespace TaskbarGroupTool.Windows
{
    public partial class StatisticsWindow : Window
    {
        private StatisticsViewModel viewModel;
        private readonly ThemeService themeService;

        public StatisticsWindow()
        {
            InitializeComponent();
            themeService = ThemeService.Instance;
            InitializeViewModel();
            ApplyTheme(themeService.IsDarkMode);
        }

        private void InitializeViewModel()
        {
            viewModel = new StatisticsViewModel();
            DataContext = viewModel;
        }

        private static Color HexColor(string hex)
        {
            return (Color)ColorConverter.ConvertFromString(hex);
        }

        private void ApplyTheme(bool isDarkMode)
        {
            if (isDarkMode)
            {
                Resources["WinBackground"]   = new SolidColorBrush(HexColor("#141419"));
                Resources["WinCard"]          = new SolidColorBrush(HexColor("#1E1E28"));
                Resources["WinBorder"]        = new SolidColorBrush(HexColor("#2E2E3A"));
                Resources["WinInput"]         = new SolidColorBrush(HexColor("#16161C"));
                Resources["WinTextPrimary"]   = new SolidColorBrush(HexColor("#D4D2CC"));
                Resources["WinTextSecondary"] = new SolidColorBrush(HexColor("#7A7872"));
                Resources["WinAccent"]        = new SolidColorBrush(HexColor("#8B7D6B"));
                Resources["WinSuccess"]       = new SolidColorBrush(HexColor("#5A9E5A"));
                Resources["WinInfo"]          = new SolidColorBrush(HexColor("#6A9EC0"));
                Resources["WinDanger"]        = new SolidColorBrush(HexColor("#8B3A3A"));
            }
            else
            {
                Resources["WinBackground"]   = new SolidColorBrush(HexColor("#EDEBE6"));
                Resources["WinCard"]          = new SolidColorBrush(HexColor("#F5F3EF"));
                Resources["WinBorder"]        = new SolidColorBrush(HexColor("#C8C4BC"));
                Resources["WinInput"]         = new SolidColorBrush(HexColor("#F9F8F5"));
                Resources["WinTextPrimary"]   = new SolidColorBrush(HexColor("#2A2A2A"));
                Resources["WinTextSecondary"] = new SolidColorBrush(HexColor("#6B6860"));
                Resources["WinAccent"]        = new SolidColorBrush(HexColor("#8B7D6B"));
                Resources["WinSuccess"]       = new SolidColorBrush(HexColor("#4A7C59"));
                Resources["WinInfo"]          = new SolidColorBrush(HexColor("#5A7C9E"));
                Resources["WinDanger"]        = new SolidColorBrush(HexColor("#A04040"));
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
