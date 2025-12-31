# DeepSeeArch - Entwickler-Dokumentation

## 🏗️ Architektur-Übersicht

### Layer-Struktur

```
┌─────────────────────────────────────┐
│           UI Layer (WPF)            │
│  - Views (XAML)                     │
│  - ViewModels (MVVM)                │
│  - Commands                         │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│         Core Layer                  │
│  - Scanner (Orchestration)          │
│  - SearchEngineAggregator           │
│  - WebCrawler                       │
│  - ContentExtractor                 │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│         AI Layer                    │
│  - LocalAIEngine                    │
│  - TextAnalyzer                     │
│  - EntityExtractor                  │
│  - SimilarityAnalyzer               │
└────────────┬────────────────────────┘
             │
┌────────────▼────────────────────────┐
│       Storage Layer                 │
│  - CaseStorageManager               │
│  - FileSystem                       │
│  - JSON Serialization               │
└─────────────────────────────────────┘
```

### Datenfluss

```
User Input (Query)
    ↓
MainViewModel.ScanCommand
    ↓
Scanner.ScanAsync()
    ↓
├─ IsUrl? → AnalyzeUrlAsync()
│              ├─ WebCrawler.FetchAsync()
│              ├─ ContentExtractor.ExtractAsync()
│              └─ Return SearchResult
│
└─ Keyword → SearchKeywordAsync()
               ├─ SearchEngineAggregator
               │   ├─ Google
               │   ├─ Bing
               │   ├─ DuckDuckGo
               │   ├─ Social Media
               │   └─ Forums
               ├─ DuplicateDetector.MarkDuplicatesAsync()
               └─ LocalAIEngine.EnrichResultsAsync()
                   ├─ TextAnalyzer.SummarizeAsync()
                   ├─ EntityExtractor.ExtractEntitiesAsync()
                   ├─ PredictCategory()
                   ├─ CalculateRelevance()
                   └─ DetectFakeProfile()
    ↓
CaseStorageManager.SaveCaseAsync()
    ↓
FilteredResults (UI Update)
```

## 🔧 Wichtige Klassen

### Scanner.cs

Haupt-Orchestrator für alle Scan-Operationen.

**Wichtige Methoden**:
- `ScanAsync(query, filter)` - Haupt-Scan-Methode
- `AnalyzeUrlAsync(url)` - Analysiert einzelne URL
- `SearchKeywordAsync(keyword)` - Multi-Engine-Suche

**Erweiterungspunkte**:
- Neue Suchmaschinen in `SearchEngineAggregator` hinzufügen
- Custom Filter-Logik in `ApplyFilter()` implementieren
- Neue Analyse-Schritte vor KI-Anreicherung einfügen

### LocalAIEngine.cs

Lokale KI-Engine ohne Cloud-Abhängigkeit.

**Komponenten**:
- `TextAnalyzer` - Text-Zusammenfassung und Keyword-Extraktion
- `EntityExtractor` - Named Entity Recognition (NER)
- `SimilarityAnalyzer` - Duplikat-Erkennung

**Erweiterung**:
```csharp
// Neues ML.NET Modell hinzufügen
private ITransformer? _customModel;

public void LoadCustomModel(string modelPath)
{
    _customModel = _mlContext.Model.Load(modelPath, out _);
}

// Vorhersage verwenden
public string PredictCustom(SearchResult result)
{
    var data = new CustomData { /* ... */ };
    var prediction = _mlContext.Model
        .CreatePredictionEngine<CustomData, CustomPrediction>(_customModel)
        .Predict(data);
    return prediction.Label;
}
```

### CaseStorageManager.cs

Verwaltet lokale Speicherung.

**Struktur**:
```
Cases/
└── [CaseId]/
    ├── case.json              # Metadaten
    ├── results.json           # Suchergebnisse
    ├── Screenshots/           # Screenshots
    ├── HTML/                  # Gespeicherte Seiten
    ├── Media/                 # Downloads
    └── Exports/              # Exports
        ├── export_20240101_120000.json
        └── export_20240101_120000.csv
```

**Methoden**:
- `CreateCaseAsync()` - Neuen Case erstellen
- `SaveCaseAsync()` - Case speichern
- `LoadCaseAsync()` - Case laden
- `ExportCaseAsync()` - Als JSON exportieren
- `ExportCaseToCsvAsync()` - Als CSV exportieren

## 🎨 UI-Entwicklung

### XAML-Struktur

Das UI verwendet Material Design und ModernWPF:

```xml
<!-- Header -->
<materialDesign:ColorZone Mode="PrimaryMid">
    <!-- Logo & Titel -->
</materialDesign:ColorZone>

<!-- Suchfeld -->
<TextBox Style="{StaticResource SearchBoxStyle}"/>

<!-- Kategorie-Tabs -->
<RadioButton Style="{StaticResource CategoryTabStyle}"/>

<!-- Ergebnisse -->
<ItemsControl ItemsSource="{Binding FilteredResults}">
    <materialDesign:Card>
        <!-- Ergebnis-Karte -->
    </materialDesign:Card>
</ItemsControl>

<!-- Status Bar -->
<materialDesign:ColorZone Mode="PrimaryLight">
    <!-- Status & Aktionen -->
</materialDesign:ColorZone>
```

