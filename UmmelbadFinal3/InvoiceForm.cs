using System;
using System.Windows.Forms;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using System.IO;

namespace UmmelbadFinal3
{
    public class InvoiceForm : Form
    {
        TextBox txtCustomer = new TextBox(){Text="Herr Mark Nehlsen"};

        TextBox txtDaysStay = new TextBox(){Text="4"};
        TextBox txtPriceStay = new TextBox(){Text="35"};

        TextBox txtDaysPower = new TextBox(){Text="4"};
        TextBox txtPricePower = new TextBox(){Text="3"};

        TextBox txtDaysWaste = new TextBox(){Text="1"};
        TextBox txtPriceWaste = new TextBox(){Text="20"};

        DateTimePicker dtFrom = new DateTimePicker();
        DateTimePicker dtTo = new DateTimePicker();

        Button btn = new Button(){Text="Rechnung erstellen", Dock=DockStyle.Bottom};

        public InvoiceForm()
        {
            Text = "Rechnung erstellen";
            Width = 420;
            Height = 420;

            var layout = new TableLayoutPanel(){Dock=DockStyle.Fill, ColumnCount=2};

            layout.Controls.Add(new Label(){Text="Kunde"},0,0);
            layout.Controls.Add(txtCustomer,1,0);

            layout.Controls.Add(new Label(){Text="Von"},0,1);
            layout.Controls.Add(dtFrom,1,1);

            layout.Controls.Add(new Label(){Text="Bis"},0,2);
            layout.Controls.Add(dtTo,1,2);

            layout.Controls.Add(new Label(){Text="--- Stellplatz ---"},0,3);
            layout.Controls.Add(new Label(),1,3);

            layout.Controls.Add(new Label(){Text="Tage"},0,4);
            layout.Controls.Add(txtDaysStay,1,4);

            layout.Controls.Add(new Label(){Text="Preis / Tag"},0,5);
            layout.Controls.Add(txtPriceStay,1,5);

            layout.Controls.Add(new Label(){Text="--- Strom ---"},0,6);
            layout.Controls.Add(new Label(),1,6);

            layout.Controls.Add(new Label(){Text="Tage"},0,7);
            layout.Controls.Add(txtDaysPower,1,7);

            layout.Controls.Add(new Label(){Text="Preis / Tag"},0,8);
            layout.Controls.Add(txtPricePower,1,8);

            layout.Controls.Add(new Label(){Text="--- Müll ---"},0,9);
            layout.Controls.Add(new Label(),1,9);

            layout.Controls.Add(new Label(){Text="Tage"},0,10);
            layout.Controls.Add(txtDaysWaste,1,10);

            layout.Controls.Add(new Label(){Text="Preis / Tag"},0,11);
            layout.Controls.Add(txtPriceWaste,1,11);

            Controls.Add(layout);
            Controls.Add(btn);

            btn.Click += (s,e)=>CreatePdf();
        }

