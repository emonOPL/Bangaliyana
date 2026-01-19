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
using Bangaliyana.Services;

namespace Bangaliyana.Areas.Identity.Pages.Account.Manage
{
    public class NotificationsModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public NotificationsModel(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            INotificationService notificationService)
        {
            _userManager = userManager;
            _context = context;
            _notificationService = notificationService;
        }

        public List<NotificationDisplayItem> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
        public NotificationPreference Preferences { get; set; } = new();

        [BindProperty]
        public PreferencesInputModel PreferencesInput { get; set; } = new();

        public class PreferencesInputModel
        {
            public bool ReceiveEmails { get; set; } = true;
            public bool OrderUpdates { get; set; } = true;
            public bool DeliveryAlerts { get; set; } = true;
            public bool Promotions { get; set; } = true;
            public bool SupportReplies { get; set; } = true;
            public bool PriceDrops { get; set; } = true;
            public bool Newsletter { get; set; } = false;
            public bool FestivalOffers { get; set; } = true;
            public bool SellerMessages { get; set; } = true;
            public bool FollowedShopNewProducts { get; set; } = true;
            public bool FollowedShopPriceDrops { get; set; } = true;
        }

        [TempData]
        public string? StatusMessage { get; set; }

        public class NotificationDisplayItem
        {
            public int Id { get; set; }
            public string Type { get; set; } = string.Empty;
            public string Icon { get; set; } = string.Empty;
            public string IconColor { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public string? Link { get; set; }
            public bool IsNew { get; set; }
            public string? PromoCode { get; set; }
            public int? OrderId { get; set; }
            public int? TicketId { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? markRead = null, string? redirect = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            // Mark specific notification as read if requested
            if (markRead.HasValue)
            {
                await _notificationService.MarkAsReadAsync(markRead.Value, user.Id);

                // Redirect to the notification's link if provided
                if (!string.IsNullOrEmpty(redirect))
                {
                    return Redirect(redirect);
                }
            }

            // Check and create welcome notification for new users
            if (user.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            {
                await _notificationService.CreateWelcomeNotificationAsync(user.Id);
            }

            // Create first order promo for new users without orders
            await _notificationService.CreateFirstOrderPromoAsync(user.Id);

            // Check and create festival notifications
            await _notificationService.CheckAndCreateFestivalNotificationsAsync(user.Id);

            // Get preferences
            Preferences = await _notificationService.GetOrCreatePreferencesAsync(user.Id);

            // Populate the input model from preferences
            PreferencesInput = new PreferencesInputModel
            {
                ReceiveEmails = Preferences.ReceiveEmails,
                OrderUpdates = Preferences.OrderUpdates,
                DeliveryAlerts = Preferences.DeliveryAlerts,
                Promotions = Preferences.Promotions,
                SupportReplies = Preferences.SupportReplies,
                PriceDrops = Preferences.PriceDrops,
                Newsletter = Preferences.Newsletter,
                FestivalOffers = Preferences.FestivalOffers,
                SellerMessages = Preferences.SellerMessages,
                FollowedShopNewProducts = Preferences.FollowedShopNewProducts,
                FollowedShopPriceDrops = Preferences.FollowedShopPriceDrops
            };

            // Get all notifications with preference filtering
            var notifications = await _notificationService.GetUserNotificationsAsync(user.Id, Preferences);

            // Map to display items
            Notifications = notifications.Select(n => new NotificationDisplayItem
            {
                Id = n.Id,
                Type = n.Type,
                Icon = n.Icon,
                IconColor = n.IconColor,
                Title = n.Title,
                Message = n.Message,
                CreatedAt = n.CreatedAt,
                Link = n.Link,
                IsNew = !n.IsRead,
                PromoCode = n.PromoCode,
                OrderId = n.OrderId,
                TicketId = n.TicketId
            }).ToList();

            UnreadCount = Notifications.Count(n => n.IsNew);

            return Page();
        }

        public async Task<IActionResult> OnPostMarkAllReadAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            await _notificationService.MarkAllAsReadAsync(user.Id);
            StatusMessage = "All notifications marked as read.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSavePreferencesAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            var preferences = new NotificationPreference
            {
                UserId = user.Id,
                ReceiveEmails = PreferencesInput.ReceiveEmails,
                OrderUpdates = PreferencesInput.OrderUpdates,
                DeliveryAlerts = PreferencesInput.DeliveryAlerts,
                Promotions = PreferencesInput.Promotions,
                SupportReplies = PreferencesInput.SupportReplies,
                PriceDrops = PreferencesInput.PriceDrops,
                Newsletter = PreferencesInput.Newsletter,
                FestivalOffers = PreferencesInput.FestivalOffers,
                SellerMessages = PreferencesInput.SellerMessages,
                FollowedShopNewProducts = PreferencesInput.FollowedShopNewProducts,
                FollowedShopPriceDrops = PreferencesInput.FollowedShopPriceDrops
            };

            await _notificationService.SavePreferencesAsync(user.Id, preferences);
            StatusMessage = "Notification preferences saved successfully.";
            return RedirectToPage();
        }
    }
}
