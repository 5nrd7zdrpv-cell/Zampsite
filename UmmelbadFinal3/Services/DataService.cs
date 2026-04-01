using System.IO;
using System.Text.Json;

namespace UmmelbadFinal3.Services
{
    public class DataService
    {
        private readonly JsonSerializerOptions _jsonOptions;

        public DataService(JsonSerializerOptions? jsonOptions = null)
        {
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions { WriteIndented = true };
        }

        public T Load<T>(string path, T fallback)
        {
            if (!File.Exists(path))
            {
                return fallback;
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json) ?? fallback;
        }

        public void Save<T>(string path, T data)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(path, json);
        }
    }
}
