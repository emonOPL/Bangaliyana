using System;
using System.Collections.Generic;
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
    public class OrdersModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;

        public OrdersModel(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
        {
            _userManager = userManager;
            _db = db;
        }

        public List<Order> Orders { get; set; } = new List<Order>();

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Get all orders for the current user (by email)
            Orders = await _db.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.Email == user.Email)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostCancelOrderAsync(int orderId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.Email == user.Email);
            if (order == null)
            {
                return NotFound("Order not found.");
            }

            // Only allow cancellation if order is pending
            if (order.Status == OrderStatus.Pending)
            {
                order.Status = OrderStatus.Cancelled;
                await _db.SaveChangesAsync();
                StatusMessage = $"Order #{orderId} has been cancelled.";
            }
            else
            {
                StatusMessage = "Error: Order cannot be cancelled at this stage.";
            }

            return RedirectToPage();
        }
    }
}