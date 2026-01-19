using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Bangaliyana.Data;
using Bangaliyana.Models;

namespace Bangaliyana.Areas.Identity.Pages.Account.Manage
{
    public class SupportTicketsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public SupportTicketsModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public List<Ticket> Tickets { get; set; } = new();
        public List<Order> RecentOrders { get; set; } = new();

        [TempData]
        public string? StatusMessage { get; set; }

        // Flag to auto-open create ticket modal
        public bool AutoOpenCreateModal { get; set; } = false;

        [BindProperty]
        public TicketInputModel Input { get; set; } = new();

        public class TicketInputModel
        {
            [Required]
            [StringLength(200)]
            [Display(Name = "Subject")]
            public string Subject { get; set; } = string.Empty;

            [Required]
            [StringLength(5000)]
            [Display(Name = "Description")]
            public string Description { get; set; } = string.Empty;

            [Display(Name = "Category")]
            public TicketCategory Category { get; set; } = TicketCategory.General;

            [Display(Name = "Priority")]
            public TicketPriority Priority { get; set; } = TicketPriority.Medium;

            [Display(Name = "Related Order")]
            public int? OrderId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            Tickets = await _context.Tickets
                .Where(t => t.UserId == user.Id)
                .Include(t => t.Order)
                .Include(t => t.Messages)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            RecentOrders = await _context.Orders
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .ToListAsync();

            // Check for pre-fill data from ContactSeller action
            if (TempData["TicketSubject"] != null)
            {
                Input.Subject = TempData["TicketSubject"]?.ToString() ?? "";
                AutoOpenCreateModal = true;
            }
            if (TempData["TicketOrderId"] != null && int.TryParse(TempData["TicketOrderId"]?.ToString(), out int orderId))
            {
                Input.OrderId = orderId;
            }
            if (TempData["TicketCategory"] != null && Enum.TryParse<TicketCategory>(TempData["TicketCategory"]?.ToString(), out var category))
            {
                Input.Category = category;
            }

            return Page();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            var ticket = new Ticket
            {
                TicketNumber = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                UserId = user.Id,
                Subject = Input.Subject,
                Description = Input.Description,
                Category = Input.Category,
                Priority = Input.Priority,
                OrderId = Input.OrderId,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            StatusMessage = $"Support ticket #{ticket.TicketNumber} created successfully. We'll respond shortly.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostReplyAsync(int ticketId, string message)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == user.Id);

            if (ticket == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                StatusMessage = "Error: Message cannot be empty.";
                return RedirectToPage();
            }

            var ticketMessage = new TicketMessage
            {
                TicketId = ticketId,
                SenderId = user.Id,
                Message = message,
                IsStaffReply = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.TicketMessages.Add(ticketMessage);

            // Update ticket status if it was waiting for customer
            if (ticket.Status == TicketStatus.WaitingForCustomer)
            {
                ticket.Status = TicketStatus.Open;
            }
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            StatusMessage = "Reply sent successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCloseAsync(int ticketId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .FirstOrDefaultAsync(t => t.Id == ticketId && t.UserId == user.Id);

            if (ticket != null && ticket.Status != TicketStatus.Closed)
            {
                ticket.Status = TicketStatus.Closed;
                ticket.ClosedAt = DateTime.UtcNow;
                ticket.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                StatusMessage = "Ticket closed successfully.";
            }

            return RedirectToPage();
        }
    }
}
