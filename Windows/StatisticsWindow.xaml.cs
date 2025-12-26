using System.Windows;
using TaskbarGroupTool.ViewModels;

namespace TaskbarGroupTool.Windows
{
    public partial class StatisticsWindow : Window
    {
        private StatisticsViewModel viewModel;

        public StatisticsWindow()
        {
            InitializeComponent();
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            viewModel = new StatisticsViewModel();
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
