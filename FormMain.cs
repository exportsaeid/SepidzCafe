using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using CafeManager;
using CafeManager.Models;

namespace CafeManager
{
    public class FormMain : Form
    {
        private List<OrderItem> _currentOrder = new List<OrderItem>();
        private List<Product> _menuProducts = new List<Product>();
        private ListBox lstMenu;
        private DataGridView dgvOrder;
        private NumericUpDown numQuantity;
        private Button btnAdd, btnCheckout, btnInvoiceManager;
        private Label lblTotal, lblCustomer, lblTable, lblMenuTitle, lblOrderTitle, lblQty;
        private TextBox txtCustomerName, txtTableNumber, txtSearch;

        public FormMain()
        {
            this.Text = "سیستم مدیریت و صندوق کافه (انبارداری هوشمند)";
            this.Size = new Size(950, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;
            this.Font = new Font("Tahoma", 10);
            this.Load += (s, e) => this.WindowState = FormWindowState.Maximized;
            this.SizeChanged += FormMain_SizeChanged;
            _menuProducts = CafeManager.GetMenu();
            InitializeCustomComponents();
            LoadMenuToUI();
        }

        private void InitializeCustomComponents()
        {
            this.Controls.Clear();

            // منوی بالا
            MenuStrip mainMainMenu = new MenuStrip { BackColor = Color.WhiteSmoke };

            // ========== منوی عملیات سیستم ==========
            ToolStripMenuItem menuOperations = new ToolStripMenuItem("⚙️ عملیات سیستم");

            menuOperations.DropDownItems.Add("📦 مدیریت انبارداری", null, (s, e) => { new InventoryForm().ShowDialog(); _menuProducts = CafeManager.GetMenu(); LoadMenuToUI(); });
            menuOperations.DropDownItems.Add("☕ مدیریت منو کافه", null, (s, e) => { new MenuManagerForm().ShowDialog(); _menuProducts = CafeManager.GetMenu(); LoadMenuToUI(); });
            menuOperations.DropDownItems.Add("🎲 مدیریت بازی و تیم‌ها", null, (s, e) => {
                using (var frm = new GameManagementForm())
                {
                    frm.ShowDialog(this);
                };
            });
            menuOperations.DropDownItems.Add("📊 تاریخچه خرید", null, (s, e) => {
                using (var frm = new PurchaseHistoryForm())
                {
                    frm.ShowDialog(this);
                }
            });
            menuOperations.DropDownItems.Add("🛒 خرید روزانه", null, (s, e) => {
                using (var frm = new PurchaseForm())
                {
                    if (frm.ShowDialog(this) == DialogResult.OK)
                    {
                        _menuProducts = CafeManager.GetMenu();
                        LoadMenuToUI();
                    }
                }
            });
            menuOperations.DropDownItems.Add("📊 انبارگردانی", null, (s, e) => {
                using (var frm = new StocktakeForm())
                {
                    if (frm.ShowDialog(this) == DialogResult.OK)
                    {
                        _menuProducts = CafeManager.GetMenu();
                        LoadMenuToUI();
                    }
                }
            });

            ToolStripMenuItem menuSettlement = new ToolStripMenuItem("🧾 تسویه گرداننده");
            menuSettlement.Click += (s, e) => {
                using (var frm = new SettlementForm())
                {
                    frm.ShowDialog(this);
                }
            };
            menuOperations.DropDownItems.Add(menuSettlement);
            mainMainMenu.Items.Add(menuOperations);

            // ========== منوی مدیریت پرسنل ==========
            ToolStripMenuItem menuPersonnel = new ToolStripMenuItem("👤 مدیریت پرسنل");
            menuPersonnel.DropDownItems.Add("👤 مدیریت پرسنل", null, (s, e) => {
                using (var frm = new EmployeeForm())
                {
                    frm.ShowDialog(this);
                }
            });
            menuPersonnel.DropDownItems.Add("📋 حضور و غیاب", null, (s, e) => {
                using (var frm = new AttendanceForm())
                {
                    frm.ShowDialog(this);
                }
            });
            menuPersonnel.DropDownItems.Add("💰 حقوق و دستمزد", null, (s, e) => {
                using (var frm = new PayrollForm())
                {
                    frm.ShowDialog(this);
                }
            });
            mainMainMenu.Items.Add(menuPersonnel);

            // ========== منوی گزارشات ==========
            ToolStripMenuItem menuReports = new ToolStripMenuItem("📊 گزارشات");
            menuReports.DropDownItems.Add("📈 گزارش فروش (تاریخ شمسی)", null, (s, e) => {
                using (var frm = new ReportForm())
                {
                    frm.ShowDialog(this);
                }
            });
            mainMainMenu.Items.Add(menuReports);

            // ========== منوی متفرقه ==========
            ToolStripMenuItem menuMisc = new ToolStripMenuItem("💼 متفرقه");

            menuMisc.DropDownItems.Add("💰 هزینه‌های متفرقه", null, (s, e) => {
                using (var frm = new MiscExpenseForm())
                {
                    frm.ShowDialog(this);
                }
            });

            menuMisc.DropDownItems.Add("📊 گزارش هزینه‌ها", null, (s, e) => {
                using (var frm = new MiscExpenseReportForm())
                {
                    frm.ShowDialog(this);
                }
            });

            // ========== اضافه کردن گزارش سود خالص به منوی متفرقه ==========
            menuMisc.DropDownItems.Add("📊 گزارش سود خالص", null, (s, e) => {
                using (var frm = new ProfitReportForm())
                {
                    frm.ShowDialog(this);
                }
            });

            mainMainMenu.Items.Add(menuMisc);
            // ==============================================

            this.MainMenuStrip = mainMainMenu;
            this.Controls.Add(mainMainMenu);

            // جستجو
            Label lblSearch = new Label { Text = "جستجوی محصول:", Size = new Size(100, 25), Location = new Point(20, 45), Font = new Font("Tahoma", 10, FontStyle.Bold) };
            txtSearch = new TextBox { Size = new Size(140, 25), Location = new Point(120, 42), Font = new Font("Tahoma", 10) };
            txtSearch.TextChanged += (s, e) => {
                var filtered = _menuProducts.Where(p => p.Name.ToLower().Contains(txtSearch.Text.ToLower())).ToList();
                lstMenu.Items.Clear();
                foreach (var p in filtered) lstMenu.Items.Add($"{p.Name} - {p.Price:N0} تومان");
            };

            // کنترل‌های بالا
            lblCustomer = new Label { Text = "نام مشتری:", Size = new Size(80, 25), Font = new Font("Tahoma", 10, FontStyle.Bold) };
            txtCustomerName = new TextBox { Size = new Size(170, 25), Font = new Font("Tahoma", 10) };
            lblTable = new Label { Text = "شماره میز:", Size = new Size(80, 25), Font = new Font("Tahoma", 10, FontStyle.Bold) };
            txtTableNumber = new TextBox { Size = new Size(120, 25), Font = new Font("Tahoma", 10) };

            lblMenuTitle = new Label { Text = "منوی کافه:", Size = new Size(220, 25), Font = new Font("Tahoma", 12, FontStyle.Bold), ForeColor = Color.DarkRed };

            lstMenu = new ListBox();
            lstMenu.Font = new Font("Tahoma", 11, FontStyle.Bold);
            lstMenu.MouseDoubleClick += (s, e) => BtnAdd_Click(btnAdd, EventArgs.Empty);

            lblQty = new Label { Text = "تعداد:", Size = new Size(50, 25), Font = new Font("Tahoma", 10, FontStyle.Bold) };
            numQuantity = new NumericUpDown { Minimum = 1, Value = 1, Size = new Size(60, 25), Font = new Font("Tahoma", 10) };

            btnAdd = new Button { Text = "افزودن به فاکتور ➕", BackColor = Color.LightGreen, FlatStyle = FlatStyle.Flat, Font = new Font("Tahoma", 10, FontStyle.Bold) };
            btnAdd.Click += BtnAdd_Click;

            lblOrderTitle = new Label { Text = "فاکتور جاری مشتری:", Size = new Size(200, 25), Font = new Font("Tahoma", 12, FontStyle.Bold), ForeColor = Color.DarkBlue };

            dgvOrder = new DataGridView
            {
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.White,
                GridColor = Color.LightGray,
                RowHeadersVisible = false,
                Font = new Font("Tahoma", 10),
                ColumnHeadersHeight = 30,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            };

            dgvOrder.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvOrder.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            dgvOrder.CellContentClick += dgvOrder_CellContentClick;

            dgvOrder.Columns.Add("Name", "نام محصول");
            dgvOrder.Columns.Add("Price", "قیمت واحد");
            dgvOrder.Columns.Add("Qty", "تعداد");
            dgvOrder.Columns.Add("Total", "قیمت کل");

            var editColumn = new DataGridViewButtonColumn
            {
                Name = "EditAction",
                HeaderText = "ویرایش",
                Text = "✏️ ویرایش",
                UseColumnTextForButtonValue = true,
                Width = 110,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.LightBlue,
                    ForeColor = Color.DarkBlue,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Tahoma", 9, FontStyle.Bold)
                }
            };

            var deleteColumn = new DataGridViewButtonColumn
            {
                Name = "DeleteAction",
                HeaderText = "حذف",
                Text = "🗑️ حذف",
                UseColumnTextForButtonValue = true,
                Width = 110,
                FlatStyle = FlatStyle.Flat,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = Color.FromArgb(255, 80, 80),
                    ForeColor = Color.White,
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    Font = new Font("Tahoma", 9, FontStyle.Bold)
                }
            };

