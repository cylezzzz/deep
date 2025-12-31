using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DeepSeeArch.Models;
using Serilog;

namespace DeepSeeArch.UI.ViewModels
{
    public class ResultDetailViewModel : INotifyPropertyChanged
    {
        private SearchResult _result;

        public SearchResult Result
        {
            get => _result;
            set
            {
                _result = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasAccountInfo));
                OnPropertyChanged(nameof(HasFuzzyMatch));
                OnPropertyChanged(nameof(IsPotentialMisuse));
            }
        }

        public bool HasAccountInfo => Result?.AccountInfo != null;
        public bool HasFuzzyMatch => Result?.FuzzyMatch != null;
        public bool IsPotentialMisuse => Result?.AccountInfo?.IsPotentialMisuse ?? false;

        public ICommand OpenUrlCommand { get; }

        public ResultDetailViewModel(SearchResult result)
        {
            _result = result;
            OpenUrlCommand = new RelayCommand(ExecuteOpenUrl);
        }

        private void ExecuteOpenUrl()
        {
            if (string.IsNullOrEmpty(Result?.Url))
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Result.Url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error opening URL {Url}", Result.Url);
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
