using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using UmmelbadFinal3.Models;

namespace UmmelbadFinal3
{
    public sealed class PlatzplanForm : Form
    {
        private readonly Panel _planPanel;
        private readonly Button _createBookingButton;
        private readonly Label _selectionInfoLabel;
        private readonly List<Stellplatz> _stellplaetze;
        private readonly List<Stellplatz> _selectedStellplaetze;
        private readonly Dictionary<int, Button> _stellplatzButtons;
        private readonly List<Buchung> _createdBuchungen;

        public PlatzplanForm()
        {
            Text = "Platzplan - Stellplatzübersicht";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1200, 820);
            MinimumSize = new Size(900, 650);

            _selectedStellplaetze = new List<Stellplatz>();
            _stellplatzButtons = new Dictionary<int, Button>();
            _createdBuchungen = new List<Buchung>();

            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(12, 10, 12, 10)
            };

            _selectionInfoLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Text = "Keine Stellplätze ausgewählt"
            };

            _createBookingButton = new Button
            {
                Dock = DockStyle.Right,
                Width = 180,
                Text = "Buchung erstellen",
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(25, 118, 210),
                ForeColor = Color.White,
                Enabled = false
            };

            _createBookingButton.FlatAppearance.BorderSize = 0;
            _createBookingButton.Click += CreateBookingButton_Click;

            topBar.Controls.Add(_selectionInfoLabel);
            topBar.Controls.Add(_createBookingButton);

            _planPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackgroundImageLayout = ImageLayout.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Hintergrundbild (Platzplan) laden.
            // Passe den Pfad bei Bedarf an, wenn das Bild woanders liegt.
            var imagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "platzplan.png");
            if (System.IO.File.Exists(imagePath))
            {
                _planPanel.BackgroundImage = Image.FromFile(imagePath);
            }

            Controls.Add(_planPanel);
            Controls.Add(topBar);

            _stellplaetze = CreateDemoStellplaetze();
            RenderStellplatzButtons();
        }

        private void RenderStellplatzButtons()
        {
            _planPanel.Controls.Clear();
            _stellplatzButtons.Clear();

            foreach (var stellplatz in _stellplaetze)
            {
                var button = new Button
                {
                    Width = 42,
                    Height = 28,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    Text = stellplatz.Nummer,
                    Tag = stellplatz,
                    BackColor = GetStatusColor(stellplatz.Status),
                    ForeColor = Color.Black,
                    Location = new Point((int)stellplatz.PosX, (int)stellplatz.PosY)
                };

                button.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 50);
                button.FlatAppearance.BorderSize = 1;
                button.Click += StellplatzButton_Click;

                _planPanel.Controls.Add(button);
                _stellplatzButtons[stellplatz.Id] = button;
            }
        }

        private void StellplatzButton_Click(object? sender, EventArgs e)
        {
            if (sender is not Button button || button.Tag is not Stellplatz stellplatz)
            {
                return;
            }

            if (_selectedStellplaetze.Contains(stellplatz))
            {
                _selectedStellplaetze.Remove(stellplatz);
            }
            else
            {
                _selectedStellplaetze.Add(stellplatz);
            }

            UpdateSelectionState();
        }

        private void UpdateSelectionState()
        {
            foreach (var stellplatz in _stellplaetze)
            {
                if (!_stellplatzButtons.TryGetValue(stellplatz.Id, out var button))
                {
                    continue;
                }

                var isSelected = _selectedStellplaetze.Contains(stellplatz);
                button.BackColor = isSelected
                    ? Color.MediumPurple
                    : GetStatusColor(stellplatz.Status);
                button.ForeColor = isSelected ? Color.White : Color.Black;
                button.FlatAppearance.BorderColor = isSelected
                    ? Color.FromArgb(56, 32, 99)
                    : Color.FromArgb(50, 50, 50);
                button.FlatAppearance.BorderSize = isSelected ? 2 : 1;
            }

            _createBookingButton.Enabled = _selectedStellplaetze.Count > 0;
            _selectionInfoLabel.Text = _selectedStellplaetze.Count == 0
                ? "Keine Stellplätze ausgewählt"
                : $"{_selectedStellplaetze.Count} Stellplatz/Stellplätze ausgewählt";
        }

        private void CreateBookingButton_Click(object? sender, EventArgs e)
        {
            if (_selectedStellplaetze.Count == 0)
            {
                return;
            }

            var buchung = new Buchung
            {
                Id = _createdBuchungen.Count + 1,
                KundenId = Guid.Empty,
                Startdatum = DateTime.Today,
                Enddatum = DateTime.Today.AddDays(1),
                StellplatzIds = _selectedStellplaetze.ConvertAll(x => x.Id)
            };

            _createdBuchungen.Add(buchung);

            var platznummern = string.Join(", ", _selectedStellplaetze.ConvertAll(x => x.Nummer));
            MessageBox.Show(
                $"Buchung #{buchung.Id} erstellt.\nStellplätze: {platznummern}",
                "Buchung erstellt",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            _selectedStellplaetze.Clear();
            UpdateSelectionState();
        }

        private static Color GetStatusColor(StellplatzStatus status)
        {
            return status switch
            {
                StellplatzStatus.Frei => Color.LimeGreen,
                StellplatzStatus.Belegt => Color.IndianRed,
                StellplatzStatus.Reserviert => Color.Gold,
                StellplatzStatus.Dauercamper => Color.DeepSkyBlue,
                _ => SystemColors.Control
            };
        }

        // Beispiel-Daten mit PosX / PosY für die Platzierung auf dem Panel.
        // Ersetze diese Liste später durch echte Daten aus DB/API.
        private static List<Stellplatz> CreateDemoStellplaetze()
        {
            return new List<Stellplatz>
            {
                new() { Id = 101, Nummer = "101", PosX = 420, PosY = 185, Status = StellplatzStatus.Frei },
                new() { Id = 102, Nummer = "102", PosX = 465, PosY = 195, Status = StellplatzStatus.Belegt },
                new() { Id = 103, Nummer = "103", PosX = 510, PosY = 205, Status = StellplatzStatus.Reserviert },
                new() { Id = 104, Nummer = "104", PosX = 555, PosY = 215, Status = StellplatzStatus.Dauercamper },
                new() { Id = 105, Nummer = "105", PosX = 600, PosY = 225, Status = StellplatzStatus.Frei },
                new() { Id = 106, Nummer = "106", PosX = 645, PosY = 235, Status = StellplatzStatus.Belegt },
                new() { Id = 107, Nummer = "107", PosX = 690, PosY = 245, Status = StellplatzStatus.Reserviert },
                new() { Id = 108, Nummer = "108", PosX = 735, PosY = 255, Status = StellplatzStatus.Dauercamper }
            };
        }

    }
}
