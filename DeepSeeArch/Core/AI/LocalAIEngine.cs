using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;
using DeepSeeArch.Models;
using Serilog;

namespace DeepSeeArch.Core.AI
{
    /// <summary>
    /// Lokale KI-Engine für Analyse und Klassifizierung
    /// Keine Cloud-Abhängigkeit, alles läuft lokal
    /// </summary>
    public class LocalAIEngine
    {
        private readonly MLContext _mlContext;
        private ITransformer? _categoryModel;
        private ITransformer? _relevanceModel;
        private ITransformer? _fakeProfileModel;
        private readonly TextAnalyzer _textAnalyzer;
        private readonly EntityExtractor _entityExtractor;
        private readonly OllamaAgent? _ollamaAgent;
        private readonly FuzzyMatchingEngine _fuzzyMatcher;
        private readonly AccountDataExtractor _accountExtractor;
        private bool _ollamaAvailable;
        
        public LocalAIEngine()
        {
            _mlContext = new MLContext(seed: 42);
            _textAnalyzer = new TextAnalyzer();
            _entityExtractor = new EntityExtractor();
            _fuzzyMatcher = new FuzzyMatchingEngine(minSimilarityScore: 70);
            _accountExtractor = new AccountDataExtractor();
            
            // Ollama initialisieren (optional)
            try
            {
                _ollamaAgent = new OllamaAgent();
                Task.Run(async () => _ollamaAvailable = await _ollamaAgent.CheckAvailabilityAsync()).Wait();
                
                if (_ollamaAvailable)
                {
                    Log.Information("LocalAIEngine initialized with Ollama support");
                }
                else
                {
                    Log.Information("LocalAIEngine initialized (Ollama not available)");
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Ollama initialization failed, continuing without Ollama support");
                _ollamaAvailable = false;
            }
        }

        /// <summary>
        /// Analysiert einen SearchResult und reichert ihn mit KI-Daten an
        /// </summary>
        public async Task<SearchResult> EnrichResultAsync(SearchResult result, string originalQuery)
        {
            try
            {
                // Fuzzy-Matching-Analyse
                var fuzzyMatch = _fuzzyMatcher.MatchText(
                    originalQuery, 
                    result.Title + " " + result.Snippet + " " + (result.ExtractedText ?? "")
                );
                result.FuzzyMatch = fuzzyMatch;

                // Account-Daten extrahieren (wenn verfügbar)
                if (!string.IsNullOrEmpty(result.HtmlContent))
                {
                    result.AccountInfo = await _accountExtractor.ExtractAsync(
                        result.HtmlContent, 
                        result.Url, 
                        result.Domain
                    );

                    // Ollama: Erweiterte Account-Analyse
                    if (_ollamaAvailable && _ollamaAgent != null && result.AccountInfo == null)
                    {
                        result.AccountInfo = await _ollamaAgent.ExtractAccountDataAsync(
                            result.HtmlContent, 
                            result.Url
                        );
                    }

                    // Identitätsmissbrauch prüfen
                    if (result.AccountInfo != null && _ollamaAvailable && _ollamaAgent != null)
                    {
                        var (isMisuse, reason) = await _ollamaAgent.DetectIdentityMisuseAsync(
                            originalQuery,
                            result.AccountInfo,
                            result.Snippet
                        );
                        
                        result.AccountInfo.IsPotentialMisuse = isMisuse;
                        result.AccountInfo.MisuseReason = reason;
                    }
                }

                // Text-Zusammenfassung
                if (!string.IsNullOrEmpty(result.ExtractedText))
                {
                    if (_ollamaAvailable && _ollamaAgent != null)
                    {
                        // Ollama: Erweiterte Analyse
                        result.AiSummary = await _ollamaAgent.AnalyzeContentContextAsync(
                            result.Title,
                            result.Snippet,
                            result.ExtractedText
                        );
                    }
                    else
                    {
                        // Fallback: Basis-Analyse
                        result.AiSummary = await _textAnalyzer.SummarizeAsync(result.ExtractedText);
                    }
                }

                // Entity-Extraktion (Namen, Orte, Organisationen)
                var entities = await _entityExtractor.ExtractEntitiesAsync(
                    result.Title + " " + result.Snippet + " " + (result.ExtractedText ?? "")
                );
                result.AiExtractedEntities = entities;

                // Kategorie-Vorhersage
                result.AiCategoryPrediction = PredictCategory(result);

                // Relevanz-Score (angepasst mit Fuzzy-Match)
                result.AiRelevanceScore = CalculateRelevance(result, fuzzyMatch);

                // Fake-Profile-Erkennung (für Social Media)
                if (result.Category == ResultCategory.Social || result.Category == ResultCategory.Profile)
                {
                    result.AiIsFakeProfile = DetectFakeProfile(result);
                }

                Log.Debug("Enriched result {Id} with AI data (Fuzzy: {Score}, Account: {HasAccount})", 
                    result.Id, 
                    fuzzyMatch?.SimilarityScore ?? 0, 
                    result.AccountInfo != null);
                    
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error enriching result {Id}", result.Id);
                return result;
            }
        }

        /// <summary>
        /// Batch-Analyse mehrerer Ergebnisse
        /// </summary>
        public async Task<List<SearchResult>> EnrichResultsAsync(List<SearchResult> results, string originalQuery)
        {
            var tasks = results.Select(r => EnrichResultAsync(r, originalQuery));
            var enriched = await Task.WhenAll(tasks);
            return enriched.ToList();
        }

        /// <summary>
        /// Generiert Such-Variationen mit Fuzzy-Matching und optional Ollama
        /// </summary>
        public async Task<List<string>> GenerateSearchVariationsAsync(string query)
        {
            // Basis-Variationen mit Fuzzy-Matching
            var variations = _fuzzyMatcher.GenerateVariations(query);

            // Erweiterte Variationen mit Ollama
            if (_ollamaAvailable && _ollamaAgent != null)
            {
                var ollamaVariations = await _ollamaAgent.GenerateSearchVariationsAsync(query);
                variations.AddRange(ollamaVariations);
                variations = variations.Distinct().ToList();
            }

            Log.Information("Generated {Count} search variations for '{Query}'", variations.Count, query);
            return variations;
        }

        /// <summary>
        /// Kategorisiert ein Ergebnis basierend auf URL, Titel und Content
        /// </summary>
        private string PredictCategory(SearchResult result)
        {
            var features = ExtractCategoryFeatures(result);
            
            // Einfache Regel-basierte Klassifizierung (kann später durch ML-Modell ersetzt werden)
            var url = result.Url.ToLower();
            var title = result.Title.ToLower();
            var text = (result.ExtractedText ?? "").ToLower();

            // Social Media
            if (url.Contains("facebook.com") || url.Contains("twitter.com") || url.Contains("instagram.com") ||
                url.Contains("linkedin.com") || url.Contains("tiktok.com") || url.Contains("reddit.com"))
                return "Social Media";

            // Forum
            if (url.Contains("forum") || url.Contains("board") || title.Contains("forum") ||
                text.Contains("posted by") || text.Contains("thread"))
                return "Forum";

            // Archive
            if (url.Contains("archive.org") || url.Contains("archive.is") || url.Contains("cached"))
                return "Archive";

            // Adult Content (Keywords)
            var adultKeywords = new[] { "xxx", "porn", "adult", "nsfw", "onlyfans", "sex" };
            if (adultKeywords.Any(k => url.Contains(k) || title.Contains(k)))
                return "Adult Content";

            // Dokumente
            if (url.EndsWith(".pdf") || url.EndsWith(".doc") || url.EndsWith(".docx") ||
                url.Contains("document") || url.Contains("/docs/"))
                return "Document";

            // Bilder
            if (url.EndsWith(".jpg") || url.EndsWith(".png") || url.EndsWith(".gif") ||
                url.Contains("images") || url.Contains("photos"))
                return "Image";

            // Videos
            if (url.Contains("youtube.com") || url.Contains("vimeo.com") || url.Contains("video") ||
                url.EndsWith(".mp4") || url.EndsWith(".avi"))
                return "Video";

            return "Web";
        }

        /// <summary>
        /// Berechnet Relevanz-Score basierend auf verschiedenen Faktoren
        /// </summary>
        private double CalculateRelevance(SearchResult result, FuzzyMatchInfo? fuzzyMatch)
        {
            double score = 0.5; // Basis-Score

            // Confidence erhöht Relevanz
            score += result.ConfidenceScore * 0.3;

            // Fuzzy-Match-Score berücksichtigen
            if (fuzzyMatch != null)
            {
                score += (fuzzyMatch.SimilarityScore / 100.0) * 0.2;
            }

            // Identity Markers erhöhen Relevanz
            if (result.IdentityMarkers.Count > 0)
                score += Math.Min(result.IdentityMarkers.Count * 0.1, 0.3);

            // Account-Daten vorhanden = höhere Relevanz
            if (result.AccountInfo != null)
            {
                score += 0.15;
                
                // Vollständige Account-Daten = noch höher
                if (!string.IsNullOrEmpty(result.AccountInfo.Email) || 
                    !string.IsNullOrEmpty(result.AccountInfo.Username))
                {
                    score += 0.1;
                }
            }

            // Frische Ergebnisse sind relevanter
            var age = DateTime.Now - result.FoundAt;
            if (age.TotalDays < 30)
                score += 0.1;
            else if (age.TotalDays > 365)
                score -= 0.1;

            // Bestimmte Kategorien sind wichtiger
            if (result.Category == ResultCategory.Profile || result.Category == ResultCategory.Social)
                score += 0.1;

            // Normalisieren auf 0-1
            return Math.Max(0, Math.Min(1, score));
        }

        /// <summary>
        /// Erkennt potenzielle Fake-Profile
        /// </summary>
        private bool DetectFakeProfile(SearchResult result)
        {
            int suspiciousIndicators = 0;

            // Keine oder wenig Content
            if (string.IsNullOrWhiteSpace(result.ExtractedText) || result.ExtractedText.Length < 100)
                suspiciousIndicators++;

            // Generische Titel
            var genericTitles = new[] { "profile", "user", "account", "member" };
            if (genericTitles.Any(t => result.Title.ToLower().Contains(t)))
                suspiciousIndicators++;

            // Wenig Metadaten
            if (result.Metadata.Count < 2)
                suspiciousIndicators++;

            // Stock-Foto Indikatoren (wenn Bild-URLs vorhanden)
            var stockPhotoKeywords = new[] { "stock", "placeholder", "default", "avatar" };
            if (result.MediaLinks.Any(m => stockPhotoKeywords.Any(k => m.ToLower().Contains(k))))
                suspiciousIndicators++;

            // Entscheidung: 2 oder mehr Indikatoren = wahrscheinlich Fake
            return suspiciousIndicators >= 2;
        }

        private Dictionary<string, float> ExtractCategoryFeatures(SearchResult result)
        {
            // Feature-Extraktion für ML-Modell (aktuell nicht verwendet, für zukünftige Erweiterung)
            return new Dictionary<string, float>
            {
                { "urlLength", result.Url.Length },
                { "titleLength", result.Title.Length },
                { "hasHttps", result.Url.StartsWith("https") ? 1f : 0f },
                { "mediaCount", result.MediaLinks.Count },
                { "identityMarkerCount", result.IdentityMarkers.Count }
            };
        }
    }

