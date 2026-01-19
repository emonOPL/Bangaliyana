using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    public interface ISellerPaymentService
    {
        // Payment operations
        Task<SellerPayment?> CreateMonthlyPaymentAsync(int sellerId);
        Task ProcessAllMonthlyPaymentsAsync(string? triggeredByUserId = null);
        Task<SellerPayment?> GetPaymentByIdAsync(int paymentId);
        Task<SellerPayment?> GetPaymentByNumberAsync(string paymentNumber);

        // Logs
        Task<IEnumerable<SellerPaymentLog>> GetPaymentLogsAsync(int page = 1, int pageSize = 50);
        Task<int> GetPaymentLogsCountAsync();
        Task AddLogAsync(PaymentLogType logType, string message, string? details = null, int? paymentId = null, int? sellerId = null, string? userId = null);

        // Status management
        Task<bool> UpdatePaymentStatusAsync(int paymentId, SellerPaymentStatus status, string? notes, string? processedByUserId);
        Task<bool> MarkAsPaidAsync(int paymentId, string transactionReference, string processedByUserId);
        Task<bool> MarkAsFailedAsync(int paymentId, string failureReason, string processedByUserId);

        // Query operations
        Task<IEnumerable<SellerPayment>> GetSellerPaymentsAsync(int sellerId, int page = 1, int pageSize = 10);
        Task<IEnumerable<SellerPayment>> GetAllPaymentsAsync(SellerPaymentStatus? status = null, string? search = null, int page = 1, int pageSize = 20);
        Task<int> GetPaymentsCountAsync(SellerPaymentStatus? status = null, string? search = null);
        Task<Dictionary<SellerPaymentStatus, int>> GetPaymentStatusCountsAsync();

        // Messaging
        Task<SellerPaymentMessage> AddPaymentMessageAsync(int paymentId, string senderId, string message, bool isStaffReply);
        Task<IEnumerable<SellerPaymentMessage>> GetPaymentMessagesAsync(int paymentId);
        Task<int> GetUnreadMessagesCountAsync(int paymentId, bool forStaff);
        Task MarkMessagesAsReadAsync(int paymentId, bool forStaff);

        // Attachments
        Task<SellerPaymentMessageAttachment> AddMessageAttachmentAsync(int messageId, string fileName, string fileUrl, string? contentType, long? fileSize);

        // Settings
        Task<SellerPaymentSettings> GetSettingsAsync();
        Task UpdateSettingsAsync(SellerPaymentSettings settings);

        // Utilities
        string GeneratePaymentNumber();
    }
}
