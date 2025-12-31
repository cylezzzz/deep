using System;
using System.Collections.Generic;

namespace DeepSeeArch.Models
{
    /// <summary>
    /// Zugriffsstatus einer URL
    /// </summary>
    public enum AccessStatus
    {
        Free,           // Frei zugänglich
        RequiresLogin,  // Login erforderlich
        Paywall,        // Bezahlschranke
        Blocked,        // Gesperrt
        Archived,       // Archiviert
        Deleted,        // Gelöscht
        Error,          // Fehler beim Zugriff
        Unknown         // Unbekannt
    }

    /// <summary>
    /// Kategorien für Suchergebnisse
    /// </summary>
    public enum ResultCategory
    {
        Web,
        Image,
        Video,
        Social,
        Forum,
        Archive,
        Adult,
        Document,
        Profile,
        Unknown
    }

    /// <summary>
    /// Einzelnes Suchergebnis
    /// </summary>
    public class SearchResult
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string Snippet { get; set; } = string.Empty;
        public ResultCategory Category { get; set; }
        public AccessStatus AccessStatus { get; set; }
        public DateTime FoundAt { get; set; } = DateTime.Now;
        public DateTime? LastSeen { get; set; }
        public List<string> IdentityMarkers { get; set; } = new();
        public double ConfidenceScore { get; set; } // 0.0 - 1.0
        public string? ExtractedText { get; set; }
        public List<string> MediaLinks { get; set; } = new();
        public List<string> OutgoingLinks { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
        public bool IsDuplicate { get; set; }
        public string? ScreenshotPath { get; set; }
        public string? HtmlContent { get; set; }
        
        // Account-Daten (NEU)
        public AccountData? AccountInfo { get; set; }
        
        // KI-Analyse Ergebnisse
        public string? AiSummary { get; set; }
        public List<string> AiExtractedEntities { get; set; } = new();
        public double? AiRelevanceScore { get; set; }
        public string? AiCategoryPrediction { get; set; }
        public bool? AiIsFakeProfile { get; set; }
        
        // Fuzzy-Match-Informationen (NEU)
        public FuzzyMatchInfo? FuzzyMatch { get; set; }
    }

    /// <summary>
    /// Account-Informationen von gefundenen Profilen/Seiten
    /// </summary>
    public class AccountData
    {
        public string? Username { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public string? ProfileUrl { get; set; }
        public string? Bio { get; set; }
        public string? Location { get; set; }
        public DateTime? AccountCreated { get; set; }
        public DateTime? LastActive { get; set; }
        public int? FollowerCount { get; set; }
        public int? FollowingCount { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsVerified { get; set; }
        public Dictionary<string, string> CustomFields { get; set; } = new();
        
        // Identitätsmissbrauch-Indikatoren
        public bool IsPotentialMisuse { get; set; }
        public string? MisuseReason { get; set; }
    }

    /// <summary>
    /// Fuzzy-Match-Informationen für ähnliche Namen/Schreibweisen
    /// </summary>
    public class FuzzyMatchInfo
    {
        public string OriginalQuery { get; set; } = string.Empty;
        public string MatchedText { get; set; } = string.Empty;
        public int SimilarityScore { get; set; } // 0-100
        public MatchType Type { get; set; }
        public List<string> Variations { get; set; } = new();
    }

    public enum MatchType
    {
        Exact,              // Exakte Übereinstimmung
        CaseInsensitive,    // Groß-/Kleinschreibung ignoriert
        TypoTolerant,       // Tippfehler-tolerant
        Phonetic,           // Klingt ähnlich
        Abbreviated,        // Abkürzung
        Partial             // Teil-Übereinstimmung
    }

    /// <summary>
    /// Ein Suchfall/Case
    /// </summary>
    public class SearchCase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastUpdated { get; set; }
        public List<SearchResult> Results { get; set; } = new();
        public SearchCaseMetadata Metadata { get; set; } = new();
        public string StoragePath { get; set; } = string.Empty;
        public SearchStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// Metadaten eines Cases
    /// </summary>
    public class SearchCaseMetadata
    {
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, string> CustomFields { get; set; } = new();
        public bool IsArchived { get; set; }
    }

    /// <summary>
    /// Statistiken eines Cases
    /// </summary>
    public class SearchStatistics
    {
        public int TotalResults { get; set; }
        public int UniqueResults { get; set; }
        public int DuplicateResults { get; set; }
        public Dictionary<ResultCategory, int> CategoryCounts { get; set; } = new();
        public Dictionary<AccessStatus, int> AccessStatusCounts { get; set; } = new();
        public Dictionary<string, int> DomainCounts { get; set; } = new();
        public double AverageConfidence { get; set; }
    }

    /// <summary>
    /// Suchfilter für die Anzeige
    /// </summary>
    public class SearchFilter
    {
        public bool HideAdult { get; set; }
        public bool HideDuplicates { get; set; }
        public double MinConfidence { get; set; } = 0.0;
        public List<ResultCategory> IncludedCategories { get; set; } = new();
        public List<AccessStatus> IncludedAccessStatuses { get; set; } = new();
        public List<string> IncludedDomains { get; set; } = new();
        public List<string> ExcludedDomains { get; set; } = new();
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
