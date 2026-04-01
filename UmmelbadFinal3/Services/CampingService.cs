using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UmmelbadFinal3.Models;

namespace UmmelbadFinal3.Services
{
    public class CampingService
    {
        private readonly DataService _dataService;
        private readonly string _dataDir;
        private readonly string _stellplaetzeFile;
        private readonly string _buchungenFile;
        private readonly string _cafeFile;

        public CampingService(string baseDirectory, DataService? dataService = null)
        {
            _dataService = dataService ?? new DataService();
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
        public List<Buchung> LoadOffeneBuchungen() => LoadBuchungen().Where(x => string.IsNullOrWhiteSpace(x.InvoiceNumber)).ToList();
        public List<CafeVerkauf> LoadOffeneCafeVerkaeufe() => LoadCafeVerkaeufe().Where(x => string.IsNullOrWhiteSpace(x.InvoiceNumber)).ToList();

        public void SaveStellplaetze(List<Stellplatz> items) => Save(_stellplaetzeFile, items);
        public void SaveBuchungen(List<Buchung> items) => Save(_buchungenFile, items);
        public void SaveCafeVerkaeufe(List<CafeVerkauf> items) => Save(_cafeFile, items);

        public void MarkiereBuchungenAlsAbgerechnet(IEnumerable<int> buchungIds, string invoiceNumber)
        {
            var idSet = buchungIds.ToHashSet();
            if (idSet.Count == 0)
            {
                return;
            }

            var buchungen = LoadBuchungen();
            foreach (var buchung in buchungen.Where(x => idSet.Contains(x.Id)))
            {
                buchung.InvoiceNumber = invoiceNumber;
            }

            SaveBuchungen(buchungen);
        }

        public void MarkiereCafeVerkaeufeAlsAbgerechnet(IEnumerable<int> verkaufIds, string invoiceNumber)
        {
            var idSet = verkaufIds.ToHashSet();
            if (idSet.Count == 0)
            {
                return;
            }

            var verkaeufe = LoadCafeVerkaeufe();
            foreach (var verkauf in verkaeufe.Where(x => idSet.Contains(x.Id)))
            {
                verkauf.InvoiceNumber = invoiceNumber;
            }

            SaveCafeVerkaeufe(verkaeufe);
        }

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
            InitializeStellplaetze();

            if (!File.Exists(_buchungenFile)) SaveBuchungen(new List<Buchung>());
            if (!File.Exists(_cafeFile)) SaveCafeVerkaeufe(new List<CafeVerkauf>());
        }

        public void InitializeStellplaetze()
        {
            var bestehendeStellplaetze = LoadStellplaetze();
            if (bestehendeStellplaetze.Count > 0) return;

            const int anzahlSpalten = 25;
            const decimal abstandX = 10m;
            const decimal abstandY = 10m;

            var stellplaetze = Enumerable.Range(1, 250)
                .Select(i =>
                {
                    var index = i - 1;
                    var spalte = index % anzahlSpalten;
                    var zeile = index / anzahlSpalten;

                    return new Stellplatz
                    {
                        Id = i,
                        Nummer = $"SP-{i:000}",
                        PosX = spalte * abstandX,
                        PosY = zeile * abstandY,
                        Status = StellplatzStatus.Frei
                    };
                })
                .ToList();

            SaveStellplaetze(stellplaetze);
        }

        private T? Load<T>(string path)
        {
            return _dataService.Load<T?>(path, default);
        }

        private void Save<T>(string path, T data)
        {
            _dataService.Save(path, data);
        }
    }
}
