using System;

namespace CafeManager.Models
{
    public class DailyExpenseSummary
    {
        public DateTime Date { get; set; }
        public double TotalExpenses { get; set; }
        public int ExpensesCount { get; set; }
    }
}