using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CafeManager.Data;
using CafeManager.Models;

namespace CafeManager.Services
{
    public class CafeManagerService
    {
        private readonly DataContext _context;
        private readonly ProductRepository _productRepo;
        private readonly InvoiceRepository _invoiceRepo;
        private readonly EmployeeRepository _employeeRepo;
        private readonly AttendanceRepository _attendanceRepo;
        private readonly PayrollRepository _payrollRepo;

        public UserSession CurrentUser { get; set; } = new UserSession();
        public List<GameSession> CompletedGames { get; set; } = new List<GameSession>();
        public Dictionary<string, GameSession> ActiveGames { get; set; } = new Dictionary<string, GameSession>();

        // ==================== Private Lists ====================
        private List<MiscellaneousExpense> _miscExpenses = new List<MiscellaneousExpense>();
        private List<Purchase> _purchases = new List<Purchase>();
        private List<Stocktake> _stocktakes = new List<Stocktake>();

        public CafeManagerService()
        {
            _context = new DataContext();

            // =====================================================
            // ترتیب ساخت بسیار مهم است!
            // اول ProductRepository، سپس InvoiceRepository
            // =====================================================
            _productRepo = new ProductRepository(_context.MenuPath);
            _invoiceRepo = new InvoiceRepository(_context.InvoicePath, _productRepo);
            _employeeRepo = new EmployeeRepository(_context.EmployeesPath);
            _attendanceRepo = new AttendanceRepository(_context.AttendancesPath);
            _payrollRepo = new PayrollRepository(_context.PayrollsPath);

            // بارگذاری داده‌ها
            LoadGamesHistory();
            LoadPurchases();
            LoadMiscExpenses();
            LoadStocktakes();
        }

        // ==================== Authentication ====================
        public bool Login(string username, string password)
        {
            if (username == "admin" && password == "123")
            {
                CurrentUser = new UserSession { Username = username, Role = "مدیر سیستم" };
                return true;
            }
            return false;
        }

        // ==================== Products ====================
        public List<Product> GetProducts() => _productRepo.GetAll();
        public Product? GetProduct(int id) => _productRepo.GetById(id);
        public void AddProduct(Product product) => _productRepo.Add(product);
        public void UpdateProduct(Product product) => _productRepo.Update(product);
        public void DeleteProduct(int id) => _productRepo.Delete(id);
        public List<Product> GetLowStockAlerts() => _productRepo.GetLowStock();
        public void UpdateStock(int id, int newStock, int threshold)
        {
            var product = _productRepo.GetById(id);
            if (product != null)
            {
                product.Stock = newStock;
                product.LowStockThreshold = threshold;
                _productRepo.Update(product);
            }
        }

        // ==================== Invoices ====================
        public List<Invoice> GetInvoices() => _invoiceRepo.GetAll();
        public Invoice? GetInvoice(int id) => _invoiceRepo.GetById(id);

        public void SaveInvoice(Invoice invoice)
        {
            invoice.OrderDate = DateTime.Now;

            foreach (var item in invoice.Items)
            {
                var product = _productRepo.GetById(item.Product.Id);
                if (product != null)
                {
                    product.Stock -= item.Quantity;
                    _productRepo.Update(product);
                }
            }

            _invoiceRepo.Add(invoice);
        }

        public void UpdateInvoice(Invoice invoice) => _invoiceRepo.Update(invoice);

        public void DeleteInvoice(int id)
        {
            var invoice = _invoiceRepo.GetById(id);
            if (invoice != null)
            {
                foreach (var item in invoice.Items)
                {
                    var product = _productRepo.GetById(item.Product.Id);
                    if (product != null)
                    {
                        product.Stock += item.Quantity;
                        _productRepo.Update(product);
                    }
                }
                _invoiceRepo.Delete(id);
            }
        }

        public List<Invoice> GetInvoicesByDate(DateTime from, DateTime to)
            => _invoiceRepo.GetByDateRange(from, to);

        public double GetTotalSales() => _invoiceRepo.GetTotalSales();

