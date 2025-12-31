using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DeepSeeArch.Core;
using DeepSeeArch.Models;
using DeepSeeArch.Storage;
using Serilog;

namespace DeepSeeArch.UI.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly Scanner _scanner;
        private readonly CaseStorageManager _storageManager;
        private SearchCase? _currentCase;
        private ObservableCollection<SearchResult> _allResults;
        private ObservableCollection<SearchResult> _filteredResults;

        #region Properties

        private string _query = string.Empty;
        public string Query
        {
            get => _query;
            set
            {
                _query = value;
                OnPropertyChanged();
            }
        }

        private bool _isScanning;
        public bool IsScanning
        {
            get => _isScanning;
            set
            {
                _isScanning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotScanning));
                OnPropertyChanged(nameof(HasResults));
                OnPropertyChanged(nameof(ShowNoResults));
            }
        }

        public bool IsNotScanning => !IsScanning;

        private string _statusMessage = "Bereit";
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SearchResult> FilteredResults
        {
            get => _filteredResults;
            set
            {
                _filteredResults = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ResultCount));
                OnPropertyChanged(nameof(HasResults));
                OnPropertyChanged(nameof(ShowNoResults));
            }
        }

        public int ResultCount => FilteredResults?.Count ?? 0;
        public bool HasResults => !IsScanning && ResultCount > 0;
        public bool ShowNoResults => !IsScanning && ResultCount == 0 && _allResults.Count == 0;

        #region Filter Properties

        private bool _showAllCategories = true;
        public bool ShowAllCategories
        {
            get => _showAllCategories;
            set
            {
                _showAllCategories = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        private bool _showWebOnly;
        public bool ShowWebOnly
        {
            get => _showWebOnly;
            set
            {
                _showWebOnly = value;
                OnPropertyChanged();
                if (value) ApplyFilter();
            }
        }

        private bool _showImagesOnly;
        public bool ShowImagesOnly
        {
            get => _showImagesOnly;
            set
            {
                _showImagesOnly = value;
                OnPropertyChanged();
                if (value) ApplyFilter();
            }
        }

        private bool _showVideosOnly;
        public bool ShowVideosOnly
        {
            get => _showVideosOnly;
            set
            {
                _showVideosOnly = value;
                OnPropertyChanged();
                if (value) ApplyFilter();
            }
        }

        private bool _showSocialOnly;
        public bool ShowSocialOnly
        {
            get => _showSocialOnly;
            set
            {
                _showSocialOnly = value;
                OnPropertyChanged();
                if (value) ApplyFilter();
            }
        }

        private bool _showForumsOnly;
        public bool ShowForumsOnly
        {
            get => _showForumsOnly;
            set
            {
                _showForumsOnly = value;
                OnPropertyChanged();
                if (value) ApplyFilter();
            }
        }

        private bool _showArchivesOnly;
        public bool ShowArchivesOnly
        {
            get => _showArchivesOnly;
            set
            {
                _showArchivesOnly = value;
                OnPropertyChanged();
                if (value) ApplyFilter();
            }
        }

        private bool _showAdultOnly;
        public bool ShowAdultOnly
        {
            get => _showAdultOnly;
            set
            {
                _showAdultOnly = value;
                OnPropertyChanged();
                if (value) ApplyFilter();
            }
        }

        private bool _showDocumentsOnly;
        public bool ShowDocumentsOnly
        {
            get => _showDocumentsOnly;
            set
            {
                _showDocumentsOnly = value;
                OnPropertyChanged();
                if (value) ApplyFilter();
            }
        }

        #endregion

        #endregion

        #region Commands

        public ICommand ScanCommand { get; }
        public ICommand SaveCaseCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand OpenCasesCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand OpenUrlCommand { get; }
        public ICommand ShowDetailsCommand { get; }
        
        // NEU: Markierungs-System
        public ICommand ToggleMarkCommand { get; }
        public ICommand ShowReportCommand { get; }
        public ICommand OpenMediaCommand { get; }

        public int MarkedCount => _allResults?.Count(r => r.IsMarked) ?? 0;

        #endregion

        public MainViewModel()
        {
            _scanner = new Scanner();
            _storageManager = new CaseStorageManager();
            _allResults = new ObservableCollection<SearchResult>();
            _filteredResults = new ObservableCollection<SearchResult>();

            // Commands initialisieren
            ScanCommand = new AsyncRelayCommand(ExecuteScanAsync, CanExecuteScan);
            SaveCaseCommand = new AsyncRelayCommand(ExecuteSaveCaseAsync, CanExecuteSaveCase);
            ExportCommand = new AsyncRelayCommand(ExecuteExportAsync, CanExecuteExport);
            OpenCasesCommand = new RelayCommand(ExecuteOpenCases);
            OpenSettingsCommand = new RelayCommand(ExecuteOpenSettings);
            OpenUrlCommand = new RelayCommand<string>(ExecuteOpenUrl);
            ShowDetailsCommand = new RelayCommand<SearchResult>(ExecuteShowDetails);
            
            // NEU: Markierungs-Commands
            ToggleMarkCommand = new RelayCommand<SearchResult>(ExecuteToggleMark);
            ShowReportCommand = new AsyncRelayCommand(ExecuteShowReportAsync, () => MarkedCount > 0);
            OpenMediaCommand = new RelayCommand<SearchResult>(ExecuteOpenMedia);

            Log.Information("MainViewModel initialized");
        }

        #region Command Implementations

        private bool CanExecuteScan()
        {
            return !IsScanning && !string.IsNullOrWhiteSpace(Query);
        }

        private async Task ExecuteScanAsync()
        {
            IsScanning = true;
            StatusMessage = "Scanne...";

            try
            {
                Log.Information("Starting scan for: {Query}", Query);

                // Neuen Case erstellen
                _currentCase = await _storageManager.CreateCaseAsync(
                    $"Scan: {Query}",
                    Query
                );

                // Scan durchführen
                var results = await _scanner.ScanAsync(Query);

                // Ergebnisse zum Case hinzufügen
                _allResults.Clear();
                foreach (var result in results)
                {
                    _allResults.Add(result);
                }

                // Case speichern
                _currentCase.Results = results;
                await _storageManager.SaveCaseAsync(_currentCase);

                // Filter anwenden
                ApplyFilter();

                StatusMessage = $"Scan abgeschlossen: {results.Count} Ergebnisse gefunden";
                Log.Information("Scan completed: {Count} results", results.Count);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fehler: {ex.Message}";
                Log.Error(ex, "Error during scan");
                MessageBox.Show($"Fehler beim Scannen: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsScanning = false;
            }
        }

        private bool CanExecuteSaveCase()
        {
            return _currentCase != null && _allResults.Count > 0;
        }

        private async Task ExecuteSaveCaseAsync()
        {
            if (_currentCase == null)
                return;

            try
            {
                await _storageManager.SaveCaseAsync(_currentCase);
                StatusMessage = $"Case '{_currentCase.Name}' gespeichert";
                MessageBox.Show("Case erfolgreich gespeichert!", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving case");
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteExport()
        {
            return _currentCase != null && _allResults.Count > 0;
        }

        private async Task ExecuteExportAsync()
        {
            if (_currentCase == null)
                return;

            try
            {
                var result = MessageBox.Show(
                    "Als CSV oder JSON exportieren?\n\nJa = CSV\nNein = JSON",
                    "Export-Format wählen",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Cancel)
                    return;

                string exportPath;
                if (result == MessageBoxResult.Yes)
                {
                    exportPath = await _storageManager.ExportCaseToCsvAsync(_currentCase.Id);
                }
                else
                {
                    exportPath = await _storageManager.ExportCaseAsync(_currentCase.Id);
                }

                StatusMessage = $"Exportiert nach: {exportPath}";
                
                var openResult = MessageBox.Show(
                    $"Export erfolgreich!\n\n{exportPath}\n\nOrdner öffnen?",
                    "Export erfolgreich",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information
                );

                if (openResult == MessageBoxResult.Yes)
                {
                    Process.Start("explorer.exe", System.IO.Path.GetDirectoryName(exportPath)!);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error exporting case");
                MessageBox.Show($"Fehler beim Exportieren: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteOpenCases()
        {
            // Hier würde ein Cases-Verwaltungsfenster geöffnet werden
            MessageBox.Show("Cases-Verwaltung wird geöffnet...\n(Noch nicht implementiert)", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteOpenSettings()
        {
            // Hier würden Einstellungen geöffnet werden
            MessageBox.Show("Einstellungen werden geöffnet...\n(Noch nicht implementiert)", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteOpenUrl(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error opening URL {Url}", url);
                MessageBox.Show($"Fehler beim Öffnen der URL: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteShowDetails(SearchResult? result)
        {
            if (result == null)
                return;

            try
            {
                var detailViewModel = new ResultDetailViewModel(result);
                var detailWindow = new ResultDetailWindow(detailViewModel)
                {
                    Owner = Application.Current.MainWindow
                };
                detailWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error showing details");
                MessageBox.Show($"Fehler beim Öffnen der Details: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Neue Command Implementations

        private void ExecuteToggleMark(SearchResult? result)
        {
            if (result == null)
                return;

            result.IsMarked = !result.IsMarked;
            
            if (result.IsMarked)
            {
                result.MarkedAt = DateTime.Now;
                
                // Optional: Notizen abfragen
                var notesDialog = new Microsoft.VisualBasic.Interaction.InputBox(
                    "Notizen zu diesem Ergebnis (optional):",
                    "Markierung",
                    result.MarkedNotes ?? "",
                    -1, -1
                );
                
                if (!string.IsNullOrWhiteSpace(notesDialog))
                {
                    result.MarkedNotes = notesDialog;
                }
                
                Log.Information("Marked result: {Title}", result.Title);
            }
            else
            {
                result.MarkedAt = null;
                result.MarkedNotes = null;
                Log.Information("Unmarked result: {Title}", result.Title);
            }

            OnPropertyChanged(nameof(MarkedCount));
            CommandManager.InvalidateRequerySuggested();
        }

        private async Task ExecuteShowReportAsync()
        {
            if (_currentCase == null || _allResults == null)
                return;

            try
            {
                var markedResults = _allResults.Where(r => r.IsMarked).ToList();
                
                if (!markedResults.Any())
                {
                    MessageBox.Show("Keine markierten Ergebnisse vorhanden.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                StatusMessage = "Erstelle Abschlussbericht...";

                var reportGenerator = new ReportGenerator();
                var reportContent = await reportGenerator.GenerateMarkdownReportAsync(markedResults, Query);
                
                // Speichern
                var reportPath = await reportGenerator.SaveReportAsync(
                    reportContent,
                    Path.Combine(_currentCase.StoragePath, "Reports"),
                    Query
                );

                StatusMessage = $"Bericht erstellt: {markedResults.Count} Ergebnisse";

                // Anzeigen
                var result = MessageBox.Show(
                    $"Abschlussbericht erstellt!\n\n" +
                    $"Markierte Ergebnisse: {markedResults.Count}\n" +
                    $"Gespeichert in:\n{reportPath}\n\n" +
                    $"Bericht jetzt öffnen?",
                    "Bericht erstellt",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information
                );

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = reportPath,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error generating report");
                MessageBox.Show($"Fehler beim Erstellen des Berichts: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteOpenMedia(SearchResult? result)
        {
            if (result == null)
                return;

            try
            {
                var mediaLinks = result.MediaLinks?.ToList() ?? new List<string>();
                
                if (!mediaLinks.Any())
                {
                    MessageBox.Show("Keine Medien-Links gefunden.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var mediaViewer = new MediaViewerWindow(mediaLinks)
                {
                    Owner = Application.Current.MainWindow
                };
                mediaViewer.ShowDialog();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error opening media");
                MessageBox.Show($"Fehler beim Öffnen der Medien: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Filter Logic

        private void ApplyFilter()
        {
            if (_allResults == null || _allResults.Count == 0)
            {
                FilteredResults = new ObservableCollection<SearchResult>();
                return;
            }

            IEnumerable<SearchResult> filtered = _allResults;

            // Kategorie-Filter
            if (!ShowAllCategories)
            {
                if (ShowWebOnly)
                    filtered = filtered.Where(r => r.Category == ResultCategory.Web);
                else if (ShowImagesOnly)
                    filtered = filtered.Where(r => r.Category == ResultCategory.Image);
                else if (ShowVideosOnly)
                    filtered = filtered.Where(r => r.Category == ResultCategory.Video);
                else if (ShowSocialOnly)
                    filtered = filtered.Where(r => r.Category == ResultCategory.Social || r.Category == ResultCategory.Profile);
                else if (ShowForumsOnly)
                    filtered = filtered.Where(r => r.Category == ResultCategory.Forum);
                else if (ShowArchivesOnly)
                    filtered = filtered.Where(r => r.Category == ResultCategory.Archive);
                else if (ShowAdultOnly)
                    filtered = filtered.Where(r => r.Category == ResultCategory.Adult);
                else if (ShowDocumentsOnly)
                    filtered = filtered.Where(r => r.Category == ResultCategory.Document);
            }

            // Nach Confidence sortieren
            filtered = filtered.OrderByDescending(r => r.ConfidenceScore);

            FilteredResults = new ObservableCollection<SearchResult>(filtered);
            
            Log.Debug("Filter applied: {Count} results", FilteredResults.Count);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #region Command Classes

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

        public void Execute(object? parameter) => _execute((T?)parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute?.Invoke() ?? true);
        }

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public event EventHandler? CanExecuteChanged;

        private void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    #endregion
}
