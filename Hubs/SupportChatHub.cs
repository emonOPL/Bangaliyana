using Microsoft.AspNetCore.SignalR;
using Bangaliyana.Services;
using Bangaliyana.Models;
using System.Security.Claims;
using System.Text.Json;

namespace Bangaliyana.Hubs
{
    /// <summary>
    /// SignalR Hub for live support chat functionality with integrated AI features
    /// Supports both AI Agent responses and Human Agent handoff
    /// </summary>
    public class SupportChatHub : Hub
    {
        private readonly ISupportChatService _chatService;
        private readonly IAIChatService _aiChatService;
        private readonly ILogger<SupportChatHub> _logger;

        // AI Agent identifier
        private const string AI_AGENT_ID = "AI_SUPPORT_AGENT";
        private const string AI_AGENT_NAME = "আপনার বাংলালিয়ানা সহকারী";

        public SupportChatHub(
            ISupportChatService chatService,
            IAIChatService aiChatService,
            ILogger<SupportChatHub> logger)
        {
            _chatService = chatService;
            _aiChatService = aiChatService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Add to support agents group if user has appropriate role
            if (Context.User?.IsInRole("SuperAdmin") == true ||
                Context.User?.IsInRole("Admin") == true ||
                Context.User?.IsInRole("Moderator") == true)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "SupportAgents");
                _logger.LogInformation("Support agent connected: {UserId}", userId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}. Reason: {Reason}",
                Context.ConnectionId, exception?.Message ?? "Normal disconnect");

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Start a new chat session (called by customer)
        /// AI Agent automatically joins and responds
        /// </summary>
        public async Task StartChat(string visitorName, string visitorEmail, string? initialQuery, string department)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Check if user already has an active session
            if (!string.IsNullOrEmpty(userId))
            {
                var existingSession = await _chatService.GetActiveSessionByUserIdAsync(userId);
                if (existingSession != null)
                {
                    await Clients.Caller.SendAsync("SessionResumed", existingSession.SessionCode, existingSession.Id);
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{existingSession.Id}");
                    await _chatService.UpdateSessionConnectionIdAsync(existingSession.Id, Context.ConnectionId, false);
                    return;
                }
            }

            var deptEnum = Enum.TryParse<InquiryCategory>(department, out var dept) ? dept : InquiryCategory.Support;
            var session = await _chatService.CreateSessionAsync(visitorName, visitorEmail, initialQuery, deptEnum, userId);

            // Also create AI Chat session for advanced tracking
            var guestSessionId = string.IsNullOrEmpty(userId) ? Context.ConnectionId : null;
            var aiSession = await _aiChatService.StartSessionAsync(userId, guestSessionId, initialQuery);

            // Store AI session ID in context for later use
            Context.Items["AIChatSessionId"] = aiSession.Id;

            // Join the session group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{session.Id}");
            await _chatService.UpdateSessionConnectionIdAsync(session.Id, Context.ConnectionId, false);

            // Notify the customer
            await Clients.Caller.SendAsync("ChatStarted", session.SessionCode, session.Id, aiSession.Id);

            _logger.LogInformation("New chat session started: {SessionCode}, AI Session: {AISessionId}", session.SessionCode, aiSession.Id);

            // Auto-assign AI Agent
            await _chatService.AssignAIAgentAsync(session.Id, AI_AGENT_ID);

            // Notify customer that AI agent joined
            await Clients.Caller.SendAsync("AgentJoined", new
            {
                agentName = AI_AGENT_NAME,
                sessionId = session.Id,
                aiSessionId = aiSession.Id,
                isAI = true
            });

            // Send AI greeting message
            var greeting = await _aiChatService.GetGreetingMessageAsync(visitorName, initialQuery);
            var greetingMessage = await _chatService.SendMessageAsync(session.Id, greeting, null, true, false);

            // Also save to AI Chat for persistence
            await _aiChatService.SaveMessageAsync(aiSession.Id, greeting, ChatMessageSender.AI, ChatMessageType.Text);

