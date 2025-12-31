using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DeepSeeArch.Models;
using Serilog;

namespace DeepSeeArch.Core
{
    /// <summary>
    /// Generiert Text-Reports aus Suchergebnissen
    /// </summary>
    public class ReportGenerator
    {
        /// <summary>
        /// Erstellt einen detaillierten Text-Report
        /// </summary>
        public async Task<string> GenerateTextReportAsync(SearchCase searchCase, ReportOptions? options = null)
        {
            options ??= new ReportOptions();
            
            var sb = new StringBuilder();
            
            // Header
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  DEEPSEEARCH REPORT - {searchCase.Name}");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"Generiert: {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine($"Suchbegriff: {searchCase.Query}");
            sb.AppendLine();

            // Zusammenfassung
            sb.AppendLine("─── ZUSAMMENFASSUNG ───");
            sb.AppendLine();
            
            var markedResults = searchCase.Results.Where(r => r.Marking.IsMarked).ToList();
            var fakeProfiles = searchCase.Results.Where(r => r.Marking.Type == MarkingType.Fake).ToList();
            var accountsWithEmail = searchCase.Results.Where(r => r.AccountInfo?.Email != null).ToList();
            var importantResults = searchCase.Results.Where(r => r.Marking.Type == MarkingType.Important).ToList();

            sb.AppendLine($"Gesamte Ergebnisse: {searchCase.Results.Count}");
            sb.AppendLine($"Markierte Ergebnisse: {markedResults.Count}");
            sb.AppendLine($"Fake-Profile: {fakeProfiles.Count}");
            sb.AppendLine($"Accounts mit E-Mail: {accountsWithEmail.Count}");
            sb.AppendLine($"Wichtige Ergebnisse: {importantResults.Count}");
            sb.AppendLine($"Profile mit Accounts: {searchCase.Results.Count(r => r.AccountInfo != null)}");
            sb.AppendLine();

            // Kategorie-Verteilung
            sb.AppendLine("─── KATEGORIEN ───");
            sb.AppendLine();
            var categoryCounts = searchCase.Results.GroupBy(r => r.Category)
                .OrderByDescending(g => g.Count());
            
            foreach (var group in categoryCounts)
            {
                sb.AppendLine($"  {GetCategoryIcon(group.Key)} {group.Key}: {group.Count}");
            }
            sb.AppendLine();

            // KRITISCHE FUNDE (Fake-Profile, Missbrauch)
            if (fakeProfiles.Any() || searchCase.Results.Any(r => r.AccountInfo?.IsPotentialMisuse == true))
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("  ⚠️  KRITISCHE FUNDE - SOFORTIGE AKTION ERFORDERLICH");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine();

                var criticalResults = searchCase.Results
                    .Where(r => r.Marking.Type == MarkingType.Fake || r.AccountInfo?.IsPotentialMisuse == true)
                    .OrderByDescending(r => r.ConfidenceScore);

                int criticalIndex = 1;
                foreach (var result in criticalResults)
                {
                    sb.AppendLine($"[KRITISCH #{criticalIndex}] {result.Title}");
                    sb.AppendLine($"URL: {result.Url}");
                    sb.AppendLine($"Grund: {result.AccountInfo?.MisuseReason ?? "Als Fake markiert"}");
                    
                    if (result.AccountInfo != null)
                    {
                        sb.AppendLine($"Username: {result.AccountInfo.Username}");
                        if (!string.IsNullOrEmpty(result.AccountInfo.Email))
                            sb.AppendLine($"E-Mail: {result.AccountInfo.Email} ⚠️");
                    }
                    
                    if (!string.IsNullOrEmpty(result.Marking.UserNotes))
                        sb.AppendLine($"Notiz: {result.Marking.UserNotes}");
                    
                    sb.AppendLine();
                    criticalIndex++;
                }
            }

            // WICHTIGE ERGEBNISSE
            if (importantResults.Any())
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("  ⭐ WICHTIGE ERGEBNISSE");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine();

                var sortedImportant = importantResults
                    .OrderByDescending(r => r.Marking.Priority)
                    .ThenByDescending(r => r.ConfidenceScore);

                int importantIndex = 1;
                foreach (var result in sortedImportant)
                {
                    sb.AppendLine($"[WICHTIG #{importantIndex}] {result.Title}");
                    sb.AppendLine($"URL: {result.Url}");
                    sb.AppendLine($"Kategorie: {result.Category}");
                    sb.AppendLine($"Priorität: {GetPriorityIcon(result.Marking.Priority)} {result.Marking.Priority}");
                    sb.AppendLine($"Confidence: {result.ConfidenceScore:P0}");
                    
                    if (result.AccountInfo != null)
                    {
                        sb.AppendLine($"Username: {result.AccountInfo.Username ?? "N/A"}");
                        if (!string.IsNullOrEmpty(result.AccountInfo.Email))
                            sb.AppendLine($"E-Mail: {result.AccountInfo.Email} ⚠️");
                        if (!string.IsNullOrEmpty(result.AccountInfo.Location))
                            sb.AppendLine($"Ort: {result.AccountInfo.Location}");
                    }
                    
                    if (!string.IsNullOrEmpty(result.Marking.UserNotes))
                        sb.AppendLine($"Notiz: {result.Marking.UserNotes}");
                    
                    if (result.Marking.Tags.Any())
                        sb.AppendLine($"Tags: {string.Join(", ", result.Marking.Tags)}");
                    
                    sb.AppendLine();
                    importantIndex++;
                }
            }

