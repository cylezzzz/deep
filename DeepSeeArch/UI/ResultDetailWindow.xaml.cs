using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using DeepSeeArch.UI.ViewModels;

namespace DeepSeeArch.UI
{
    public partial class ResultDetailWindow : Window
    {
        public ResultDetailWindow(ResultDetailViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}
