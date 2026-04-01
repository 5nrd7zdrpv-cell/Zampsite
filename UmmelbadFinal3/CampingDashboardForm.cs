using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using UmmelbadFinal3.Models;
using UmmelbadFinal3.Services;

namespace UmmelbadFinal3
{
    public class CampingDashboardForm : Form
    {
        private readonly CampingService _campingService;
        private readonly CustomerService _customerService;
        private List<Stellplatz> _stellplaetze = new();
        private List<Buchung> _buchungen = new();
        private List<CafeVerkauf> _cafeVerkaeufe = new();
        private List<Customer> _kunden = new();

        private readonly FlowLayoutPanel _stellplatzPanel = new() { Dock = DockStyle.Fill, AutoScroll = true };
        private readonly ComboBox _cmbStellplatz = new();
        private readonly ComboBox _cmbKunde = new();
        private readonly DateTimePicker _dtStart = new() { Format = DateTimePickerFormat.Short };
        private readonly DateTimePicker _dtEnd = new() { Format = DateTimePickerFormat.Short };
        private readonly CheckBox _chkDauer = new() { Text = "Dauercamper" };
        private readonly NumericUpDown _numNachtpreis = new() { DecimalPlaces = 2, Value = 25, Maximum = 1000 };
        private readonly NumericUpDown _numJahrespreis = new() { DecimalPlaces = 2, Maximum = 100000 };
        private readonly Label _lblBuchungPreis = new() { AutoSize = true };
        private readonly ListBox _lstCafe = new() { Dock = DockStyle.Fill };

        private readonly Label _lblTodayOccupied = new() { AutoSize = true };
        private readonly Label _lblFree = new() { AutoSize = true };
        private readonly Label _lblRevenueDay = new() { AutoSize = true };
        private readonly Label _lblRevenueMonth = new() { AutoSize = true };

        private readonly List<CafePosition> _aktuellerCafeAuftrag = new();

        public CampingDashboardForm()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _campingService = new CampingService(baseDir);
            _customerService = new CustomerService(baseDir);

            InitializeComponent();
            Laden();
        }

        private void InitializeComponent()
        {
            Text = "Waldcampingplatz Ummelbad - Verwaltung";
            Width = 1280;
            Height = 820;
            StartPosition = FormStartPosition.CenterScreen;

            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(CreateDashboardTab());
            tabs.TabPages.Add(CreateStellplatzTab());
            tabs.TabPages.Add(CreateBuchungTab());
            tabs.TabPages.Add(CreateCafeTab());
            tabs.TabPages.Add(CreateRechnungTab());

            Controls.Add(tabs);
        }

