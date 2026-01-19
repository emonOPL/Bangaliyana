using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    public interface ISellerMonthlyReportService
    {
        // ============ Report Management ============
        /// <summary>
        /// Gets the current month's report for a seller, creating one if it doesn't exist
        /// </summary>
        Task<SellerMonthlyReport> GetOrCreateCurrentReportAsync(int sellerId);

        /// <summary>
        /// Gets a report by its ID
        /// </summary>
        Task<SellerMonthlyReport?> GetReportByIdAsync(int reportId);

        /// <summary>
        /// Gets a report for a specific seller and month
        /// </summary>
        Task<SellerMonthlyReport?> GetReportAsync(int sellerId, int year, int month);

        /// <summary>
        /// Gets all reports for a specific seller
        /// </summary>
        Task<IEnumerable<SellerMonthlyReport>> GetSellerReportsAsync(int sellerId, int page = 1, int pageSize = 12);

        /// <summary>
        /// Gets total count of reports for a seller
        /// </summary>
        Task<int> GetSellerReportsCountAsync(int sellerId);

        /// <summary>
        /// Gets all reports for a specific month across all sellers
        /// </summary>
        Task<IEnumerable<SellerMonthlyReport>> GetAllReportsAsync(int year, int month, MonthlyReportStatus? status = null, int page = 1, int pageSize = 20);

        /// <summary>
        /// Gets count of reports for a specific month
        /// </summary>
        Task<int> GetReportsCountAsync(int year, int month, MonthlyReportStatus? status = null);

        // ============ Order Item Tracking ============
        /// <summary>
        /// Adds an order item to the current month's report when order is delivered
        /// </summary>
        Task AddOrderItemToReportAsync(int orderItemId, int orderId);

        /// <summary>
        /// Processes return deduction - deducts from appropriate report
        /// </summary>
        Task ProcessReturnDeductionAsync(int orderItemId, int orderId);

        /// <summary>
        /// Processes refund deduction - deducts from appropriate report
        /// </summary>
        Task ProcessRefundDeductionAsync(int orderItemId, int orderId);

        // ============ Monthly Finalization ============
        /// <summary>
        /// Finalizes all open reports for a specific month
        /// </summary>
        Task FinalizeMonthAsync(int year, int month, string? userId = null);

        /// <summary>
        /// Finalizes a single report by ID
        /// </summary>
        Task FinalizeSellerReportAsync(int reportId, string? userId = null);

        /// <summary>
        /// Main month-end processing - called by Hangfire on 1st of month
        /// </summary>
        Task<MonthEndProcessingResult> ProcessMonthEndAsync(string? triggeredByUserId = null);

        // ============ Recalculation ============
        /// <summary>
        /// Recalculates all totals for a report
        /// </summary>
        Task RecalculateReportAsync(int reportId);

        // ============ Payout Integration ============
        /// <summary>
        /// Creates a manual payout from a finalized report
        /// </summary>
        Task<ManualSellerPayout?> CreatePayoutFromReportAsync(int reportId, string processedByUserId);

        /// <summary>
        /// Links an existing payout to a report and updates status
        /// </summary>
        Task LinkPayoutToReportAsync(int reportId, int payoutId);

        /// <summary>
        /// Marks report as paid after payout completion
        /// </summary>
        Task MarkReportAsPaidAsync(int reportId);

        // ============ Statistics ============
        /// <summary>
        /// Gets summary statistics for a month
        /// </summary>
        Task<SellerMonthlyReportSummary> GetMonthlySummaryAsync(int year, int month);

        /// <summary>
        /// Gets total pending balance for a seller (all unpaid reports)
        /// </summary>
        Task<decimal> GetSellerPendingBalanceAsync(int sellerId);

        /// <summary>
        /// Gets all reports pending payout
        /// </summary>
        Task<IEnumerable<SellerMonthlyReport>> GetReportsPendingPayoutAsync();

        // ============ Adjustments ============
        /// <summary>
        /// Adds a manual adjustment to a report
        /// </summary>
        Task AddAdjustmentAsync(int reportId, decimal amount, string reason, string userId);

        // ============ Utilities ============
        /// <summary>
        /// Generates a unique report number
        /// </summary>
        string GenerateReportNumber(int sellerId, int year, int month);

        /// <summary>
        /// Gets report items for a specific report
        /// </summary>
        Task<IEnumerable<MonthlyReportOrderItem>> GetReportItemsAsync(int reportId, int page = 1, int pageSize = 50);

        /// <summary>
        /// Gets count of items in a report
        /// </summary>
        Task<int> GetReportItemsCountAsync(int reportId);

        // ============ Historical Data ============
        /// <summary>
        /// Seeds historical data by processing all existing delivered orders
        /// </summary>
        Task<HistoricalDataSeedResult> SeedHistoricalDataAsync();
    }

    /// <summary>
    /// Result of historical data seeding
    /// </summary>
    public class HistoricalDataSeedResult
    {
        public bool Success { get; set; }
        public int OrdersProcessed { get; set; }
        public int ItemsAdded { get; set; }
        public int ReportsCreated { get; set; }
        public int ReportsUpdated { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Summary statistics for a month's reports
    /// </summary>
    public class SellerMonthlyReportSummary
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
        public int TotalSellers { get; set; }
        public int OpenReports { get; set; }
        public int EligibleForPayout { get; set; }
        public int CarriedForward { get; set; }
        public int PaidReports { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalCommissions { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalPayable { get; set; }
        public decimal TotalCarriedForward { get; set; }
        public decimal TotalPaid { get; set; }
    }

    /// <summary>
    /// Result of month-end processing
    /// </summary>
    public class MonthEndProcessingResult
    {
        public bool Success { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalReportsProcessed { get; set; }
        public int ReportsEligibleForPayout { get; set; }
        public int ReportsCarriedForward { get; set; }
        public decimal TotalAmountEligible { get; set; }
        public decimal TotalAmountCarriedForward { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
