using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace UmmelbadFinal3
{
    public class InvoiceForm : Form
    {
        private readonly BindingList<InvoiceItem> _items = new BindingList<InvoiceItem>();
        private readonly DataGridView _dgvPositions = new DataGridView();
        private readonly Button _btnAddPosition = new Button();

        private readonly Label _lblNet = new Label();
        private readonly Label _lblTax = new Label();
        private readonly Label _lblGross = new Label();

        private readonly CultureInfo _culture = new CultureInfo("de-DE");

        public InvoiceForm()
        {
            InitializeComponent();
            ConfigureDataGridView();
            BindData();
            AddDefaultPosition();
        }

        private void InitializeComponent()
        {
            Text = "Rechnungspositionen";
            Width = 980;
            Height = 620;
            StartPosition = FormStartPosition.CenterScreen;

            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(12)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var topPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 0, 0, 8)
            };

            _btnAddPosition.Text = "+";
            _btnAddPosition.Name = "btnAddPosition";
            _btnAddPosition.AutoSize = true;
            _btnAddPosition.Font = new Font(Font.FontFamily, 12, FontStyle.Bold);
            _btnAddPosition.Click += BtnAddPosition_Click;

            topPanel.Controls.Add(_btnAddPosition);

            _dgvPositions.Name = "dgvPositions";
            _dgvPositions.Dock = DockStyle.Fill;
            _dgvPositions.AllowUserToAddRows = false;
            _dgvPositions.AllowUserToDeleteRows = false;
            _dgvPositions.AutoGenerateColumns = false;
            _dgvPositions.RowHeadersVisible = false;
            _dgvPositions.SelectionMode = DataGridViewSelectionMode.CellSelect;
            _dgvPositions.MultiSelect = false;
            _dgvPositions.CellValueChanged += DgvPositions_CellValueChanged;
            _dgvPositions.CellValidating += DgvPositions_CellValidating;
            _dgvPositions.CurrentCellDirtyStateChanged += DgvPositions_CurrentCellDirtyStateChanged;
            _dgvPositions.CellContentClick += DgvPositions_CellContentClick;

            var totalsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                AutoSize = true,
                Padding = new Padding(0, 10, 0, 0)
            };
            totalsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            totalsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var totalsCaptionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            totalsCaptionPanel.Controls.Add(new Label { Text = "Summe Netto:", AutoSize = true });
            totalsCaptionPanel.Controls.Add(new Label { Text = "Summe Steuer:", AutoSize = true });
            totalsCaptionPanel.Controls.Add(new Label { Text = "Summe Brutto:", AutoSize = true, Font = new Font(Font, FontStyle.Bold) });

            var totalsValuePanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            _lblNet.AutoSize = true;
            _lblTax.AutoSize = true;
            _lblGross.AutoSize = true;
            _lblGross.Font = new Font(Font, FontStyle.Bold);

            totalsValuePanel.Controls.Add(_lblNet);
            totalsValuePanel.Controls.Add(_lblTax);
            totalsValuePanel.Controls.Add(_lblGross);

            totalsPanel.Controls.Add(totalsCaptionPanel, 0, 0);
            totalsPanel.Controls.Add(totalsValuePanel, 1, 0);

            mainLayout.Controls.Add(topPanel, 0, 0);
            mainLayout.Controls.Add(_dgvPositions, 0, 1);
            mainLayout.Controls.Add(totalsPanel, 0, 2);

            Controls.Add(mainLayout);
        }

        private void ConfigureDataGridView()
        {
            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(InvoiceItem.Title),
                HeaderText = "Titel",
                FillWeight = 220,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });

            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(InvoiceItem.Quantity),
                HeaderText = "Menge",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N2",
                    FormatProvider = _culture,
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(InvoiceItem.UnitPrice),
                HeaderText = "EP / Einzelpreis",
                Width = 130,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "C2",
                    FormatProvider = _culture,
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(InvoiceItem.TaxRate),
                HeaderText = "UST %",
                Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "N2",
                    FormatProvider = _culture,
                    Alignment = DataGridViewContentAlignment.MiddleRight
                }
            });

            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(InvoiceItem.NetTotal),
                HeaderText = "Netto",
                Width = 110,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "C2",
                    FormatProvider = _culture,
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    BackColor = Color.FromArgb(245, 245, 245)
                }
            });

            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(InvoiceItem.TaxAmount),
                HeaderText = "Steuer",
                Width = 110,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "C2",
                    FormatProvider = _culture,
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    BackColor = Color.FromArgb(245, 245, 245)
                }
            });

            _dgvPositions.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = nameof(InvoiceItem.GrossTotal),
                HeaderText = "Brutto",
                Width = 120,
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Format = "C2",
                    FormatProvider = _culture,
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    BackColor = Color.FromArgb(245, 245, 245)
                }
            });

            _dgvPositions.Columns.Add(new DataGridViewButtonColumn
            {
                HeaderText = string.Empty,
                Width = 45,
                Text = "X",
                UseColumnTextForButtonValue = true,
                FlatStyle = FlatStyle.Standard
            });
        }

        private void BindData()
        {
            _dgvPositions.DataSource = _items;
            _items.ListChanged += (_, _) => UpdateTotals();
        }

        private void AddDefaultPosition()
        {
            _items.Add(new InvoiceItem
            {
                Title = string.Empty,
                Quantity = 1m,
                UnitPrice = 0m,
                TaxRate = 19m
            });
        }

        private void BtnAddPosition_Click(object? sender, EventArgs e)
        {
            _items.Add(new InvoiceItem
            {
                Title = string.Empty,
                Quantity = 1m,
                UnitPrice = 0m,
                TaxRate = 19m
            });

            if (_dgvPositions.Rows.Count > 0)
            {
                var newRowIndex = _dgvPositions.Rows.Count - 1;
                _dgvPositions.CurrentCell = _dgvPositions.Rows[newRowIndex].Cells[0];
                _dgvPositions.BeginEdit(true);
            }
        }

        private void DgvPositions_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (_dgvPositions.IsCurrentCellDirty)
            {
                _dgvPositions.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void DgvPositions_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            _dgvPositions.Refresh();
            UpdateTotals();
        }

        private void DgvPositions_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0)
            {
                return;
            }

            if (_dgvPositions.Columns[e.ColumnIndex] is DataGridViewButtonColumn)
            {
                _items.RemoveAt(e.RowIndex);
                UpdateTotals();
            }
        }

        private void DgvPositions_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
        {
            if (!ValidateInput(e.ColumnIndex, Convert.ToString(e.FormattedValue), out var errorMessage))
            {
                e.Cancel = true;
                _dgvPositions.Rows[e.RowIndex].ErrorText = errorMessage;
                MessageBox.Show(errorMessage, "Ungültiger Wert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                _dgvPositions.Rows[e.RowIndex].ErrorText = string.Empty;
            }
        }

        private bool ValidateInput(int columnIndex, string? input, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (input is null)
            {
                return true;
            }

            var dataPropertyName = _dgvPositions.Columns[columnIndex].DataPropertyName;
            if (string.IsNullOrWhiteSpace(dataPropertyName))
            {
                return true;
            }

            if (dataPropertyName == nameof(InvoiceItem.Quantity))
            {
                if (!decimal.TryParse(input, NumberStyles.Number, _culture, out var quantity) || quantity <= 0)
                {
                    errorMessage = "Menge muss größer als 0 sein.";
                    return false;
                }
            }

            if (dataPropertyName == nameof(InvoiceItem.UnitPrice))
            {
                var normalized = input.Replace("€", string.Empty).Trim();
                if (!decimal.TryParse(normalized, NumberStyles.Currency, _culture, out var unitPrice) || unitPrice < 0)
                {
                    errorMessage = "Einzelpreis muss größer oder gleich 0 sein.";
                    return false;
                }
            }

            if (dataPropertyName == nameof(InvoiceItem.TaxRate))
            {
                if (!decimal.TryParse(input, NumberStyles.Number, _culture, out var taxRate) || taxRate < 0)
                {
                    errorMessage = "UST % muss größer oder gleich 0 sein.";
                    return false;
                }
            }

            return true;
        }

        private void UpdateTotals()
        {
            var totalNet = _items.Sum(i => i.NetTotal);
            var totalTax = _items.Sum(i => i.TaxAmount);
            var totalGross = _items.Sum(i => i.GrossTotal);

            _lblNet.Text = totalNet.ToString("C2", _culture);
            _lblTax.Text = totalTax.ToString("C2", _culture);
            _lblGross.Text = totalGross.ToString("C2", _culture);
        }
    }
}
