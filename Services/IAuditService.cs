using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    /// <summary>
    /// Service for managing audit logs
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// Log an action
        /// </summary>
        Task LogAsync(AuditLog log);

        /// <summary>
        /// Log an action with simplified parameters
        /// </summary>
        Task LogAsync(
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
            string? errorMessage = null);

        /// <summary>
        /// Get audit logs with filtering and pagination
        /// </summary>
        Task<(List<AuditLog> Logs, int TotalCount)> GetLogsAsync(
            string? userId = null,
            AuditActionType? actionType = null,
            AuditModule? module = null,
            string? entityType = null,
            string? entityId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? searchTerm = null,
            int page = 1,
            int pageSize = 50);

        /// <summary>
        /// Get a single audit log by ID
        /// </summary>
        Task<AuditLog?> GetLogByIdAsync(int id);

        /// <summary>
        /// Get audit logs for a specific entity
        /// </summary>
        Task<List<AuditLog>> GetEntityHistoryAsync(string entityType, string entityId);

        /// <summary>
        /// Get recent activity for a user
        /// </summary>
        Task<List<AuditLog>> GetUserActivityAsync(string userId, int count = 20);

        /// <summary>
        /// Get audit statistics
        /// </summary>
        Task<AuditStatistics> GetStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Delete old audit logs (for maintenance)
        /// </summary>
        Task<int> DeleteOldLogsAsync(int daysToKeep = 90);

        /// <summary>
        /// Export audit logs to CSV
        /// </summary>
        Task<byte[]> ExportToCsvAsync(
            string? userId = null,
            AuditActionType? actionType = null,
            AuditModule? module = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);
    }

    /// <summary>
    /// Audit statistics
    /// </summary>
    public class AuditStatistics
    {
        public int TotalLogs { get; set; }
        public int TodayLogs { get; set; }
        public int ThisWeekLogs { get; set; }
        public int ThisMonthLogs { get; set; }
        public Dictionary<AuditActionType, int> ActionTypeCounts { get; set; } = new();
        public Dictionary<AuditModule, int> ModuleCounts { get; set; } = new();
        public List<UserActivitySummary> TopActiveUsers { get; set; } = new();
        public List<RecentActivity> RecentActivities { get; set; } = new();
    }

    public class UserActivitySummary
    {
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public int ActionCount { get; set; }
    }

    public class RecentActivity
    {
        public string UserEmail { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public AuditModule Module { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
