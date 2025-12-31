using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using AngleSharp;
using AngleSharp.Html.Parser;
using DeepSeeArch.Models;
using DeepSeeArch.Core.AI;
using Serilog;

namespace DeepSeeArch.Core
{
    /// <summary>
    /// Haupt-Scanner mit Multi-Engine-Unterstützung
    /// </summary>
    public class Scanner
    {
        private readonly LocalAIEngine _aiEngine;
        private readonly WebCrawler _crawler;
        private readonly SearchEngineAggregator _searchAggregator;
        private readonly ContentExtractor _contentExtractor;
        private readonly DuplicateDetector _duplicateDetector;

        public Scanner()
        {
            _aiEngine = new LocalAIEngine();
            _crawler = new WebCrawler();
            _searchAggregator = new SearchEngineAggregator();
            _contentExtractor = new ContentExtractor();
            _duplicateDetector = new DuplicateDetector();
            
            Log.Information("Scanner initialized with all engines");
        }

        /// <summary>
        /// Hauptscan-Methode: automatische Erkennung von URL vs. Keyword-Suche
        /// </summary>
        public async Task<List<SearchResult>> ScanAsync(string query, SearchFilter? filter = null)
        {
            Log.Information("Starting scan for query: {Query}", query);

            List<SearchResult> results;

            if (IsUrl(query))
            {
                // URL-Analyse
                results = await AnalyzeUrlAsync(query);
            }
            else
            {
                // Keyword/Personen-Suche mit Variationen
                results = await SearchKeywordAsync(query);
            }

            // Duplikate markieren
            results = await _duplicateDetector.MarkDuplicatesAsync(results);

            // KI-Anreicherung (mit originalem Query für Fuzzy-Matching)
            results = await _aiEngine.EnrichResultsAsync(results, query);

            // Filter anwenden (wenn vorhanden)
            if (filter != null)
            {
                results = ApplyFilter(results, filter);
            }

            Log.Information("Scan completed: {Count} results found", results.Count);
            return results;
        }

