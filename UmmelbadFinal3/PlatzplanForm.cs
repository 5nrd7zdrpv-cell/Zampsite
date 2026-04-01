using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
                BackgroundImageLayout = ImageLayout.Stretch,
                BorderStyle = BorderStyle.FixedSingle
            };

            _planPanel.Resize += (_, _) => RepositionStellplatzButtons();

            LoadPlanBackgroundImage();

            Controls.Add(_planPanel);
            Controls.Add(topBar);

            _stellplaetze = CreateDemoStellplaetze();
            RenderStellplatzButtons();
        }

        private void LoadPlanBackgroundImage()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectDir = AppContext.BaseDirectory;

            var candidatePaths = new[]
            {
                Path.Combine(baseDir, "platzplan.png"),
                Path.Combine(baseDir, "platzplan.jpg"),
                Path.Combine(baseDir, "plan.png"),
                Path.Combine(projectDir, "platzplan.png"),
                Path.Combine(projectDir, "platzplan.jpg"),
                Path.Combine(projectDir, "plan.png")
            };

            foreach (var imagePath in candidatePaths)
            {
                if (!File.Exists(imagePath))
                {
                    continue;
                }

                _planPanel.BackgroundImage = Image.FromFile(imagePath);
                return;
            }
        }

        private void RenderStellplatzButtons()
        {
            _planPanel.Controls.Clear();
            _stellplatzButtons.Clear();

            foreach (var stellplatz in _stellplaetze)
            {
                var button = new Button
                {
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                    Text = stellplatz.Nummer,
                    Tag = stellplatz,
                    BackColor = GetStatusColor(stellplatz.Status),
                    ForeColor = Color.Black
                };

                button.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 50);
                button.FlatAppearance.BorderSize = 1;
                button.Click += StellplatzButton_Click;

                _planPanel.Controls.Add(button);
                _stellplatzButtons[stellplatz.Id] = button;
            }

            RepositionStellplatzButtons();
        }

        private void RepositionStellplatzButtons()
        {
            foreach (var stellplatz in _stellplaetze)
            {
                if (!_stellplatzButtons.TryGetValue(stellplatz.Id, out var button))
                {
                    continue;
                }

                var x = (float)stellplatz.PosX * _planPanel.ClientSize.Width;
                var y = (float)stellplatz.PosY * _planPanel.ClientSize.Height;

                var buttonWidth = Math.Max(34, _planPanel.ClientSize.Width / 28);
                var buttonHeight = Math.Max(22, _planPanel.ClientSize.Height / 36);

                button.Size = new Size(buttonWidth, buttonHeight);
                button.Location = new Point(
                    Math.Max(0, (int)x - button.Width / 2),
                    Math.Max(0, (int)y - button.Height / 2));
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

        // PosX/PosY sind absichtlich normiert (0..1), damit Buttons bei Resize exakt auf dem Plan bleiben.
        private static List<Stellplatz> CreateDemoStellplaetze()
        {
            return new List<Stellplatz>
            {
                // Oberer linker Bereich
                new() { Id = 101, Nummer = "101", PosX = 0.385m, PosY = 0.305m, Status = StellplatzStatus.Frei },
                new() { Id = 102, Nummer = "102", PosX = 0.410m, PosY = 0.323m, Status = StellplatzStatus.Belegt },
                new() { Id = 103, Nummer = "103", PosX = 0.435m, PosY = 0.341m, Status = StellplatzStatus.Reserviert },
                new() { Id = 104, Nummer = "104", PosX = 0.462m, PosY = 0.360m, Status = StellplatzStatus.Dauercamper },

                // Zentraler Block
                new() { Id = 120, Nummer = "120", PosX = 0.505m, PosY = 0.365m, Status = StellplatzStatus.Frei },
                new() { Id = 121, Nummer = "121", PosX = 0.530m, PosY = 0.385m, Status = StellplatzStatus.Belegt },
                new() { Id = 122, Nummer = "122", PosX = 0.555m, PosY = 0.405m, Status = StellplatzStatus.Reserviert },
                new() { Id = 123, Nummer = "123", PosX = 0.578m, PosY = 0.425m, Status = StellplatzStatus.Dauercamper },

                // Unterer linker Block
                new() { Id = 65, Nummer = "65", PosX = 0.360m, PosY = 0.655m, Status = StellplatzStatus.Frei },
                new() { Id = 66, Nummer = "66", PosX = 0.385m, PosY = 0.675m, Status = StellplatzStatus.Belegt },
                new() { Id = 67, Nummer = "67", PosX = 0.410m, PosY = 0.695m, Status = StellplatzStatus.Reserviert },
                new() { Id = 68, Nummer = "68", PosX = 0.435m, PosY = 0.715m, Status = StellplatzStatus.Dauercamper },

                // Rechter Bereich
                new() { Id = 154, Nummer = "154", PosX = 0.685m, PosY = 0.548m, Status = StellplatzStatus.Frei },
                new() { Id = 155, Nummer = "155", PosX = 0.710m, PosY = 0.568m, Status = StellplatzStatus.Belegt },
                new() { Id = 156, Nummer = "156", PosX = 0.735m, PosY = 0.588m, Status = StellplatzStatus.Reserviert },
                new() { Id = 157, Nummer = "157", PosX = 0.760m, PosY = 0.608m, Status = StellplatzStatus.Dauercamper }
            };
        }
    }
}
