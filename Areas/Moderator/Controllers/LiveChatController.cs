using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Bangaliyana.Services;
using System.Security.Claims;

namespace Bangaliyana.Areas.Moderator.Controllers
{
    [Area("Moderator")]
    [Authorize(Policy = "RequireModeratorRole")]
    public class LiveChatController : Controller
    {
        private readonly ISupportChatService _chatService;
        private readonly ILogger<LiveChatController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public LiveChatController(
            ISupportChatService chatService,
            ILogger<LiveChatController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _chatService = chatService;
            _logger = logger;
            _localizer = localizer;
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
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "LiveChat";

            return View("~/Areas/Admin/Views/SupportChat/Index.cshtml");
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
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "LiveChat";

            if (sessionId.HasValue)
            {
                var session = await _chatService.GetSessionByIdAsync(sessionId.Value);
                if (session != null)
                {
                    ViewBag.CurrentSession = session;
                }
            }

            return View("~/Areas/Admin/Views/SupportChat/Chat.cshtml");
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
            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "LiveChat";

            return View("~/Areas/Admin/Views/SupportChat/History.cshtml", sessions);
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

            ViewBag.AreaName = "Moderator";
            ViewBag.ControllerName = "LiveChat";

            return View("~/Areas/Admin/Views/SupportChat/SessionDetails.cshtml", session);
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