### ViewModel-Pattern

```csharp
// Property mit INotifyPropertyChanged
private string _property;
public string Property
{
    get => _property;
    set
    {
        _property = value;
        OnPropertyChanged();
    }
}

// Command
public ICommand MyCommand { get; }

// Constructor
public MainViewModel()
{
    MyCommand = new AsyncRelayCommand(ExecuteMyAsync, CanExecuteMy);
}

// Implementation
private async Task ExecuteMyAsync()
{
    // Async Logik
}

private bool CanExecuteMy()
{
    return /* Bedingung */;
}
```

## 🔍 Erweiterungen

### Neue Suchmaschine hinzufügen

**1. In SearchEngineAggregator.cs**:

```csharp
public async Task<List<SearchResult>> SearchNewEngineAsync(string query)
{
    var results = new List<SearchResult>();
    
    try
    {
        var encodedQuery = HttpUtility.UrlEncode(query);
        var url = $"https://newengine.com/search?q={encodedQuery}";
        
        var html = await FetchHtmlAsync(url);
        if (string.IsNullOrEmpty(html))
            return results;
        
        return ParseNewEngineResults(html, query);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error searching NewEngine");
        return results;
    }
}

private List<SearchResult> ParseNewEngineResults(string html, string query)
{
    var results = new List<SearchResult>();
    var doc = new HtmlDocument();
    doc.LoadHtml(html);
    
    // Custom Parsing-Logik
    var items = doc.DocumentNode.SelectNodes("//div[@class='result']");
    
    foreach (var item in items)
    {
        // Titel, URL, Snippet extrahieren
        results.Add(new SearchResult
        {
            Title = /* ... */,
            Url = /* ... */,
            Snippet = /* ... */,
            // ...
        });
    }
    
    return results;
}
```

**2. In Scanner.cs**:

```csharp
private async Task<List<SearchResult>> SearchKeywordAsync(string keyword)
{
    // Bestehende Engines...
    
    // Neue Engine hinzufügen
    var newEngineResults = await _searchAggregator.SearchNewEngineAsync(keyword);
    allResults.AddRange(newEngineResults);
    
    // ...
}
```

### Neuen KI-Analyzer hinzufügen

```csharp
public class SentimentAnalyzer
{
    public async Task<SentimentResult> AnalyzeSentimentAsync(string text)
    {
        // ML.NET Sentiment Analysis
        // oder einfache Keyword-basierte Analyse
        
        return new SentimentResult
        {
            Score = /* 0.0 - 1.0 */,
            Label = /* Positive/Negative/Neutral */
        };
    }
}

// In LocalAIEngine.cs integrieren
private readonly SentimentAnalyzer _sentimentAnalyzer;

public async Task<SearchResult> EnrichResultAsync(SearchResult result)
{
    // Bestehende Analysen...
    
    // Sentiment hinzufügen
    var sentiment = await _sentimentAnalyzer.AnalyzeSentimentAsync(result.ExtractedText);
    result.Metadata["Sentiment"] = sentiment.Label;
    result.Metadata["SentimentScore"] = sentiment.Score.ToString();
    
    return result;
}
```

### Screenshot-Funktion (Selenium)

```csharp
// NuGet: Selenium.WebDriver
// NuGet: Selenium.Support

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

public class ScreenshotCapture
{
    public async Task<string> CaptureAsync(string url, string savePath)
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--disable-gpu");
        
        using var driver = new ChromeDriver(options);
        
        driver.Navigate().GoToUrl(url);
        await Task.Delay(2000); // Warten bis Seite geladen
        
        var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
        screenshot.SaveAsFile(savePath);
        
        return savePath;
    }
}

// Integration in Scanner
public async Task<SearchResult> AnalyzeUrlAsync(string url)
{
    // ... bestehende Logik ...
    
    // Screenshot erstellen
    var screenshotPath = Path.Combine(
        _currentCase.StoragePath, 
        "Screenshots", 
        $"{SanitizeFileName(url)}.png"
    );
    
    result.ScreenshotPath = await _screenshotCapture.CaptureAsync(url, screenshotPath);
    
    return result;
}
```

## 🧪 Testing

### Unit Tests

```csharp
using Xunit;

public class ScannerTests
{
    [Fact]
    public async Task ScanAsync_ValidUrl_ReturnsResults()
    {
        // Arrange
        var scanner = new Scanner();
        var url = "https://example.com";
        
        // Act
        var results = await scanner.ScanAsync(url);
        
        // Assert
        Assert.NotEmpty(results);
        Assert.Equal(url, results[0].Url);
    }
    
    [Fact]
    public async Task ScanAsync_Keyword_ReturnsMultipleResults()
    {
        // Arrange
        var scanner = new Scanner();
        var keyword = "test query";
        
        // Act
        var results = await scanner.ScanAsync(keyword);
        
        // Assert
        Assert.True(results.Count > 0);
    }
}
```

