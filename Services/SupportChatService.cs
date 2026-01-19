using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    public class SupportChatService : ISupportChatService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<SupportChatService> _logger;
        private readonly ISiteSettingsService _siteSettingsService;

        public SupportChatService(ApplicationDbContext db, ILogger<SupportChatService> logger, ISiteSettingsService siteSettingsService)
        {
            _db = db;
            _logger = logger;
            _siteSettingsService = siteSettingsService;
        }

        #region Session Management

        public async Task<SupportChatSession> CreateSessionAsync(string visitorName, string visitorEmail, string? initialQuery, InquiryCategory department, string? userId = null)
        {
            var session = new SupportChatSession
            {
                SessionCode = GenerateSessionCode(),
                VisitorName = visitorName,
                VisitorEmail = visitorEmail,
                InitialQuery = initialQuery,
                Department = department,
                UserId = userId,
                Status = ChatSessionStatus.Waiting,
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow
            };

            _db.SupportChatSessions.Add(session);
            await _db.SaveChangesAsync();

            // Add system message
            await SendMessageAsync(session.Id, "Chat session started. Please wait while we connect you with a support agent.", null, false, true);

            _logger.LogInformation("Support chat session created: {SessionCode}", session.SessionCode);
            return session;
        }

        public async Task<SupportChatSession?> GetSessionByIdAsync(int sessionId)
        {
            return await _db.SupportChatSessions
                .Include(s => s.User)
                .Include(s => s.Agent)
                .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(s => s.Id == sessionId);
        }

        public async Task<SupportChatSession?> GetSessionByCodeAsync(string sessionCode)
        {
            return await _db.SupportChatSessions
                .Include(s => s.User)
                .Include(s => s.Agent)
                .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(s => s.SessionCode == sessionCode);
        }

        public async Task<SupportChatSession?> GetActiveSessionByUserIdAsync(string userId)
        {
            return await _db.SupportChatSessions
                .Include(s => s.Messages.OrderBy(m => m.CreatedAt))
                .FirstOrDefaultAsync(s => s.UserId == userId &&
                    (s.Status == ChatSessionStatus.Waiting || s.Status == ChatSessionStatus.Active));
        }

        public async Task<List<SupportChatSession>> GetWaitingSessionsAsync()
        {
            return await _db.SupportChatSessions
                .Include(s => s.User)
                .Where(s => s.Status == ChatSessionStatus.Waiting)
                .OrderBy(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<SupportChatSession>> GetActiveSessionsAsync()
        {
            return await _db.SupportChatSessions
                .Include(s => s.User)
                .Include(s => s.Agent)
                .Where(s => s.Status == ChatSessionStatus.Active)
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync();
        }

        public async Task<List<SupportChatSession>> GetSessionsByAgentIdAsync(string agentId)
        {
            return await _db.SupportChatSessions
                .Include(s => s.User)
                .Where(s => s.AgentId == agentId && s.Status == ChatSessionStatus.Active)
                .OrderByDescending(s => s.LastActivityAt)
                .ToListAsync();
        }

        public async Task<List<SupportChatSession>> GetAllSessionsAsync(int page = 1, int pageSize = 20)
        {
            return await _db.SupportChatSessions
                .Include(s => s.User)
                .Include(s => s.Agent)
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetWaitingSessionsCountAsync()
        {
            return await _db.SupportChatSessions.CountAsync(s => s.Status == ChatSessionStatus.Waiting);
        }

        public async Task<int> GetActiveSessionsCountAsync()
        {
            return await _db.SupportChatSessions.CountAsync(s => s.Status == ChatSessionStatus.Active);
        }

        #endregion

        #region Session Actions

        public async Task<bool> AssignAgentAsync(int sessionId, string agentId)
        {
            var session = await _db.SupportChatSessions.FindAsync(sessionId);
            if (session == null || session.Status != ChatSessionStatus.Waiting)
                return false;

            session.AgentId = agentId;
            session.Status = ChatSessionStatus.Active;
            session.AgentJoinedAt = DateTime.UtcNow;
            session.LastActivityAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Add system message
            var agent = await _db.Users.FindAsync(agentId);
            var agentName = agent?.FirstName ?? "Support Agent";
            await SendMessageAsync(sessionId, $"{agentName} has joined the chat.", null, false, true);

            _logger.LogInformation("Agent {AgentId} assigned to session {SessionId}", agentId, sessionId);
            return true;
        }

        public async Task<bool> AssignAIAgentAsync(int sessionId, string aiAgentId)
        {
            var session = await _db.SupportChatSessions.FindAsync(sessionId);
            if (session == null)
                return false;

            // Don't set AgentId (foreign key constraint) - keep it null for AI sessions
            // AI sessions are identified by Status=Active with AgentId=null
            session.Status = ChatSessionStatus.Active;
            session.AgentJoinedAt = DateTime.UtcNow;
            session.LastActivityAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            _logger.LogInformation("AI Agent assigned to session {SessionId}", sessionId);
            return true;
        }

        public async Task<bool> CloseSessionAsync(int sessionId, string? closedBy = null)
        {
            var session = await _db.SupportChatSessions.FindAsync(sessionId);
            if (session == null)
                return false;

            session.Status = ChatSessionStatus.Closed;
            session.ClosedAt = DateTime.UtcNow;
            session.LastActivityAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // Add system message with dynamic site name
            var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
            var siteName = siteSettings?.SiteName ?? "us";
            await SendMessageAsync(sessionId, $"Chat session has ended. Thank you for contacting {siteName}!", null, false, true);

            _logger.LogInformation("Session {SessionId} closed by {ClosedBy}", sessionId, closedBy ?? "System");
            return true;
        }

        public async Task<bool> AbandonSessionAsync(int sessionId)
        {
            var session = await _db.SupportChatSessions.FindAsync(sessionId);
            if (session == null)
                return false;

            session.Status = ChatSessionStatus.Abandoned;
            session.ClosedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Session {SessionId} abandoned", sessionId);
            return true;
        }

        public async Task UpdateSessionConnectionIdAsync(int sessionId, string connectionId, bool isAgent)
        {
            var session = await _db.SupportChatSessions.FindAsync(sessionId);
            if (session == null) return;

            if (isAgent)
                session.AgentConnectionId = connectionId;
            else
                session.CustomerConnectionId = connectionId;

            session.LastActivityAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        public async Task UpdateLastActivityAsync(int sessionId)
        {
            var session = await _db.SupportChatSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.LastActivityAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        #endregion

        #region Messages

        public async Task<SupportChatMessage> SendMessageAsync(int sessionId, string content, string? senderId, bool isAgentMessage, bool isSystemMessage = false)
        {
            var message = new SupportChatMessage
            {
                SessionId = sessionId,
                Content = content,
                SenderId = senderId,
                IsAgentMessage = isAgentMessage,
                IsSystemMessage = isSystemMessage,
                CreatedAt = DateTime.UtcNow
            };

            _db.SupportChatMessages.Add(message);

            // Update session last activity
            var session = await _db.SupportChatSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.LastActivityAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return message;
        }

        public async Task<SupportChatMessage> SendMessageWithAttachmentAsync(int sessionId, string content, string? senderId, bool isAgentMessage, string attachmentUrl, string attachmentName, string attachmentType)
        {
            var message = new SupportChatMessage
            {
                SessionId = sessionId,
                Content = content,
                SenderId = senderId,
                IsAgentMessage = isAgentMessage,
                AttachmentUrl = attachmentUrl,
                AttachmentName = attachmentName,
                AttachmentType = attachmentType,
                CreatedAt = DateTime.UtcNow
            };

            _db.SupportChatMessages.Add(message);

            var session = await _db.SupportChatSessions.FindAsync(sessionId);
            if (session != null)
            {
                session.LastActivityAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return message;
        }

        public async Task<List<SupportChatMessage>> GetSessionMessagesAsync(int sessionId)
        {
            return await _db.SupportChatMessages
                .Include(m => m.Sender)
                .Where(m => m.SessionId == sessionId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task MarkMessagesAsReadAsync(int sessionId, bool byAgent)
        {
            var messages = await _db.SupportChatMessages
                .Where(m => m.SessionId == sessionId && !m.IsRead && m.IsAgentMessage != byAgent)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
        }

        public async Task<bool> UpdateMessageFeedbackAsync(int messageId, bool wasHelpful, string? comment)
        {
            var message = await _db.SupportChatMessages.FindAsync(messageId);
            if (message == null)
                return false;

            message.WasHelpful = wasHelpful;
            message.UserFeedback = comment;
            await _db.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Rating

        public async Task<bool> RateSessionAsync(int sessionId, int rating, string? feedback)
        {
            var session = await _db.SupportChatSessions.FindAsync(sessionId);
            if (session == null)
                return false;

            session.Rating = rating;
            session.Feedback = feedback;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Session {SessionId} rated: {Rating}", sessionId, rating);
            return true;
        }

        #endregion

        #region Statistics

        public async Task<ChatStatistics> GetStatisticsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var sessions = await _db.SupportChatSessions.ToListAsync();
            var todayMessages = await _db.SupportChatMessages.CountAsync(m => m.CreatedAt >= today);

            var avgResponseTime = sessions
                .Where(s => s.AgentJoinedAt.HasValue)
                .Select(s => (s.AgentJoinedAt!.Value - s.CreatedAt).TotalMinutes)
                .DefaultIfEmpty(0)
                .Average();

            return new ChatStatistics
            {
                TotalSessions = sessions.Count,
                WaitingSessions = sessions.Count(s => s.Status == ChatSessionStatus.Waiting),
                ActiveSessions = sessions.Count(s => s.Status == ChatSessionStatus.Active),
                ClosedToday = sessions.Count(s => s.ClosedAt.HasValue && s.ClosedAt.Value.Date == today),
                AverageRating = sessions.Where(s => s.Rating.HasValue).Select(s => (double)s.Rating!.Value).DefaultIfEmpty(0).Average(),
                AverageResponseTimeMinutes = avgResponseTime,
                TotalMessagesToday = todayMessages
            };
        }

        public async Task<List<SupportChatSession>> GetRecentSessionsAsync(int count = 10)
        {
            return await _db.SupportChatSessions
                .Include(s => s.User)
                .Include(s => s.Agent)
                .OrderByDescending(s => s.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        #endregion

        #region Helpers

        private static string GenerateSessionCode()
        {
            return $"CHAT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        }

        #endregion
    }
}