        void CreatePdf()
        {
            double daysStay = double.Parse(txtDaysStay.Text);
            double priceStay = double.Parse(txtPriceStay.Text);

            double daysPower = double.Parse(txtDaysPower.Text);
            double pricePower = double.Parse(txtPricePower.Text);

            double daysWaste = double.Parse(txtDaysWaste.Text);
            double priceWaste = double.Parse(txtPriceWaste.Text);

            double net7 = daysStay * priceStay;
            double vat7 = net7 * 0.07;

            double net19_power = daysPower * pricePower;
            double net19_waste = daysWaste * priceWaste;
            double net19 = net19_power + net19_waste;
            double vat19 = net19 * 0.19;

            double total = net7 + vat7 + net19 + vat19;

            string number = "WC-" + DateTime.Now.ToString("yyyyMMdd-HHmm");

            var doc = new PdfDocument();
            var page = doc.AddPage();
            var gfx = XGraphics.FromPdfPage(page);

            var font = new XFont("Arial", 10);
            var bold = new XFont("Arial", 10, XFontStyle.Bold);
            var title = new XFont("Arial", 16, XFontStyle.Bold);

            int left = 40;
            int right = 360;

            // LOGO
            try
            {
                if (File.Exists("logo.png"))
                {
                    var img = XImage.FromFile("logo.png");
                    gfx.DrawImage(img, 400, 30, 140, 60);
                }
            }
            catch { }

            // HEADER LINKS
            int yL = 40;
            gfx.DrawString("Waldcampingplatz Ummelbad", bold, XBrushes.Black, left, yL); yL += 15;
            gfx.DrawString("Inhaberin: Silvana Zampich", font, XBrushes.Black, left, yL); yL += 15;
            gfx.DrawString("Ummelweg 100", font, XBrushes.Black, left, yL); yL += 15;
            gfx.DrawString("27412 Hepstedt", font, XBrushes.Black, left, yL); yL += 15;

            // HEADER RECHTS
            int yR = 110;
            gfx.DrawString("RECHNUNG", title, XBrushes.Black, right, yR); yR += 25;
            gfx.DrawString("Nr: " + number, font, XBrushes.Black, right, yR); yR += 15;
            gfx.DrawString("Datum: " + DateTime.Now.ToString("dd.MM.yyyy"), font, XBrushes.Black, right, yR);

            // KUNDE
            int y = 150;
            gfx.DrawString("Kunde:", bold, XBrushes.Black, left, y); y += 15;
            gfx.DrawString(txtCustomer.Text, font, XBrushes.Black, left, y);

            y += 30;

            // TABELLE
            int col1 = left;
            int col2 = left + 40;
            int col3 = left + 260;
            int col4 = left + 340;
            int col5 = left + 440;

            gfx.DrawString("Pos.", bold, XBrushes.Black, col1, y);
            gfx.DrawString("Leistung", bold, XBrushes.Black, col2, y);
            gfx.DrawString("Menge", bold, XBrushes.Black, col3, y);
            gfx.DrawString("EP", bold, XBrushes.Black, col4, y);
            gfx.DrawString("Gesamt", bold, XBrushes.Black, col5, y);

            y += 10;
            gfx.DrawLine(XPens.Black, left, y, 560, y);
            y += 15;

            // POS 1
            gfx.DrawString("1", font, XBrushes.Black, col1, y);
            gfx.DrawString("Stellplatz (7%)", font, XBrushes.Black, col2, y);
            gfx.DrawString(daysStay + " Tage", font, XBrushes.Black, col3, y);
            gfx.DrawString(priceStay.ToString("F2") + " €", font, XBrushes.Black, col4, y);
            gfx.DrawString(net7.ToString("F2") + " €", font, XBrushes.Black, col5, y);

            y += 20;

            // POS 2
            gfx.DrawString("2", font, XBrushes.Black, col1, y);
            gfx.DrawString("Strom (19%)", font, XBrushes.Black, col2, y);
            gfx.DrawString(daysPower + " Tage", font, XBrushes.Black, col3, y);
            gfx.DrawString(pricePower.ToString("F2") + " €", font, XBrushes.Black, col4, y);
            gfx.DrawString(net19_power.ToString("F2") + " €", font, XBrushes.Black, col5, y);

            y += 20;

            // POS 3
            gfx.DrawString("3", font, XBrushes.Black, col1, y);
            gfx.DrawString("Müll (19%)", font, XBrushes.Black, col2, y);
            gfx.DrawString(daysWaste + " Tage", font, XBrushes.Black, col3, y);
            gfx.DrawString(priceWaste.ToString("F2") + " €", font, XBrushes.Black, col4, y);
            gfx.DrawString(net19_waste.ToString("F2") + " €", font, XBrushes.Black, col5, y);

            y += 30;

            // SUMMEN
            gfx.DrawString("Netto 7%: " + net7.ToString("F2") + " €", font, XBrushes.Black, right, y); y += 15;
            gfx.DrawString("MwSt 7%: " + vat7.ToString("F2") + " €", font, XBrushes.Black, right, y); y += 15;
            gfx.DrawString("Netto 19%: " + net19.ToString("F2") + " €", font, XBrushes.Black, right, y); y += 15;
            gfx.DrawString("MwSt 19%: " + vat19.ToString("F2") + " €", font, XBrushes.Black, right, y); y += 15;

            gfx.DrawString("Gesamt: " + total.ToString("F2") + " €", bold, XBrushes.Black, right, y);

            doc.Save("Rechnung_FINAL.pdf");
        }
    }
}
