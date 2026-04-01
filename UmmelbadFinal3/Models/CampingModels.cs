using System;
using System.Collections.Generic;
using System.Linq;

namespace UmmelbadFinal3.Models
{
    public enum StellplatzStatus
    {
        Frei,
        Belegt,
        Reserviert,
        Dauercamper
    }

    public class Stellplatz
    {
        public int Id { get; set; }
        public string Nummer { get; set; } = string.Empty;
        public decimal PosX { get; set; }
        public decimal PosY { get; set; }
        public StellplatzStatus Status { get; set; } = StellplatzStatus.Frei;
        public int? AktiveBuchungId { get; set; }
        public string Notizen { get; set; } = string.Empty;
    }

    public class Buchung
    {
        public int Id { get; set; }
        public Guid KundenId { get; set; }
        public List<int> StellplatzIds { get; set; } = new();
        public DateTime Startdatum { get; set; } = DateTime.Today;
        public DateTime Enddatum { get; set; } = DateTime.Today;
        public decimal Gesamtpreis { get; set; }
        public string Notizen { get; set; } = string.Empty;
    }

    public class Produkt
    {
        public string Name { get; set; } = string.Empty;
        public decimal Preis { get; set; }
    }

    public class CafeVerkauf
    {
        public int Id { get; set; }
        public DateTime Zeitpunkt { get; set; } = DateTime.Now;
        public List<CafePosition> Positionen { get; set; } = new();
        public Guid? KundenId { get; set; }
        public int? StellplatzId { get; set; }
        public decimal Gesamt => Positionen.Sum(p => p.Preis * p.Menge);
    }

    public class CafePosition
    {
        public string Name { get; set; } = string.Empty;
        public decimal Preis { get; set; }
        public int Menge { get; set; } = 1;
    }
}
