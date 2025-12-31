using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DeepSeeArch.Models;
using Serilog;

namespace DeepSeeArch.Storage
{
    public class CaseStorageManager
    {
        private readonly string _basePath;

        public CaseStorageManager()
        {
            _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DeepSeeArch", "Cases");
            Directory.CreateDirectory(_basePath);
        }

        public async Task<SearchCase> CreateCaseAsync(string name, string query)
        {
            var sc = new SearchCase { Id = Guid.NewGuid().ToString(), Name = name, Query = query };
            sc.StoragePath = Path.Combine(_basePath, sc.Id);
            Directory.CreateDirectory(sc.StoragePath);
            await SaveCaseAsync(sc);
            return sc;
        }

        public async Task SaveCaseAsync(SearchCase sc)
        {
            var path = Path.Combine(sc.StoragePath, "case.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(sc));
        }

        public async Task<SearchCase?> LoadCaseAsync(string id)
        {
            var path = Path.Combine(_basePath, id, "case.json");
            if (!File.Exists(path)) return null;
            return JsonSerializer.Deserialize<SearchCase>(await File.ReadAllTextAsync(path));
        }
    }
}
EOF
cat /tmp/CaseStorageManager.cs
Ausgabe

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DeepSeeArch.Models;
using Serilog;

namespace DeepSeeArch.Storage
{
    public class CaseStorageManager
    {
        private readonly string _basePath;

        public CaseStorageManager()
        {
            _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DeepSeeArch", "Cases");
            Directory.CreateDirectory(_basePath);
        }

        public async Task<SearchCase> CreateCaseAsync(string name, string query)
        {
            var sc = new SearchCase { Id = Guid.NewGuid().ToString(), Name = name, Query = query };
            sc.StoragePath = Path.Combine(_basePath, sc.Id);
            Directory.CreateDirectory(sc.StoragePath);
            await SaveCaseAsync(sc);
            return sc;
        }

        public async Task SaveCaseAsync(SearchCase sc)
        {
            var path = Path.Combine(sc.StoragePath, "case.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(sc));
        }

        public async Task<SearchCase?> LoadCaseAsync(string id)
        {
            var path = Path.Combine(_basePath, id, "case.json");
            if (!File.Exists(path)) return null;
            return JsonSerializer.Deserialize<SearchCase>(await File.ReadAllTextAsync(path));
        }
    }
}