            dgvOrder.Columns.Add(editColumn);
            dgvOrder.Columns.Add(deleteColumn);

            lblTotal = new Label
            {
                Text = "جمع کل فاکتور: 0 تومان",
                Size = new Size(400, 30),
                Font = new Font("Tahoma", 12, FontStyle.Bold),
                ForeColor = Color.DarkRed
            };

            btnCheckout = new Button
            {
                Text = "ثبت فاکتور ",
                Size = new Size(180, 45),
                BackColor = Color.LightCoral,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Tahoma", 10, FontStyle.Bold)
            };
            btnCheckout.Click += BtnCheckout_Click;

            btnInvoiceManager = new Button
            {
                Text = "مدیریت فاکتورها",
                Size = new Size(180, 45),
                BackColor = Color.SkyBlue,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Tahoma", 10, FontStyle.Bold)
            };
            btnInvoiceManager.Click += (s, e) => {
                using (var frm = new InvoiceForm())
                {
                    frm.ShowDialog(this);
                }
            };

            this.Controls.AddRange(new Control[]
            {
                lblSearch, txtSearch, lblCustomer, txtCustomerName, lblTable, txtTableNumber,
                lblMenuTitle, lstMenu, lblQty, numQuantity, btnAdd, lblOrderTitle, dgvOrder,
                lblTotal, btnCheckout, btnInvoiceManager
            });
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (lstMenu.SelectedItem == null) return;

