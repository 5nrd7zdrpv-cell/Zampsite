using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using UmmelbadFinal3.Models;
using UmmelbadFinal3.Services;

namespace UmmelbadFinal3
{
    public class InvoiceForm : Form
    {
        private readonly BindingList<InvoiceItem> _items = new();
        private readonly CultureInfo _culture = new("de-DE");

        private readonly InvoiceService _invoiceService;
        private readonly InvoiceNumberService _invoiceNumberService;
        private readonly PdfService _pdfService;

        private readonly TextBox _txtCustomerName = new();
        private readonly TextBox _txtCustomerAddress = new();
        private readonly TextBox _txtCustomerCity = new();
        private readonly TextBox _txtCustomerEmail = new();
        private readonly TextBox _txtCustomerPhone = new();

        private readonly TextBox _txtInvoiceNumber = new();
        private readonly DateTimePicker _dtpInvoiceDate = new();
        private readonly DateTimePicker _dtpServiceDate = new();

        private readonly DataGridView _dgvPositions = new();
        private readonly Label _lblNet = new();
        private readonly Label _lblTax = new();
        private readonly Label _lblGross = new();

        public InvoiceForm()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _invoiceService = new InvoiceService(baseDir);
            _invoiceNumberService = new InvoiceNumberService(baseDir);
            _pdfService = new PdfService(baseDir);

            InitializeComponent();
            ConfigureDataGridView();
            _dgvPositions.DataSource = _items;
            _items.ListChanged += (_, _) => UpdateTotals();

            ResetInvoice();
        }

        private void InitializeComponent()
        {
            Text = "Rechnungsmodul";
            Width = 1200;
            Height = 760;
            StartPosition = FormStartPosition.CenterScreen;

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            root.Controls.Add(CreateCustomerPanel(), 0, 0);
            root.Controls.Add(CreateInvoiceDataPanel(), 0, 1);
            root.Controls.Add(_dgvPositions, 0, 2);
            root.Controls.Add(CreateButtonsPanel(), 0, 3);
            root.Controls.Add(CreateTotalsPanel(), 0, 4);

            Controls.Add(root);
        }

