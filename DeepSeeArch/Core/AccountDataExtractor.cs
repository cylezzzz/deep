using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using DeepSeeArch.Models;
using Serilog;

namespace DeepSeeArch.Core
{
    /// <summary>
    /// Extrahiert Account-Daten von verschiedenen Plattformen
    /// </summary>
    public class AccountDataExtractor
    {
        private readonly Dictionary<string, PlatformExtractor> _platformExtractors;

        public AccountDataExtractor()
        {
            _platformExtractors = new Dictionary<string, PlatformExtractor>
            {
                { "facebook.com", new FacebookExtractor() },
                { "twitter.com", new TwitterExtractor() },
                { "x.com", new TwitterExtractor() },
                { "instagram.com", new InstagramExtractor() },
                { "linkedin.com", new LinkedInExtractor() },
                { "github.com", new GitHubExtractor() },
                { "reddit.com", new RedditExtractor() },
                { "tiktok.com", new TikTokExtractor() }
            };
        }

        /// <summary>
        /// Extrahiert Account-Daten aus HTML
        /// </summary>
        public async Task<AccountData?> ExtractAsync(string html, string url, string domain)
        {
            if (string.IsNullOrEmpty(html))
                return null;

            AccountData? accountData = null;

            // Plattform-spezifische Extraktion
            foreach (var (platformDomain, extractor) in _platformExtractors)
            {
                if (domain.Contains(platformDomain))
                {
                    accountData = await extractor.ExtractAsync(html, url);
                    if (accountData != null)
                    {
                        Log.Debug("Extracted account data from {Platform}", platformDomain);
                        break;
                    }
                }
            }

            // Fallback: Generische Extraktion
            if (accountData == null)
            {
                accountData = await GenericExtractAsync(html);
            }

            return accountData;
        }

