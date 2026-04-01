using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using UmmelbadFinal3.Models;

namespace UmmelbadFinal3.Services
{
    public class DataService
    {
        private readonly string _dataDirectory;
        private readonly string _stellplaetzeFile;
        private readonly string _buchungenFile;
        private readonly string _produkteFile;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true
        };

        public DataService(string baseDirectory)
        {
            _dataDirectory = Path.Combine(baseDirectory, "Data");
            Directory.CreateDirectory(_dataDirectory);

            _stellplaetzeFile = Path.Combine(_dataDirectory, "stellplaetze.json");
            _buchungenFile = Path.Combine(_dataDirectory, "buchungen.json");
            _produkteFile = Path.Combine(_dataDirectory, "produkte.json");
        }

        public List<Stellplatz> LoadStellplaetze() => LoadList<Stellplatz>(_stellplaetzeFile);

        public void SaveStellplaetze(List<Stellplatz> stellplaetze) => SaveList(_stellplaetzeFile, stellplaetze);

        public List<Buchung> LoadBuchungen() => LoadList<Buchung>(_buchungenFile);

        public void SaveBuchungen(List<Buchung> buchungen) => SaveList(_buchungenFile, buchungen);

        public List<Produkt> LoadProdukte() => LoadList<Produkt>(_produkteFile);

        public void SaveProdukte(List<Produkt> produkte) => SaveList(_produkteFile, produkte);

        private List<T> LoadList<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new List<T>();
                }

                var json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<T>();
                }

                return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }

        private void SaveList<T>(string filePath, List<T> data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? _dataDirectory);

                var json = JsonSerializer.Serialize(data ?? new List<T>(), _jsonOptions);
                var tempFile = filePath + ".tmp";
                var backupFile = filePath + ".bak";

                File.WriteAllText(tempFile, json);

                if (File.Exists(filePath))
                {
                    File.Copy(filePath, backupFile, true);
                    File.Delete(filePath);
                }

                File.Move(tempFile, filePath);
            }
            catch
            {
                // Absichtlich still: App soll stabil bleiben.
            }
        }
    }
}