        private Control CreateCustomerPanel()
        {
            var panel = new GroupBox { Text = "Kundendaten", Dock = DockStyle.Fill, Height = 140 };
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
            for (int i = 0; i < 4; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

            AddLabeledField(grid, "Name", _txtCustomerName, 0, 0);
            AddLabeledField(grid, "Adresse", _txtCustomerAddress, 1, 0);
            AddLabeledField(grid, "PLZ/Ort", _txtCustomerCity, 2, 0);
            AddLabeledField(grid, "E-Mail", _txtCustomerEmail, 0, 1);
            AddLabeledField(grid, "Telefon", _txtCustomerPhone, 1, 1);

            panel.Controls.Add(grid);
            return panel;
        }

        private Control CreateInvoiceDataPanel()
        {
            var panel = new GroupBox { Text = "Rechnungsdaten", Dock = DockStyle.Fill, Height = 95 };
            var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6 };
            for (int i = 0; i < 6; i++) grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6f));

            _txtInvoiceNumber.ReadOnly = true;
            _dtpInvoiceDate.Format = DateTimePickerFormat.Short;
            _dtpServiceDate.Format = DateTimePickerFormat.Short;
            _dtpServiceDate.ShowCheckBox = true;

            AddLabeledField(grid, "Rechnungsnummer", _txtInvoiceNumber, 0, 0);
            AddLabeledField(grid, "Rechnungsdatum", _dtpInvoiceDate, 2, 0);
            AddLabeledField(grid, "Leistungsdatum", _dtpServiceDate, 4, 0);

            panel.Controls.Add(grid);
            return panel;
        }

        private Control CreateButtonsPanel()
        {
            var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true };

            var btnAdd = new Button { Text = "+ Position hinzufügen", AutoSize = true };
            var btnDelete = new Button { Text = "Position löschen", AutoSize = true };
            var btnSave = new Button { Text = "Rechnung speichern", AutoSize = true };
            var btnLoad = new Button { Text = "Rechnung laden", AutoSize = true };
            var btnPdf = new Button { Text = "PDF erstellen", AutoSize = true };

            btnAdd.Click += (_, _) => AddPosition();
            btnDelete.Click += (_, _) => DeleteSelectedPosition();
            btnSave.Click += (_, _) => SaveInvoice();
            btnLoad.Click += (_, _) => LoadInvoice();
            btnPdf.Click += (_, _) => ExportPdf();

            panel.Controls.AddRange(new Control[] { btnAdd, btnDelete, btnSave, btnLoad, btnPdf });
            return panel;
        }

        private Control CreateTotalsPanel()
        {
            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

            var values = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, Dock = DockStyle.Right };
            _lblGross.Font = new Font(Font, FontStyle.Bold);
            values.Controls.Add(_lblNet);
            values.Controls.Add(_lblTax);
            values.Controls.Add(_lblGross);

            panel.Controls.Add(new Label { Text = "Summen (Netto / Steuer / Brutto)", AutoSize = true, Dock = DockStyle.Left }, 0, 0);
            panel.Controls.Add(values, 1, 0);
            return panel;
        }

        private static void AddLabeledField(TableLayoutPanel grid, string text, Control control, int col, int row)
        {
            while (grid.RowStyles.Count <= row)
            {
                grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            var label = new Label { Text = text, AutoSize = true, Margin = new Padding(3, 8, 3, 0) };
            control.Dock = DockStyle.Top;
            grid.Controls.Add(label, col, row);
            grid.Controls.Add(control, col + 1, row);
            grid.SetColumnSpan(control, 1);
        }

        private void ConfigureDataGridView()
        {
            _dgvPositions.Dock = DockStyle.Fill;
            _dgvPositions.AutoGenerateColumns = false;
            _dgvPositions.AllowUserToAddRows = false;
            _dgvPositions.RowHeadersVisible = false;
            _dgvPositions.CellValueChanged += (_, e) => { if (e.RowIndex >= 0) UpdateTotals(); };
            _dgvPositions.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (_dgvPositions.IsCurrentCellDirty)
                {
                    _dgvPositions.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };
            _dgvPositions.CellValidating += DgvPositions_CellValidating;

            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InvoiceItem.Title), HeaderText = "Titel", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InvoiceItem.Quantity), HeaderText = "Menge", Width = 90 });
            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InvoiceItem.UnitPrice), HeaderText = "EP", Width = 90 });
            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InvoiceItem.TaxRate), HeaderText = "UST %", Width = 80 });
            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InvoiceItem.NetTotal), HeaderText = "Netto", Width = 100, ReadOnly = true });
            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InvoiceItem.TaxAmount), HeaderText = "Steuer", Width = 100, ReadOnly = true });
            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InvoiceItem.GrossTotal), HeaderText = "Brutto", Width = 100, ReadOnly = true });
        }

        private void DgvPositions_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
        {
            var prop = _dgvPositions.Columns[e.ColumnIndex].DataPropertyName;
            var input = Convert.ToString(e.FormattedValue) ?? string.Empty;
            if (prop == nameof(InvoiceItem.Quantity) && (!decimal.TryParse(input, NumberStyles.Number, _culture, out var q) || q <= 0))
            {
                e.Cancel = true;
                ShowValidation("Menge muss > 0 sein.");
            }

            if (prop == nameof(InvoiceItem.UnitPrice) && (!decimal.TryParse(input, NumberStyles.Number, _culture, out var p) || p < 0))
            {
                e.Cancel = true;
                ShowValidation("Preis muss >= 0 sein.");
            }
        }

        private void ShowValidation(string message)
        {
            MessageBox.Show(message, "Validierung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void AddPosition() => _items.Add(new InvoiceItem { Quantity = 1, TaxRate = 19 });

        private void DeleteSelectedPosition()
        {
            if (_dgvPositions.CurrentRow?.DataBoundItem is InvoiceItem item)
            {
                _items.Remove(item);
            }
        }

        private void ResetInvoice()
        {
            _txtInvoiceNumber.Text = _invoiceNumberService.GetNextInvoiceNumber(DateTime.Today);
            _dtpInvoiceDate.Value = DateTime.Today;
            _dtpServiceDate.Checked = false;
            _items.Clear();
            AddPosition();
            UpdateTotals();
        }

        private bool ValidateInvoice()
        {
            if (string.IsNullOrWhiteSpace(_txtCustomerName.Text))
            {
                ShowValidation("Kunde darf nicht leer sein.");
                return false;
            }

            if (_items.Count == 0)
            {
                ShowValidation("Mindestens eine Position ist erforderlich.");
                return false;
            }

            if (_items.Any(i => i.Quantity <= 0 || i.UnitPrice < 0))
            {
                ShowValidation("Alle Positionen müssen Menge > 0 und Preis >= 0 haben.");
                return false;
            }

            return true;
        }

        private Invoice BuildInvoiceFromUi()
        {
            return new Invoice
            {
                InvoiceNumber = _txtInvoiceNumber.Text,
                InvoiceDate = _dtpInvoiceDate.Value.Date,
                ServiceDate = _dtpServiceDate.Checked ? _dtpServiceDate.Value.Date : null,
                CustomerName = _txtCustomerName.Text.Trim(),
                CustomerAddress = _txtCustomerAddress.Text.Trim(),
                CustomerCity = _txtCustomerCity.Text.Trim(),
                CustomerEmail = _txtCustomerEmail.Text.Trim(),
                CustomerPhone = _txtCustomerPhone.Text.Trim(),
                Items = _items.ToList()
            };
        }

        private void FillUiFromInvoice(Invoice invoice)
        {
            _txtInvoiceNumber.Text = invoice.InvoiceNumber;
            _dtpInvoiceDate.Value = invoice.InvoiceDate;
            _dtpServiceDate.Checked = invoice.ServiceDate.HasValue;
            if (invoice.ServiceDate.HasValue)
            {
                _dtpServiceDate.Value = invoice.ServiceDate.Value;
            }

            _txtCustomerName.Text = invoice.CustomerName;
            _txtCustomerAddress.Text = invoice.CustomerAddress;
            _txtCustomerCity.Text = invoice.CustomerCity;
            _txtCustomerEmail.Text = invoice.CustomerEmail;
            _txtCustomerPhone.Text = invoice.CustomerPhone;

            _items.Clear();
            foreach (var item in invoice.Items)
            {
                _items.Add(item);
            }

            UpdateTotals();
        }

        private void SaveInvoice()
        {
            if (!ValidateInvoice()) return;
            var invoice = BuildInvoiceFromUi();
            var path = _invoiceService.Save(invoice);
            MessageBox.Show($"Gespeichert: {path}", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ResetInvoice();
        }

        private void LoadInvoice()
        {
            using var dialog = new OpenFileDialog
            {
                InitialDirectory = _invoiceService.InvoiceDirectory,
                Filter = "JSON Dateien (*.json)|*.json"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var invoice = _invoiceService.Load(dialog.FileName);
                if (invoice != null)
                {
                    FillUiFromInvoice(invoice);
                }
            }
        }

        private void ExportPdf()
        {
            if (!ValidateInvoice()) return;
            var path = _pdfService.Export(BuildInvoiceFromUi());
            MessageBox.Show($"PDF erstellt: {path}", "PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void UpdateTotals()
        {
            _lblNet.Text = $"Netto: {_items.Sum(x => x.NetTotal).ToString("C2", _culture)}";
            _lblTax.Text = $"Steuer: {_items.Sum(x => x.TaxAmount).ToString("C2", _culture)}";
            _lblGross.Text = $"Brutto: {_items.Sum(x => x.GrossTotal).ToString("C2", _culture)}";
            _dgvPositions.Refresh();
        }
    }
}
