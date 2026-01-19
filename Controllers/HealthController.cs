using Bangaliyana.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Controllers
{
    /// <summary>
    /// Simple health check endpoint for deployment diagnostics
    /// </summary>
    public class HealthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<HealthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        [Route("/health")]
        public async Task<IActionResult> Index()
        {
            var result = new
            {
                Status = "OK",
                Timestamp = DateTime.UtcNow,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                Database = await CheckDatabaseAsync(),
                Configuration = new
                {
                    HangfireEnabled = _configuration.GetValue<bool>("Hangfire:Enabled", true),
                    BackgroundServicesEnabled = _configuration.GetValue<bool>("BackgroundServices:Enabled", true),
                    BaseUrl = _configuration["App:BaseUrl"] ?? "Not configured"
                }
            };

            return Json(result);
        }

        private async Task<object> CheckDatabaseAsync()
        {
            try
            {
                // Try to connect to database
                var canConnect = await _context.Database.CanConnectAsync();

                if (!canConnect)
                {
                    return new { Status = "Error", Message = "Cannot connect to database" };
                }

                // Check if migrations are applied
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();

                return new
                {
                    Status = "Connected",
                    AppliedMigrations = appliedMigrations.Count(),
                    PendingMigrations = pendingMigrations.Count(),
                    HasPendingMigrations = pendingMigrations.Any()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database health check failed");
                return new { Status = "Error", Message = ex.Message };
            }
        }
    }
}