        private TabPage CreateDashboardTab()
        {
            var tab = new TabPage("Übersicht");
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20) };
            panel.Controls.AddRange(new Control[] { _lblTodayOccupied, _lblFree, _lblRevenueDay, _lblRevenueMonth });
            tab.Controls.Add(panel);
            return tab;
        }

        private TabPage CreateStellplatzTab()
        {
            var tab = new TabPage("Stellplätze");
            tab.Controls.Add(_stellplatzPanel);
            return tab;
        }

        private TabPage CreateBuchungTab()
        {
            var tab = new TabPage("Schnellbuchung");
            var panel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(12) };

            panel.Controls.Add(new Label { Text = "Stellplatz", AutoSize = true }, 0, 0);
            panel.Controls.Add(_cmbStellplatz, 1, 0);
            panel.Controls.Add(new Label { Text = "Kunde", AutoSize = true }, 0, 1);
            panel.Controls.Add(_cmbKunde, 1, 1);
            panel.Controls.Add(new Label { Text = "Start", AutoSize = true }, 0, 2);
            panel.Controls.Add(_dtStart, 1, 2);
            panel.Controls.Add(new Label { Text = "Ende", AutoSize = true }, 0, 3);
            panel.Controls.Add(_dtEnd, 1, 3);
            panel.Controls.Add(new Label { Text = "Nachtpreis", AutoSize = true }, 0, 4);
            panel.Controls.Add(_numNachtpreis, 1, 4);
            panel.Controls.Add(_chkDauer, 0, 5);
            panel.Controls.Add(new Label { Text = "Jahrespreis", AutoSize = true }, 0, 6);
            panel.Controls.Add(_numJahrespreis, 1, 6);
            panel.Controls.Add(_lblBuchungPreis, 1, 7);

            var btn = new Button { Text = "Buchung speichern", AutoSize = true };
            btn.Click += (_, _) => SchnellbuchungSpeichern();
            panel.Controls.Add(btn, 1, 8);

            _dtStart.ValueChanged += (_, _) => UpdateBuchungPreis();
            _dtEnd.ValueChanged += (_, _) => UpdateBuchungPreis();
            _numNachtpreis.ValueChanged += (_, _) => UpdateBuchungPreis();
            _chkDauer.CheckedChanged += (_, _) => UpdateBuchungPreis();

            tab.Controls.Add(panel);
            return tab;
        }

        private TabPage CreateCafeTab()
        {
            var tab = new TabPage("Café-Kasse");
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new Padding(12) };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

            var left = new FlowLayoutPanel { Dock = DockStyle.Fill };
            foreach (var produkt in _campingService.GetStandardProdukte())
            {
                var btn = new Button { Text = $"{produkt.Name}\n{produkt.Preis:C}", Width = 140, Height = 70, Tag = produkt };
                btn.Click += (_, _) => AddCafePosition((Produkt)btn.Tag);
                left.Controls.Add(btn);
            }

            var right = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            right.Controls.Add(_lstCafe, 0, 0);

            var btnSave = new Button { Text = "Verkauf abschließen", AutoSize = true };
            btnSave.Click += (_, _) => SaveCafeSale();
            right.Controls.Add(btnSave, 0, 1);

            var btnClear = new Button { Text = "Auftrag leeren", AutoSize = true };
            btnClear.Click += (_, _) => { _aktuellerCafeAuftrag.Clear(); RefreshCafeList(); };
            right.Controls.Add(btnClear, 0, 2);

            root.Controls.Add(left, 0, 0);
            root.Controls.Add(right, 1, 0);
            tab.Controls.Add(root);
            return tab;
        }

        private TabPage CreateRechnungTab()
        {
            var tab = new TabPage("Rechnungen");
            var btn = new Button { Text = "Rechnungsmodul öffnen", AutoSize = true, Location = new Point(20, 20) };
            btn.Click += (_, _) => new InvoiceForm().ShowDialog(this);
            tab.Controls.Add(btn);
            return tab;
        }

        private void Laden()
        {
            _stellplaetze = _campingService.LoadStellplaetze();
            _buchungen = _campingService.LoadBuchungen();
            _cafeVerkaeufe = _campingService.LoadCafeVerkaeufe();
            _kunden = _customerService.LoadCustomers();

            _cmbStellplatz.DataSource = _stellplaetze;
            _cmbStellplatz.DisplayMember = nameof(Stellplatz.NummerOderName);
            _cmbKunde.DataSource = _kunden;
            _cmbKunde.DisplayMember = nameof(Customer.Name);

            RenderStellplaetze();
            UpdateBuchungPreis();
            UpdateDashboard();
        }

        private void RenderStellplaetze()
        {
            _stellplatzPanel.Controls.Clear();
            foreach (var s in _stellplaetze.OrderBy(x => x.Id))
            {
                var btn = new Button
                {
                    Text = $"{s.NummerOderName}\n{s.Status}",
                    Width = 130,
                    Height = 70,
                    BackColor = ToColor(s.Status),
                    Tag = s
                };

                btn.Click += (_, _) => EditStellplatz((Stellplatz)btn.Tag);
                _stellplatzPanel.Controls.Add(btn);
            }
        }

        private static Color ToColor(StellplatzStatus status) => status switch
        {
            StellplatzStatus.Frei => Color.LightGreen,
            StellplatzStatus.Belegt => Color.LightCoral,
            StellplatzStatus.Reserviert => Color.Khaki,
            StellplatzStatus.Dauercamper => Color.LightSkyBlue,
            _ => SystemColors.Control
        };

        private void EditStellplatz(Stellplatz stellplatz)
        {
            var status = Microsoft.VisualBasic.Interaction.InputBox("Status (Frei, Belegt, Reserviert, Dauercamper)", "Stellplatz", stellplatz.Status.ToString());
            if (Enum.TryParse<StellplatzStatus>(status, true, out var parsed))
            {
                stellplatz.Status = parsed;
                _campingService.SaveStellplaetze(_stellplaetze);
                RenderStellplaetze();
                UpdateDashboard();
            }
        }

        private void SchnellbuchungSpeichern()
        {
            if (_cmbKunde.SelectedItem is not Customer kunde || _cmbStellplatz.SelectedItem is not Stellplatz stellplatz)
            {
                MessageBox.Show("Bitte Kunde und Stellplatz wählen.");
                return;
            }

            var buchung = new Buchung
            {
                Id = _buchungen.Count == 0 ? 1 : _buchungen.Max(x => x.Id) + 1,
                KundenId = kunde.Id,
                StellplatzId = stellplatz.Id,
                Startdatum = _dtStart.Value.Date,
                Enddatum = _dtEnd.Value.Date,
                IstDauercamper = _chkDauer.Checked,
                Jahrespreis = _chkDauer.Checked ? _numJahrespreis.Value : null,
                Gesamtpreis = _chkDauer.Checked ? _numJahrespreis.Value : _campingService.BerechneBuchungspreis(_dtStart.Value, _dtEnd.Value, _numNachtpreis.Value)
            };

            _buchungen.Add(buchung);
            stellplatz.Status = buchung.IstDauercamper ? StellplatzStatus.Dauercamper : StellplatzStatus.Belegt;
            stellplatz.AktuelleKundenId = kunde.Id;
            _campingService.SaveBuchungen(_buchungen);
            _campingService.SaveStellplaetze(_stellplaetze);
            RenderStellplaetze();
            UpdateDashboard();
            MessageBox.Show("Buchung gespeichert.");
        }

        private void UpdateBuchungPreis()
        {
            var preis = _chkDauer.Checked
                ? _numJahrespreis.Value
                : _campingService.BerechneBuchungspreis(_dtStart.Value, _dtEnd.Value, _numNachtpreis.Value);
            _lblBuchungPreis.Text = $"Gesamtpreis: {preis:C}";
        }

        private void AddCafePosition(Produkt produkt)
        {
            var existing = _aktuellerCafeAuftrag.FirstOrDefault(p => p.Name == produkt.Name && p.Preis == produkt.Preis);
            if (existing == null)
            {
                _aktuellerCafeAuftrag.Add(new CafePosition { Name = produkt.Name, Preis = produkt.Preis, Menge = 1 });
            }
            else
            {
                existing.Menge++;
            }
            RefreshCafeList();
        }

        private void RefreshCafeList()
        {
            _lstCafe.Items.Clear();
            foreach (var p in _aktuellerCafeAuftrag)
            {
                _lstCafe.Items.Add($"{p.Menge}x {p.Name} - {(p.Preis * p.Menge):C}");
            }
            _lstCafe.Items.Add($"---- Gesamt: {_aktuellerCafeAuftrag.Sum(x => x.Preis * x.Menge):C}");
        }

        private void SaveCafeSale()
        {
            if (_aktuellerCafeAuftrag.Count == 0)
            {
                MessageBox.Show("Kein Artikel im Auftrag.");
                return;
            }

            var sale = new CafeVerkauf
            {
                Id = _cafeVerkaeufe.Count == 0 ? 1 : _cafeVerkaeufe.Max(x => x.Id) + 1,
                Zeitpunkt = DateTime.Now,
                Positionen = _aktuellerCafeAuftrag.Select(x => new CafePosition { Name = x.Name, Preis = x.Preis, Menge = x.Menge }).ToList(),
                KundenId = (_cmbKunde.SelectedItem as Customer)?.Id,
                StellplatzId = (_cmbStellplatz.SelectedItem as Stellplatz)?.Id
            };

            _cafeVerkaeufe.Add(sale);
            _campingService.SaveCafeVerkaeufe(_cafeVerkaeufe);
            _aktuellerCafeAuftrag.Clear();
            RefreshCafeList();
            UpdateDashboard();
            MessageBox.Show("Café-Verkauf gespeichert.");
        }

        private void UpdateDashboard()
        {
            var today = DateTime.Today;
            var occupied = _stellplaetze.Count(s => s.Status is StellplatzStatus.Belegt or StellplatzStatus.Dauercamper);
            var free = _stellplaetze.Count(s => s.Status == StellplatzStatus.Frei);
            var day = _cafeVerkaeufe.Where(v => v.Zeitpunkt.Date == today).Sum(v => v.Gesamt);
            var month = _cafeVerkaeufe.Where(v => v.Zeitpunkt.Year == today.Year && v.Zeitpunkt.Month == today.Month).Sum(v => v.Gesamt)
                + _buchungen.Where(b => b.Startdatum.Year == today.Year && b.Startdatum.Month == today.Month).Sum(b => b.Gesamtpreis);

            _lblTodayOccupied.Text = $"Belegte Stellplätze heute: {occupied}";
            _lblFree.Text = $"Freie Stellplätze: {free}";
            _lblRevenueDay.Text = $"Tagesumsatz: {day:C}";
            _lblRevenueMonth.Text = $"Monatsumsatz: {month:C}";
        }
    }
}
