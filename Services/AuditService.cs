using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace Bangaliyana.Services
{
    /// <summary>
    /// Service implementation for managing audit logs
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditService> _logger;

        public AuditService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Log an action
        /// </summary>
        public async Task LogAsync(AuditLog log)
        {
            try
            {
                // Add HTTP context info if available
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    log.IpAddress ??= GetClientIpAddress(httpContext);
                    log.UserAgent ??= httpContext.Request.Headers["User-Agent"].ToString();
                    log.RequestUrl ??= $"{httpContext.Request.Path}{httpContext.Request.QueryString}";
                    log.HttpMethod ??= httpContext.Request.Method;
                }

                log.CreatedAt = DateTime.UtcNow;
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save audit log");
            }
        }

        /// <summary>
        /// Log an action with simplified parameters
        /// </summary>
        public async Task LogAsync(
            string userId,
            string userEmail,
            AuditActionType actionType,
            AuditModule module,
            string description,
            string? entityType = null,
            string? entityId = null,
            string? entityName = null,
            object? oldValues = null,
            object? newValues = null,
            bool isSuccess = true,
            string? errorMessage = null)
        {
            var log = new AuditLog
            {
                UserId = userId,
                UserEmail = userEmail,
                ActionType = actionType,
                Module = module,
                Description = description,
                EntityType = entityType,
                EntityId = entityId,
                EntityName = entityName,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            await LogAsync(log);
        }

        /// <summary>
        /// Get audit logs with filtering and pagination
        /// </summary>
        public async Task<(List<AuditLog> Logs, int TotalCount)> GetLogsAsync(
            string? userId = null,
            AuditActionType? actionType = null,
            AuditModule? module = null,
            string? entityType = null,
            string? entityId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 50)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UserId == userId);

            if (actionType.HasValue)
                query = query.Where(l => l.ActionType == actionType.Value);

            if (module.HasValue)
                query = query.Where(l => l.Module == module.Value);

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(l => l.EntityType == entityType);

            if (!string.IsNullOrEmpty(entityId))
                query = query.Where(l => l.EntityId == entityId);

            if (fromDate.HasValue)
                query = query.Where(l => l.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(l => l.CreatedAt <= toDate.Value.AddDays(1));

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(l =>
                    l.Description.ToLower().Contains(term) ||
                    l.UserEmail.ToLower().Contains(term) ||
                    (l.EntityName != null && l.EntityName.ToLower().Contains(term)));
            }

            var totalCount = await query.CountAsync();

            var logs = await query
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(l => l.User)
                .ToListAsync();

            return (logs, totalCount);
        }

        /// <summary>
        /// Get a single audit log by ID
        /// </summary>
        public async Task<AuditLog?> GetLogByIdAsync(int id)
        {
            return await _context.AuditLogs
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        /// <summary>
        /// Get audit logs for a specific entity
        /// </summary>
        public async Task<List<AuditLog>> GetEntityHistoryAsync(string entityType, string entityId)
        {
            return await _context.AuditLogs
                .Where(l => l.EntityType == entityType && l.EntityId == entityId)
                .OrderByDescending(l => l.CreatedAt)
                .Take(100)
                .ToListAsync();
        }

        /// <summary>
        /// Get recent activity for a user
        /// </summary>
        public async Task<List<AuditLog>> GetUserActivityAsync(string userId, int count = 20)
        {
            return await _context.AuditLogs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        /// <summary>
        /// Get audit statistics
        /// </summary>
        public async Task<AuditStatistics> GetStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var today = DateTime.UtcNow.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var query = _context.AuditLogs.AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(l => l.CreatedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(l => l.CreatedAt <= toDate.Value.AddDays(1));

            var stats = new AuditStatistics
            {
                TotalLogs = await query.CountAsync(),
                TodayLogs = await _context.AuditLogs.CountAsync(l => l.CreatedAt >= today),
                ThisWeekLogs = await _context.AuditLogs.CountAsync(l => l.CreatedAt >= weekStart),
                ThisMonthLogs = await _context.AuditLogs.CountAsync(l => l.CreatedAt >= monthStart)
            };

            // Action type counts
            var actionCounts = await query
                .GroupBy(l => l.ActionType)
                .Select(g => new { ActionType = g.Key, Count = g.Count() })
                .ToListAsync();
            stats.ActionTypeCounts = actionCounts.ToDictionary(x => x.ActionType, x => x.Count);

            // Module counts
            var moduleCounts = await query
                .GroupBy(l => l.Module)
                .Select(g => new { Module = g.Key, Count = g.Count() })
                .ToListAsync();
            stats.ModuleCounts = moduleCounts.ToDictionary(x => x.Module, x => x.Count);

            // Top active users (last 7 days)
            stats.TopActiveUsers = await _context.AuditLogs
                .Where(l => l.CreatedAt >= today.AddDays(-7))
                .GroupBy(l => new { l.UserId, l.UserEmail, l.UserName })
                .Select(g => new UserActivitySummary
                {
                    UserId = g.Key.UserId,
                    UserEmail = g.Key.UserEmail,
                    UserName = g.Key.UserName,
                    ActionCount = g.Count()
                })
                .OrderByDescending(x => x.ActionCount)
                .Take(10)
                .ToListAsync();

            // Recent activities
            stats.RecentActivities = await _context.AuditLogs
                .OrderByDescending(l => l.CreatedAt)
                .Take(10)
                .Select(l => new RecentActivity
                {
                    UserEmail = l.UserEmail,
                    Description = l.Description,
                    Module = l.Module,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return stats;
        }

        /// <summary>
        /// Delete old audit logs
        /// </summary>
        public async Task<int> DeleteOldLogsAsync(int daysToKeep = 90)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
            var oldLogs = await _context.AuditLogs
                .Where(l => l.CreatedAt < cutoffDate)
                .ToListAsync();

            _context.AuditLogs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted {Count} audit logs older than {Days} days", oldLogs.Count, daysToKeep);
            return oldLogs.Count;
        }

        /// <summary>
        /// Export audit logs to CSV
        /// </summary>
        public async Task<byte[]> ExportToCsvAsync(
            string? userId = null,
            AuditActionType? actionType = null,
            AuditModule? module = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var (logs, _) = await GetLogsAsync(
                userId: userId,
                actionType: actionType,
                module: module,
                fromDate: fromDate,
                toDate: toDate,
                page: 1,
                pageSize: 10000);

            var sb = new StringBuilder();
            sb.AppendLine("ID,Date,User Email,User Name,Action Type,Module,Description,Entity Type,Entity ID,Entity Name,Success,IP Address");

            foreach (var log in logs)
            {
                sb.AppendLine($"{log.Id}," +
                    $"\"{log.CreatedAt:yyyy-MM-dd HH:mm:ss}\"," +
                    $"\"{EscapeCsv(log.UserEmail)}\"," +
                    $"\"{EscapeCsv(log.UserName)}\"," +
                    $"\"{log.ActionType}\"," +
                    $"\"{log.Module}\"," +
                    $"\"{EscapeCsv(log.Description)}\"," +
                    $"\"{EscapeCsv(log.EntityType)}\"," +
                    $"\"{EscapeCsv(log.EntityId)}\"," +
                    $"\"{EscapeCsv(log.EntityName)}\"," +
                    $"{log.IsSuccess}," +
                    $"\"{EscapeCsv(log.IpAddress)}\"");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }
            return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("\"", "\"\"");
        }
    }
}
