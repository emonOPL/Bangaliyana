using Bangaliyana.Data;
using Bangaliyana.Models;
using Bangaliyana.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Bangaliyana.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    [EnableRateLimiting("payment")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<PaymentController> _logger;
        private readonly IEmailService _emailService;
        private readonly IStringLocalizer<SharedResources> _localizer;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(ApplicationDbContext db, ILogger<PaymentController> logger, IEmailService emailService, IStringLocalizer<SharedResources> localizer, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _logger = logger;
            _emailService = emailService;
            _localizer = localizer;
            _userManager = userManager;
        }

        // Called after order is created to start payment flow
        public async Task<IActionResult> StartPayment(int orderId)
        {
            var userId = _userManager.GetUserId(User);

            // 1) Get order from DB
            var order = await _db.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return NotFound();

            // Security check: Ensure user owns this order
            if (order.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to access payment for order {OrderId} belonging to another user", userId, orderId);
                return Forbid();
            }

            // 2) Redirect to gateway or open payment page.
            // For live integration you would:
            //  - create a payment session with bKash/Nagad/Rocket API, get a checkout URL or token
            //  - redirect user to that URL
            // Here we show a placeholder page informing the user to pay and then webhook/callback will be handled.
            return View(order);
        }

        // Gateway callback (POST) — gateway posts transaction result to this endpoint
        // Note: In production, validate callback signature from payment gateway
        [HttpPost]
        public async Task<IActionResult> PaymentCallback(int orderId, string transactionId, bool success)
        {
            _logger.LogInformation("Payment callback received for Order {OrderId}, TransactionId: {TransactionId}, Success: {Success}", orderId, transactionId, success);

            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                _logger.LogWarning("Payment callback for non-existent order {OrderId}", orderId);
                return NotFound();
            }

            // Validate order is in a state that can receive payment
            if (order.IsPaymentReceived)
            {
                _logger.LogWarning("Duplicate payment callback for order {OrderId} which already has payment received", orderId);
                return BadRequest("Payment already processed");
            }

            if (success)
            {
                order.IsPaymentReceived = true;
                order.TransactionId = transactionId;
                order.Status = OrderStatus.Paid;
                order.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                order.IsPaymentReceived = false;
                order.Status = OrderStatus.Pending; // or Cancelled depending on gateway result
                order.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            if (success)
            {
                // send order confirmation
                await _emailService.SendEmailAsync(order.Email, "Payment Received", $"Payment received for order #{order.Id}. Txn: {transactionId}");
                _logger.LogInformation("Payment confirmed for Order {OrderId}", orderId);
            }

            // Return a simple response (gateway may expect 200) or redirect user to confirmation page
            return Ok();
        }
    }
}
