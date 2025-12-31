# DeepSeeArch — Professional Identity & Online Presence Scanner

**Vollständiger lokaler Desktop-Scanner für Windows mit integrierter KI**

## 🎯 Überblick

DeepSeeArch ist ein professionelles Windows-Desktop-Tool zur vollständigen Sichtbarmachung von Online-Präsenzen. Das Programm kombiniert Multi-Engine-Websuche, intelligentes Scraping und lokale KI-Analyse für maximale Erfolgsquote und Zuverlässigkeit.

### Kernfeatures

✅ **Multi-Engine-Suche**
- Google, Bing, DuckDuckGo parallel
- Social Media (LinkedIn, Facebook, Twitter, Instagram)
- Foren (Reddit, Stack Overflow, Quora)
- Archive (Wayback Machine, Archive.is)

✅ **Lokale KI-Engine**
- Text-Analyse und Zusammenfassung
- Entity-Extraktion (Namen, Orte, Organisationen)
- Automatische Kategorisierung
- Fake-Profile-Erkennung
- Duplikat-Erkennung
- Relevanz-Scoring

✅ **Intelligentes Scraping**
- HTML-Content-Extraktion
- Medien-Links (Bilder, Videos)
- Metadaten-Analyse
- Zugriffsstatus-Erkennung
- Screenshot-Funktion (geplant)

✅ **Professionelle UI**
- Material Design
- Clean & Modern (Google/Gemini-Style)
- Kategorie-Tabs für einfache Filterung
- Echtzeit-Status-Updates
- Responsive Design

✅ **Lokale Datenspeicherung**
- Case-basierte Verwaltung
- Vollständige Rohdaten-Archivierung
- Export als CSV oder JSON
- Keine Cloud-Abhängigkeit

## 🏗️ Architektur

```
DeepSeeArch/
├── Core/
│   ├── Scanner.cs                    # Haupt-Scanner mit Multi-Engine
│   ├── SearchEngineAggregator.cs     # Such-Aggregator
│   └── AI/
│       └── LocalAIEngine.cs          # Lokale KI-Engine (ML.NET)
├── Models/
│   └── SearchModels.cs               # Datenmodelle
├── Storage/
│   └── CaseStorageManager.cs         # Lokale Speicherverwaltung
├── UI/
│   ├── MainWindow.xaml               # Haupt-UI
│   └── ViewModels/
│       └── MainViewModel.cs          # MVVM ViewModel
└── App.xaml                          # Application & Theme
```

## 🚀 Technologie-Stack

- **.NET 8** - Moderne C# Plattform
- **WPF + MVVM** - Clean Architecture
- **Material Design Themes** - Professionelle UI
- **ML.NET** - Lokale Machine Learning
- **HtmlAgilityPack** - HTML Parsing
- **Serilog** - Logging
- **SQLite** - Lokale Datenbank (optional)

## 📦 Installation

### Voraussetzungen
- Windows 10/11
- Visual Studio 2022
- .NET 8 SDK

### Build

```bash
# Repository klonen
git clone https://github.com/yourusername/DeepSeeArch.git
cd DeepSeeArch

# Solution öffnen
DeepSeeArch.sln

# In Visual Studio:
# 1. Restore NuGet Packages (automatisch)
# 2. Build Solution (Ctrl+Shift+B)
# 3. Run (F5)
```

## 🎮 Verwendung

### Schnellstart

1. **Programm starten**
2. **Suchbegriff eingeben**:
   - Name: "Max Mustermann"
   - Alias: "maxmuster_dev"
   - URL: "https://example.com/profile"
3. **"Scan" klicken** oder Enter drücken
4. **Ergebnisse durchsuchen** mit Kategorie-Tabs
5. **Case speichern** für spätere Analyse
6. **Exportieren** als CSV oder JSON

### Kategorie-Filter

- **Alle** - Zeigt alle Ergebnisse (Standard)
- **Web** - Normale Webseiten
- **Bilder** - Bildquellen
- **Videos** - Video-Plattformen
- **Social** - Social Media Profile
- **Foren** - Forum-Beiträge
- **Archive** - Archivierte Inhalte
- **18+** - Explizite Inhalte (nur mit Warnung)
- **Dokumente** - PDFs, Docs, etc.

### KI-Funktionen

Die lokale KI analysiert automatisch:

- **Text-Zusammenfassung** - Kompakte Zusammenfassungen langer Texte
- **Entity-Extraktion** - Erkennt Namen, Orte, Organisationen
- **Kategorisierung** - Automatische Zuordnung zu Kategorien
- **Relevanz-Score** - Bewertung der Wichtigkeit
- **Fake-Erkennung** - Identifiziert potenzielle Fake-Profile
- **Duplikate** - Findet ähnliche/doppelte Ergebnisse

### Case-Verwaltung

Jeder Scan wird als "Case" gespeichert:

