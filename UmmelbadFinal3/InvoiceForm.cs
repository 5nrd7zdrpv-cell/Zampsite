using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
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
        private readonly CustomerService _customerService;
        private readonly CampingService _campingService;
        private List<Stellplatz> _stellplaetze = new();
        private readonly HashSet<int> _selectedBookingIds = new();
        private readonly HashSet<int> _selectedCafeSaleIds = new();

        private readonly ComboBox _cmbCustomers = new();
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

        private List<Customer> _customers = new();

        public InvoiceForm()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _invoiceService = new InvoiceService(baseDir);
            _invoiceNumberService = new InvoiceNumberService(baseDir);
            _pdfService = new PdfService(baseDir);
            _customerService = new CustomerService(baseDir);
            _campingService = new CampingService(baseDir);

            InitializeComponent();
            ConfigureDataGridView();
            _dgvPositions.DataSource = _items;
            _items.ListChanged += (_, _) => UpdateTotals();

            LoadCustomers();
            _stellplaetze = _campingService.LoadStellplaetze();
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
            var panel = new GroupBox { Text = "Kundendaten", Dock = DockStyle.Fill, AutoSize = true };
            var grid = new TableLayoutPanel
            {
                ColumnCount = 4,
                RowCount = 4,
                Dock = DockStyle.Top,
                AutoSize = true,
                Padding = new Padding(4)
            };

            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            for (var i = 0; i < 4; i++)
            {
                grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            }

            _cmbCustomers.Name = "cmbCustomers";
            _cmbCustomers.Dock = DockStyle.Fill;
            _cmbCustomers.DropDownStyle = ComboBoxStyle.DropDown;
            _cmbCustomers.DisplayMember = nameof(Customer.Name);
            _cmbCustomers.SelectedIndexChanged += (_, _) => FillCustomerFieldsFromSelection();

            ConfigureCustomerTextBox(_txtCustomerName);
            ConfigureCustomerTextBox(_txtCustomerAddress);
            ConfigureCustomerTextBox(_txtCustomerCity);
            ConfigureCustomerTextBox(_txtCustomerEmail);
            ConfigureCustomerTextBox(_txtCustomerPhone);

            AddCustomerField(grid, "Kunde", _cmbCustomers, 0, 0);
            AddCustomerField(grid, "Name", _txtCustomerName, 0, 1);
            AddCustomerField(grid, "Adresse", _txtCustomerAddress, 2, 1);
            AddCustomerField(grid, "PLZ/Ort", _txtCustomerCity, 0, 2);
            AddCustomerField(grid, "E-Mail", _txtCustomerEmail, 2, 2);
            AddCustomerField(grid, "Telefon", _txtCustomerPhone, 0, 3);

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
            var btnList = new Button { Text = "Rechnungsübersicht", AutoSize = true };
            var btnAddBooking = new Button { Text = "Stellplatz-Buchung hinzufügen", AutoSize = true };
            var btnAddMultiPitch = new Button { Text = "Mehrere Stellplätze hinzufügen", AutoSize = true };
            var btnAddCafe = new Button { Text = "Café-Produkte hinzufügen", AutoSize = true };

            btnAdd.Click += (_, _) => AddPosition();
            btnDelete.Click += (_, _) => DeleteSelectedPosition();
            btnSave.Click += (_, _) => SaveInvoice();
            btnLoad.Click += (_, _) => LoadInvoice();
            btnPdf.Click += (_, _) => ExportPdf();
            btnList.Click += (_, _) => OpenInvoiceList();
            btnAddBooking.Click += (_, _) => AddCampingBooking();
            btnAddMultiPitch.Click += (_, _) => AddMultiPitchBooking();
            btnAddCafe.Click += (_, _) => AddCafeProducts();

            panel.Controls.AddRange(new Control[] { btnAdd, btnDelete, btnAddBooking, btnAddMultiPitch, btnAddCafe, btnSave, btnLoad, btnPdf, btnList });
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

        private static void AddCustomerField(TableLayoutPanel grid, string text, Control control, int labelColumn, int row)
        {
            var label = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(4)
            };

            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(4);

            grid.Controls.Add(label, labelColumn, row);
            grid.Controls.Add(control, labelColumn + 1, row);
        }

        private static void ConfigureCustomerTextBox(TextBox textBox)
        {
            textBox.Dock = DockStyle.Fill;
            textBox.Margin = new Padding(4);
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
            _dgvPositions.CellValueChanged += DgvPositions_CellValueChanged;
            _dgvPositions.CellEndEdit += DgvPositions_CellEndEdit;
            _dgvPositions.DataError += (_, e) =>
            {
                e.ThrowException = false;
                e.Cancel = false;
            };
            _dgvPositions.CurrentCellDirtyStateChanged += (_, _) =>
            {
                if (_dgvPositions.IsCurrentCellDirty)
                {
                    _dgvPositions.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            };

            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn { Name = nameof(InvoiceItem.Title), DataPropertyName = nameof(InvoiceItem.Title), HeaderText = "Titel", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn { Name = nameof(InvoiceItem.Quantity), DataPropertyName = nameof(InvoiceItem.Quantity), HeaderText = "Menge", Width = 90 });
            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn { Name = nameof(InvoiceItem.UnitPrice), DataPropertyName = nameof(InvoiceItem.UnitPrice), HeaderText = "EP", Width = 90 });
            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn { Name = nameof(InvoiceItem.TaxRate), DataPropertyName = nameof(InvoiceItem.TaxRate), HeaderText = "UST %", Width = 80 });
            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn { Name = nameof(InvoiceItem.NetTotal), DataPropertyName = nameof(InvoiceItem.NetTotal), HeaderText = "Netto", Width = 100, ReadOnly = true });
            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn { Name = nameof(InvoiceItem.TaxAmount), DataPropertyName = nameof(InvoiceItem.TaxAmount), HeaderText = "Steuer", Width = 100, ReadOnly = true });
            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn { Name = nameof(InvoiceItem.GrossTotal), DataPropertyName = nameof(InvoiceItem.GrossTotal), HeaderText = "Brutto", Width = 100, ReadOnly = true });
        }

        private void DgvPositions_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            if (_dgvPositions.Rows[e.RowIndex].DataBoundItem is not InvoiceItem item)
            {
                return;
            }

            var row = _dgvPositions.Rows[e.RowIndex];
            item.Quantity = SafeParse(row.Cells[nameof(InvoiceItem.Quantity)].Value);
            item.UnitPrice = SafeParse(row.Cells[nameof(InvoiceItem.UnitPrice)].Value);
            item.TaxRate = SafeParse(row.Cells[nameof(InvoiceItem.TaxRate)].Value);

            UpdateTotals();
        }

        private void DgvPositions_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
            {
                return;
            }

            var columnName = _dgvPositions.Columns[e.ColumnIndex].DataPropertyName;
            if (columnName != nameof(InvoiceItem.Quantity) &&
                columnName != nameof(InvoiceItem.UnitPrice) &&
                columnName != nameof(InvoiceItem.TaxRate))
            {
                return;
            }

            var cell = _dgvPositions.Rows[e.RowIndex].Cells[e.ColumnIndex];
            var parsed = SafeParse(cell.Value);
            cell.Value = parsed.ToString("N2", _culture);
        }

        private decimal SafeParse(object? value)
        {
            if (value == null)
            {
                return 0m;
            }

            var text = Convert.ToString(value)?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0m;
            }

            if (decimal.TryParse(text, NumberStyles.Any, _culture, out var result))
            {
                return result;
            }

            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }

            return 0m;
        }

        private void ShowValidation(string message)
        {
            MessageBox.Show(message, "Validierung", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void AddPosition() => _items.Add(new InvoiceItem { Quantity = 1, UnitPrice = 0, TaxRate = 19 });

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
            _cmbCustomers.SelectedIndex = -1;
            _cmbCustomers.Text = string.Empty;
            ClearCustomerFields();
            _items.Clear();
            _selectedBookingIds.Clear();
            _selectedCafeSaleIds.Clear();
            AddPosition();
            UpdateTotals();
        }

        private void LoadCustomers()
        {
            _customers = _customerService.LoadCustomers();
            _cmbCustomers.DataSource = null;
            _cmbCustomers.DataSource = _customers;
            _cmbCustomers.DisplayMember = nameof(Customer.Name);
            _cmbCustomers.SelectedIndex = -1;
        }

        private void FillCustomerFieldsFromSelection()
        {
            if (_cmbCustomers.SelectedItem is not Customer selected)
            {
                return;
            }

            _txtCustomerName.Text = selected.Name;
            _txtCustomerAddress.Text = selected.Address;
            _txtCustomerCity.Text = selected.City;
            _txtCustomerEmail.Text = selected.Email;
            _txtCustomerPhone.Text = selected.Phone;
        }

        private void ClearCustomerFields()
        {
            _txtCustomerName.Text = string.Empty;
            _txtCustomerAddress.Text = string.Empty;
            _txtCustomerCity.Text = string.Empty;
            _txtCustomerEmail.Text = string.Empty;
            _txtCustomerPhone.Text = string.Empty;
        }

        private void AddCampingBooking()
        {
            var offeneBuchungen = _campingService.LoadOffeneBuchungen()
                .Where(x => !_selectedBookingIds.Contains(x.Id))
                .OrderBy(x => x.Startdatum)
                .ToList();

            if (offeneBuchungen.Count == 0)
            {
                MessageBox.Show("Keine offenen Stellplatz-Buchungen gefunden.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var picker = new Form
            {
                Text = "Stellplatz-Buchung auswählen",
                Width = 820,
                Height = 420,
                StartPosition = FormStartPosition.CenterParent
            };

            var list = new ListBox { Dock = DockStyle.Fill };
            foreach (var buchung in offeneBuchungen)
            {
                var stellplatzNamen = ResolvePitchNumbers(buchung.StellplatzIds);
                list.Items.Add(new BookingPickerEntry(buchung, $"#{buchung.Id} | {buchung.Startdatum:dd.MM.yyyy} - {buchung.Enddatum:dd.MM.yyyy} | {stellplatzNamen} | {buchung.Gesamtpreis.ToString("C2", _culture)}"));
            }
            list.DisplayMember = nameof(BookingPickerEntry.DisplayText);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft };
            var btnOk = new Button { Text = "Übernehmen", AutoSize = true };
            var btnCancel = new Button { Text = "Abbrechen", AutoSize = true };
            buttonPanel.Controls.Add(btnOk);
            buttonPanel.Controls.Add(btnCancel);

            btnOk.Click += (_, _) => picker.DialogResult = DialogResult.OK;
            btnCancel.Click += (_, _) => picker.DialogResult = DialogResult.Cancel;

            picker.Controls.Add(list);
            picker.Controls.Add(buttonPanel);

            if (picker.ShowDialog(this) != DialogResult.OK || list.SelectedItem is not BookingPickerEntry selected)
            {
                return;
            }

            var selectedBooking = selected.Buchung;
            var pitchNumbers = ResolvePitchNumbers(selectedBooking.StellplatzIds);
            _items.Add(new InvoiceItem
            {
                Title = $"Stellplatz-Buchung {pitchNumbers} ({selectedBooking.Startdatum:dd.MM.yyyy}-{selectedBooking.Enddatum:dd.MM.yyyy})",
                Quantity = 1,
                UnitPrice = selectedBooking.Gesamtpreis,
                TaxRate = 7m
            });

            _selectedBookingIds.Add(selectedBooking.Id);
            UpdateTotals();
        }

        private void AddMultiPitchBooking()
        {
            if (_stellplaetze.Count == 0)
            {
                _stellplaetze = _campingService.LoadStellplaetze();
            }

            using var dialog = new MultiPitchDialog(_stellplaetze);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            if (dialog.SelectedPitchIds.Count == 0)
            {
                ShowValidation("Bitte mindestens einen Stellplatz auswählen.");
                return;
            }

            var pitchNumbers = ResolvePitchNumbers(dialog.SelectedPitchIds);
            _items.Add(new InvoiceItem
            {
                Title = $"Stellplätze {pitchNumbers} ({dialog.StartDate:dd.MM.yyyy}-{dialog.EndDate:dd.MM.yyyy})",
                Quantity = 1,
                UnitPrice = dialog.TotalPrice,
                TaxRate = dialog.TaxRate
            });

            UpdateTotals();
        }

        private void AddCafeProducts()
        {
            var offeneVerkaeufe = _campingService.LoadOffeneCafeVerkaeufe()
                .Where(x => !_selectedCafeSaleIds.Contains(x.Id))
                .OrderBy(x => x.Zeitpunkt)
                .ToList();

            if (offeneVerkaeufe.Count == 0)
            {
                MessageBox.Show("Keine offenen Café-Verkäufe gefunden.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var picker = new Form
            {
                Text = "Café-Verkauf auswählen",
                Width = 820,
                Height = 420,
                StartPosition = FormStartPosition.CenterParent
            };

            var list = new ListBox { Dock = DockStyle.Fill };
            foreach (var verkauf in offeneVerkaeufe)
            {
                list.Items.Add(new CafePickerEntry(verkauf, $"#{verkauf.Id} | {verkauf.Zeitpunkt:dd.MM.yyyy HH:mm} | {verkauf.Gesamt.ToString("C2", _culture)}"));
            }
            list.DisplayMember = nameof(CafePickerEntry.DisplayText);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 52, FlowDirection = FlowDirection.RightToLeft };
            var btnOk = new Button { Text = "Übernehmen", AutoSize = true };
            var btnCancel = new Button { Text = "Abbrechen", AutoSize = true };
            buttonPanel.Controls.Add(btnOk);
            buttonPanel.Controls.Add(btnCancel);

            btnOk.Click += (_, _) => picker.DialogResult = DialogResult.OK;
            btnCancel.Click += (_, _) => picker.DialogResult = DialogResult.Cancel;

            picker.Controls.Add(list);
            picker.Controls.Add(buttonPanel);

            if (picker.ShowDialog(this) != DialogResult.OK || list.SelectedItem is not CafePickerEntry selected)
            {
                return;
            }

            foreach (var position in selected.Verkauf.Positionen)
            {
                _items.Add(new InvoiceItem
                {
                    Title = $"Café: {position.Name} (Verkauf #{selected.Verkauf.Id})",
                    Quantity = position.Menge,
                    UnitPrice = position.Preis,
                    TaxRate = 19m
                });
            }

            _selectedCafeSaleIds.Add(selected.Verkauf.Id);
            UpdateTotals();
        }

        private string ResolvePitchNumbers(IEnumerable<int> pitchIds)
        {
            var idSet = pitchIds.ToHashSet();
            var numbers = _stellplaetze
                .Where(x => idSet.Contains(x.Id))
                .Select(x => x.Nummer)
                .OrderBy(x => x)
                .ToList();

            return numbers.Count == 0 ? "Unbekannt" : string.Join(", ", numbers);
        }

        private bool ValidateInvoice()
        {
            if (string.IsNullOrWhiteSpace(_txtCustomerName.Text))
            {
                ShowValidation("Name ist Pflichtfeld.");
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

        private Customer BuildCustomerFromUi()
        {
            return new Customer
            {
                Name = _txtCustomerName.Text.Trim(),
                Address = _txtCustomerAddress.Text.Trim(),
                City = _txtCustomerCity.Text.Trim(),
                Email = _txtCustomerEmail.Text.Trim(),
                Phone = _txtCustomerPhone.Text.Trim()
            };
        }

        private Invoice BuildInvoiceFromUi()
        {
            var customer = BuildCustomerFromUi();
            return new Invoice
            {
                InvoiceNumber = _txtInvoiceNumber.Text,
                InvoiceDate = _dtpInvoiceDate.Value.Date,
                ServiceDate = _dtpServiceDate.Checked ? _dtpServiceDate.Value.Date : null,
                Customer = customer,
                CustomerNameSnapshot = customer.Name,
                Items = _items.ToList(),
                IncludedBookingIds = _selectedBookingIds.ToList(),
                IncludedCafeSaleIds = _selectedCafeSaleIds.ToList()
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

            _txtCustomerName.Text = invoice.Customer.Name;
            _txtCustomerAddress.Text = invoice.Customer.Address;
            _txtCustomerCity.Text = invoice.Customer.City;
            _txtCustomerEmail.Text = invoice.Customer.Email;
            _txtCustomerPhone.Text = invoice.Customer.Phone;
            _cmbCustomers.SelectedIndex = _customers.FindIndex(c => c.Id == invoice.Customer.Id);

            _items.Clear();
            foreach (var item in invoice.Items)
            {
                _items.Add(item);
            }
            _selectedBookingIds.Clear();
            foreach (var bookingId in invoice.IncludedBookingIds)
            {
                _selectedBookingIds.Add(bookingId);
            }
            _selectedCafeSaleIds.Clear();
            foreach (var saleId in invoice.IncludedCafeSaleIds)
            {
                _selectedCafeSaleIds.Add(saleId);
            }

            UpdateTotals();
        }

        private void SaveInvoice()
        {
            if (!ValidateInvoice()) return;

            var invoice = BuildInvoiceFromUi();
            var customer = _customerService.GetOrCreateCustomer(_customers, invoice.Customer);
            invoice.Customer = customer;
            invoice.CustomerNameSnapshot = customer.Name;

            var path = _invoiceService.Save(invoice);
            _campingService.MarkiereBuchungenAlsAbgerechnet(invoice.IncludedBookingIds, invoice.InvoiceNumber);
            _campingService.MarkiereCafeVerkaeufeAlsAbgerechnet(invoice.IncludedCafeSaleIds, invoice.InvoiceNumber);
            LoadCustomers();

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

        private void OpenInvoiceList()
        {
            using var listForm = new InvoiceListForm(_invoiceService, FillUiFromInvoice);
            listForm.ShowDialog(this);
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

        private sealed class BookingPickerEntry
        {
            public BookingPickerEntry(Buchung buchung, string displayText)
            {
                Buchung = buchung;
                DisplayText = displayText;
            }

            public Buchung Buchung { get; }
            public string DisplayText { get; }
        }

        private sealed class CafePickerEntry
        {
            public CafePickerEntry(CafeVerkauf verkauf, string displayText)
            {
                Verkauf = verkauf;
                DisplayText = displayText;
            }

            public CafeVerkauf Verkauf { get; }
            public string DisplayText { get; }
        }

        private sealed class MultiPitchDialog : Form
        {
            private readonly CheckedListBox _lstPitches = new() { Dock = DockStyle.Fill, CheckOnClick = true };
            private readonly DateTimePicker _dtStart = new() { Format = DateTimePickerFormat.Short };
            private readonly DateTimePicker _dtEnd = new() { Format = DateTimePickerFormat.Short };
            private readonly NumericUpDown _numTotalPrice = new() { DecimalPlaces = 2, Maximum = 1000000, Minimum = 0, Value = 25 };
            private readonly NumericUpDown _numTaxRate = new() { DecimalPlaces = 2, Maximum = 100, Minimum = 0, Value = 7 };

            public MultiPitchDialog(IEnumerable<Stellplatz> stellplaetze)
            {
                Text = "Mehrere Stellplätze abrechnen";
                Width = 600;
                Height = 500;
                StartPosition = FormStartPosition.CenterParent;

                var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

                foreach (var platz in stellplaetze.OrderBy(x => x.Nummer))
                {
                    _lstPitches.Items.Add(new PitchEntry(platz), false);
                }
                _lstPitches.DisplayMember = nameof(PitchEntry.DisplayText);

                var grid = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true };
                grid.Controls.Add(new Label { Text = "Startdatum", AutoSize = true }, 0, 0);
                grid.Controls.Add(_dtStart, 1, 0);
                grid.Controls.Add(new Label { Text = "Enddatum", AutoSize = true }, 0, 1);
                grid.Controls.Add(_dtEnd, 1, 1);
                grid.Controls.Add(new Label { Text = "Gesamtpreis netto", AutoSize = true }, 0, 2);
                grid.Controls.Add(_numTotalPrice, 1, 2);
                grid.Controls.Add(new Label { Text = "Steuersatz %", AutoSize = true }, 0, 3);
                grid.Controls.Add(_numTaxRate, 1, 3);

                var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
                var btnOk = new Button { Text = "Übernehmen", AutoSize = true };
                var btnCancel = new Button { Text = "Abbrechen", AutoSize = true };
                btnOk.Click += (_, _) => DialogResult = DialogResult.OK;
                btnCancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
                buttonPanel.Controls.Add(btnOk);
                buttonPanel.Controls.Add(btnCancel);

                root.Controls.Add(_lstPitches, 0, 0);
                root.Controls.Add(grid, 0, 1);
                root.Controls.Add(buttonPanel, 0, 2);

                Controls.Add(root);
            }

            public List<int> SelectedPitchIds => _lstPitches.CheckedItems
                .Cast<PitchEntry>()
                .Select(x => x.Stellplatz.Id)
                .ToList();

            public DateTime StartDate => _dtStart.Value.Date;
            public DateTime EndDate => _dtEnd.Value.Date;
            public decimal TotalPrice => _numTotalPrice.Value;
            public decimal TaxRate => _numTaxRate.Value;

            private sealed class PitchEntry
            {
                public PitchEntry(Stellplatz stellplatz)
                {
                    Stellplatz = stellplatz;
                    DisplayText = $"{stellplatz.Nummer} ({stellplatz.Status})";
                }

                public Stellplatz Stellplatz { get; }
                public string DisplayText { get; }
            }
        }
    }
}
