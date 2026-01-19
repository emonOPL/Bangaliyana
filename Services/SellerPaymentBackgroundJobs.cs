namespace Bangaliyana.Services
{
    public class SellerPaymentBackgroundJobs
    {
        private readonly ISellerPaymentService _paymentService;
        private readonly ISellerMonthlyReportService _monthlyReportService;
        private readonly ILogger<SellerPaymentBackgroundJobs> _logger;

        public SellerPaymentBackgroundJobs(
            ISellerPaymentService paymentService,
            ISellerMonthlyReportService monthlyReportService,
            ILogger<SellerPaymentBackgroundJobs> logger)
        {
            _paymentService = paymentService;
            _monthlyReportService = monthlyReportService;
            _logger = logger;
        }

        /// <summary>
        /// Processes all monthly payments for eligible sellers.
        /// This job is scheduled to run on the 1st of every month at 12:00 AM Bangladesh time (UTC+6).
        /// </summary>
        public async Task ProcessMonthlyPaymentsAsync(string? triggeredByUserId = null)
        {
            _logger.LogInformation("Starting scheduled monthly payment processing at {Time}", DateTime.UtcNow);

            try
            {
                await _paymentService.ProcessAllMonthlyPaymentsAsync(triggeredByUserId);
                _logger.LogInformation("Monthly payment processing completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during monthly payment processing");
                throw; // Re-throw to mark the job as failed in Hangfire
            }
        }

        /// <summary>
        /// Processes month-end finalization for all seller monthly reports.
        /// This job runs on the 1st of every month at 12:01 AM Bangladesh time (UTC+6).
        /// It finalizes the previous month's reports and creates new reports for the current month.
        /// </summary>
        public async Task ProcessMonthEndReportsAsync(string? triggeredByUserId = null)
        {
            _logger.LogInformation("Starting month-end report processing at {Time}", DateTime.UtcNow);

            try
            {
                var result = await _monthlyReportService.ProcessMonthEndAsync(triggeredByUserId);

                if (result.Success)
                {
                    _logger.LogInformation(
                        "Month-end processing completed successfully for {Year}-{Month}. " +
                        "Reports processed: {TotalReports}, Eligible for payout: {EligibleCount} (৳{EligibleAmount:N0}), " +
                        "Carried forward: {CarriedCount} (৳{CarriedAmount:N0})",
                        result.Year,
                        result.Month,
                        result.TotalReportsProcessed,
                        result.ReportsEligibleForPayout,
                        result.TotalAmountEligible,
                        result.ReportsCarriedForward,
                        result.TotalAmountCarriedForward);
                }
                else
                {
                    _logger.LogWarning(
                        "Month-end processing completed with errors for {Year}-{Month}. Errors: {Errors}",
                        result.Year,
                        result.Month,
                        string.Join("; ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during month-end report processing");
                throw; // Re-throw to mark the job as failed in Hangfire
            }
        }
    }
}
