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

            InitializeComponent();
            ConfigureDataGridView();
            _dgvPositions.DataSource = _items;
            _items.ListChanged += (_, _) => UpdateTotals();

            LoadCustomers();
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

            btnAdd.Click += (_, _) => AddPosition();
            btnDelete.Click += (_, _) => DeleteSelectedPosition();
            btnSave.Click += (_, _) => SaveInvoice();
            btnLoad.Click += (_, _) => LoadInvoice();
            btnPdf.Click += (_, _) => ExportPdf();
            btnList.Click += (_, _) => OpenInvoiceList();

            panel.Controls.AddRange(new Control[] { btnAdd, btnDelete, btnSave, btnLoad, btnPdf, btnList });
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
    }
}