        public void UpdateItemQuantity(int invoiceId, int productId, int newQuantity)
        {
            var invoice = _invoiceRepo.GetById(invoiceId);
            if (invoice != null)
            {
                var item = invoice.Items.FirstOrDefault(i => i.Product.Id == productId);
                if (item != null)
                {
                    var product = _productRepo.GetById(productId);
                    if (product != null)
                    {
                        int diff = newQuantity - item.Quantity;
                        product.Stock -= diff;
                        _productRepo.Update(product);
                    }
                    item.Quantity = newQuantity;
                    _invoiceRepo.Update(invoice);
                }
            }
        }

        public void DeleteItemFromInvoice(int invoiceId, int productId)
        {
            var invoice = _invoiceRepo.GetById(invoiceId);
            if (invoice != null)
            {
                var item = invoice.Items.FirstOrDefault(i => i.Product.Id == productId);
                if (item != null)
                {
                    var product = _productRepo.GetById(productId);
                    if (product != null)
                    {
                        product.Stock += item.Quantity;
                        _productRepo.Update(product);
                    }
                    invoice.Items.Remove(item);
                    _invoiceRepo.Update(invoice);
                }
            }
        }

        public void UpdateSettlementStatus(int invoiceId, bool isSettled, string payMethod)
        {
            var invoice = _invoiceRepo.GetById(invoiceId);
            if (invoice != null)
            {
                invoice.IsSettled = isSettled;
                invoice.PayMethod = payMethod;
                _invoiceRepo.Update(invoice);
            }
        }

        // ==================== Employees ====================
        public List<Employee> GetEmployees(bool activeOnly = true)
        {
            return activeOnly ? _employeeRepo.GetActive() : _employeeRepo.GetAll();
        }
        public Employee? GetEmployee(int id) => _employeeRepo.GetById(id);
        public void AddEmployee(Employee employee) => _employeeRepo.Add(employee);
        public void UpdateEmployee(Employee employee) => _employeeRepo.Update(employee);
        public void DeleteEmployee(int id) => _employeeRepo.Delete(id);

        // ==================== Attendance ====================
        public List<Attendance> GetAttendances(int employeeId, DateTime? from = null, DateTime? to = null)
            => _attendanceRepo.GetByEmployeeAndDateRange(employeeId, from, to);
        public Attendance? GetAttendance(int id) => _attendanceRepo.GetById(id);
        public void AddAttendance(Attendance attendance) => _attendanceRepo.Add(attendance);
        public void UpdateAttendance(Attendance attendance) => _attendanceRepo.Update(attendance);
        public void DeleteAttendance(int id) => _attendanceRepo.Delete(id);

        // ==================== Payroll ====================
        public List<Payroll> GetPayrolls(int? employeeId = null, int? year = null, int? month = null)
            => _payrollRepo.GetByFilters(employeeId, year, month);
        public Payroll? GetPayroll(int id) => _payrollRepo.GetById(id);

        public Payroll? CalculatePayroll(int employeeId, int year, int month)
        {
            var employee = GetEmployee(employeeId);
            if (employee == null) return null;

            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var attendances = GetAttendances(employeeId, startDate, endDate);

            double totalWorkHours = 0;
            double overtimeHours = 0;

            foreach (var a in attendances.Where(a => a.IsPresent))
            {
                double hours = a.WorkHours.TotalHours;
                totalWorkHours += hours;
                if (hours > 8)
                    overtimeHours += (hours - 8);
            }

            double baseSalary = employee.BaseSalary;
            double hourlyRate = employee.HourlyRate > 0 ? employee.HourlyRate : (baseSalary / (22 * 8));
            double overtimePay = overtimeHours * hourlyRate * employee.OvertimeRate;
            double netSalary = baseSalary + overtimePay;

            return new Payroll
            {
                EmployeeId = employeeId,
                Year = year,
                Month = month,
                BaseSalary = baseSalary,
                TotalWorkHours = totalWorkHours,
                OvertimeHours = overtimeHours,
                OvertimePay = overtimePay,
                Bonus = 0,
                Deductions = 0,
                NetSalary = netSalary,
                IsPaid = false,
                Notes = "محاسبه خودکار"
            };
        }