        /// <summary>
        /// Generische Account-Daten-Extraktion
        /// </summary>
        private async Task<AccountData?> GenericExtractAsync(string html)
        {
            return await Task.Run(() =>
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var accountData = new AccountData();
                bool hasData = false;

                // Email-Extraktion mit Regex
                var emailPattern = @"\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}\b";
                var emailMatches = Regex.Matches(html, emailPattern);
                if (emailMatches.Any())
                {
                    accountData.Email = emailMatches.First().Value;
                    hasData = true;
                }

                // Meta-Tags durchsuchen
                var metaTags = doc.DocumentNode.SelectNodes("//meta[@name or @property]");
                if (metaTags != null)
                {
                    foreach (var meta in metaTags)
                    {
                        var name = meta.GetAttributeValue("name", "") ?? meta.GetAttributeValue("property", "");
                        var content = meta.GetAttributeValue("content", "");

                        name = name.ToLower();
                        
                        if (name.Contains("author") || name.Contains("creator"))
                        {
                            accountData.DisplayName = content;
                            hasData = true;
                        }
                        else if (name.Contains("description"))
                        {
                            accountData.Bio = content;
                            hasData = true;
                        }
                    }
                }

                // OpenGraph-Tags
                var ogTitle = doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
                if (ogTitle != null)
                {
                    accountData.DisplayName = ogTitle.GetAttributeValue("content", "");
                    hasData = true;
                }

                var ogDescription = doc.DocumentNode.SelectSingleNode("//meta[@property='og:description']");
                if (ogDescription != null && string.IsNullOrEmpty(accountData.Bio))
                {
                    accountData.Bio = ogDescription.GetAttributeValue("content", "");
                    hasData = true;
                }

                var ogImage = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']");
                if (ogImage != null)
                {
                    accountData.AvatarUrl = ogImage.GetAttributeValue("content", "");
                    hasData = true;
                }

                return hasData ? accountData : null;
            });
        }
    }

    /// <summary>
    /// Basis-Klasse für plattform-spezifische Extraktoren
    /// </summary>
    public abstract class PlatformExtractor
    {
        public abstract Task<AccountData?> ExtractAsync(string html, string url);

        protected string? ExtractBySelector(HtmlDocument doc, string selector)
        {
            var node = doc.DocumentNode.SelectSingleNode(selector);
            return node?.InnerText.Trim();
        }

        protected string? ExtractMetaContent(HtmlDocument doc, string property)
        {
            var node = doc.DocumentNode.SelectSingleNode($"//meta[@property='{property}']");
            return node?.GetAttributeValue("content", null);
        }
    }

    public class FacebookExtractor : PlatformExtractor
    {
        public override async Task<AccountData?> ExtractAsync(string html, string url)
        {
            return await Task.Run(() =>
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var accountData = new AccountData
                {
                    ProfileUrl = url
                };

                // Meta-Tags
                accountData.DisplayName = ExtractMetaContent(doc, "og:title");
                accountData.Bio = ExtractMetaContent(doc, "og:description");
                accountData.AvatarUrl = ExtractMetaContent(doc, "og:image");

                // Username aus URL
                var match = Regex.Match(url, @"facebook\.com/([^/]+)");
                if (match.Success)
                {
                    accountData.Username = match.Groups[1].Value;
                }

                return !string.IsNullOrEmpty(accountData.Username) || !string.IsNullOrEmpty(accountData.DisplayName) 
                    ? accountData 
                    : null;
            });
        }
    }

    public class TwitterExtractor : PlatformExtractor
    {
        public override async Task<AccountData?> ExtractAsync(string html, string url)
        {
            return await Task.Run(() =>
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var accountData = new AccountData
                {
                    ProfileUrl = url
                };

                // Username aus URL
                var match = Regex.Match(url, @"(?:twitter|x)\.com/([^/]+)");
                if (match.Success)
                {
                    accountData.Username = "@" + match.Groups[1].Value;
                }

                // Meta-Tags
                accountData.DisplayName = ExtractMetaContent(doc, "og:title");
                accountData.Bio = ExtractMetaContent(doc, "og:description");
                accountData.AvatarUrl = ExtractMetaContent(doc, "og:image");

                // Follower aus Text extrahieren
                var followerMatch = Regex.Match(html, @"(\d+(?:,\d+)*)\s*Followers");
                if (followerMatch.Success)
                {
                    var followerStr = followerMatch.Groups[1].Value.Replace(",", "");
                    if (int.TryParse(followerStr, out int followers))
                    {
                        accountData.FollowerCount = followers;
                    }
                }

                return !string.IsNullOrEmpty(accountData.Username) ? accountData : null;
            });
        }
    }

    public class InstagramExtractor : PlatformExtractor
    {
        public override async Task<AccountData?> ExtractAsync(string html, string url)
        {
            return await Task.Run(() =>
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var accountData = new AccountData
                {
                    ProfileUrl = url
                };

                // Username aus URL
                var match = Regex.Match(url, @"instagram\.com/([^/]+)");
                if (match.Success)
                {
                    accountData.Username = "@" + match.Groups[1].Value;
                }

                accountData.DisplayName = ExtractMetaContent(doc, "og:title");
                accountData.Bio = ExtractMetaContent(doc, "og:description");
                accountData.AvatarUrl = ExtractMetaContent(doc, "og:image");

                // Follower/Following aus JSON-LD
                var scriptNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
                if (scriptNodes != null)
                {
                    foreach (var script in scriptNodes)
                    {
                        var json = script.InnerText;
                        var followerMatch = Regex.Match(json, @"""follower"":\s*(\d+)");
                        if (followerMatch.Success && int.TryParse(followerMatch.Groups[1].Value, out int followers))
                        {
                            accountData.FollowerCount = followers;
                        }
                    }
                }

                return !string.IsNullOrEmpty(accountData.Username) ? accountData : null;
            });
        }
    }

    public class LinkedInExtractor : PlatformExtractor
    {
        public override async Task<AccountData?> ExtractAsync(string html, string url)
        {
            return await Task.Run(() =>
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var accountData = new AccountData
                {
                    ProfileUrl = url
                };

                accountData.DisplayName = ExtractMetaContent(doc, "og:title");
                accountData.Bio = ExtractMetaContent(doc, "og:description");
                accountData.AvatarUrl = ExtractMetaContent(doc, "og:image");

                // Location aus Meta oder Text
                var locationMatch = Regex.Match(html, @"<span[^>]*>([^<]+(?:Germany|Deutschland|Austria|Switzerland)[^<]*)</span>");
                if (locationMatch.Success)
                {
                    accountData.Location = locationMatch.Groups[1].Value.Trim();
                }

                return !string.IsNullOrEmpty(accountData.DisplayName) ? accountData : null;
            });
        }
    }

    public class GitHubExtractor : PlatformExtractor
    {
        public override async Task<AccountData?> ExtractAsync(string html, string url)
        {
            return await Task.Run(() =>
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var accountData = new AccountData
                {
                    ProfileUrl = url
                };

                // Username aus URL
                var match = Regex.Match(url, @"github\.com/([^/]+)");
                if (match.Success)
                {
                    accountData.Username = match.Groups[1].Value;
                }

                accountData.DisplayName = ExtractMetaContent(doc, "og:title");
                accountData.Bio = ExtractMetaContent(doc, "og:description");
                accountData.AvatarUrl = ExtractMetaContent(doc, "og:image");

                // Follower
                var followerNode = doc.DocumentNode.SelectSingleNode("//a[contains(@href, 'followers')]//span");
                if (followerNode != null && int.TryParse(followerNode.InnerText.Trim(), out int followers))
                {
                    accountData.FollowerCount = followers;
                }

                return !string.IsNullOrEmpty(accountData.Username) ? accountData : null;
            });
        }
    }

    public class RedditExtractor : PlatformExtractor
    {
        public override async Task<AccountData?> ExtractAsync(string html, string url)
        {
            return await Task.Run(() =>
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var accountData = new AccountData
                {
                    ProfileUrl = url
                };

                // Username aus URL
                var match = Regex.Match(url, @"reddit\.com/(?:user|u)/([^/]+)");
                if (match.Success)
                {
                    accountData.Username = "u/" + match.Groups[1].Value;
                }

                accountData.DisplayName = ExtractMetaContent(doc, "og:title");
                accountData.Bio = ExtractMetaContent(doc, "og:description");

                // Karma
                var karmaMatch = Regex.Match(html, @"(\d+(?:,\d+)*)\s*(?:post|comment)?\s*karma");
                if (karmaMatch.Success)
                {
                    var karmaStr = karmaMatch.Groups[1].Value.Replace(",", "");
                    accountData.CustomFields["Karma"] = karmaStr;
                }

                return !string.IsNullOrEmpty(accountData.Username) ? accountData : null;
            });
        }
    }

    public class TikTokExtractor : PlatformExtractor
    {
        public override async Task<AccountData?> ExtractAsync(string html, string url)
        {
            return await Task.Run(() =>
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var accountData = new AccountData
                {
                    ProfileUrl = url
                };

                // Username aus URL
                var match = Regex.Match(url, @"tiktok\.com/@([^/]+)");
                if (match.Success)
                {
                    accountData.Username = "@" + match.Groups[1].Value;
                }

                accountData.DisplayName = ExtractMetaContent(doc, "og:title");
                accountData.Bio = ExtractMetaContent(doc, "og:description");
                accountData.AvatarUrl = ExtractMetaContent(doc, "og:image");

                return !string.IsNullOrEmpty(accountData.Username) ? accountData : null;
            });
        }
    }
}
