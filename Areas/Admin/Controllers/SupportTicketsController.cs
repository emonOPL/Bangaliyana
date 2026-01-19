using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using X.PagedList.Extensions;

namespace Bangaliyana.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public class SupportTicketsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<SupportTicketsController> _logger;
        private readonly IStringLocalizer<SharedResources> _localizer;

        public SupportTicketsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ILogger<SupportTicketsController> logger,
            IStringLocalizer<SharedResources> localizer)
        {
            _db = db;
            _userManager = userManager;
            _logger = logger;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(int? page, string? status, string? priority, string? category, string? search)
        {
            var query = _db.Tickets
                .Include(t => t.User)
                .Include(t => t.Order)
                .Include(t => t.AssignedTo)
                .Include(t => t.Messages)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TicketStatus>(status, out var statusEnum))
            {
                query = query.Where(t => t.Status == statusEnum);
            }

            if (!string.IsNullOrEmpty(priority) && Enum.TryParse<TicketPriority>(priority, out var priorityEnum))
            {
                query = query.Where(t => t.Priority == priorityEnum);
            }

            if (!string.IsNullOrEmpty(category) && Enum.TryParse<TicketCategory>(category, out var categoryEnum))
            {
                query = query.Where(t => t.Category == categoryEnum);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t =>
                    t.TicketNumber.Contains(search) ||
                    t.Subject.Contains(search) ||
                    (t.User != null && (t.User.FirstName.Contains(search) || t.User.LastName.Contains(search) || t.User.Email!.Contains(search))));
            }

            // Order by priority (Urgent first) then by most recent
            query = query.OrderByDescending(t => t.Priority)
                         .ThenByDescending(t => t.UpdatedAt);

            var tickets = await query.ToListAsync();
            var pagedTickets = tickets.ToPagedList(page ?? 1, 15);

            // Stats
            ViewBag.TotalTickets = await _db.Tickets.CountAsync();
            ViewBag.OpenTickets = await _db.Tickets.CountAsync(t => t.Status == TicketStatus.Open);
            ViewBag.InProgressTickets = await _db.Tickets.CountAsync(t => t.Status == TicketStatus.InProgress);
            ViewBag.UrgentTickets = await _db.Tickets.CountAsync(t => t.Priority == TicketPriority.Urgent && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Resolved);

            // Filter values for view
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentPriority = priority;
            ViewBag.CurrentCategory = category;
            ViewBag.CurrentSearch = search;
            ViewBag.StatusList = Enum.GetValues<TicketStatus>().ToList();
            ViewBag.PriorityList = Enum.GetValues<TicketPriority>().ToList();
            ViewBag.CategoryList = Enum.GetValues<TicketCategory>().ToList();

            return View(pagedTickets);
        }

        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _db.Tickets
                .Include(t => t.User)
                .Include(t => t.Order)
                    .ThenInclude(o => o!.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .Include(t => t.AssignedTo)
                .Include(t => t.Messages.OrderBy(m => m.CreatedAt))
                    .ThenInclude(m => m.Sender)
                .Include(t => t.Attachments)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
            {
                TempData["error"] = _localizer["TicketNotFound"].Value;
                return RedirectToAction("Index");
            }

            // Get admin users for assignment dropdown
            var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
            ViewBag.AdminUsers = adminUsers.ToList();
            ViewBag.StatusList = Enum.GetValues<TicketStatus>().ToList();
            ViewBag.PriorityList = Enum.GetValues<TicketPriority>().ToList();

            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int ticketId, string message, bool isInternalNote = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }

            var ticket = await _db.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                return Json(new { success = false, message = "Ticket not found." });
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { success = false, message = "Message cannot be empty." });
            }

            try
            {
                var ticketMessage = new TicketMessage
                {
                    TicketId = ticketId,
                    SenderId = user.Id,
                    Message = message,
                    IsStaffReply = true,
                    IsInternalNote = isInternalNote,
                    CreatedAt = DateTime.UtcNow
                };

                _db.TicketMessages.Add(ticketMessage);

                // Update ticket status to WaitingForCustomer if it was Open and this is not an internal note
                if (!isInternalNote && (ticket.Status == TicketStatus.Open || ticket.Status == TicketStatus.InProgress))
                {
                    ticket.Status = TicketStatus.WaitingForCustomer;
                }
                ticket.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Json(new { success = true, message = isInternalNote ? "Internal note added." : "Reply sent successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding reply to ticket {TicketId}", ticketId);
                return Json(new { success = false, message = "An error occurred while sending the reply." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int ticketId, TicketStatus status)
        {
            var ticket = await _db.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                return Json(new { success = false, message = "Ticket not found." });
            }

            try
            {
                ticket.Status = status;
                ticket.UpdatedAt = DateTime.UtcNow;

                if (status == TicketStatus.Resolved)
                {
                    ticket.ResolvedAt = DateTime.UtcNow;
                }
                else if (status == TicketStatus.Closed)
                {
                    ticket.ClosedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();

                return Json(new { success = true, message = $"Status updated to {status}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for ticket {TicketId}", ticketId);
                return Json(new { success = false, message = "An error occurred while updating the status." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePriority(int ticketId, TicketPriority priority)
        {
            var ticket = await _db.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                return Json(new { success = false, message = "Ticket not found." });
            }

            try
            {
                ticket.Priority = priority;
                ticket.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Json(new { success = true, message = $"Priority updated to {priority}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating priority for ticket {TicketId}", ticketId);
                return Json(new { success = false, message = "An error occurred while updating the priority." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTicket(int ticketId, string? assignedToId)
        {
            var ticket = await _db.Tickets.FindAsync(ticketId);
            if (ticket == null)
            {
                return Json(new { success = false, message = "Ticket not found." });
            }

            try
            {
                ticket.AssignedToId = string.IsNullOrEmpty(assignedToId) ? null : assignedToId;
                ticket.UpdatedAt = DateTime.UtcNow;

                // Set to In Progress if assigned and currently Open
                if (!string.IsNullOrEmpty(assignedToId) && ticket.Status == TicketStatus.Open)
                {
                    ticket.Status = TicketStatus.InProgress;
                }

                await _db.SaveChangesAsync();

                var assignedUser = string.IsNullOrEmpty(assignedToId) ? null : await _userManager.FindByIdAsync(assignedToId);
                var assignedName = assignedUser?.FullName ?? "Unassigned";

                return Json(new { success = true, message = $"Ticket assigned to {assignedName}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning ticket {TicketId}", ticketId);
                return Json(new { success = false, message = "An error occurred while assigning the ticket." });
            }
        }
    }
}
