# DeepSeeArch - Build & Setup Guide

## 🚀 Schnellstart

### Voraussetzungen

1. **Windows 10/11** (64-bit)
2. **Visual Studio 2022** (Community Edition oder höher)
   - Workload: ".NET Desktop Development"
3. **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)

### Installation

#### Methode 1: Visual Studio

```bash
# 1. Repository klonen
git clone https://github.com/yourusername/DeepSeeArch.git
cd DeepSeeArch

# 2. Solution in Visual Studio öffnen
DeepSeeArch.sln

# 3. NuGet Packages wiederherstellen (automatisch beim ersten Build)
# 4. Build Solution (Ctrl+Shift+B)
# 5. Run (F5)
```

#### Methode 2: Command Line

```bash
# 1. Repository klonen
git clone https://github.com/yourusername/DeepSeeArch.git
cd DeepSeeArch

# 2. NuGet Packages wiederherstellen
dotnet restore

# 3. Build
dotnet build --configuration Release

# 4. Run
dotnet run --project DeepSeeArch/DeepSeeArch.csproj
```

## 📦 Dependencies

### NuGet Packages

Das Projekt verwendet folgende NuGet-Pakete (werden automatisch heruntergeladen):

**ML & AI:**
- Microsoft.ML (3.0.1)
- Microsoft.ML.OnnxRuntime (1.17.0)
- Microsoft.SemanticKernel (1.4.0)
- Catalyst (1.0.31520)

**Web Scraping:**
- HtmlAgilityPack (1.11.58)
- AngleSharp (1.1.2)
- Selenium.WebDriver (4.17.0)
- Flurl.Http (4.0.2)

**UI:**
- MaterialDesignThemes (5.0.0)
- MaterialDesignColors (3.0.0)
- ModernWpfUI (0.9.6)

**Data:**
- Microsoft.EntityFrameworkCore.Sqlite (8.0.1)
- Newtonsoft.Json (13.0.3)

**Logging:**
- Serilog (3.1.1)
- Serilog.Sinks.File (5.0.0)
- Serilog.Sinks.Console (5.0.1)

**MVVM:**
- CommunityToolkit.Mvvm (8.2.2)

### Manuelle Installation (falls nötig)

```bash
cd DeepSeeArch

# ML.NET
dotnet add package Microsoft.ML
dotnet add package Microsoft.ML.OnnxRuntime

# Web Scraping
dotnet add package HtmlAgilityPack
dotnet add package AngleSharp
dotnet add package Selenium.WebDriver

# UI
dotnet add package MaterialDesignThemes
dotnet add package ModernWpfUI

# Weitere
dotnet add package Serilog
dotnet add package CommunityToolkit.Mvvm
```

## 🛠️ Build-Konfigurationen

### Debug Build

```bash
dotnet build --configuration Debug
```

Eigenschaften:
- Debugging-Symbole aktiviert
- Keine Optimierungen
- Detailliertes Logging
- Schnellerer Build

### Release Build

```bash
dotnet build --configuration Release
```

Eigenschaften:
- Optimiert für Performance
- Keine Debug-Symbole
- Reduziertes Logging
- Kleinere Dateigröße

### Publish (Standalone)

Erstellt eine eigenständige Anwendung ohne .NET Runtime-Abhängigkeit:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Optionen:
- `-r win-x64` - Target Runtime
- `--self-contained true` - Inkludiert .NET Runtime
- `-p:PublishSingleFile=true` - Single EXE File

Output: `bin/Release/net8.0-windows/win-x64/publish/DeepSeeArch.exe`

## 🔧 Troubleshooting

### Problem: NuGet Restore schlägt fehl

**Lösung 1:** NuGet-Quellen überprüfen
```bash
dotnet nuget list source
```

**Lösung 2:** Cache leeren
```bash
dotnet nuget locals all --clear
dotnet restore
```

### Problem: Material Design Themes nicht gefunden

**Lösung:** Manuell installieren
```bash
dotnet add package MaterialDesignThemes
dotnet add package MaterialDesignColors
```

### Problem: "Platform target mismatch"

**Lösung:** In der .csproj Datei Platform Target setzen:
```xml
<PropertyGroup>
  <PlatformTarget>x64</PlatformTarget>
</PropertyGroup>
```

### Problem: Selenium ChromeDriver nicht gefunden