        public void SavePayroll(Payroll payroll)
        {
            var existing = _payrollRepo.GetById(payroll.Id);
            if (existing != null)
                _payrollRepo.Update(payroll);
            else
                _payrollRepo.Add(payroll);
        }

        public void MarkPayrollAsPaid(int payrollId, DateTime paymentDate)
        {
            var payroll = _payrollRepo.GetById(payrollId);
            if (payroll != null)
            {
                payroll.IsPaid = true;
                payroll.PaymentDate = paymentDate;
                _payrollRepo.Update(payroll);
            }
        }

        public void DeletePayroll(int id) => _payrollRepo.Delete(id);

        // ==================== Games ====================
        public void AddCompletedGame(GameSession game)
        {
            var existing = CompletedGames.FirstOrDefault(g => g.Id == game.Id);
            if (existing != null)
            {
                var index = CompletedGames.IndexOf(existing);
                CompletedGames[index] = game;
            }
            else
                CompletedGames.Add(game);

            SaveGamesHistory();
        }

        public List<GameSession> GetGamesHistory()
            => CompletedGames.OrderByDescending(g => g.StartTime).ToList();

        public List<GameSession> GetGamesByDate(DateTime from, DateTime to)
            => CompletedGames.Where(g => g.StartTime.Date >= from.Date && g.StartTime.Date <= to.Date)
                             .OrderByDescending(g => g.StartTime).ToList();

        public List<GameSession> GetGamesByMaster(string gameMasterName)
            => CompletedGames.Where(g => g.GameMasterName.Contains(gameMasterName, StringComparison.OrdinalIgnoreCase))
                             .OrderByDescending(g => g.StartTime).ToList();

        public double GetTotalRevenueFromGames()
            => CompletedGames.Sum(g => g.TotalRevenue);

        public double GetTotalGameMasterShare()
            => CompletedGames.Sum(g => g.GameMasterShare);

        private void LoadGamesHistory()
        {
            try
            {
                CompletedGames.Clear();
                if (!File.Exists(_context.GamesHistoryPath)) return;

                var lines = File.ReadAllLines(_context.GamesHistoryPath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(new[] { "||" }, StringSplitOptions.None);
                    if (parts.Length >= 11)
                    {
                        var game = new GameSession
                        {
                            Id = parts[0],
                            GameName = parts[1],
                            TableNumber = parts[2],
                            GameMasterName = parts[3],
                            RevenuePercent = double.Parse(parts[4]),
                            Players = parts[5].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                            StartTime = DateTime.Parse(parts[6]),
                            EndTime = !string.IsNullOrEmpty(parts[7]) ? DateTime.Parse(parts[7]) : (DateTime?)null,
                            TotalRevenue = double.Parse(parts[8]),
                            GameMasterShare = double.Parse(parts[9]),
                            IsActive = bool.Parse(parts[10])
                        };

                        if (parts.Length >= 12 && !string.IsNullOrEmpty(parts[11]))
                        {
                            var paymentParts = parts[11].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var payment in paymentParts)
                            {
                                var kv = payment.Split(':');
                                if (kv.Length == 2)
                                    game.PlayerPayments[kv[0]] = double.Parse(kv[1]);
                            }
                        }

                        if (parts.Length >= 13 && !string.IsNullOrEmpty(parts[12]))
                        {
                            var invoiceParts = parts[12].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var invoice in invoiceParts)
                            {
                                var kv = invoice.Split(':');
                                if (kv.Length == 2)
                                    game.PlayerInvoiceIds[kv[0]] = int.Parse(kv[1]);
                            }
                        }

                        if (parts.Length >= 14 && !string.IsNullOrEmpty(parts[13]))
                            game.IsSettled = bool.Parse(parts[13]);

                        if (parts.Length >= 15 && !string.IsNullOrEmpty(parts[14]))
                            game.SettlementDate = DateTime.Parse(parts[14]);

                        CompletedGames.Add(game);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading games: {ex.Message}");
            }
        }