    /// <summary>
    /// Text-Analyse und Zusammenfassung
    /// </summary>
    public class TextAnalyzer
    {
        public async Task<string> SummarizeAsync(string text)
        {
            return await Task.Run(() =>
            {
                if (string.IsNullOrWhiteSpace(text))
                    return string.Empty;

                // Einfache Zusammenfassung: Erste 3 Sätze
                var sentences = text.Split('.', '!', '?')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Take(3);

                var summary = string.Join(". ", sentences);
                if (summary.Length > 300)
                    summary = summary.Substring(0, 297) + "...";

                return summary + (summary.EndsWith(".") ? "" : ".");
            });
        }

        public async Task<Dictionary<string, int>> GetKeywordsAsync(string text, int topN = 10)
        {
            return await Task.Run(() =>
            {
                var words = text.ToLower()
                    .Split(' ', '.', ',', '!', '?', ';', ':', '\n', '\r', '\t')
                    .Where(w => w.Length > 3)
                    .GroupBy(w => w)
                    .OrderByDescending(g => g.Count())
                    .Take(topN)
                    .ToDictionary(g => g.Key, g => g.Count());

                return words;
            });
        }
    }

    /// <summary>
    /// Entity-Extraktion (Namen, Orte, Organisationen)
    /// </summary>
    public class EntityExtractor
    {
        public async Task<List<string>> ExtractEntitiesAsync(string text)
        {
            return await Task.Run(() =>
            {
                var entities = new List<string>();

                if (string.IsNullOrWhiteSpace(text))
                    return entities;

                // Einfache NER: Wörter die mit Großbuchstaben beginnen (außer am Satzanfang)
                var words = text.Split(' ', '\n', '\r', '\t');
                bool sentenceStart = true;

                foreach (var word in words)
                {
                    var cleaned = new string(word.Where(c => char.IsLetter(c) || c == '-').ToArray());
                    
                    if (string.IsNullOrEmpty(cleaned))
                    {
                        if (word.Contains('.') || word.Contains('!') || word.Contains('?'))
                            sentenceStart = true;
                        continue;
                    }

                    if (!sentenceStart && char.IsUpper(cleaned[0]) && cleaned.Length > 2)
                    {
                        entities.Add(cleaned);
                    }

                    if (word.Contains('.') || word.Contains('!') || word.Contains('?'))
                        sentenceStart = true;
                    else
                        sentenceStart = false;
                }

                return entities.Distinct().Take(20).ToList();
            });
        }
    }