### Integration Tests

```csharp
public class EndToEndTests
{
    [Fact]
    public async Task FullWorkflow_CreateSaveCaseLoadCase_Success()
    {
        // Arrange
        var storage = new CaseStorageManager();
        var scanner = new Scanner();
        
        // Act
        var searchCase = await storage.CreateCaseAsync("Test", "test query");
        var results = await scanner.ScanAsync("test query");
        
        searchCase.Results = results;
        await storage.SaveCaseAsync(searchCase);
        
        var loadedCase = await storage.LoadCaseAsync(searchCase.Id);
        
        // Assert
        Assert.NotNull(loadedCase);
        Assert.Equal(searchCase.Id, loadedCase.Id);
        Assert.Equal(results.Count, loadedCase.Results.Count);
        
        // Cleanup
        await storage.DeleteCaseAsync(searchCase.Id);
    }
}
```

## 🚀 Performance-Optimierung

### Parallele Verarbeitung

```csharp
// Parallele Suchmaschinen-Anfragen
var tasks = new List<Task<List<SearchResult>>>
{
    _searchAggregator.SearchGoogleAsync(keyword),
    _searchAggregator.SearchBingAsync(keyword),
    _searchAggregator.SearchDuckDuckGoAsync(keyword)
};

var results = await Task.WhenAll(tasks);
var allResults = results.SelectMany(r => r).ToList();
```

### Caching

```csharp
public class CachedWebCrawler : WebCrawler
{
    private readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
    
    public override async Task<CrawlResult?> FetchAsync(string url)
    {
        if (_cache.TryGetValue(url, out CrawlResult cachedResult))
        {
            Log.Debug("Cache hit for {Url}", url);
            return cachedResult;
        }
        
        var result = await base.FetchAsync(url);
        
        if (result?.Success == true)
        {
            _cache.Set(url, result, TimeSpan.FromMinutes(10));
        }
        
        return result;
    }
}
```

## 📊 Monitoring & Logging

### Serilog-Konfiguration erweitern

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.WithThreadId()
    .Enrich.WithMachineName()
    .WriteTo.File(
        logPath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{ThreadId}] {Message:lj}{NewLine}{Exception}"
    )
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}"
    )
    .CreateLogger();
```

### Performance-Metriken

```csharp
public class PerformanceMonitor
{
    private readonly Stopwatch _stopwatch = new();
    
    public void StartOperation(string operation)
    {
        _stopwatch.Restart();
        Log.Debug("Starting {Operation}", operation);
    }
    
    public void EndOperation(string operation)
    {
        _stopwatch.Stop();
        Log.Information("{Operation} completed in {Duration}ms", 
            operation, _stopwatch.ElapsedMilliseconds);
    }
}

// Verwendung
var monitor = new PerformanceMonitor();
monitor.StartOperation("Scan");
var results = await scanner.ScanAsync(query);
monitor.EndOperation("Scan");
```

## 🔐 Sicherheit

### Input-Validierung

```csharp
public static class InputValidator
{
    public static bool IsValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
    
    public static string SanitizeQuery(string query)
    {
        // Entferne gefährliche Zeichen
        var sanitized = query.Trim();
        sanitized = Regex.Replace(sanitized, @"[<>""']", "");
        return sanitized;
    }
}
```

### Rate Limiting

```csharp
public class RateLimiter
{
    private readonly Dictionary<string, DateTime> _lastRequests = new();
    private readonly TimeSpan _minInterval = TimeSpan.FromSeconds(1);
    
    public async Task WaitIfNeededAsync(string domain)
    {
        if (_lastRequests.TryGetValue(domain, out var lastRequest))
        {
            var elapsed = DateTime.Now - lastRequest;
            if (elapsed < _minInterval)
            {
                var wait = _minInterval - elapsed;
                Log.Debug("Rate limiting: waiting {Wait}ms for {Domain}", 
                    wait.TotalMilliseconds, domain);
                await Task.Delay(wait);
            }
        }
        
        _lastRequests[domain] = DateTime.Now;
    }
}
```

## 📝 Best Practices

1. **Immer async/await verwenden** für I/O-Operationen
2. **IDisposable implementieren** für Ressourcen (HttpClient, etc.)
3. **Exceptions loggen** aber nicht schlucken
4. **Null-Checks** für alle externen Daten
5. **MVVM strikt befolgen** - keine Business-Logic im Code-Behind
6. **Dependency Injection** für bessere Testbarkeit (optional)
7. **Comprehensive Logging** auf allen Ebenen

## 🎓 Weiterführende Ressourcen

- [ML.NET Documentation](https://docs.microsoft.com/en-us/dotnet/machine-learning/)
- [Material Design in XAML](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit)
- [HtmlAgilityPack](https://html-agility-pack.net/)
- [Serilog](https://serilog.net/)
- [WPF MVVM](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/data/data-binding-overview)

---

**Happy Coding!** 🚀