            // Get default quick replies
            var quickReplies = GetDefaultQuickReplies();

            await Clients.Caller.SendAsync("ReceiveMessage", new
            {
                id = greetingMessage.Id,
                sessionId = session.Id,
                aiSessionId = aiSession.Id,
                content = greetingMessage.Content,
                isAgentMessage = true,
                isSystemMessage = false,
                senderName = AI_AGENT_NAME,
                createdAt = greetingMessage.CreatedAt,
                quickReplies = quickReplies
            });

            // If there's an initial query, process it
            if (!string.IsNullOrEmpty(initialQuery))
            {
                // Save user message to AI Chat
                await _aiChatService.SaveMessageAsync(aiSession.Id, initialQuery, ChatMessageSender.User, ChatMessageType.Text);

                // Small delay to make it feel more natural
                await Clients.Caller.SendAsync("UserTyping", new { sessionId = session.Id, isAgent = true, isTyping = true });
                await Task.Delay(1500);

                var aiResponse = await _aiChatService.GetResponseAsync(initialQuery, aiSession.Id, userId);
                var responseMessage = await _chatService.SendMessageAsync(session.Id, aiResponse.Message, null, true, false);

                // Save AI response to AI Chat
                await _aiChatService.SaveMessageAsync(
                    aiSession.Id,
                    aiResponse.Message,
                    ChatMessageSender.AI,
                    aiResponse.ProductSuggestions?.Any() == true ? ChatMessageType.ProductCarousel : ChatMessageType.Text,
                    aiResponse.ProductSuggestions?.Any() == true ? JsonSerializer.Serialize(aiResponse.ProductSuggestions) : null,
                    aiResponse.DetectedIntent);

                await Clients.Caller.SendAsync("UserTyping", new { sessionId = session.Id, isAgent = true, isTyping = false });

                await Clients.Caller.SendAsync("ReceiveMessage", new
                {
                    id = responseMessage.Id,
                    sessionId = session.Id,
                    aiSessionId = aiSession.Id,
                    content = responseMessage.Content,
                    isAgentMessage = true,
                    isSystemMessage = false,
                    senderName = AI_AGENT_NAME,
                    createdAt = responseMessage.CreatedAt,
                    products = aiResponse.ProductSuggestions,
                    quickReplies = aiResponse.QuickReplies ?? GetContextualQuickReplies(aiResponse.DetectedIntent),
                    intent = aiResponse.DetectedIntent,
                    confidence = aiResponse.ConfidenceScore,
                    requiresHumanAgent = aiResponse.RequiresHumanHandoff
                });
            }
        }

        /// <summary>
        /// Join an existing chat session (by session code)
        /// </summary>
        public async Task JoinSession(string sessionCode)
        {
            var session = await _chatService.GetSessionByCodeAsync(sessionCode);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Session not found");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{session.Id}");

            var messages = await _chatService.GetSessionMessagesAsync(session.Id);
            await Clients.Caller.SendAsync("SessionJoined", new
            {
                sessionId = session.Id,
                sessionCode = session.SessionCode,
                status = session.Status.ToString(),
                messages = messages.Select(m => new
                {
                    id = m.Id,
                    content = m.Content,
                    isAgentMessage = m.IsAgentMessage,
                    isSystemMessage = m.IsSystemMessage,
                    senderName = m.IsSystemMessage ? "System" : (m.IsAgentMessage ? AI_AGENT_NAME : session.VisitorName),
                    attachmentUrl = m.AttachmentUrl,
                    attachmentName = m.AttachmentName,
                    createdAt = m.CreatedAt,
                    wasHelpful = m.WasHelpful
                })
            });
        }

        /// <summary>
        /// Agent accepts a chat (called by support agent)
        /// </summary>
        public async Task AcceptChat(int sessionId)
        {
            var agentId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(agentId))
            {
                await Clients.Caller.SendAsync("Error", "Unauthorized");
                return;
            }

            var success = await _chatService.AssignAgentAsync(sessionId, agentId);
            if (!success)
            {
                await Clients.Caller.SendAsync("Error", "Could not accept chat. It may have been accepted by another agent.");
                return;
            }

            var session = await _chatService.GetSessionByIdAsync(sessionId);
            if (session == null) return;

            // Join the session group
            await Groups.AddToGroupAsync(Context.ConnectionId, $"chat_{sessionId}");
            await _chatService.UpdateSessionConnectionIdAsync(sessionId, Context.ConnectionId, true);

            var agentName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Support Agent";

            // Notify the customer that an agent has joined
            await Clients.Group($"chat_{sessionId}").SendAsync("AgentJoined", new
            {
                agentName = agentName,
                sessionId = sessionId,
                isAI = false
            });

            // Send system message about agent joining
            var systemMessage = await _chatService.SendMessageAsync(sessionId, $"Human agent {agentName} has joined the chat.", null, false, true);
            await Clients.Group($"chat_{sessionId}").SendAsync("ReceiveMessage", new
            {
                id = systemMessage.Id,
                sessionId = sessionId,
                content = systemMessage.Content,
                isAgentMessage = false,
                isSystemMessage = true,
                senderName = "System",
                createdAt = systemMessage.CreatedAt
            });

            // Notify all agents to update their queue
            await Clients.Group("SupportAgents").SendAsync("ChatAccepted", sessionId);

            // Send session details to the agent
            var messages = await _chatService.GetSessionMessagesAsync(sessionId);
            await Clients.Caller.SendAsync("ChatReady", new
            {
                sessionId = session.Id,
                sessionCode = session.SessionCode,
                visitorName = session.VisitorName,
                visitorEmail = session.VisitorEmail,
                initialQuery = session.InitialQuery,
                department = session.Department.ToString(),
                messages = messages.Select(m => new
                {
                    id = m.Id,
                    content = m.Content,
                    isAgentMessage = m.IsAgentMessage,
                    isSystemMessage = m.IsSystemMessage,
                    senderName = m.IsSystemMessage ? "System" : (m.IsAgentMessage ? AI_AGENT_NAME : session.VisitorName),
                    createdAt = m.CreatedAt,
                    wasHelpful = m.WasHelpful
                })
            });

            _logger.LogInformation("Agent {AgentId} accepted chat {SessionId}", agentId, sessionId);
        }

        /// <summary>
        /// Send a message in the chat
        /// AI Agent automatically responds to customer messages
        /// </summary>
        public async Task SendMessage(int sessionId, string content, int? aiSessionId = null)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var session = await _chatService.GetSessionByIdAsync(sessionId);

            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Session not found");
                return;
            }

            // Determine if sender is agent (human agent, not AI)
            var isAISession = (string.IsNullOrEmpty(session.AgentId) || session.AgentId == AI_AGENT_ID) && session.Status == ChatSessionStatus.Active;
            var isHumanAgent = !string.IsNullOrEmpty(userId) && userId == session.AgentId && session.AgentId != AI_AGENT_ID;
            var senderName = isHumanAgent
                ? (Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Support Agent")
                : session.VisitorName;

            // Save customer message
            var message = await _chatService.SendMessageAsync(sessionId, content, userId, isHumanAgent);

            // Also save to AI Chat if AI session
            if (aiSessionId.HasValue && !isHumanAgent)
            {
                await _aiChatService.SaveMessageAsync(aiSessionId.Value, content, ChatMessageSender.User, ChatMessageType.Text);
            }

            // Broadcast customer message
            await Clients.Group($"chat_{sessionId}").SendAsync("ReceiveMessage", new
            {
                id = message.Id,
                sessionId = sessionId,
                aiSessionId = aiSessionId,
                content = message.Content,
                isAgentMessage = message.IsAgentMessage,
                isSystemMessage = false,
                senderName = senderName,
                createdAt = message.CreatedAt
            });

            // If this is an AI session and message is from customer, generate AI response
            if (isAISession && !isHumanAgent)
            {
                // Show typing indicator
                await Clients.Caller.SendAsync("UserTyping", new
                {
                    sessionId = sessionId,
                    isAgent = true,
                    isTyping = true
                });

                // Small delay to simulate typing
                await Task.Delay(1000 + new Random().Next(500, 1500));

                // Get AI response
                var aiResponse = await _aiChatService.GetResponseAsync(content, aiSessionId ?? 0, session.UserId);
                var responseMessage = await _chatService.SendMessageAsync(sessionId, aiResponse.Message, null, true, false);

                // Save to AI Chat
                if (aiSessionId.HasValue)
                {
                    await _aiChatService.SaveMessageAsync(
                        aiSessionId.Value,
                        aiResponse.Message,
                        ChatMessageSender.AI,
                        aiResponse.ProductSuggestions?.Any() == true ? ChatMessageType.ProductCarousel : ChatMessageType.Text,
                        aiResponse.ProductSuggestions?.Any() == true ? JsonSerializer.Serialize(aiResponse.ProductSuggestions) : null,
                        aiResponse.DetectedIntent);
                }

                // Hide typing indicator
                await Clients.Caller.SendAsync("UserTyping", new
                {
                    sessionId = sessionId,
                    isAgent = true,
                    isTyping = false
                });

                // Send AI response with enhanced data
                await Clients.Group($"chat_{sessionId}").SendAsync("ReceiveMessage", new
                {
                    id = responseMessage.Id,
                    sessionId = sessionId,
                    aiSessionId = aiSessionId,
                    content = responseMessage.Content,
                    isAgentMessage = true,
                    isSystemMessage = false,
                    senderName = AI_AGENT_NAME,
                    createdAt = responseMessage.CreatedAt,
                    products = aiResponse.ProductSuggestions,
                    quickReplies = aiResponse.QuickReplies ?? GetContextualQuickReplies(aiResponse.DetectedIntent),
                    intent = aiResponse.DetectedIntent,
                    confidence = aiResponse.ConfidenceScore,
                    requiresHumanAgent = aiResponse.RequiresHumanHandoff
                });

                // If AI suggests human handoff, notify
                if (aiResponse.RequiresHumanHandoff)
                {
                    await Clients.Caller.SendAsync("SuggestHumanAgent", new
                    {
                        sessionId = sessionId,
                        reason = aiResponse.HandoffReason ?? "AI recommends connecting with a human agent for better assistance."
                    });
                }
            }
        }

        /// <summary>
        /// Request handoff to human agent
        /// </summary>
        public async Task RequestHumanAgent(int sessionId, int? aiSessionId, string? reason)
        {
            var session = await _chatService.GetSessionByIdAsync(sessionId);
            if (session == null)
            {
                await Clients.Caller.SendAsync("Error", "Session not found");
                return;
            }

            // Create handoff request in AI Chat system
            HandoffRequestResponse? handoffResult = null;
            if (aiSessionId.HasValue)
            {
                handoffResult = await _aiChatService.RequestHandoffAsync(aiSessionId.Value, reason);
            }

            // Update session status
            session.Status = ChatSessionStatus.Waiting;
            // Note: In a full implementation, you'd update the session in the database

            // Send system message
            var systemMessage = await _chatService.SendMessageAsync(
                sessionId,
                "আপনার request পাঠানো হয়েছে। একজন human agent শীঘ্রই আপনার সাথে যোগ দেবেন...",
                null, false, true);

            await Clients.Group($"chat_{sessionId}").SendAsync("ReceiveMessage", new
            {
                id = systemMessage.Id,
                sessionId = sessionId,
                content = systemMessage.Content,
                isAgentMessage = false,
                isSystemMessage = true,
                senderName = "System",
                createdAt = systemMessage.CreatedAt
            });

            // Notify customer
            await Clients.Caller.SendAsync("HandoffRequested", new
            {
                success = true,
                sessionId = sessionId,
                queuePosition = handoffResult?.QueuePosition ?? 1,
                estimatedWaitMinutes = handoffResult?.EstimatedWaitMinutes ?? 5,
                message = "আপনার request পাঠানো হয়েছে। অনুগ্রহ করে অপেক্ষা করুন..."
            });

            // Notify agents about new waiting customer
            await Clients.Group("SupportAgents").SendAsync("NewWaitingChat", new
            {
                sessionId = sessionId,
                visitorName = session.VisitorName,
                reason = reason,
                waitingSince = DateTime.UtcNow
            });

            _logger.LogInformation("Human agent requested for session {SessionId}, reason: {Reason}", sessionId, reason);
        }

        /// <summary>
        /// Submit feedback for a message
        /// </summary>
        public async Task SubmitMessageFeedback(int messageId, int? aiSessionId, bool wasHelpful, string? comment)
        {
            try
            {
                // Update the SupportChatMessage feedback
                await _chatService.UpdateMessageFeedbackAsync(messageId, wasHelpful, comment);

                // Also update AI chat feedback if there's an AI session
                if (aiSessionId.HasValue)
                {
                    await _aiChatService.SubmitMessageFeedbackAsync(messageId, wasHelpful, comment);
                }

                await Clients.Caller.SendAsync("FeedbackSubmitted", new
                {
                    messageId = messageId,
                    success = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting feedback for message {MessageId}", messageId);
            }
        }

        /// <summary>
        /// Close the chat session
        /// </summary>
        public async Task CloseChat(int sessionId, int? aiSessionId = null)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            await _chatService.CloseSessionAsync(sessionId, userId);

            // Also close AI session
            if (aiSessionId.HasValue)
            {
                await _aiChatService.EndSessionAsync(aiSessionId.Value);
            }

            // Notify everyone in the session
            await Clients.Group($"chat_{sessionId}").SendAsync("ChatClosed", sessionId);

            // Notify agents to update their lists
            await Clients.Group("SupportAgents").SendAsync("SessionClosed", sessionId);

            _logger.LogInformation("Chat session {SessionId} closed", sessionId);
        }

        /// <summary>
        /// Customer rates the chat session
        /// </summary>
        public async Task RateChat(int sessionId, int rating, string? feedback, int? aiSessionId = null)
        {
            await _chatService.RateSessionAsync(sessionId, rating, feedback);

            // Also save to AI Chat
            if (aiSessionId.HasValue)
            {
                await _aiChatService.EndSessionAsync(aiSessionId.Value, feedback, rating);
            }

            await Clients.Caller.SendAsync("RatingSubmitted", true);
            _logger.LogInformation("Chat session {SessionId} rated: {Rating}", sessionId, rating);
        }

        /// <summary>
        /// Typing indicator
        /// </summary>
        public async Task Typing(int sessionId, bool isTyping)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var session = await _chatService.GetSessionByIdAsync(sessionId);
            var isAgent = !string.IsNullOrEmpty(userId) && session?.AgentId == userId;

            await Clients.OthersInGroup($"chat_{sessionId}").SendAsync("UserTyping", new
            {
                sessionId = sessionId,
                isAgent = isAgent,
                isTyping = isTyping
            });
        }

        /// <summary>
        /// Get waiting sessions count (for agents)
        /// </summary>
        public async Task GetWaitingCount()
        {
            var count = await _chatService.GetWaitingSessionsCountAsync();
            await Clients.Caller.SendAsync("WaitingCount", count);
        }

        /// <summary>
        /// Get all waiting sessions (for agents)
        /// </summary>
        public async Task GetWaitingSessions()
        {
            var sessions = await _chatService.GetWaitingSessionsAsync();
            await Clients.Caller.SendAsync("WaitingSessions", sessions.Select(s => new
            {
                sessionId = s.Id,
                sessionCode = s.SessionCode,
                visitorName = s.VisitorName,
                visitorEmail = s.VisitorEmail,
                initialQuery = s.InitialQuery,
                department = s.Department.ToString(),
                createdAt = s.CreatedAt,
                waitingMinutes = (DateTime.UtcNow - s.CreatedAt).TotalMinutes
            }));
        }

        /// <summary>
        /// Get handoff queue (for agents)
        /// </summary>
        public async Task GetHandoffQueue()
        {
            // Check if user has any of the required roles
            var isAuthorized = Context.User?.IsInRole("SuperAdmin") == true ||
                               Context.User?.IsInRole("Admin") == true ||
                               Context.User?.IsInRole("Moderator") == true;

            if (!isAuthorized)
            {
                await Clients.Caller.SendAsync("Error", "Unauthorized");
                return;
            }

            try
            {
                var queue = await _aiChatService.GetHandoffQueueAsync();
                await Clients.Caller.SendAsync("HandoffQueue", queue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting handoff queue");
            }
        }

        #region Helper Methods

        private static List<QuickReplyButton> GetDefaultQuickReplies()
        {
            return new List<QuickReplyButton>
            {
                new() { Text = "অর্ডার ট্র্যাক করুন", Action = "send_message", Payload = "আমার অর্ডার ট্র্যাক করুন", Icon = "fas fa-truck" },
                new() { Text = "প্রোডাক্ট খুঁজুন", Action = "send_message", Payload = "প্রোডাক্ট দেখতে চাই", Icon = "fas fa-search" },
                new() { Text = "কুপন কোড", Action = "send_message", Payload = "কোন কুপন আছে?", Icon = "fas fa-tags" },
                new() { Text = "রিটার্ন পলিসি", Action = "send_message", Payload = "রিটার্ন পলিসি কি?", Icon = "fas fa-undo" }
            };
        }

        private static List<QuickReplyButton>? GetContextualQuickReplies(string? intent)
        {
            if (string.IsNullOrEmpty(intent)) return null;

            return intent.ToLower() switch
            {
                "order_tracking" => new List<QuickReplyButton>
                {
                    new() { Text = "অন্য অর্ডার ট্র্যাক", Action = "send_message", Payload = "অন্য অর্ডার ট্র্যাক করুন" },
                    new() { Text = "রিটার্ন করতে চাই", Action = "send_message", Payload = "রিটার্ন করতে চাই" },
                    new() { Text = "Human Agent", Action = "request_human", Payload = "human_agent" }
                },
                "product_search" or "product_query" => new List<QuickReplyButton>
                {
                    new() { Text = "আরো দেখুন", Action = "send_message", Payload = "আরো প্রোডাক্ট দেখান" },
                    new() { Text = "কার্টে যোগ করুন", Action = "send_message", Payload = "কার্টে যোগ করুন" },
                    new() { Text = "তুলনা করুন", Action = "send_message", Payload = "এই প্রোডাক্টগুলো তুলনা করুন" }
                },
                "cart" or "checkout" => new List<QuickReplyButton>
                {
                    new() { Text = "কার্ট দেখুন", Action = "open_url", Payload = "/Customer/Home/Cart" },
                    new() { Text = "কুপন লাগান", Action = "send_message", Payload = "কুপন কোড দিন" },
                    new() { Text = "Checkout", Action = "open_url", Payload = "/Customer/Home/Checkout" }
                },
                "coupon" or "discount" => new List<QuickReplyButton>
                {
                    new() { Text = "আরো অফার", Action = "send_message", Payload = "আরো অফার দেখান" },
                    new() { Text = "কার্ট দেখুন", Action = "open_url", Payload = "/Customer/Home/Cart" },
                    new() { Text = "বেস্ট ডিল", Action = "send_message", Payload = "বেস্ট ডিল দেখান" }
                },
                "return" or "refund" => new List<QuickReplyButton>
                {
                    new() { Text = "রিটার্ন পলিসি", Action = "send_message", Payload = "রিটার্ন পলিসি বিস্তারিত" },
                    new() { Text = "রিফান্ড স্ট্যাটাস", Action = "send_message", Payload = "রিফান্ড স্ট্যাটাস চেক করুন" },
                    new() { Text = "Human Agent", Action = "request_human", Payload = "human_agent" }
                },
                _ => null
            };
        }

        #endregion
    }
}