            string name = lstMenu.SelectedItem.ToString().Split('-')[0].Trim();
            var product = _menuProducts.FirstOrDefault(p => p.Name == name);
            if (product == null) return;

            int qtyToAdd = (int)numQuantity.Value;

            var existingItem = _currentOrder.FirstOrDefault(item => item.Product.Id == product.Id);

            if (existingItem != null)
            {
                existingItem.Quantity += qtyToAdd;
            }
            else
            {
                _currentOrder.Add(new OrderItem
                {
                    Product = product,
                    Quantity = qtyToAdd
                });
            }

            UpdateOrderGrid();
        }

        private void UpdateOrderGrid()
        {
            dgvOrder.Rows.Clear();
            double total = 0;

            foreach (var i in _currentOrder)
            {
                dgvOrder.Rows.Add(
                    i.Product.Name,
                    i.Product.Price.ToString("N0"),
                    i.Quantity,
                    i.TotalPrice.ToString("N0")
                );
                total += i.TotalPrice;
            }

            lblTotal.Text = $"جمع کل فاکتور: {total:N0} تومان";
        }

        private void FormMain_SizeChanged(object sender, EventArgs e)
        {
            int w = this.ClientSize.Width, h = this.ClientSize.Height;
            if (w < 100 || h < 100) return;

            lblCustomer.Location = new Point(300, 48);
            txtCustomerName.Location = new Point(390, 45);
            lblTable.Location = new Point(580, 48);
            txtTableNumber.Location = new Point(670, 45);
            lblMenuTitle.Location = new Point(20, 95);
            lstMenu.Location = new Point(20, 125);
            lstMenu.Size = new Size(240, h - 240);

            lstMenu.Font = new Font("Tahoma", 11, FontStyle.Bold);

            lblQty.Location = new Point(20, h - 105);
            numQuantity.Location = new Point(80, h - 107);
            btnAdd.Location = new Point(20, h - 70);
            btnAdd.Size = new Size(240, 40);

            int oL = 300;
            lblOrderTitle.Location = new Point(oL, 95);
            dgvOrder.Location = new Point(oL, 125);
            dgvOrder.Size = new Size(w - 330, h - 240);
            lblTotal.Location = new Point(oL, h - 105);
            btnCheckout.Location = new Point(oL, h - 72);
            btnInvoiceManager.Location = new Point(oL + 200, h - 72);
        }

