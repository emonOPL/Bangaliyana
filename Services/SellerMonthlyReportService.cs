using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bangaliyana.Services
{
    public class SellerMonthlyReportService : ISellerMonthlyReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SellerMonthlyReportService> _logger;

        public SellerMonthlyReportService(
            ApplicationDbContext context,
            INotificationService notificationService,
            ILogger<SellerMonthlyReportService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _logger = logger;
        }

        #region Report Management

        public async Task<SellerMonthlyReport> GetOrCreateCurrentReportAsync(int sellerId)
        {
            var now = DateTime.UtcNow;
            var year = now.Year;
            var month = now.Month;

            var report = await _context.SellerMonthlyReports
                .FirstOrDefaultAsync(r => r.SellerId == sellerId && r.Year == year && r.Month == month);

            if (report == null)
            {
                var seller = await _context.Sellers.FindAsync(sellerId);
                if (seller == null)
                    throw new InvalidOperationException($"Seller with ID {sellerId} not found");

                // Get carryforward from previous month
                var previousMonth = month == 1 ? 12 : month - 1;
                var previousYear = month == 1 ? year - 1 : year;

                var previousReport = await _context.SellerMonthlyReports
                    .FirstOrDefaultAsync(r => r.SellerId == sellerId && r.Year == previousYear && r.Month == previousMonth);

                var carryForward = previousReport?.CarryForwardToNext ?? 0;

                report = new SellerMonthlyReport
                {
                    SellerId = sellerId,
                    Year = year,
                    Month = month,
                    ReportNumber = GenerateReportNumber(sellerId, year, month),
                    CommissionRate = seller.CommissionRate,
                    CarryForwardFromPrevious = carryForward,
                    PeriodStart = new DateTime(year, month, 1),
                    PeriodEnd = new DateTime(year, month, DateTime.DaysInMonth(year, month)),
                    Status = MonthlyReportStatus.Open,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.SellerMonthlyReports.Add(report);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new monthly report {ReportNumber} for seller {SellerId}", report.ReportNumber, sellerId);
            }

            return report;
        }

        public async Task<SellerMonthlyReport?> GetReportByIdAsync(int reportId)
        {
            return await _context.SellerMonthlyReports
                .Include(r => r.Seller)
                .Include(r => r.ManualSellerPayout)
                .Include(r => r.FinalizedBy)
                .FirstOrDefaultAsync(r => r.Id == reportId);
        }

        public async Task<SellerMonthlyReport?> GetReportAsync(int sellerId, int year, int month)
        {
            return await _context.SellerMonthlyReports
                .Include(r => r.Seller)
                .FirstOrDefaultAsync(r => r.SellerId == sellerId && r.Year == year && r.Month == month);
        }

        public async Task<IEnumerable<SellerMonthlyReport>> GetSellerReportsAsync(int sellerId, int page = 1, int pageSize = 12)
        {
            return await _context.SellerMonthlyReports
                .Include(r => r.ManualSellerPayout)
                .Where(r => r.SellerId == sellerId)
                .OrderByDescending(r => r.Year)
                .ThenByDescending(r => r.Month)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetSellerReportsCountAsync(int sellerId)
        {
            return await _context.SellerMonthlyReports
                .CountAsync(r => r.SellerId == sellerId);
        }

        public async Task<IEnumerable<SellerMonthlyReport>> GetAllReportsAsync(int year, int month, MonthlyReportStatus? status = null, int page = 1, int pageSize = 20)
        {
            var query = _context.SellerMonthlyReports
                .Include(r => r.Seller)
                .Where(r => r.Year == year && r.Month == month);

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            return await query
                .OrderByDescending(r => r.NetPayable)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetReportsCountAsync(int year, int month, MonthlyReportStatus? status = null)
        {
            var query = _context.SellerMonthlyReports.Where(r => r.Year == year && r.Month == month);

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            return await query.CountAsync();
        }

        #endregion

        #region Order Item Tracking

        public async Task AddOrderItemToReportAsync(int orderItemId, int orderId)
        {
            try
            {
                var orderItem = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .FirstOrDefaultAsync(oi => oi.Id == orderItemId);

                if (orderItem?.SellerId == null)
                {
                    _logger.LogWarning("Order item {OrderItemId} has no seller", orderItemId);
                    return;
                }

                var order = await _context.Orders.FindAsync(orderId);
                if (order == null || !order.IsPaymentReceived)
                {
                    _logger.LogWarning("Order {OrderId} not found or payment not received", orderId);
                    return;
                }

                // Check if already added
                var existing = await _context.MonthlyReportOrderItems
                    .AnyAsync(roi => roi.OrderItemId == orderItemId);

                if (existing)
                {
                    _logger.LogDebug("Order item {OrderItemId} already added to a report", orderItemId);
                    return;
                }

                var report = await GetOrCreateCurrentReportAsync(orderItem.SellerId.Value);
                var seller = await _context.Sellers.FindAsync(orderItem.SellerId.Value);

                var commissionRate = seller?.CommissionRate ?? 5m;
                var commissionAmount = Math.Round(orderItem.TotalPrice * (commissionRate / 100), 2);
                var netAmount = Math.Round(orderItem.TotalPrice - commissionAmount, 2);

                var reportItem = new MonthlyReportOrderItem
                {
                    MonthlyReportId = report.Id,
                    OrderItemId = orderItemId,
                    OrderId = orderId,
                    ItemAmount = orderItem.TotalPrice,
                    CommissionRate = commissionRate,
                    CommissionAmount = commissionAmount,
                    NetAmount = netAmount,
                    Status = ReportItemStatus.Delivered,
                    DeliveredAt = order.DeliveredAt ?? DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.MonthlyReportOrderItems.Add(reportItem);
                await _context.SaveChangesAsync();

                // Recalculate report totals
                await RecalculateReportAsync(report.Id);

                _logger.LogInformation("Added order item {OrderItemId} to report {ReportId}", orderItemId, report.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding order item {OrderItemId} to report", orderItemId);
                throw;
            }
        }

        public async Task ProcessReturnDeductionAsync(int orderItemId, int orderId)
        {
            try
            {
                var reportItem = await _context.MonthlyReportOrderItems
                    .Include(roi => roi.MonthlyReport)
                    .FirstOrDefaultAsync(roi => roi.OrderItemId == orderItemId);

                if (reportItem == null)
                {
                    _logger.LogWarning("Order item {OrderItemId} was never added to a report, skipping return deduction", orderItemId);
                    return;
                }

                if (reportItem.Status != ReportItemStatus.Delivered)
                {
                    _logger.LogWarning("Order item {OrderItemId} already has status {Status}", orderItemId, reportItem.Status);
                    return;
                }

                var sellerId = reportItem.MonthlyReport?.SellerId;
                if (sellerId == null) return;

                // Determine which report to apply deduction to
                int deductionReportId;
                if (reportItem.MonthlyReport?.Status == MonthlyReportStatus.Open)
                {
                    // Original report is still open, deduct from it
                    deductionReportId = reportItem.MonthlyReportId;
                }
                else
                {
                    // Original report is finalized, add deduction to current month's report
                    var currentReport = await GetOrCreateCurrentReportAsync(sellerId.Value);
                    deductionReportId = currentReport.Id;
                }

                reportItem.Status = ReportItemStatus.Returned;
                reportItem.DeductionAmount = reportItem.NetAmount;
                reportItem.DeductionReportId = deductionReportId;
                reportItem.DeductionReason = "Order returned by customer";
                reportItem.ReturnedAt = DateTime.UtcNow;
                reportItem.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Recalculate both reports if different
                await RecalculateReportAsync(reportItem.MonthlyReportId);
                if (deductionReportId != reportItem.MonthlyReportId)
                {
                    await RecalculateReportAsync(deductionReportId);
                }

                _logger.LogInformation("Processed return deduction for order item {OrderItemId}", orderItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing return deduction for order item {OrderItemId}", orderItemId);
                throw;
            }
        }

        public async Task ProcessRefundDeductionAsync(int orderItemId, int orderId)
        {
            try
            {
                var reportItem = await _context.MonthlyReportOrderItems
                    .Include(roi => roi.MonthlyReport)
                    .FirstOrDefaultAsync(roi => roi.OrderItemId == orderItemId);

                if (reportItem == null)
                {
                    _logger.LogWarning("Order item {OrderItemId} was never added to a report, skipping refund deduction", orderItemId);
                    return;
                }

                if (reportItem.Status != ReportItemStatus.Delivered)
                {
                    _logger.LogWarning("Order item {OrderItemId} already has status {Status}", orderItemId, reportItem.Status);
                    return;
                }

                var sellerId = reportItem.MonthlyReport?.SellerId;
                if (sellerId == null) return;

                // Determine which report to apply deduction to
                int deductionReportId;
                if (reportItem.MonthlyReport?.Status == MonthlyReportStatus.Open)
                {
                    deductionReportId = reportItem.MonthlyReportId;
                }
                else
                {
                    var currentReport = await GetOrCreateCurrentReportAsync(sellerId.Value);
                    deductionReportId = currentReport.Id;
                }

                reportItem.Status = ReportItemStatus.Refunded;
                reportItem.DeductionAmount = reportItem.NetAmount;
                reportItem.DeductionReportId = deductionReportId;
                reportItem.DeductionReason = "Order refunded to customer";
                reportItem.RefundedAt = DateTime.UtcNow;
                reportItem.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                await RecalculateReportAsync(reportItem.MonthlyReportId);
                if (deductionReportId != reportItem.MonthlyReportId)
                {
                    await RecalculateReportAsync(deductionReportId);
                }

                _logger.LogInformation("Processed refund deduction for order item {OrderItemId}", orderItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund deduction for order item {OrderItemId}", orderItemId);
                throw;
            }
        }

        #endregion

        #region Monthly Finalization

        public async Task FinalizeMonthAsync(int year, int month, string? userId = null)
        {
            var settings = await _context.SellerPaymentSettings.FirstOrDefaultAsync();
            var minimumPayout = settings?.MinimumPayoutAmount ?? 500m;

            var openReports = await _context.SellerMonthlyReports
                .Where(r => r.Year == year && r.Month == month && r.Status == MonthlyReportStatus.Open)
                .ToListAsync();

            foreach (var report in openReports)
            {
                await RecalculateReportAsync(report.Id);
                await _context.Entry(report).ReloadAsync();

                if (report.NetPayable >= minimumPayout)
                {
                    report.PayoutAmount = report.NetPayable;
                    report.CarryForwardToNext = 0;
                    report.Status = MonthlyReportStatus.PendingPayout;
                }
                else
                {
                    report.PayoutAmount = 0;
                    report.CarryForwardToNext = report.NetPayable > 0 ? report.NetPayable : 0;
                    report.Status = report.NetPayable > 0 ? MonthlyReportStatus.CarriedForward : MonthlyReportStatus.Finalized;
                }

                report.FinalizedAt = DateTime.UtcNow;
                report.FinalizedByUserId = userId;
                report.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Create next month's reports with carryforward for those who have balance
            var nextMonth = month == 12 ? 1 : month + 1;
            var nextYear = month == 12 ? year + 1 : year;

            foreach (var report in openReports.Where(r => r.CarryForwardToNext > 0))
            {
                await GetOrCreateCurrentReportAsync(report.SellerId);
            }

            _logger.LogInformation("Finalized {Count} reports for {Month}/{Year}", openReports.Count, month, year);
        }

        public async Task FinalizeSellerReportAsync(int reportId, string? userId = null)
        {
            var report = await _context.SellerMonthlyReports.FindAsync(reportId);
            if (report == null)
                throw new InvalidOperationException($"Report with ID {reportId} not found");

            if (report.Status != MonthlyReportStatus.Open)
                throw new InvalidOperationException("Only open reports can be finalized");

            var settings = await _context.SellerPaymentSettings.FirstOrDefaultAsync();
            var minimumPayout = settings?.MinimumPayoutAmount ?? 500m;

            await RecalculateReportAsync(reportId);
            await _context.Entry(report).ReloadAsync();

            if (report.NetPayable >= minimumPayout)
            {
                report.PayoutAmount = report.NetPayable;
                report.CarryForwardToNext = 0;
                report.Status = MonthlyReportStatus.PendingPayout;
            }
            else
            {
                report.PayoutAmount = 0;
                report.CarryForwardToNext = report.NetPayable > 0 ? report.NetPayable : 0;
                report.Status = report.NetPayable > 0 ? MonthlyReportStatus.CarriedForward : MonthlyReportStatus.Finalized;
            }

            report.FinalizedAt = DateTime.UtcNow;
            report.FinalizedByUserId = userId;
            report.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Finalized report {ReportId} with status {Status}", reportId, report.Status);
        }

        public async Task<MonthEndProcessingResult> ProcessMonthEndAsync(string? triggeredByUserId = null)
        {
            var result = new MonthEndProcessingResult();

            try
            {
                var now = DateTime.UtcNow;
                var previousMonth = now.Month == 1 ? 12 : now.Month - 1;
                var previousYear = now.Month == 1 ? now.Year - 1 : now.Year;

                result.Year = previousYear;
                result.Month = previousMonth;

                _logger.LogInformation("Starting month-end processing for {Month}/{Year}", previousMonth, previousYear);

                await FinalizeMonthAsync(previousYear, previousMonth, triggeredByUserId);

                // Get statistics
                var processedReports = await _context.SellerMonthlyReports
                    .Where(r => r.Year == previousYear && r.Month == previousMonth)
                    .ToListAsync();

                result.TotalReportsProcessed = processedReports.Count;
                result.ReportsEligibleForPayout = processedReports.Count(r => r.Status == MonthlyReportStatus.PendingPayout);
                result.ReportsCarriedForward = processedReports.Count(r => r.Status == MonthlyReportStatus.CarriedForward);
                result.TotalAmountEligible = processedReports.Where(r => r.Status == MonthlyReportStatus.PendingPayout).Sum(r => r.PayoutAmount);
                result.TotalAmountCarriedForward = processedReports.Sum(r => r.CarryForwardToNext);
                result.Success = true;

                _logger.LogInformation("Month-end processing completed: {Processed} reports, {Eligible} eligible, {Carried} carried forward",
                    result.TotalReportsProcessed, result.ReportsEligibleForPayout, result.ReportsCarriedForward);

                // Send notifications to sellers
                foreach (var report in processedReports.Where(r => r.Status == MonthlyReportStatus.PendingPayout))
                {
                    var seller = await _context.Sellers.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == report.SellerId);
                    if (seller?.UserId != null)
                    {
                        await _notificationService.CreateNotificationAsync(new UserNotification
                        {
                            UserId = seller.UserId,
                            Title = "Payment Report Ready",
                            Message = $"Your payment report for {new DateTime(previousYear, previousMonth, 1):MMMM yyyy} is ready. Amount: ৳{report.PayoutAmount:N0}",
                            Type = "seller",
                            Icon = "fa-money-bill-wave",
                            IconColor = "text-success",
                            Link = "/Seller/Reports",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during month-end processing");
                result.Success = false;
                result.Errors.Add(ex.Message);
            }

            return result;
        }

        #endregion

        #region Recalculation

        public async Task RecalculateReportAsync(int reportId)
        {
            var report = await _context.SellerMonthlyReports
                .Include(r => r.ReportOrderItems)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null) return;

            // Get all items in this report
            var items = report.ReportOrderItems;

            // Sum delivered items (not returned/refunded)
            var deliveredItems = items.Where(i => i.Status == ReportItemStatus.Delivered).ToList();
            report.TotalSales = Math.Round(deliveredItems.Sum(i => i.ItemAmount), 2);
            report.CommissionAmount = Math.Round(deliveredItems.Sum(i => i.CommissionAmount), 2);
            report.TotalItemsDelivered = deliveredItems.Count;
            report.TotalOrders = deliveredItems.Select(i => i.OrderId).Distinct().Count();

            // Sum deductions applied to this report
            var deductions = await _context.MonthlyReportOrderItems
                .Where(i => i.DeductionReportId == reportId)
                .ToListAsync();

            report.RefundDeductions = Math.Round(deductions.Where(i => i.Status == ReportItemStatus.Refunded).Sum(i => i.DeductionAmount), 2);
            report.ReturnDeductions = Math.Round(deductions.Where(i => i.Status == ReportItemStatus.Returned).Sum(i => i.DeductionAmount), 2);

            // Calculate net payable
            var grossPayable = report.TotalSales - report.CommissionAmount;
            report.NetPayable = Math.Round(grossPayable - report.RefundDeductions - report.ReturnDeductions
                               + report.CarryForwardFromPrevious + report.Adjustments, 2);

            if (report.NetPayable < 0) report.NetPayable = 0;

            report.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogDebug("Recalculated report {ReportId}: Sales={Sales}, Commission={Commission}, NetPayable={NetPayable}",
                reportId, report.TotalSales, report.CommissionAmount, report.NetPayable);
        }

        #endregion

        #region Payout Integration

        public async Task<ManualSellerPayout?> CreatePayoutFromReportAsync(int reportId, string processedByUserId)
        {
            var report = await _context.SellerMonthlyReports
                .Include(r => r.Seller)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report == null)
                throw new InvalidOperationException($"Report with ID {reportId} not found");

            if (report.Status != MonthlyReportStatus.PendingPayout)
                throw new InvalidOperationException("Only reports with PendingPayout status can create payouts");

            var seller = report.Seller;
            if (seller == null)
                throw new InvalidOperationException("Seller not found for report");

            // Create manual payout
            var payout = new ManualSellerPayout
            {
                PayoutNumber = $"PO{DateTime.UtcNow:yyyyMMdd}{new Random().Next(1000, 9999)}",
                SellerId = report.SellerId,
                Amount = report.PayoutAmount,
                PaymentMethod = ManualPayoutMethod.BankTransfer, // Default, can be changed
                Status = ManualPayoutStatus.Pending,
                TotalSales = report.TotalSales,
                CommissionRate = report.CommissionRate,
                CommissionAmount = report.CommissionAmount,
                PeriodStart = report.PeriodStart,
                PeriodEnd = report.PeriodEnd,
                ProcessedById = processedByUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ManualSellerPayouts.Add(payout);
            await _context.SaveChangesAsync();

            // Link payout to report
            report.ManualSellerPayoutId = payout.Id;
            report.Status = MonthlyReportStatus.PayoutCreated;
            report.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created payout {PayoutNumber} from report {ReportId}", payout.PayoutNumber, reportId);

            return payout;
        }

        public async Task LinkPayoutToReportAsync(int reportId, int payoutId)
        {
            var report = await _context.SellerMonthlyReports.FindAsync(reportId);
            if (report == null)
                throw new InvalidOperationException($"Report with ID {reportId} not found");

            report.ManualSellerPayoutId = payoutId;
            report.Status = MonthlyReportStatus.PayoutCreated;
            report.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        public async Task MarkReportAsPaidAsync(int reportId)
        {
            var report = await _context.SellerMonthlyReports.FindAsync(reportId);
            if (report == null)
                throw new InvalidOperationException($"Report with ID {reportId} not found");

            report.Status = MonthlyReportStatus.Paid;
            report.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked report {ReportId} as paid", reportId);
        }

        #endregion

        #region Statistics

        public async Task<SellerMonthlyReportSummary> GetMonthlySummaryAsync(int year, int month)
        {
            var reports = await _context.SellerMonthlyReports
                .Where(r => r.Year == year && r.Month == month)
                .ToListAsync();

            return new SellerMonthlyReportSummary
            {
                Year = year,
                Month = month,
                TotalSellers = reports.Count,
                OpenReports = reports.Count(r => r.Status == MonthlyReportStatus.Open),
                EligibleForPayout = reports.Count(r => r.Status == MonthlyReportStatus.PendingPayout),
                CarriedForward = reports.Count(r => r.Status == MonthlyReportStatus.CarriedForward),
                PaidReports = reports.Count(r => r.Status == MonthlyReportStatus.Paid),
                TotalSales = reports.Sum(r => r.TotalSales),
                TotalCommissions = reports.Sum(r => r.CommissionAmount),
                TotalDeductions = reports.Sum(r => r.RefundDeductions + r.ReturnDeductions),
                TotalPayable = reports.Sum(r => r.NetPayable),
                TotalCarriedForward = reports.Sum(r => r.CarryForwardToNext),
                TotalPaid = reports.Where(r => r.Status == MonthlyReportStatus.Paid).Sum(r => r.PayoutAmount)
            };
        }

        public async Task<decimal> GetSellerPendingBalanceAsync(int sellerId)
        {
            return await _context.SellerMonthlyReports
                .Where(r => r.SellerId == sellerId &&
                           (r.Status == MonthlyReportStatus.Open ||
                            r.Status == MonthlyReportStatus.PendingPayout ||
                            r.Status == MonthlyReportStatus.PayoutCreated))
                .SumAsync(r => r.NetPayable);
        }

        public async Task<IEnumerable<SellerMonthlyReport>> GetReportsPendingPayoutAsync()
        {
            return await _context.SellerMonthlyReports
                .Include(r => r.Seller)
                .Where(r => r.Status == MonthlyReportStatus.PendingPayout)
                .OrderByDescending(r => r.PayoutAmount)
                .ToListAsync();
        }

        #endregion

        #region Adjustments

        public async Task AddAdjustmentAsync(int reportId, decimal amount, string reason, string userId)
        {
            var report = await _context.SellerMonthlyReports.FindAsync(reportId);
            if (report == null)
                throw new InvalidOperationException($"Report with ID {reportId} not found");

            if (report.Status != MonthlyReportStatus.Open)
                throw new InvalidOperationException("Adjustments can only be added to open reports");

            report.Adjustments += amount;
            report.AdminNotes = string.IsNullOrEmpty(report.AdminNotes)
                ? $"Adjustment: {amount:N2} - {reason} (by {userId})"
                : $"{report.AdminNotes}\nAdjustment: {amount:N2} - {reason} (by {userId})";
            report.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await RecalculateReportAsync(reportId);

            _logger.LogInformation("Added adjustment {Amount} to report {ReportId}: {Reason}", amount, reportId, reason);
        }

        #endregion

        #region Utilities

        public string GenerateReportNumber(int sellerId, int year, int month)
        {
            return $"RPT-{year}{month:D2}-{sellerId:D4}";
        }

        public async Task<IEnumerable<MonthlyReportOrderItem>> GetReportItemsAsync(int reportId, int page = 1, int pageSize = 50)
        {
            return await _context.MonthlyReportOrderItems
                .Include(roi => roi.OrderItem)
                    .ThenInclude(oi => oi!.Product)
                .Include(roi => roi.Order)
                .Where(roi => roi.MonthlyReportId == reportId)
                .OrderByDescending(roi => roi.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetReportItemsCountAsync(int reportId)
        {
            return await _context.MonthlyReportOrderItems
                .CountAsync(roi => roi.MonthlyReportId == reportId);
        }

        #endregion

        #region Historical Data Seeding

        public async Task<HistoricalDataSeedResult> SeedHistoricalDataAsync()
        {
            var result = new HistoricalDataSeedResult();
            _logger.LogInformation("Starting historical data seeding for monthly reports");

            try
            {
                // Get all delivered orders with payment received that are not yet in reports
                var existingOrderItemIds = await _context.MonthlyReportOrderItems
                    .Select(roi => roi.OrderItemId)
                    .ToListAsync();

                var deliveredOrders = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                            .ThenInclude(p => p!.Seller)
                    .Where(o => o.Status == OrderStatus.Delivered &&
                               o.IsPaymentReceived &&
                               o.DeliveredAt != null)
                    .OrderBy(o => o.DeliveredAt)
                    .ToListAsync();

                foreach (var order in deliveredOrders)
                {
                    try
                    {
                        var deliveredDate = order.DeliveredAt ?? order.UpdatedAt;

                        foreach (var item in order.OrderItems.Where(oi => oi.SellerId != null && !existingOrderItemIds.Contains(oi.Id)))
                        {
                            var sellerId = item.SellerId!.Value;
                            var itemYear = deliveredDate.Year;
                            var itemMonth = deliveredDate.Month;

                            // Get or create report for the month the order was delivered
                            var report = await _context.SellerMonthlyReports
                                .FirstOrDefaultAsync(r => r.SellerId == sellerId && r.Year == itemYear && r.Month == itemMonth);

                            if (report == null)
                            {
                                var seller = await _context.Sellers.FindAsync(sellerId);
                                if (seller == null) continue;

                                var periodStart = new DateTime(itemYear, itemMonth, 1);
                                var periodEnd = periodStart.AddMonths(1).AddDays(-1);

                                report = new SellerMonthlyReport
                                {
                                    SellerId = sellerId,
                                    Year = itemYear,
                                    Month = itemMonth,
                                    ReportNumber = GenerateReportNumber(sellerId, itemYear, itemMonth),
                                    CommissionRate = seller.CommissionRate,
                                    PeriodStart = periodStart,
                                    PeriodEnd = periodEnd,
                                    // Historical reports should be finalized since month has passed
                                    Status = IsCurrentMonth(itemYear, itemMonth) ? MonthlyReportStatus.Open : MonthlyReportStatus.Finalized,
                                    CreatedAt = DateTime.UtcNow
                                };

                                _context.SellerMonthlyReports.Add(report);
                                await _context.SaveChangesAsync();
                                result.ReportsCreated++;
                            }
                            else
                            {
                                result.ReportsUpdated++;
                            }

                            // Check if item already exists
                            var existingItem = await _context.MonthlyReportOrderItems
                                .FirstOrDefaultAsync(roi => roi.OrderItemId == item.Id);

                            if (existingItem == null)
                            {
                                var seller = await _context.Sellers.FindAsync(sellerId);
                                var commissionRate = seller?.CommissionRate ?? 5m;
                                var commissionAmount = Math.Round(item.TotalPrice * (commissionRate / 100), 2);

                                var reportItem = new MonthlyReportOrderItem
                                {
                                    MonthlyReportId = report.Id,
                                    OrderItemId = item.Id,
                                    OrderId = order.Id,
                                    ItemAmount = item.TotalPrice,
                                    CommissionRate = commissionRate,
                                    CommissionAmount = commissionAmount,
                                    NetAmount = Math.Round(item.TotalPrice - commissionAmount, 2),
                                    Status = ReportItemStatus.Delivered,
                                    DeliveredAt = deliveredDate,
                                    CreatedAt = DateTime.UtcNow
                                };

                                _context.MonthlyReportOrderItems.Add(reportItem);
                                result.ItemsAdded++;
                            }
                        }

                        result.OrdersProcessed++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error processing order {order.Id}: {ex.Message}");
                        _logger.LogError(ex, "Error processing historical order {OrderId}", order.Id);
                    }
                }

                await _context.SaveChangesAsync();

                // Recalculate all affected reports
                var affectedReportIds = await _context.SellerMonthlyReports
                    .Select(r => r.Id)
                    .ToListAsync();

                foreach (var reportId in affectedReportIds)
                {
                    try
                    {
                        await RecalculateReportAsync(reportId);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error recalculating report {reportId}: {ex.Message}");
                        _logger.LogError(ex, "Error recalculating report {ReportId}", reportId);
                    }
                }

                // Process carryforward for historical reports
                var sellers = await _context.Sellers.Select(s => s.Id).ToListAsync();
                foreach (var sellerId in sellers)
                {
                    try
                    {
                        await ProcessHistoricalCarryforwardAsync(sellerId);
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error processing carryforward for seller {sellerId}: {ex.Message}");
                    }
                }

                result.Success = !result.Errors.Any();
                _logger.LogInformation("Historical data seeding completed. Orders: {Orders}, Items: {Items}, Reports Created: {Created}, Updated: {Updated}",
                    result.OrdersProcessed, result.ItemsAdded, result.ReportsCreated, result.ReportsUpdated);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during historical data seeding");
                result.Success = false;
                result.Errors.Add($"Fatal error: {ex.Message}");
                return result;
            }
        }

        private bool IsCurrentMonth(int year, int month)
        {
            var now = DateTime.UtcNow;
            return year == now.Year && month == now.Month;
        }

        private async Task ProcessHistoricalCarryforwardAsync(int sellerId)
        {
            var reports = await _context.SellerMonthlyReports
                .Where(r => r.SellerId == sellerId && r.Status == MonthlyReportStatus.Finalized)
                .OrderBy(r => r.Year)
                .ThenBy(r => r.Month)
                .ToListAsync();

            decimal carryForward = 0;

            foreach (var report in reports)
            {
                report.CarryForwardFromPrevious = carryForward;
                report.NetPayable = Math.Round(report.TotalSales - report.CommissionAmount - report.RefundDeductions - report.ReturnDeductions + report.Adjustments + report.CarryForwardFromPrevious, 2);

                if (report.NetPayable >= 500)
                {
                    report.PayoutAmount = report.NetPayable;
                    report.CarryForwardToNext = 0;
                    report.Status = MonthlyReportStatus.PendingPayout;
                    carryForward = 0;
                }
                else
                {
                    report.CarryForwardToNext = report.NetPayable;
                    report.PayoutAmount = 0;
                    report.Status = MonthlyReportStatus.CarriedForward;
                    carryForward = report.NetPayable;
                }

                report.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        #endregion
    }
}
