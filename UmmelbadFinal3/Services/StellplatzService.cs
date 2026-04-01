using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UmmelbadFinal3.Models;

namespace UmmelbadFinal3.Services
{
    public class StellplatzService
    {
        private readonly DataService _dataService;
        private readonly string _stellplaetzeFile;

        public StellplatzService(string baseDirectory, DataService? dataService = null)
        {
            _dataService = dataService ?? new DataService();
            _stellplaetzeFile = Path.Combine(baseDirectory, "CampingData", "stellplaetze.json");
        }

        public List<Stellplatz> LadeAlleStellplaetze()
        {
            return _dataService.Load(_stellplaetzeFile, new List<Stellplatz>());
        }

        public bool SetzeStatus(int stellplatzId, StellplatzStatus status)
        {
            var stellplaetze = LadeAlleStellplaetze();
            var stellplatz = stellplaetze.FirstOrDefault(sp => sp.Id == stellplatzId);
            if (stellplatz == null)
            {
                return false;
            }

            stellplatz.Status = status;
            _dataService.Save(_stellplaetzeFile, stellplaetze);
            return true;
        }

        public bool WeiseBuchungZu(int stellplatzId, int buchungId)
        {
            var stellplaetze = LadeAlleStellplaetze();
            var stellplatz = stellplaetze.FirstOrDefault(sp => sp.Id == stellplatzId);
            if (stellplatz == null)
            {
                return false;
            }

            stellplatz.AktiveBuchungId = buchungId;
            stellplatz.Status = StellplatzStatus.Belegt;
            _dataService.Save(_stellplaetzeFile, stellplaetze);
            return true;
        }
    }
}
