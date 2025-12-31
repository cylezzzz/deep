using System.Windows;
using System.Windows.Input;
using DeepSeeArch.UI.ViewModels;

namespace DeepSeeArch.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var viewModel = DataContext as MainViewModel;
                if (viewModel?.ScanCommand.CanExecute(null) == true)
                {
                    viewModel.ScanCommand.Execute(null);
                }
            }
        }
    }
}