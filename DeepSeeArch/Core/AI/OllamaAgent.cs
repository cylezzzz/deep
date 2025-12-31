using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OllamaSharp;
using OllamaSharp.Models;
using DeepSeeArch.Models;
using Serilog;

namespace DeepSeeArch.Core.AI
{
    public class OllamaAgent
    {
        private readonly OllamaApiClient _client;
        private readonly string _defaultModel;
        private bool _isAvailable;

        public OllamaAgent(string ollamaUrl = "http://localhost:11434", string defaultModel = "llama2")
        {
            _client = new OllamaApiClient(ollamaUrl);
            _defaultModel = defaultModel;
            Log.Information("OllamaAgent initialized");
        }

        public async Task<bool> CheckAvailabilityAsync()
        {
            try
            {
                var models = await _client.ListLocalModels();
                _isAvailable = models != null && models.Any();
                return _isAvailable;
            }
            catch { _isAvailable = false; return false; }
        }

        public async Task<AccountData?> ExtractAccountDataAsync(string content, string url)
        {
            if (!_isAvailable) return null;
            try
            {
                var prompt = $"Extract account data from: {content.Substring(0, Math.Min(2000, content.Length))}";
                var response = await _client.Generate(new GenerateRequest { Model = _defaultModel, Prompt = prompt, Stream = false });
                return new AccountData(); // Placeholder
            }
            catch { return null; }
        }

        public async Task<(bool, string)> DetectIdentityMisuseAsync(string name, AccountData acc, string ctx)
        {
            if (!_isAvailable) return (false, "N/A");
            return (false, "OK");
        }

        public async Task<List<string>> GenerateSearchVariationsAsync(string name)
        {
            if (!_isAvailable) return new List<string> { name };
            return new List<string> { name, name.ToLower(), name.Replace(" ", "") };
        }

        public async Task<string> AnalyzeContentContextAsync(string title, string snippet, string content)
        {
            if (!_isAvailable) return "N/A";
            return "Content analysis";
        }
    }
}
EOF
cat /tmp/OllamaAgent.cs
Ausgabe

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OllamaSharp;
using OllamaSharp.Models;
using DeepSeeArch.Models;
using Serilog;

namespace DeepSeeArch.Core.AI
{
    public class OllamaAgent
    {
        private readonly OllamaApiClient _client;
        private readonly string _defaultModel;
        private bool _isAvailable;

        public OllamaAgent(string ollamaUrl = "http://localhost:11434", string defaultModel = "llama2")
        {
            _client = new OllamaApiClient(ollamaUrl);
            _defaultModel = defaultModel;
            Log.Information("OllamaAgent initialized");
        }

        public async Task<bool> CheckAvailabilityAsync()
        {
            try
            {
                var models = await _client.ListLocalModels();
                _isAvailable = models != null && models.Any();
                return _isAvailable;
            }
            catch { _isAvailable = false; return false; }
        }

        public async Task<AccountData?> ExtractAccountDataAsync(string content, string url)
        {
            if (!_isAvailable) return null;
            try
            {
                var prompt = $"Extract account data from: {content.Substring(0, Math.Min(2000, content.Length))}";
                var response = await _client.Generate(new GenerateRequest { Model = _defaultModel, Prompt = prompt, Stream = false });
                return new AccountData(); // Placeholder
            }
            catch { return null; }
        }

        public async Task<(bool, string)> DetectIdentityMisuseAsync(string name, AccountData acc, string ctx)
        {
            if (!_isAvailable) return (false, "N/A");
            return (false, "OK");
        }

        public async Task<List<string>> GenerateSearchVariationsAsync(string name)
        {
            if (!_isAvailable) return new List<string> { name };
            return new List<string> { name, name.ToLower(), name.Replace(" ", "") };
        }

        public async Task<string> AnalyzeContentContextAsync(string title, string snippet, string content)
        {
            if (!_isAvailable) return "N/A";
            return "Content analysis";
        }
    }
}