        private void SaveGamesHistory()
        {
            try
            {
                var lines = new List<string>();
                foreach (var game in CompletedGames)
                {
                    string playersStr = string.Join(",", game.Players);
                    string playerPaymentsStr = string.Join(",", game.PlayerPayments.Select(p => $"{p.Key}:{p.Value}"));
                    string playerInvoicesStr = string.Join(",", game.PlayerInvoiceIds.Select(p => $"{p.Key}:{p.Value}"));
                    string endTimeStr = game.EndTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "";

                    lines.Add($"{game.Id}||{game.GameName}||{game.TableNumber}||{game.GameMasterName}||{game.RevenuePercent}||{playersStr}||{game.StartTime:yyyy-MM-dd HH:mm:ss}||{endTimeStr}||{game.TotalRevenue}||{game.GameMasterShare}||{game.IsActive}||{playerPaymentsStr}||{playerInvoicesStr}||{game.IsSettled}||{game.SettlementDate?.ToString("yyyy-MM-dd HH:mm:ss")}");
                }
                File.WriteAllLines(_context.GamesHistoryPath, lines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving games: {ex.Message}");
            }
        }

        // ==================== Purchases ====================
        public List<Purchase> GetPurchases(DateTime? from = null, DateTime? to = null)
        {
            var query = _purchases.AsQueryable();
            if (from.HasValue) query = query.Where(p => p.PurchaseDate >= from.Value);
            if (to.HasValue) query = query.Where(p => p.PurchaseDate <= to.Value);
            return query.OrderByDescending(p => p.PurchaseDate).ToList();
        }

        public Purchase? GetPurchaseById(int id)
            => _purchases.FirstOrDefault(p => p.Id == id);

        public void AddPurchase(Purchase purchase)
        {
            purchase.Id = _purchases.Count > 0 ? _purchases.Max(p => p.Id) + 1 : 1;
            _purchases.Add(purchase);
            SavePurchases();
        }

        public void UpdatePurchase(Purchase purchase)
        {
            var existing = GetPurchaseById(purchase.Id);
            if (existing != null)
            {
                existing.PurchaseDate = purchase.PurchaseDate;
                existing.SupplierName = purchase.SupplierName;
                existing.InvoiceNumber = purchase.InvoiceNumber;
                existing.Items = purchase.Items;
                SavePurchases();
            }
        }

        public void DeletePurchase(int id)
        {
            var purchase = GetPurchaseById(id);
            if (purchase != null && !purchase.IsConfirmed)
            {
                _purchases.Remove(purchase);
                SavePurchases();
            }
        }

        public void ConfirmPurchase(int purchaseId)
        {
            var purchase = GetPurchaseById(purchaseId);
            if (purchase == null || purchase.IsConfirmed) return;

            foreach (var item in purchase.Items)
            {
                var product = _productRepo.GetById(item.ProductId);
                if (product != null)
                {
                    product.Stock += item.Quantity;
                    _productRepo.Update(product);
                }
            }

            purchase.IsConfirmed = true;
            SavePurchases();
        }

        private void SavePurchases()
        {
            try
            {
                var lines = new List<string>();
                foreach (var p in _purchases)
                {
                    lines.Add($"H||{p.Id}||{p.PurchaseDate:yyyy-MM-dd HH:mm:ss}||{p.SupplierName}||{p.InvoiceNumber}||{p.IsConfirmed}");
                    foreach (var item in p.Items)
                        lines.Add($"D||{item.ProductId}||{item.Quantity}||{item.UnitPurchasePrice}");
                }
                File.WriteAllLines(_context.PurchasesPath, lines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving purchases: {ex.Message}");
            }
        }

        private void LoadPurchases()
        {
            try
            {
                _purchases.Clear();
                if (!File.Exists(_context.PurchasesPath)) return;

                var lines = File.ReadAllLines(_context.PurchasesPath);
                Purchase current = null;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(new[] { "||" }, StringSplitOptions.None);

                    if (parts[0] == "H")
                    {
                        current = new Purchase
                        {
                            Id = int.Parse(parts[1]),
                            PurchaseDate = DateTime.Parse(parts[2]),
                            SupplierName = parts[3],
                            InvoiceNumber = parts[4],
                            IsConfirmed = bool.Parse(parts[5]),
                            Items = new List<PurchaseItem>()
                        };
                        _purchases.Add(current);
                    }
                    else if (parts[0] == "D" && current != null && parts.Length >= 4)
                    {
                        var item = new PurchaseItem
                        {
                            ProductId = int.Parse(parts[1]),
                            Quantity = int.Parse(parts[2]),
                            UnitPurchasePrice = double.Parse(parts[3])
                        };
                        item.Product = _productRepo.GetById(item.ProductId);
                        current.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading purchases: {ex.Message}");
            }
        }

        // ==================== Stocktakes ====================
        public List<Stocktake> GetStocktakes(DateTime? from = null, DateTime? to = null)
        {
            var query = _stocktakes.AsQueryable();
            if (from.HasValue) query = query.Where(s => s.StocktakeDate >= from.Value);
            if (to.HasValue) query = query.Where(s => s.StocktakeDate <= to.Value);
            return query.OrderByDescending(s => s.StocktakeDate).ToList();
        }

        public Stocktake? GetStocktakeById(int id)
            => _stocktakes.FirstOrDefault(s => s.Id == id);

        public void AddStocktake(Stocktake stocktake)
        {
            stocktake.Id = _stocktakes.Count > 0 ? _stocktakes.Max(s => s.Id) + 1 : 1;
            _stocktakes.Add(stocktake);
            SaveStocktakes();
        }

        public void FinalizeStocktake(int stocktakeId)
        {
            var stocktake = GetStocktakeById(stocktakeId);
            if (stocktake == null || stocktake.IsFinalized) return;

            foreach (var item in stocktake.Items)
            {
                var product = _productRepo.GetById(item.ProductId);
                if (product != null)
                {
                    product.Stock = item.PhysicalStock;
                    _productRepo.Update(product);
                }
            }

            stocktake.IsFinalized = true;
            SaveStocktakes();
        }

        public void DeleteStocktake(int id)
        {
            var stocktake = GetStocktakeById(id);
            if (stocktake != null && !stocktake.IsFinalized)
            {
                _stocktakes.Remove(stocktake);
                SaveStocktakes();
            }
        }

        private void SaveStocktakes()
        {
            try
            {
                var lines = new List<string>();
                foreach (var st in _stocktakes)
                {
                    lines.Add($"H||{st.Id}||{st.StocktakeDate:yyyy-MM-dd HH:mm:ss}||{st.Note}||{st.IsFinalized}");
                    foreach (var item in st.Items)
                        lines.Add($"D||{item.ProductId}||{item.SystemStock}||{item.PhysicalStock}");
                }
                File.WriteAllLines(_context.StocktakesPath, lines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving stocktakes: {ex.Message}");
            }
        }

        private void LoadStocktakes()
        {
            try
            {
                _stocktakes.Clear();
                if (!File.Exists(_context.StocktakesPath)) return;

                var lines = File.ReadAllLines(_context.StocktakesPath);
                Stocktake current = null;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(new[] { "||" }, StringSplitOptions.None);

                    if (parts[0] == "H")
                    {
                        current = new Stocktake
                        {
                            Id = int.Parse(parts[1]),
                            StocktakeDate = DateTime.Parse(parts[2]),
                            Note = parts[3],
                            IsFinalized = bool.Parse(parts[4]),
                            Items = new List<StocktakeItem>()
                        };
                        _stocktakes.Add(current);
                    }
                    else if (parts[0] == "D" && current != null && parts.Length >= 4)
                    {
                        var item = new StocktakeItem
                        {
                            ProductId = int.Parse(parts[1]),
                            SystemStock = int.Parse(parts[2]),
                            PhysicalStock = int.Parse(parts[3])
                        };
                        item.Product = _productRepo.GetById(item.ProductId);
                        current.Items.Add(item);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading stocktakes: {ex.Message}");
            }
        }

        // ==================== Miscellaneous Expenses ====================
        public List<MiscellaneousExpense> GetMiscExpenses(DateTime? from = null, DateTime? to = null)
        {
            var query = _miscExpenses.AsQueryable();
            if (from.HasValue) query = query.Where(e => e.ExpenseDate >= from.Value);
            if (to.HasValue) query = query.Where(e => e.ExpenseDate <= to.Value);
            return query.OrderByDescending(e => e.ExpenseDate).ToList();
        }

        public MiscellaneousExpense? GetMiscExpenseById(int id)
            => _miscExpenses.FirstOrDefault(e => e.Id == id);

        public void AddMiscExpense(MiscellaneousExpense expense)
        {
            expense.Id = _miscExpenses.Count > 0 ? _miscExpenses.Max(e => e.Id) + 1 : 1;
            _miscExpenses.Add(expense);
            SaveMiscExpenses();
        }

        public void UpdateMiscExpense(MiscellaneousExpense updated)
        {
            var existing = GetMiscExpenseById(updated.Id);
            if (existing != null)
            {
                existing.ExpenseDate = updated.ExpenseDate;
                existing.Description = updated.Description;
                existing.Category = updated.Category;
                existing.Amount = updated.Amount;
                existing.PaymentMethod = updated.PaymentMethod;
                existing.ReceiptNumber = updated.ReceiptNumber;
                existing.Notes = updated.Notes;
                existing.IsConfirmed = updated.IsConfirmed;
                SaveMiscExpenses();
            }
        }

        public void DeleteMiscExpense(int id)
        {
            var expense = GetMiscExpenseById(id);
            if (expense != null)
            {
                _miscExpenses.Remove(expense);
                SaveMiscExpenses();
            }
        }

        public double GetTotalMiscExpenses(DateTime from, DateTime to)
        {
            return _miscExpenses.Where(e => e.ExpenseDate >= from && e.ExpenseDate <= to && e.IsConfirmed)
                                .Sum(e => e.Amount);
        }

        private void SaveMiscExpenses()
        {
            try
            {
                var lines = new List<string>();
                foreach (var e in _miscExpenses)
                {
                    lines.Add($"{e.Id}||{e.ExpenseDate:yyyy-MM-dd HH:mm:ss}||{e.Description}||{e.Category}||{e.Amount}||{e.PaymentMethod}||{e.ReceiptNumber}||{e.Notes}||{e.IsConfirmed}");
                }
                File.WriteAllLines(_context.MiscExpensesPath, lines);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving misc expenses: {ex.Message}");
            }
        }

        private void LoadMiscExpenses()
        {
            try
            {
                _miscExpenses.Clear();
                if (!File.Exists(_context.MiscExpensesPath)) return;

                var lines = File.ReadAllLines(_context.MiscExpensesPath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var parts = line.Split(new[] { "||" }, StringSplitOptions.None);
                    if (parts.Length >= 9)
                    {
                        _miscExpenses.Add(new MiscellaneousExpense
                        {
                            Id = int.Parse(parts[0]),
                            ExpenseDate = DateTime.Parse(parts[1]),
                            Description = parts[2],
                            Category = parts[3],
                            Amount = double.Parse(parts[4]),
                            PaymentMethod = parts[5],
                            ReceiptNumber = parts[6],
                            Notes = parts[7],
                            IsConfirmed = bool.Parse(parts[8])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading misc expenses: {ex.Message}");
            }
        }

        // ==================== Logs ====================
        public List<SystemLog> GetLogs()
        {
            var logs = new List<SystemLog>();

            foreach (var inv in _invoiceRepo.GetAll())
                logs.Add(new SystemLog
                {
                    Timestamp = inv.OrderDate,
                    Action = $"فاکتور شماره {inv.Id} برای میز {inv.TableNumber} به مبلغ {inv.TotalAmount:N0} تومان ثبت شد."
                });

            foreach (var game in CompletedGames)
                logs.Add(new SystemLog
                {
                    Timestamp = game.EndTime ?? game.StartTime,
                    Action = $"بازی {game.GameName} - میز {game.TableNumber} - گرداننده: {game.GameMasterName} - فروش: {game.TotalRevenue:N0} تومان"
                });

            if (!logs.Any())
                logs.Add(new SystemLog { Timestamp = DateTime.Now, Action = "هیچ گزارشی در سیستم ثبت نشده است." });

            return logs.OrderByDescending(l => l.Timestamp).ToList();
        }
    }
}