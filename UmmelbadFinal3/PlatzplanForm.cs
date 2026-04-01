using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace UmmelbadFinal3
{
    public sealed class PlatzplanForm : Form
    {
        private readonly Panel _planPanel;
        private readonly List<StellplatzUiModel> _stellplaetze;

        public PlatzplanForm()
        {
            Text = "Platzplan - Stellplatzübersicht";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1200, 820);
            MinimumSize = new Size(900, 650);

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

            _stellplaetze = CreateDemoStellplaetze();
            RenderStellplatzButtons();
        }

        private void RenderStellplatzButtons()
        {
            _planPanel.Controls.Clear();

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
                    Location = new Point(stellplatz.PosX, stellplatz.PosY)
                };

                button.FlatAppearance.BorderColor = Color.FromArgb(50, 50, 50);
                button.FlatAppearance.BorderSize = 1;
                button.Click += StellplatzButton_Click;

                _planPanel.Controls.Add(button);
            }
        }

        private void StellplatzButton_Click(object? sender, EventArgs e)
        {
            if (sender is not Button button || button.Tag is not StellplatzUiModel stellplatz)
            {
                return;
            }

            MessageBox.Show(
                $"Stellplatz {stellplatz.Nummer} ausgewählt.\nStatus: {stellplatz.Status}",
                "Stellplatz auswählen",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static Color GetStatusColor(StellplatzStatusUi status)
        {
            return status switch
            {
                StellplatzStatusUi.Frei => Color.LimeGreen,
                StellplatzStatusUi.Belegt => Color.IndianRed,
                StellplatzStatusUi.Reserviert => Color.Gold,
                StellplatzStatusUi.Dauercamper => Color.DeepSkyBlue,
                _ => SystemColors.Control
            };
        }

        // Beispiel-Daten mit PosX / PosY für die Platzierung auf dem Panel.
        // Ersetze diese Liste später durch echte Daten aus DB/API.
        private static List<StellplatzUiModel> CreateDemoStellplaetze()
        {
            return new List<StellplatzUiModel>
            {
                new() { Nummer = "101", PosX = 420, PosY = 185, Status = StellplatzStatusUi.Frei },
                new() { Nummer = "102", PosX = 465, PosY = 195, Status = StellplatzStatusUi.Belegt },
                new() { Nummer = "103", PosX = 510, PosY = 205, Status = StellplatzStatusUi.Reserviert },
                new() { Nummer = "104", PosX = 555, PosY = 215, Status = StellplatzStatusUi.Dauercamper },
                new() { Nummer = "105", PosX = 600, PosY = 225, Status = StellplatzStatusUi.Frei },
                new() { Nummer = "106", PosX = 645, PosY = 235, Status = StellplatzStatusUi.Belegt },
                new() { Nummer = "107", PosX = 690, PosY = 245, Status = StellplatzStatusUi.Reserviert },
                new() { Nummer = "108", PosX = 735, PosY = 255, Status = StellplatzStatusUi.Dauercamper }
            };
        }

        private sealed class StellplatzUiModel
        {
            public string Nummer { get; set; } = string.Empty;
            public int PosX { get; set; }
            public int PosY { get; set; }
            public StellplatzStatusUi Status { get; set; }
        }

        private enum StellplatzStatusUi
        {
            Frei,
            Belegt,
            Reserviert,
            Dauercamper
        }
    }
}