        /// <summary>
        /// Analysiert eine einzelne URL vollständig
        /// </summary>
        private async Task<List<SearchResult>> AnalyzeUrlAsync(string url)
        {
            var result = new SearchResult
            {
                Url = url,
                Domain = ExtractDomain(url),
                Category = ResultCategory.Web
            };

            try
            {
                // HTTP Request
                var content = await _crawler.FetchAsync(url);
                
                if (content != null)
                {
                    result.AccessStatus = AccessStatus.Free;
                    result.HtmlContent = content.Html;
                    
                    // Content extrahieren
                    var extracted = await _contentExtractor.ExtractAsync(content.Html, url);
                    result.Title = extracted.Title;
                    result.ExtractedText = extracted.Text;
                    result.MediaLinks = extracted.MediaUrls;
                    result.OutgoingLinks = extracted.Links;
                    result.Metadata = extracted.Metadata;
                    
                    // Kategorie bestimmen
                    result.Category = DetermineCategory(url, extracted.Text);
                    
                    result.ConfidenceScore = 1.0; // Direkte URL = hohe Confidence
                }
                else
                {
                    result.AccessStatus = AccessStatus.Error;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error analyzing URL {Url}", url);
                result.AccessStatus = AccessStatus.Error;
                result.Snippet = $"Error: {ex.Message}";
            }

            return new List<SearchResult> { result };
        }

        /// <summary>
        /// Sucht nach Keywords/Namen über mehrere Suchmaschinen
        /// Nutzt Fuzzy-Matching und Such-Variationen
        /// </summary>
        private async Task<List<SearchResult>> SearchKeywordAsync(string keyword)
        {
            // Generiere Such-Variationen
            Log.Information("Generating search variations for '{Keyword}'", keyword);
            var variations = await _aiEngine.GenerateSearchVariationsAsync(keyword);
            
            // Multi-Engine-Suche mit Original-Query
            var allResults = new List<SearchResult>();

            // Google-ähnliche Suche (über verschiedene Engines)
            var googleResults = await _searchAggregator.SearchGoogleAsync(keyword);
            allResults.AddRange(googleResults);

            // Bing
            var bingResults = await _searchAggregator.SearchBingAsync(keyword);
            allResults.AddRange(bingResults);

            // DuckDuckGo
            var duckResults = await _searchAggregator.SearchDuckDuckGoAsync(keyword);
            allResults.AddRange(duckResults);

            // Spezielle Quellen
            var socialResults = await _searchAggregator.SearchSocialMediaAsync(keyword);
            allResults.AddRange(socialResults);

            var forumResults = await _searchAggregator.SearchForumsAsync(keyword);
            allResults.AddRange(forumResults);

            var archiveResults = await _searchAggregator.SearchArchivesAsync(keyword);
            allResults.AddRange(archiveResults);

            // Suche mit Variationen (Top 5)
            Log.Information("Searching with variations...");
            foreach (var variation in variations.Take(5))
            {
                if (variation.Equals(keyword, StringComparison.OrdinalIgnoreCase))
                    continue;

                Log.Debug("Searching variation: {Variation}", variation);
                
                // Nur eine Engine pro Variation (um nicht zu viele Requests zu machen)
                var variationResults = await _searchAggregator.SearchGoogleAsync(variation);
                allResults.AddRange(variationResults);
            }

            // Confidence-Score basierend auf Identity Markers berechnen
            foreach (var result in allResults)
            {
                result.ConfidenceScore = CalculateConfidenceScore(result, keyword);
            }

            Log.Information("Found {Count} results across all engines", allResults.Count);
            return allResults;
        }

        private bool IsUrl(string query)
        {
            return query.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   query.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                   query.StartsWith("www.", StringComparison.OrdinalIgnoreCase);
        }

        private string ExtractDomain(string url)
        {
            try
            {
                var uri = new Uri(url);
                return uri.Host;
            }
            catch
            {
                return "unknown";
            }
        }

        private ResultCategory DetermineCategory(string url, string text)
        {
            url = url.ToLower();
            text = text.ToLower();

            // Social Media
            if (url.Contains("facebook.com") || url.Contains("twitter.com") || 
                url.Contains("instagram.com") || url.Contains("linkedin.com"))
                return ResultCategory.Social;

            // Forum
            if (url.Contains("forum") || url.Contains("board") || text.Contains("posted by"))
                return ResultCategory.Forum;

            // Archive
            if (url.Contains("archive"))
                return ResultCategory.Archive;

            // Adult
            var adultKeywords = new[] { "xxx", "porn", "adult", "nsfw" };
            if (adultKeywords.Any(k => url.Contains(k)))
                return ResultCategory.Adult;

            // Document
            if (url.EndsWith(".pdf") || url.EndsWith(".doc"))
                return ResultCategory.Document;

            // Image
            if (url.EndsWith(".jpg") || url.EndsWith(".png") || url.EndsWith(".gif"))
                return ResultCategory.Image;

            // Video
            if (url.Contains("youtube.com") || url.Contains("vimeo.com"))
                return ResultCategory.Video;

            return ResultCategory.Web;
        }

        private double CalculateConfidenceScore(SearchResult result, string searchTerm)
        {
            double score = 0.5; // Basis

            var lowerText = (result.Title + " " + result.Snippet + " " + result.ExtractedText).ToLower();
            var lowerSearch = searchTerm.ToLower();

            // Exakte Übereinstimmung im Titel
            if (result.Title.ToLower().Contains(lowerSearch))
                score += 0.3;

            // Im Snippet
            if (result.Snippet.ToLower().Contains(lowerSearch))
                score += 0.2;

            // Identity Markers gefunden
            score += Math.Min(result.IdentityMarkers.Count * 0.05, 0.3);

            // Domain-Reputation (bekannte Plattformen)
            var trustedDomains = new[] { "linkedin.com", "facebook.com", "twitter.com", "github.com" };
            if (trustedDomains.Any(d => result.Domain.Contains(d)))
                score += 0.1;

            return Math.Min(score, 1.0);
        }

        private List<SearchResult> ApplyFilter(List<SearchResult> results, SearchFilter filter)
        {
            var filtered = results.AsEnumerable();

            if (filter.HideAdult)
                filtered = filtered.Where(r => r.Category != ResultCategory.Adult);

            if (filter.HideDuplicates)
                filtered = filtered.Where(r => !r.IsDuplicate);

            if (filter.MinConfidence > 0)
                filtered = filtered.Where(r => r.ConfidenceScore >= filter.MinConfidence);

            if (filter.IncludedCategories.Any())
                filtered = filtered.Where(r => filter.IncludedCategories.Contains(r.Category));

            if (filter.IncludedAccessStatuses.Any())
                filtered = filtered.Where(r => filter.IncludedAccessStatuses.Contains(r.AccessStatus));

            if (filter.FromDate.HasValue)
                filtered = filtered.Where(r => r.FoundAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
                filtered = filtered.Where(r => r.FoundAt <= filter.ToDate.Value);

            return filtered.ToList();
        }
    }

    /// <summary>
    /// Web Crawler für HTTP-Requests
    /// </summary>
    public class WebCrawler
    {
        private readonly HttpClient _client;

        public WebCrawler()
        {
            _client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _client.DefaultRequestHeaders.Add("User-Agent", 
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<CrawlResult?> FetchAsync(string url)
        {
            try
            {
                var response = await _client.GetAsync(url);
                
                if (response.IsSuccessStatusCode)
                {
                    var html = await response.Content.ReadAsStringAsync();
                    return new CrawlResult
                    {
                        Url = url,
                        Html = html,
                        StatusCode = (int)response.StatusCode,
                        Success = true
                    };
                }
                
                return new CrawlResult
                {
                    Url = url,
                    StatusCode = (int)response.StatusCode,
                    Success = false
                };
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching URL {Url}", url);
                return null;
            }
        }
    }

    public class CrawlResult
    {
        public string Url { get; set; } = string.Empty;
        public string Html { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// Extrahiert strukturierten Content aus HTML
    /// </summary>
    public class ContentExtractor
    {
        public async Task<ExtractedContent> ExtractAsync(string html, string url)
        {
            return await Task.Run(() =>
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var content = new ExtractedContent();

                // Titel
                content.Title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText?.Trim() ?? "Untitled";

                // Text extrahieren
                var bodyNode = doc.DocumentNode.SelectSingleNode("//body");
                if (bodyNode != null)
                {
                    content.Text = CleanText(bodyNode.InnerText);
                }

                // Bilder
                var imgNodes = doc.DocumentNode.SelectNodes("//img[@src]");
                if (imgNodes != null)
                {
                    content.MediaUrls.AddRange(imgNodes.Select(n => MakeAbsoluteUrl(n.GetAttributeValue("src", ""), url)));
                }

                // Links
                var linkNodes = doc.DocumentNode.SelectNodes("//a[@href]");
                if (linkNodes != null)
                {
                    content.Links.AddRange(linkNodes.Select(n => MakeAbsoluteUrl(n.GetAttributeValue("href", ""), url)));
                }

                // Meta-Tags
                var metaNodes = doc.DocumentNode.SelectNodes("//meta");
                if (metaNodes != null)
                {
                    foreach (var meta in metaNodes)
                    {
                        var name = meta.GetAttributeValue("name", "") ?? meta.GetAttributeValue("property", "");
                        var content_attr = meta.GetAttributeValue("content", "");
                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(content_attr))
                        {
                            content.Metadata[name] = content_attr;
                        }
                    }
                }

                return content;
            });
        }

        private string CleanText(string text)
        {
            return System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        }

        private string MakeAbsoluteUrl(string url, string baseUrl)
        {
            if (url.StartsWith("http"))
                return url;
            
            try
            {
                var baseUri = new Uri(baseUrl);
                var absoluteUri = new Uri(baseUri, url);
                return absoluteUri.ToString();
            }
            catch
            {
                return url;
            }
        }
    }

    public class ExtractedContent
    {
        public string Title { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public List<string> MediaUrls { get; set; } = new();
        public List<string> Links { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
