using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DeepSeeArch.Core.AI;
using Serilog;

namespace DeepSeeArch.UI.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly string _settingsPath;
        private OllamaAgent? _ollamaAgent;

        public SettingsViewModel()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DeepSeeArch"
            );
            Directory.CreateDirectory(appDataPath);
            _settingsPath = Path.Combine(appDataPath, "settings.json");

            LoadSettings();
            InitializeOllama();
            
            RefreshModelsCommand = new RelayCommand(async () => await LoadAvailableModelsAsync());
            SaveCommand = new RelayCommand(SaveSettings);
        }

        #region Properties

        private string _ollamaUrl = "http://localhost:11434";
        public string OllamaUrl
        {
            get => _ollamaUrl;
            set
            {
                _ollamaUrl = value;
                OnPropertyChanged(nameof(OllamaUrl));
                InitializeOllama();
            }
        }

        private ObservableCollection<string> _availableModels = new();
        public ObservableCollection<string> AvailableModels
        {
            get => _availableModels;
            set
            {
                _availableModels = value;
                OnPropertyChanged(nameof(AvailableModels));
            }
        }

        private string? _selectedPrimaryModel = "deepseek-coder-v2:latest";
        public string? SelectedPrimaryModel
        {
            get => _selectedPrimaryModel;
            set
            {
                _selectedPrimaryModel = value;
                OnPropertyChanged(nameof(SelectedPrimaryModel));
            }
        }

        private string? _selectedAnalysisModel = "qwen2.5:latest";
        public string? SelectedAnalysisModel
        {
            get => _selectedAnalysisModel;
            set
            {
                _selectedAnalysisModel = value;
                OnPropertyChanged(nameof(SelectedAnalysisModel));
            }
        }

        private int _minSimilarityScore = 70;
        public int MinSimilarityScore
        {
            get => _minSimilarityScore;
            set
            {
                _minSimilarityScore = value;
                OnPropertyChanged(nameof(MinSimilarityScore));
            }
        }

        private int _maxResultsPerEngine = 50;
        public int MaxResultsPerEngine
        {
            get => _maxResultsPerEngine;
            set
            {
                _maxResultsPerEngine = value;
                OnPropertyChanged(nameof(MaxResultsPerEngine));
            }
        }

        private int _searchTimeout = 30;
        public int SearchTimeout
        {
            get => _searchTimeout;
            set
            {
                _searchTimeout = value;
                OnPropertyChanged(nameof(SearchTimeout));
            }
        }

        private bool _filterAdultContent = true;
        public bool FilterAdultContent
        {
            get => _filterAdultContent;
            set
            {
                _filterAdultContent = value;
                OnPropertyChanged(nameof(FilterAdultContent));
            }
        }

        #endregion

        #region Commands

        public ICommand RefreshModelsCommand { get; }
        public ICommand SaveCommand { get; }

        #endregion

        #region Methods

        private void InitializeOllama()
        {
            _ollamaAgent = new OllamaAgent(_ollamaUrl, _selectedPrimaryModel ?? "llama2");
            _ = LoadAvailableModelsAsync();
        }

        private async Task LoadAvailableModelsAsync()
        {
            try
            {
                if (_ollamaAgent == null) return;

                var isAvailable = await _ollamaAgent.CheckAvailabilityAsync();
                if (!isAvailable)
                {
                    MessageBox.Show(
                        "Ollama ist nicht erreichbar!\n\nStelle sicher dass Ollama läuft:\n• Windows: ollama serve\n• URL prüfen: " + _ollamaUrl,
                        "Ollama nicht verfügbar",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                var models = await _ollamaAgent.ListAvailableModelsAsync();
                
                AvailableModels.Clear();
                foreach (var model in models.OrderBy(m => m))
                {
                    AvailableModels.Add(model);
                }

                Log.Information("Loaded {Count} Ollama models", models.Count);

                // Auto-select best models
                if (!AvailableModels.Contains(_selectedPrimaryModel))
                {
                    _selectedPrimaryModel = models.FirstOrDefault(m => m.Contains("deepseek-coder-v2")) 
                        ?? models.FirstOrDefault(m => m.Contains("deepseek-coder"))
                        ?? models.FirstOrDefault(m => m.Contains("qwen"))
                        ?? models.FirstOrDefault();
                    OnPropertyChanged(nameof(SelectedPrimaryModel));
                }

                if (!AvailableModels.Contains(_selectedAnalysisModel))
                {
                    _selectedAnalysisModel = models.FirstOrDefault(m => m.Contains("qwen")) 
                        ?? models.FirstOrDefault(m => m.Contains("llama3"))
                        ?? models.FirstOrDefault();
                    OnPropertyChanged(nameof(SelectedAnalysisModel));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading Ollama models");
                MessageBox.Show(
                    $"Fehler beim Laden der Modelle:\n{ex.Message}",
                    "Fehler",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void SaveSettings()
        {
            try
            {
                var settings = new AppSettings
                {
                    OllamaUrl = _ollamaUrl,
                    PrimaryModel = _selectedPrimaryModel ?? "llama2",
                    AnalysisModel = _selectedAnalysisModel ?? "llama2",
                    MinSimilarityScore = _minSimilarityScore,
                    MaxResultsPerEngine = _maxResultsPerEngine,
                    SearchTimeout = _searchTimeout,
                    FilterAdultContent = _filterAdultContent
                };

                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);

                Log.Information("Settings saved to {Path}", _settingsPath);
                MessageBox.Show("Einstellungen gespeichert!", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);

                // Close window
                Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault()?.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error saving settings");
                MessageBox.Show($"Fehler beim Speichern:\n{ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    Log.Information("No settings file found, using defaults");
                    return;
                }

                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                if (settings != null)
                {
                    _ollamaUrl = settings.OllamaUrl;
                    _selectedPrimaryModel = settings.PrimaryModel;
                    _selectedAnalysisModel = settings.AnalysisModel;
                    _minSimilarityScore = settings.MinSimilarityScore;
                    _maxResultsPerEngine = settings.MaxResultsPerEngine;
                    _searchTimeout = settings.SearchTimeout;
                    _filterAdultContent = settings.FilterAdultContent;

                    OnPropertyChanged(string.Empty); // Refresh all
                    Log.Information("Settings loaded from {Path}", _settingsPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error loading settings");
            }
        }

        public static AppSettings? GetCurrentSettings()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DeepSeeArch"
            );
            var settingsPath = Path.Combine(appDataPath, "settings.json");

            if (!File.Exists(settingsPath))
                return new AppSettings();

            try
            {
                var json = File.ReadAllText(settingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    public class AppSettings
    {
        public string OllamaUrl { get; set; } = "http://localhost:11434";
        public string PrimaryModel { get; set; } = "deepseek-coder-v2:latest";
        public string AnalysisModel { get; set; } = "qwen2.5:latest";
        public int MinSimilarityScore { get; set; } = 70;
        public int MaxResultsPerEngine { get; set; } = 50;
        public int SearchTimeout { get; set; } = 30;
        public bool FilterAdultContent { get; set; } = true;
    }
}