**Lösung:** ChromeDriver manuell herunterladen
1. Download von [ChromeDriver](https://chromedriver.chromium.org/)
2. Platzieren im Projekt-Root oder PATH

### Problem: UI erscheint nicht korrekt

**Lösung:** Material Design Themes überprüfen in App.xaml:
```xml
<ResourceDictionary.MergedDictionaries>
    <materialDesign:BundledTheme BaseTheme="Light" ... />
    <!-- ... -->
</ResourceDictionary.MergedDictionaries>
```

## 🧪 Testing

### Unit Tests ausführen

```bash
# Wenn Tests vorhanden (zukünftig)
dotnet test
```

### Manual Testing

1. **URL-Analyse testen:**
   - Input: `https://example.com`
   - Erwartung: Einzelnes Ergebnis mit extrahiertem Content

2. **Keyword-Suche testen:**
   - Input: `test query`
   - Erwartung: Multiple Ergebnisse von verschiedenen Engines

3. **Filter testen:**
   - Ergebnisse scannen
   - Kategorie-Tabs durchklicken
   - Prüfen ob Filterung funktioniert

4. **Case-Speicherung testen:**
   - Scan durchführen
   - "Case speichern" klicken
   - Programm neu starten
   - "Cases verwalten" öffnen
   - Prüfen ob Case vorhanden

## 📝 Development Workflow

### 1. Feature entwickeln

```bash
# Branch erstellen
git checkout -b feature/my-feature

# Änderungen machen
# ...

# Testen
dotnet build
dotnet run

# Commit
git add .
git commit -m "Add my feature"

# Push
git push origin feature/my-feature
```

### 2. Code-Qualität

**Code-Style:**
- Folge C# Naming Conventions
- Verwende async/await für I/O
- Implement IDisposable wo nötig
- Kommentiere öffentliche APIs

**Logging:**
```csharp
Log.Debug("Debug info");
Log.Information("Normal flow");
Log.Warning("Warning condition");
Log.Error(ex, "Error occurred");
Log.Fatal(ex, "Fatal error");
```

### 3. Pull Request

1. Code review selbst durchführen
2. Build testen (Debug & Release)
3. Manual testing
4. PR erstellen mit Beschreibung

## 🚢 Deployment

### Installer erstellen (Optional)

**Option 1: ClickOnce**
1. In Visual Studio: Project → Publish
2. Folge Wizard
3. Generiert Setup.exe

**Option 2: MSIX Package**
```xml
<PropertyGroup>
  <GenerateAppInstallerFile>true</GenerateAppInstallerFile>
  <AppxPackageSigningEnabled>true</AppxPackageSigningEnabled>
</PropertyGroup>
```

**Option 3: Inno Setup**
Verwende [Inno Setup](https://jrsoftware.org/isinfo.php) für klassischen Installer.

### Portable Version

```bash
# Self-contained single file
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Output kopieren
copy bin/Release/net8.0-windows/win-x64/publish/DeepSeeArch.exe DeepSeeArch-Portable.exe
```

## 📊 Performance-Profiling

### Mit Visual Studio

1. Debug → Performance Profiler
2. Select: CPU Usage, Memory Usage
3. Start
4. Programm normal verwenden
5. Stop
6. Analyse

### Mit dotnet-trace

```bash
# Install
dotnet tool install --global dotnet-trace

# Capture
dotnet-trace collect --process-id [PID]

# Analyze in PerfView or SpeedScope
```

## 🔐 Signing (Code Signing)

Für Produktion sollte die EXE signiert werden:

```powershell
# Mit eigenen Zertifikat
signtool sign /f MyCert.pfx /p Password /t http://timestamp.digicert.com DeepSeeArch.exe
```

## 📋 Checkliste für Release

- [ ] Version Number in .csproj aktualisiert
- [ ] CHANGELOG.md aktualisiert
- [ ] Alle Tests bestanden
- [ ] Release Build erfolgreich
- [ ] Manual Testing durchgeführt
- [ ] Dokumentation aktualisiert
- [ ] Code signiert (optional)
- [ ] GitHub Release erstellt
- [ ] Installer getestet

## 🆘 Support

Bei Problemen:
1. Logs überprüfen: `Documents/DeepSeeArch/Logs/`
2. Issue auf GitHub erstellen
3. Stack Overflow (Tag: deepseearch)

## 📚 Weitere Ressourcen

- [.NET 8 Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [WPF Documentation](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)
- [Material Design](https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit/wiki)
- [ML.NET Tutorials](https://dotnet.microsoft.com/learn/ml-dotnet)

---

**Viel Erfolg beim Entwickeln!** 🎉