            // ACCOUNTS MIT E-MAIL-ADRESSEN
            if (accountsWithEmail.Any())
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("  📧 GEFUNDENE E-MAIL-ADRESSEN");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine();

                int emailIndex = 1;
                foreach (var result in accountsWithEmail.OrderBy(r => r.AccountInfo!.Email))
                {
                    sb.AppendLine($"[E-MAIL #{emailIndex}] {result.AccountInfo!.Email}");
                    sb.AppendLine($"Plattform: {result.Domain}");
                    sb.AppendLine($"Username: {result.AccountInfo.Username ?? "N/A"}");
                    sb.AppendLine($"Display Name: {result.AccountInfo.DisplayName ?? "N/A"}");
                    sb.AppendLine($"URL: {result.Url}");
                    
                    if (result.Marking.IsMarked)
                        sb.AppendLine($"Status: {GetMarkingTypeIcon(result.Marking.Type)} {result.Marking.Type}");
                    
                    sb.AppendLine();
                    emailIndex++;
                }
            }

            // ALLE ERGEBNISSE (wenn gewünscht)
            if (options.IncludeAllResults)
            {
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine("  📋 ALLE ERGEBNISSE");
                sb.AppendLine("═══════════════════════════════════════════════════════════════");
                sb.AppendLine();

                var allResults = searchCase.Results
                    .OrderByDescending(r => r.ConfidenceScore);

                int allIndex = 1;
                foreach (var result in allResults)
                {
                    sb.AppendLine($"[#{allIndex}] {result.Title}");
                    sb.AppendLine($"URL: {result.Url}");
                    sb.AppendLine($"Kategorie: {GetCategoryIcon(result.Category)} {result.Category}");
                    sb.AppendLine($"Confidence: {result.ConfidenceScore:P0}");
                    
                    if (result.FuzzyMatch != null)
                    {
                        sb.AppendLine($"Match: {result.FuzzyMatch.Type} ({result.FuzzyMatch.SimilarityScore}%)");
                    }
                    
                    if (result.AccountInfo != null)
                    {
                        sb.AppendLine($"Account: {result.AccountInfo.Username ?? "N/A"}");
                        if (!string.IsNullOrEmpty(result.AccountInfo.Email))
                            sb.AppendLine($"E-Mail: {result.AccountInfo.Email}");
                    }
                    
                    if (result.Marking.IsMarked)
                    {
                        sb.AppendLine($"Markierung: {GetMarkingTypeIcon(result.Marking.Type)} {result.Marking.Type}");
                        if (!string.IsNullOrEmpty(result.Marking.UserNotes))
                            sb.AppendLine($"Notiz: {result.Marking.UserNotes}");
                    }
                    
                    sb.AppendLine();
                    allIndex++;
                }
            }

            // Footer
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  Ende des Reports - {searchCase.Results.Count} Ergebnisse analysiert");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");

            var reportText = sb.ToString();
            
            // In Datei speichern
            if (options.SaveToFile)
            {
                var filePath = await SaveReportToFileAsync(searchCase, reportText, options.OutputPath);
                Log.Information("Report saved to {Path}", filePath);
            }

            return reportText;
        }

        private async Task<string> SaveReportToFileAsync(SearchCase searchCase, string content, string? outputPath)
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                outputPath = Path.Combine(documentsPath, "DeepSeeArch", "Reports");
            }

            Directory.CreateDirectory(outputPath);

            var fileName = $"Report_{SanitizeFileName(searchCase.Name)}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            var filePath = Path.Combine(outputPath, fileName);

            await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
            
            return filePath;
        }

        private string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
        }

        private string GetCategoryIcon(ResultCategory category) => category switch
        {
            ResultCategory.Social => "👥",
            ResultCategory.Profile => "👤",
            ResultCategory.Image => "🖼️",
            ResultCategory.Video => "🎥",
            ResultCategory.Forum => "💬",
            ResultCategory.Document => "📄",
            ResultCategory.Archive => "📦",
            ResultCategory.Adult => "🔞",
            _ => "🌐"
        };

        private string GetMarkingTypeIcon(MarkingType type) => type switch
        {
            MarkingType.Important => "⭐",
            MarkingType.ToDelete => "🗑️",
            MarkingType.Fake => "⚠️",
            MarkingType.Legitimate => "✅",
            MarkingType.NeedsAction => "🔔",
            MarkingType.Archived => "📦",
            _ => "📌"
        };

        private string GetPriorityIcon(Priority priority) => priority switch
        {
            Priority.Urgent => "🔴",
            Priority.High => "🟠",
            Priority.Normal => "🟡",
            Priority.Low => "🟢",
            _ => "⚪"
        };
    }

    /// <summary>
    /// Optionen für Report-Generierung
    /// </summary>
    public class ReportOptions
    {
        public bool IncludeAllResults { get; set; } = true;
        public bool SaveToFile { get; set; } = true;
        public string? OutputPath { get; set; }
        public bool IncludeStatistics { get; set; } = true;
        public bool IncludeScreenshots { get; set; } = false;
    }
}
