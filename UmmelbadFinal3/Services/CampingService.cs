using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UmmelbadFinal3.Models;

namespace UmmelbadFinal3.Services
{
    public class CampingService
    {
        private readonly string _dataDir;
        private readonly string _stellplaetzeFile;
        private readonly string _buchungenFile;
        private readonly string _cafeFile;
        private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        public CampingService(string baseDirectory)
        {
            _dataDir = Path.Combine(baseDirectory, "CampingData");
            Directory.CreateDirectory(_dataDir);
            _stellplaetzeFile = Path.Combine(_dataDir, "stellplaetze.json");
            _buchungenFile = Path.Combine(_dataDir, "buchungen.json");
            _cafeFile = Path.Combine(_dataDir, "cafe_verkaeufe.json");

            EnsureDefaults();
        }

        public List<Stellplatz> LoadStellplaetze() => Load<List<Stellplatz>>(_stellplaetzeFile) ?? new List<Stellplatz>();
        public List<Buchung> LoadBuchungen() => Load<List<Buchung>>(_buchungenFile) ?? new List<Buchung>();
        public List<CafeVerkauf> LoadCafeVerkaeufe() => Load<List<CafeVerkauf>>(_cafeFile) ?? new List<CafeVerkauf>();

        public void SaveStellplaetze(List<Stellplatz> items) => Save(_stellplaetzeFile, items);
        public void SaveBuchungen(List<Buchung> items) => Save(_buchungenFile, items);
        public void SaveCafeVerkaeufe(List<CafeVerkauf> items) => Save(_cafeFile, items);

        public decimal BerechneBuchungspreis(DateTime start, DateTime ende, decimal preisProNacht)
        {
            var naechte = Math.Max(1, (ende.Date - start.Date).Days);
            return Math.Round(naechte * preisProNacht, 2);
        }

        public List<Produkt> GetStandardProdukte() => new()
        {
            new Produkt { Name = "Kaffee", Preis = 2.80m },
            new Produkt { Name = "Kuchen", Preis = 3.20m },
            new Produkt { Name = "Eis", Preis = 2.50m },
            new Produkt { Name = "Currywurst", Preis = 5.90m },
            new Produkt { Name = "Getränke", Preis = 2.90m }
        };

        private void EnsureDefaults()
        {
            if (!File.Exists(_stellplaetzeFile))
            {
                var defaults = Enumerable.Range(1, 20)
                    .Select(i => new Stellplatz { Id = i, Nummer = $"SP-{i:00}" })
                    .ToList();
                SaveStellplaetze(defaults);
            }

            if (!File.Exists(_buchungenFile)) SaveBuchungen(new List<Buchung>());
            if (!File.Exists(_cafeFile)) SaveCafeVerkaeufe(new List<CafeVerkauf>());
        }

        private T? Load<T>(string path)
        {
            if (!File.Exists(path)) return default;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json);
        }

        private void Save<T>(string path, T data)
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            File.WriteAllText(path, json);
        }
    }
}
