namespace SmartBoardingHouse.Models.Response
{
    public class MonthlyStatisticsResponse
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DebtAmount { get; set; }
        public string InvoiceStatus { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public RoomStatistic? Room { get; set; }
        public UtilitiesStatistic Utilities { get; set; } = new();
        public List<BreakdownItem> Breakdown { get; set; } = new();
    }

    public class YearlyStatisticsResponse
    {
        public int Year { get; set; }
        public SummaryStatistic Summary { get; set; } = new();
        public List<MonthlyStatistic> MonthlyData { get; set; } = new();
        public UsageStatistic Utilities { get; set; } = new();
        public PaymentStatusStatistic PaymentStatus { get; set; } = new();
    }

    public class RoomStatistic
    {
        public string RoomNumber { get; set; } = string.Empty;
        public decimal MonthlyRent { get; set; }
    }

    public class UtilitiesStatistic
    {
        public UtilityStatistic? Electric { get; set; }
        public UtilityStatistic? Water { get; set; }
    }

    public class UtilityStatistic
    {
        public double Usage { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Cost { get; set; }
        public double CurrentReading { get; set; }
        public double PreviousReading { get; set; }
        public bool Verified { get; set; }
    }

    public class BreakdownItem
    {
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public int Percentage { get; set; }
    }

    public class MonthlyStatistic
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal DebtAmount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class SummaryStatistic
    {
        public decimal TotalYear { get; set; }
        public decimal PaidYear { get; set; }
        public decimal DebtYear { get; set; }
        public int MonthsWithInvoice { get; set; }
        public decimal AverageMonthly { get; set; }
    }

    public class UsageStatistic
    {
        public UtilitySummary Electric { get; set; } = new();
        public UtilitySummary Water { get; set; } = new();
    }

    public class UtilitySummary
    {
        public double TotalUsage { get; set; }
        public decimal TotalCost { get; set; }
        public double AverageUsage { get; set; }
        public decimal AverageCost { get; set; }
        public int MonthsRecorded { get; set; }
    }

    public class PaymentStatusStatistic
    {
        public int Paid { get; set; }
        public int Unpaid { get; set; }
        public int Partial { get; set; }
        public int Overdue { get; set; }
    }
}
