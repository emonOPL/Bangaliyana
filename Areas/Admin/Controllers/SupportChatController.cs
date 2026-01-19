using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Bangaliyana.Services;
using System.Security.Claims;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "RequireAdminRole")]
    public class SupportChatController : Controller
    {
        private readonly ISupportChatService _chatService;
        private readonly ILogger<SupportChatController> _logger;

        public SupportChatController(ISupportChatService chatService, ILogger<SupportChatController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// Dashboard view with live chat queue and active sessions
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var statistics = await _chatService.GetStatisticsAsync();
            var waitingSessions = await _chatService.GetWaitingSessionsAsync();
            var activeSessions = await _chatService.GetActiveSessionsAsync();
            var recentSessions = await _chatService.GetRecentSessionsAsync(10);

            ViewBag.Statistics = statistics;
            ViewBag.WaitingSessions = waitingSessions;
            ViewBag.ActiveSessions = activeSessions;
            ViewBag.RecentSessions = recentSessions;

            return View();
        }

        /// <summary>
        /// Agent chat interface
        /// </summary>
        public async Task<IActionResult> Chat(int? sessionId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Get agent's active sessions
            var mySessions = await _chatService.GetSessionsByAgentIdAsync(userId ?? "");
            var waitingSessions = await _chatService.GetWaitingSessionsAsync();

            ViewBag.MySessions = mySessions;
            ViewBag.WaitingSessions = waitingSessions;

            if (sessionId.HasValue)
            {
                var session = await _chatService.GetSessionByIdAsync(sessionId.Value);
                if (session != null)
                {
                    ViewBag.CurrentSession = session;
                }
            }

            return View();
        }

        /// <summary>
        /// View all chat history
        /// </summary>
        public async Task<IActionResult> History(int page = 1)
        {
            var sessions = await _chatService.GetAllSessionsAsync(page, 20);
            var statistics = await _chatService.GetStatisticsAsync();

            ViewBag.Page = page;
            ViewBag.Statistics = statistics;

            return View(sessions);
        }

        /// <summary>
        /// View specific session details
        /// </summary>
        public async Task<IActionResult> SessionDetails(int id)
        {
            var session = await _chatService.GetSessionByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            return View(session);
        }

        /// <summary>
        /// Get waiting sessions count (for badge updates)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWaitingCount()
        {
            var count = await _chatService.GetWaitingSessionsCountAsync();
            return Json(new { count });
        }
    }
}
