using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FuzzySharp;
using DeepSeeArch.Models;
using Serilog;

namespace DeepSeeArch.Core.AI
{
    /// <summary>
    /// Fuzzy-Matching-Engine für tolerante Namenssuche
    /// Findet auch ähnliche Schreibweisen, Tippfehler und Variationen
    /// </summary>
    public class FuzzyMatchingEngine
    {
        private readonly int _minSimilarityScore;
        private readonly List<string> _searchVariations;

        public FuzzyMatchingEngine(int minSimilarityScore = 70)
        {
            _minSimilarityScore = minSimilarityScore;
            _searchVariations = new List<string>();
        }

        /// <summary>
        /// Generiert automatisch Such-Variationen für einen Namen
        /// </summary>
        public List<string> GenerateVariations(string name)
        {
            var variations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            // Original
            variations.Add(name);
            
            // Lowercase/Uppercase
            variations.Add(name.ToLower());
            variations.Add(name.ToUpper());
            
            // Ohne Leerzeichen
            variations.Add(name.Replace(" ", ""));
            
            // Mit Unterstrich statt Leerzeichen
            variations.Add(name.Replace(" ", "_"));
            
            // Mit Bindestrich statt Leerzeichen
            variations.Add(name.Replace(" ", "-"));
            
            // Ohne Sonderzeichen
            variations.Add(Regex.Replace(name, @"[^a-zA-Z0-9]", ""));
            
            // Umgekehrte Reihenfolge bei mehreren Wörtern
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length > 1)
            {
                variations.Add(string.Join(" ", words.Reverse()));
                variations.Add(string.Join("", words.Reverse()));
            }
            
            // Abkürzungen (Initialen)
            if (words.Length > 1)
            {
                var initials = string.Join("", words.Select(w => w[0]));
                variations.Add(initials);
                variations.Add(initials.ToLower());
            }
            
            // Häufige Tippfehler-Muster
            variations.UnionWith(GenerateTypoVariations(name));
            
            // Alternative Schreibweisen
            variations.UnionWith(GenerateAlternativeSpellings(name));
            
            Log.Debug("Generated {Count} variations for '{Name}'", variations.Count, name);
            _searchVariations.AddRange(variations);
            
            return variations.ToList();
        }

        /// <summary>
        /// Generiert Tippfehler-Variationen
        /// </summary>
        private List<string> GenerateTypoVariations(string name)
        {
            var variations = new List<string>();
            
            // Vertauschte benachbarte Zeichen
            for (int i = 0; i < name.Length - 1; i++)
            {
                var chars = name.ToCharArray();
                (chars[i], chars[i + 1]) = (chars[i + 1], chars[i]);
                variations.Add(new string(chars));
            }
            
            // Fehlende Zeichen
            for (int i = 0; i < name.Length; i++)
            {
                variations.Add(name.Remove(i, 1));
            }
            
            // Doppelte Zeichen
            for (int i = 0; i < name.Length; i++)
            {
                variations.Add(name.Insert(i, name[i].ToString()));
            }
            
            return variations.Distinct().ToList();
        }

        /// <summary>
        /// Generiert alternative Schreibweisen
        /// </summary>
        private List<string> GenerateAlternativeSpellings(string name)
        {
            var variations = new List<string>();
            
            // Häufige Ersetzungen
            var replacements = new Dictionary<string, string[]>
            {
                { "ph", new[] { "f" } },
                { "f", new[] { "ph" } },
                { "c", new[] { "k", "s" } },
                { "k", new[] { "c" } },
                { "s", new[] { "c", "z" } },
                { "z", new[] { "s" } },
                { "ei", new[] { "ai", "ey" } },
                { "ai", new[] { "ei", "ay" } },
                { "y", new[] { "i" } },
                { "i", new[] { "y" } }
            };
            
            foreach (var (from, toList) in replacements)
            {
                if (name.ToLower().Contains(from))
                {
                    foreach (var to in toList)
                    {
                        variations.Add(Regex.Replace(name, from, to, RegexOptions.IgnoreCase));
                    }
                }
            }
            
            return variations.Distinct().ToList();
        }

        /// <summary>
        /// Prüft ob ein Text mit dem Suchnamen matched (Fuzzy)
        /// </summary>
        public FuzzyMatchInfo? MatchText(string searchQuery, string textToMatch)
        {
            // Exakte Übereinstimmung
            if (textToMatch.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                return new FuzzyMatchInfo
                {
                    OriginalQuery = searchQuery,
                    MatchedText = textToMatch,
                    SimilarityScore = 100,
                    Type = MatchType.Exact
                };
            }
            
            // Case-insensitive
            if (textToMatch.Equals(searchQuery, StringComparison.OrdinalIgnoreCase))
            {
                return new FuzzyMatchInfo
                {
                    OriginalQuery = searchQuery,
                    MatchedText = textToMatch,
                    SimilarityScore = 95,
                    Type = MatchType.CaseInsensitive
                };
            }
            
            // Fuzzy-Matching mit FuzzySharp
            var score = Fuzz.PartialRatio(searchQuery.ToLower(), textToMatch.ToLower());
            
            if (score >= _minSimilarityScore)
            {
                var matchType = DetermineMatchType(searchQuery, textToMatch, score);
                
                return new FuzzyMatchInfo
                {
                    OriginalQuery = searchQuery,
                    MatchedText = textToMatch,
                    SimilarityScore = score,
                    Type = matchType,
                    Variations = _searchVariations.ToList()
                };
            }
            
            // Prüfe Variationen
            foreach (var variation in _searchVariations)
            {
                var variationScore = Fuzz.PartialRatio(variation.ToLower(), textToMatch.ToLower());
                if (variationScore >= _minSimilarityScore)
                {
                    return new FuzzyMatchInfo
                    {
                        OriginalQuery = searchQuery,
                        MatchedText = textToMatch,
                        SimilarityScore = variationScore,
                        Type = MatchType.TypoTolerant,
                        Variations = new List<string> { variation }
                    };
                }
            }
            
            return null;
        }

