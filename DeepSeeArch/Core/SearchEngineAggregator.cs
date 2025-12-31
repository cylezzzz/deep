using System.Collections.Generic;
using System.Threading.Tasks;
using DeepSeeArch.Models;
using Serilog;

namespace DeepSeeArch.Core
{
    public class SearchEngineAggregator
    {
        public async Task<List<SearchResult>> SearchGoogleAsync(string query)
        {
            Log.Information("Search: {Query}", query);
            return await Task.FromResult(new List<SearchResult>());
        }

        public async Task<List<SearchResult>> SearchBingAsync(string q) => await Task.FromResult(new List<SearchResult>());
        public async Task<List<SearchResult>> SearchDuckDuckGoAsync(string q) => await Task.FromResult(new List<SearchResult>());
        public async Task<List<SearchResult>> SearchSocialMediaAsync(string q) => await Task.FromResult(new List<SearchResult>());
        public async Task<List<SearchResult>> SearchForumsAsync(string q) => await Task.FromResult(new List<SearchResult>());
        public async Task<List<SearchResult>> SearchArchivesAsync(string q) => await Task.FromResult(new List<SearchResult>());
    }
}
EOF
cat /tmp/SearchEngineAggregator.cs
Ausgabe

using System.Collections.Generic;
using System.Threading.Tasks;
using DeepSeeArch.Models;
using Serilog;

namespace DeepSeeArch.Core
{
    public class SearchEngineAggregator
    {
        public async Task<List<SearchResult>> SearchGoogleAsync(string query)
        {
            Log.Information("Search: {Query}", query);
            return await Task.FromResult(new List<SearchResult>());
        }

        public async Task<List<SearchResult>> SearchBingAsync(string q) => await Task.FromResult(new List<SearchResult>());
        public async Task<List<SearchResult>> SearchDuckDuckGoAsync(string q) => await Task.FromResult(new List<SearchResult>());
        public async Task<List<SearchResult>> SearchSocialMediaAsync(string q) => await Task.FromResult(new List<SearchResult>());
        public async Task<List<SearchResult>> SearchForumsAsync(string q) => await Task.FromResult(new List<SearchResult>());
        public async Task<List<SearchResult>> SearchArchivesAsync(string q) => await Task.FromResult(new List<SearchResult>());
    }
}