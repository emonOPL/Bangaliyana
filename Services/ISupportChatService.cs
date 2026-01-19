using Bangaliyana.Models;

namespace Bangaliyana.Services
{
    public interface ISupportChatService
    {
        // Session Management
        Task<SupportChatSession> CreateSessionAsync(string visitorName, string visitorEmail, string? initialQuery, InquiryCategory department, string? userId = null);
        Task<SupportChatSession?> GetSessionByIdAsync(int sessionId);
        Task<SupportChatSession?> GetSessionByCodeAsync(string sessionCode);
        Task<SupportChatSession?> GetActiveSessionByUserIdAsync(string userId);
        Task<List<SupportChatSession>> GetWaitingSessionsAsync();
        Task<List<SupportChatSession>> GetActiveSessionsAsync();
        Task<List<SupportChatSession>> GetSessionsByAgentIdAsync(string agentId);
        Task<List<SupportChatSession>> GetAllSessionsAsync(int page = 1, int pageSize = 20);
        Task<int> GetWaitingSessionsCountAsync();
        Task<int> GetActiveSessionsCountAsync();

        // Session Actions
        Task<bool> AssignAgentAsync(int sessionId, string agentId);
        Task<bool> AssignAIAgentAsync(int sessionId, string aiAgentId);
        Task<bool> CloseSessionAsync(int sessionId, string? closedBy = null);
        Task<bool> AbandonSessionAsync(int sessionId);
        Task UpdateSessionConnectionIdAsync(int sessionId, string connectionId, bool isAgent);
        Task UpdateLastActivityAsync(int sessionId);

        // Messages
        Task<SupportChatMessage> SendMessageAsync(int sessionId, string content, string? senderId, bool isAgentMessage, bool isSystemMessage = false);
        Task<SupportChatMessage> SendMessageWithAttachmentAsync(int sessionId, string content, string? senderId, bool isAgentMessage, string attachmentUrl, string attachmentName, string attachmentType);
        Task<List<SupportChatMessage>> GetSessionMessagesAsync(int sessionId);
        Task MarkMessagesAsReadAsync(int sessionId, bool byAgent);
        Task<bool> UpdateMessageFeedbackAsync(int messageId, bool wasHelpful, string? comment);

        // Rating
        Task<bool> RateSessionAsync(int sessionId, int rating, string? feedback);

        // Statistics
        Task<ChatStatistics> GetStatisticsAsync();
        Task<List<SupportChatSession>> GetRecentSessionsAsync(int count = 10);
    }

    public class ChatStatistics
    {
        public int TotalSessions { get; set; }
        public int WaitingSessions { get; set; }
        public int ActiveSessions { get; set; }
        public int ClosedToday { get; set; }
        public double AverageRating { get; set; }
        public double AverageResponseTimeMinutes { get; set; }
        public int TotalMessagesToday { get; set; }
    }
}
