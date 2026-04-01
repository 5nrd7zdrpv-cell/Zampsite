using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using UmmelbadFinal3.Models;
using UmmelbadFinal3.Services;

namespace UmmelbadFinal3
{
    public class InvoiceListForm : Form
    {
        private readonly InvoiceService _invoiceService;
        private readonly Action<Invoice> _openInvoiceAction;
        private readonly BindingList<InvoiceListRow> _rows = new();
        private readonly DataGridView _grid = new();

        public InvoiceListForm(InvoiceService invoiceService, Action<Invoice> openInvoiceAction)
        {
            _invoiceService = invoiceService;
            _openInvoiceAction = openInvoiceAction;

            InitializeComponent();
            LoadInvoices();
        }

        private void InitializeComponent()
        {
            Text = "Rechnungsübersicht";
            Width = 900;
            Height = 550;
            StartPosition = FormStartPosition.CenterParent;

            var panel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(12) };
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var controls = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            var btnSortByDate = new Button { Text = "Nach Datum", AutoSize = true };
            var btnSortByCustomer = new Button { Text = "Nach Kunde", AutoSize = true };

            btnSortByDate.Click += (_, _) => SortRows(byCustomer: false);
            btnSortByCustomer.Click += (_, _) => SortRows(byCustomer: true);

            controls.Controls.Add(btnSortByDate);
            controls.Controls.Add(btnSortByCustomer);

            _grid.Dock = DockStyle.Fill;
            _grid.ReadOnly = true;
            _grid.AllowUserToAddRows = false;
            _grid.AllowUserToDeleteRows = false;
            _grid.AutoGenerateColumns = false;
            _grid.RowHeadersVisible = false;
            _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            _grid.MultiSelect = false;
            _grid.DoubleClick += (_, _) => OpenSelectedInvoice();

            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InvoiceListRow.InvoiceNumber), HeaderText = "Rechnungsnummer", Width = 170 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InvoiceListRow.InvoiceDate), HeaderText = "Datum", Width = 120, DefaultCellStyle = new DataGridViewCellStyle { Format = "dd.MM.yyyy" } });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InvoiceListRow.CustomerName), HeaderText = "Kundenname", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(InvoiceListRow.TotalGross), HeaderText = "Gesamtbetrag", Width = 140, DefaultCellStyle = new DataGridViewCellStyle { Format = "C2" } });

            _grid.DataSource = _rows;

            panel.Controls.Add(controls, 0, 0);
            panel.Controls.Add(_grid, 0, 1);
            Controls.Add(panel);
        }

        private void LoadInvoices()
        {
            var loaded = _invoiceService.LoadAll().Select(InvoiceListRow.FromInvoice).ToList();
            ReplaceRows(loaded);
        }

        private void SortRows(bool byCustomer)
        {
            var sorted = byCustomer
                ? _rows.OrderBy(r => r.CustomerName).ThenByDescending(r => r.InvoiceDate).ToList()
                : _rows.OrderByDescending(r => r.InvoiceDate).ThenBy(r => r.CustomerName).ToList();

            ReplaceRows(sorted);
        }

        private void ReplaceRows(List<InvoiceListRow> rows)
        {
            _rows.Clear();
            foreach (var row in rows)
            {
                _rows.Add(row);
            }
        }

        private void OpenSelectedInvoice()
        {
            if (_grid.CurrentRow?.DataBoundItem is not InvoiceListRow selectedRow)
            {
                return;
            }

            var path = _invoiceService.FindInvoicePathByNumber(selectedRow.InvoiceNumber);
            if (path == null)
            {
                MessageBox.Show("Rechnungsdatei wurde nicht gefunden.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var invoice = _invoiceService.Load(path);
            if (invoice == null)
            {
                MessageBox.Show("Rechnung konnte nicht geladen werden.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _openInvoiceAction(invoice);
            Close();
        }

        private sealed class InvoiceListRow
        {
            public string InvoiceNumber { get; set; } = string.Empty;
            public DateTime InvoiceDate { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public decimal TotalGross { get; set; }

            public static InvoiceListRow FromInvoice(Invoice invoice)
            {
                return new InvoiceListRow
                {
                    InvoiceNumber = invoice.InvoiceNumber,
                    InvoiceDate = invoice.InvoiceDate,
                    CustomerName = string.IsNullOrWhiteSpace(invoice.CustomerNameSnapshot) ? invoice.Customer.Name : invoice.CustomerNameSnapshot,
                    TotalGross = invoice.TotalGross
                };
            }
        }
    }
}