        private void LoadMenuToUI()
        {
            lstMenu.Items.Clear();
            lstMenu.Font = new Font("Tahoma", 11, FontStyle.Bold);
            foreach (var p in _menuProducts)
                lstMenu.Items.Add($"{p.Name} - {p.Price:N0} تومان");
        }

        private void BtnCheckout_Click(object sender, EventArgs e)
        {
            if (_currentOrder.Count == 0)
            {
                MessageBox.Show("هیچ آیتمی در فاکتور وجود ندارد!", "هشدار", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var newInvoice = new Invoice
            {
                CustomerName = txtCustomerName.Text.Trim(),
                TableNumber = txtTableNumber.Text.Trim(),
                OrderDate = DateTime.Now,
                Items = new List<OrderItem>(_currentOrder)
            };

            CafeManager.SaveInvoice(newInvoice);

            _currentOrder.Clear();
            UpdateOrderGrid();
            txtCustomerName.Clear();
            txtTableNumber.Clear();
            numQuantity.Value = 1;

            MessageBox.Show($"✅ فاکتور با موفقیت ثبت شد.\nشماره فاکتور: {newInvoice.Id}\nتاریخ: {newInvoice.OrderDate:yyyy/MM/dd HH:mm}",
                            "ثبت فاکتور",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

            txtCustomerName.Focus();
        }

        private void dgvOrder_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dgvOrder.Columns[e.ColumnIndex].Name == "DeleteAction")
            {
                _currentOrder.RemoveAt(e.RowIndex);
                UpdateOrderGrid();
            }
            else if (dgvOrder.Columns[e.ColumnIndex].Name == "EditAction")
            {
                Form f = new Form
                {
                    Text = "ویرایش تعداد",
                    Size = new Size(250, 180),
                    RightToLeft = RightToLeft.Yes,
                    StartPosition = FormStartPosition.CenterParent
                };

                Label lbl = new Label { Text = "تعداد جدید:", Location = new Point(30, 30), Size = new Size(100, 25), Font = new Font("Tahoma", 10, FontStyle.Bold) };
                NumericUpDown n = new NumericUpDown
                {
                    Value = _currentOrder[e.RowIndex].Quantity,
                    Location = new Point(140, 28),
                    Size = new Size(70, 25),
                    Minimum = 1,
                    Font = new Font("Tahoma", 10)
                };

                Button b = new Button
                {
                    Text = "تایید تغییرات",
                    Location = new Point(80, 80),
                    Size = new Size(100, 35),
                    BackColor = Color.LightGreen,
                    Font = new Font("Tahoma", 10, FontStyle.Bold)
                };

                b.Click += (s, ev) =>
                {
                    _currentOrder[e.RowIndex].Quantity = (int)n.Value;
                    UpdateOrderGrid();
                    f.Close();
                };

                f.Controls.Add(lbl);
                f.Controls.Add(n);
                f.Controls.Add(b);
                f.ShowDialog();
            }
        }
    }
}