    /// <summary>
    /// Ähnlichkeits-Analyse für Duplikat-Erkennung
    /// </summary>
    public class SimilarityAnalyzer
    {
        /// <summary>
        /// Berechnet Ähnlichkeit zwischen zwei Texten (Jaccard-Index)
        /// </summary>
        public double CalculateSimilarity(string text1, string text2)
        {
            var words1 = GetWords(text1);
            var words2 = GetWords(text2);

            if (words1.Count == 0 && words2.Count == 0)
                return 1.0;

            var intersection = words1.Intersect(words2).Count();
            var union = words1.Union(words2).Count();

            return union > 0 ? (double)intersection / union : 0.0;
        }

        private HashSet<string> GetWords(string text)
        {
            return text.ToLower()
                .Split(' ', '.', ',', '!', '?', ';', ':', '\n', '\r', '\t')
                .Where(w => w.Length > 2)
                .ToHashSet();
        }

        /// <summary>
        /// Findet Duplikate in einer Liste von Ergebnissen
        /// </summary>
        public List<(SearchResult r1, SearchResult r2, double similarity)> FindDuplicates(
            List<SearchResult> results, 
            double threshold = 0.8)
        {
            var duplicates = new List<(SearchResult, SearchResult, double)>();

            for (int i = 0; i < results.Count; i++)
            {
                for (int j = i + 1; j < results.Count; j++)
                {
                    var text1 = results[i].Title + " " + results[i].Snippet;
                    var text2 = results[j].Title + " " + results[j].Snippet;
                    
                    var similarity = CalculateSimilarity(text1, text2);
                    
                    if (similarity >= threshold)
                    {
                        duplicates.Add((results[i], results[j], similarity));
                    }
                }
            }

            return duplicates;
        }
    }
}
