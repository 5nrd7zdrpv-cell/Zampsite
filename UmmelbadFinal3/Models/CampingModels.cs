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

    public enum Zahlungsstatus
    {
        Offen,
        Teilbezahlt,
        Bezahlt
    }

    public class Stellplatz
    {
        public int Id { get; set; }
        public string NummerOderName { get; set; } = string.Empty;
        public StellplatzStatus Status { get; set; } = StellplatzStatus.Frei;
        public Guid? AktuelleKundenId { get; set; }
        public string Notizen { get; set; } = string.Empty;
        public string Warnhinweis { get; set; } = string.Empty;
    }

    public class Buchung
    {
        public int Id { get; set; }
        public Guid KundenId { get; set; }
        public int StellplatzId { get; set; }
        public DateTime Startdatum { get; set; } = DateTime.Today;
        public DateTime Enddatum { get; set; } = DateTime.Today;
        public bool IstDauercamper { get; set; }
        public decimal Gesamtpreis { get; set; }
        public decimal? Jahrespreis { get; set; }
        public Zahlungsstatus Zahlungsstatus { get; set; } = Zahlungsstatus.Offen;
        public string Notizen { get; set; } = string.Empty;
        public StromAbrechnung Strom { get; set; } = new();
    }

    public class StromAbrechnung
    {
        public decimal ZaehlerStart { get; set; }
        public decimal ZaehlerEnde { get; set; }
        public decimal PreisProKwh { get; set; } = 0.6m;
        public decimal Verbrauch => Math.Max(0, ZaehlerEnde - ZaehlerStart);
        public decimal Gesamt => Math.Round(Verbrauch * PreisProKwh, 2);
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
