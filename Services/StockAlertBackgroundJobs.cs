namespace Bangaliyana.Services
{
    /// <summary>
    /// Background jobs for stock-related alerts and notifications.
    /// </summary>
    public class StockAlertBackgroundJobs
    {
        private readonly IStockAlertService _stockAlertService;
        private readonly ILogger<StockAlertBackgroundJobs> _logger;

        public StockAlertBackgroundJobs(
            IStockAlertService stockAlertService,
            ILogger<StockAlertBackgroundJobs> logger)
        {
            _stockAlertService = stockAlertService;
            _logger = logger;
        }

        /// <summary>
        /// Processes daily low stock alerts for all sellers.
        /// This job is scheduled to run daily at 9:00 AM Bangladesh time (3:00 AM UTC).
        /// </summary>
        public async Task ProcessDailyLowStockAlertsAsync()
        {
            _logger.LogInformation("Starting daily low stock alert processing at {Time}", DateTime.UtcNow);

            try
            {
                await _stockAlertService.CheckAndSendLowStockAlertsAsync();
                _logger.LogInformation("Daily low stock alert processing completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during daily low stock alert processing");
                throw; // Re-throw to mark the job as failed in Hangfire
            }
        }
    }
}