```
Documents/DeepSeeArch/Cases/
└── [Case-ID]/
    ├── case.json          # Metadaten
    ├── results.json       # Alle Ergebnisse
    ├── Screenshots/       # Screenshots (geplant)
    ├── HTML/              # Gespeicherte HTML-Seiten
    ├── Media/             # Heruntergeladene Medien
    └── Exports/           # CSV/JSON Exports
```

## 🔧 Konfiguration

### Logging

Logs werden gespeichert in:
```
Documents/DeepSeeArch/Logs/log-YYYYMMDD.txt
```

### Filter-Einstellungen

Filter sind **standardmäßig deaktiviert**. Sie können aktiviert werden:
- Adult-Content ausblenden
- Duplikate ausblenden
- Minimale Confidence einstellen

⚠️ **Wichtig**: Filter verändern nur die Ansicht, niemals die gespeicherten Rohdaten.

## 🎨 UI-Design

Das Interface orientiert sich an:
- **Google Search** - Für vertraute Bedienung
- **Material Design** - Für moderne Ästhetik
- **Gemini** - Für professionelles Aussehen

Farben:
- Primär: Blau (#2196F3)
- Sekundär: Cyan (#00BCD4)
- Hintergrund: Weiß / Hellgrau

## 🔬 KI-Details

### Lokale KI-Engine

**Keine Cloud-Abhängigkeit** - Alles läuft lokal:

1. **Text-Analyse**:
   - Satz-basierte Zusammenfassung
   - Keyword-Extraktion
   - Sentiment-Analyse (geplant)

2. **Entity Recognition**:
   - Namens-Erkennung
   - Orts-Erkennung
   - Organisations-Erkennung
   - Pattern-Matching

3. **Klassifizierung**:
   - URL-Pattern-Analyse
   - Content-Type-Erkennung
   - Domain-Reputation
   - Keyword-Matching

4. **Duplikat-Erkennung**:
   - Jaccard-Ähnlichkeit
   - Text-Vergleich
   - URL-Normalisierung

### Algorithmen

- **Confidence-Score**: 
  ```
  Base (0.5) 
  + Title-Match (0.3)
  + Snippet-Match (0.2)
  + Identity-Markers (0.05 each, max 0.3)
  + Domain-Reputation (0.1)
  ```

- **Relevanz-Score**:
  ```
  Confidence * 0.3
  + Identity-Markers * 0.1
  + Freshness * 0.1
  + Category-Boost * 0.1
  ```

## 📊 Performance

- **Scan-Geschwindigkeit**: ~5-10 Sekunden pro Query
- **Multi-Engine**: 3-6 Suchmaschinen parallel
- **KI-Analyse**: <100ms pro Ergebnis
- **Speicher**: ~200-500 MB RAM
- **Storage**: ~10-50 MB pro Case

## 🔒 Datenschutz & Sicherheit

✅ **Vollständig lokal** - Keine Cloud-Übertragung
✅ **Keine Telemetrie** - Keine Daten nach außen
✅ **Eigene Daten** - Alles auf dem eigenen PC
✅ **Open Source** - Transparenter Code

⚠️ **Wichtig**: Das Tool macht öffentliche Daten sichtbar, sammelt aber selbst keine Daten.

## 🚧 Roadmap

### Phase 1 (Aktuell) ✅
- [x] Basis-Scanner
- [x] Multi-Engine-Suche
- [x] Lokale KI-Engine
- [x] Material Design UI
- [x] Case-Verwaltung
- [x] Export-Funktionen

### Phase 2 (Geplant)
- [ ] Screenshot-Funktion
- [ ] Erweiterte Social-Media-APIs
- [ ] Browser-Automation (Selenium)
- [ ] OCR für Bilder
- [ ] Erweiterte NLP-Modelle

### Phase 3 (Zukunft)
- [ ] Graph-Visualisierung
- [ ] Timeline-Ansicht
- [ ] Beziehungs-Analyse
- [ ] Batch-Scans
- [ ] Scheduled Scans
- [ ] API-Interface

## 🤝 Beitragen

Contributions sind willkommen!

1. Fork das Repository
2. Feature-Branch erstellen
3. Änderungen committen
4. Pull Request erstellen

## 📝 Lizenz

[MIT License](LICENSE) - Frei verwendbar

## ⚖️ Rechtliches

**Wichtig**: 
- Nur für legale Zwecke verwenden
- Respektiere robots.txt und Terms of Service
- Keine missbräuchliche Nutzung
- Datenschutz beachten

## 🆘 Support

- **Issues**: GitHub Issues
- **Dokumentation**: Dieses README
- **Logs**: `Documents/DeepSeeArch/Logs/`

## 🙏 Credits

Entwickelt mit:
- Material Design Themes (MaterialDesignInXamlToolkit)
- ML.NET (Microsoft)
- HtmlAgilityPack
- ModernWPF
- Serilog

---

**DeepSeeArch** - Transparenz durch Technologie 🔍