        /// <summary>
        /// Bestimmt den Match-Typ basierend auf dem Score und Mustern
        /// </summary>
        private MatchType DetermineMatchType(string query, string text, int score)
        {
            if (score == 100)
                return MatchType.Exact;
            
            if (score >= 90)
                return MatchType.CaseInsensitive;
            
            if (score >= 80)
                return MatchType.TypoTolerant;
            
            if (IsPhoneticMatch(query, text))
                return MatchType.Phonetic;
            
            if (IsAbbreviation(query, text))
                return MatchType.Abbreviated;
            
            return MatchType.Partial;
        }

        /// <summary>
        /// Prüft ob es sich um phonetische Ähnlichkeit handelt
        /// </summary>
        private bool IsPhoneticMatch(string query, string text)
        {
            // Vereinfachte phonetische Überprüfung
            var phoneticPairs = new[]
            {
                ("f", "ph"),
                ("c", "k"),
                ("s", "z"),
                ("ei", "ai"),
                ("y", "i")
            };
            
            foreach (var (a, b) in phoneticPairs)
            {
                var normalized1 = query.ToLower().Replace(a, b);
                var normalized2 = text.ToLower().Replace(a, b);
                
                if (normalized1 == normalized2)
                    return true;
            }
            
            return false;
        }

        /// <summary>
        /// Prüft ob es sich um eine Abkürzung handelt
        /// </summary>
        private bool IsAbbreviation(string query, string text)
        {
            var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var textWords = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            if (queryWords.Length <= 1 || textWords.Length <= 1)
                return false;
            
            // Prüfe ob query die Initialen von text sind
            var initials = string.Join("", textWords.Select(w => w[0]));
            if (initials.Equals(query, StringComparison.OrdinalIgnoreCase))
                return true;
            
            // Oder umgekehrt
            var queryInitials = string.Join("", queryWords.Select(w => w[0]));
            if (text.Equals(queryInitials, StringComparison.OrdinalIgnoreCase))
                return true;
            
            return false;
        }

        /// <summary>
        /// Findet alle Matches in einem Text
        /// </summary>
        public List<FuzzyMatchInfo> FindAllMatches(string searchQuery, string text)
        {
            var matches = new List<FuzzyMatchInfo>();
            
            // Zerteile Text in Wörter und Phrasen
            var words = text.Split(' ', '.', ',', '!', '?', ';', ':', '\n', '\r', '\t');
            
            foreach (var word in words)
            {
                if (string.IsNullOrWhiteSpace(word))
                    continue;
                
                var match = MatchText(searchQuery, word);
                if (match != null)
                {
                    matches.Add(match);
                }
            }
            
            // Auch längere Phrasen checken
            var sentences = text.Split('.', '!', '?');
            foreach (var sentence in sentences)
            {
                if (string.IsNullOrWhiteSpace(sentence))
                    continue;
                
                var match = MatchText(searchQuery, sentence.Trim());
                if (match != null && !matches.Any(m => m.MatchedText == match.MatchedText))
                {
                    matches.Add(match);
                }
            }
            
            return matches.OrderByDescending(m => m.SimilarityScore).ToList();
        }

        /// <summary>
        /// Berechnet einen gewichteten Similarity-Score für ein gesamtes Dokument
        /// </summary>
        public int CalculateDocumentSimilarity(string searchQuery, string title, string snippet, string content)
        {
            var scores = new List<int>();
            
            // Titel hat höchstes Gewicht
            var titleMatches = FindAllMatches(searchQuery, title);
            if (titleMatches.Any())
            {
                scores.Add(titleMatches.Max(m => m.SimilarityScore) * 3); // 3x Gewichtung
            }
            
            // Snippet hat mittleres Gewicht
            var snippetMatches = FindAllMatches(searchQuery, snippet);
            if (snippetMatches.Any())
            {
                scores.Add(snippetMatches.Max(m => m.SimilarityScore) * 2); // 2x Gewichtung
            }
            
            // Content hat niedrigstes Gewicht
            var contentMatches = FindAllMatches(searchQuery, content ?? "");
            if (contentMatches.Any())
            {
                scores.Add(contentMatches.Max(m => m.SimilarityScore)); // 1x Gewichtung
            }
            
            if (!scores.Any())
                return 0;
            
            // Durchschnitt der gewichteten Scores
            return (int)Math.Round(scores.Average());
        }
    }
}
