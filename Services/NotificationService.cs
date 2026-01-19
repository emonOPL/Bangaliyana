using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    public interface INotificationService
    {
        Task<List<UserNotification>> GetUserNotificationsAsync(string userId, NotificationPreference? preferences = null);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(int notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
        Task<NotificationPreference> GetOrCreatePreferencesAsync(string userId);
        Task SavePreferencesAsync(string userId, NotificationPreference preferences);
        Task CreateNotificationAsync(UserNotification notification);
        Task CreateWelcomeNotificationAsync(string userId);
        Task CreateFirstOrderPromoAsync(string userId);
        Task CheckAndCreateFestivalNotificationsAsync(string userId);
        Task<List<FestivalDate>> GetUpcomingFestivalsAsync(int daysAhead = 7);
        Task SeedFestivalDatesAsync();
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationService> _logger;
        private readonly IRealTimeNotificationService _realTimeService;
        private readonly ISiteSettingsService _siteSettingsService;

        public NotificationService(
            ApplicationDbContext context,
            ILogger<NotificationService> logger,
            IRealTimeNotificationService realTimeService,
            ISiteSettingsService siteSettingsService)
        {
            _context = context;
            _logger = logger;
            _realTimeService = realTimeService;
            _siteSettingsService = siteSettingsService;
        }

        public async Task<List<UserNotification>> GetUserNotificationsAsync(string userId, NotificationPreference? preferences = null)
        {
            // Get user preferences if not provided
            if (preferences == null)
            {
                preferences = await GetOrCreatePreferencesAsync(userId);
            }

            // Sync order notifications to database
            await SyncOrderNotificationsAsync(userId, preferences);

            // Sync ticket notifications to database
            await SyncTicketNotificationsAsync(userId, preferences);

            // Get all stored notifications with preference filtering
            var query = _context.UserNotifications
                .Where(n => n.UserId == userId);

            // Apply preference filtering
            var allowedTypes = new List<string>();

            if (preferences.OrderUpdates || preferences.DeliveryAlerts)
                allowedTypes.Add("order");
            if (preferences.SupportReplies)
                allowedTypes.Add("support");
            if (preferences.Promotions)
                allowedTypes.Add("promo");
            if (preferences.FestivalOffers)
                allowedTypes.Add("festival");

            // Always show welcome and reward notifications
            allowedTypes.Add("welcome");
            allowedTypes.Add("reward");

            query = query.Where(n => allowedTypes.Contains(n.Type));

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return notifications;
        }

        private async Task SyncOrderNotificationsAsync(string userId, NotificationPreference preferences)
        {
            if (!preferences.OrderUpdates && !preferences.DeliveryAlerts)
                return;

            // Get recent orders
            var recentOrders = await _context.Orders
                .Where(o => o.UserId == userId && o.UpdatedAt >= DateTime.UtcNow.AddDays(-30))
                .OrderByDescending(o => o.UpdatedAt)
                .Take(20)
                .ToListAsync();

            foreach (var order in recentOrders)
            {
                // Check if notification already exists for this order and status
                var existingNotification = await _context.UserNotifications
                    .FirstOrDefaultAsync(n => n.UserId == userId &&
                                              n.OrderId == order.Id &&
                                              n.Type == "order" &&
                                              n.Title == GetOrderNotificationTitle(order.Status));

                if (existingNotification == null)
                {
                    var notification = GenerateOrderNotification(order, userId);
                    if (notification != null)
                    {
                        _context.UserNotifications.Add(notification);
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private string GetOrderNotificationTitle(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Confirmed => "Order Confirmed",
                OrderStatus.Shipped => "Order Shipped",
                OrderStatus.OutForDelivery => "Out for Delivery",
                OrderStatus.Delivered => "Order Delivered",
                OrderStatus.Cancelled => "Order Cancelled",
                OrderStatus.Refunded => "Refund Processed",
                _ => ""
            };
        }

        private async Task SyncTicketNotificationsAsync(string userId, NotificationPreference preferences)
        {
            if (!preferences.SupportReplies)
                return;

            // Get recent tickets with messages
            var recentTickets = await _context.Tickets
                .Where(t => t.UserId == userId)
                .Include(t => t.Messages)
                .OrderByDescending(t => t.UpdatedAt)
                .Take(10)
                .ToListAsync();

            foreach (var ticket in recentTickets)
            {
                var staffReplies = ticket.Messages?
                    .Where(m => m.IsStaffReply && m.CreatedAt >= DateTime.UtcNow.AddDays(-30))
                    .OrderByDescending(m => m.CreatedAt)
                    .ToList();

                if (staffReplies?.Any() == true)
                {
                    foreach (var reply in staffReplies.Take(3))
                    {
                        // Check if notification already exists
                        var exists = await _context.UserNotifications
                            .AnyAsync(n => n.UserId == userId &&
                                          n.TicketId == ticket.Id &&
                                          n.Type == "support" &&
                                          n.CreatedAt == reply.CreatedAt);

                        if (!exists)
                        {
                            var notification = new UserNotification
                            {
                                UserId = userId,
                                Type = "support",
                                Icon = "fa-headset",
                                IconColor = "text-info",
                                Title = "Support Reply",
                                Message = $"Staff replied to your ticket #{ticket.TicketNumber}",
                                Link = "/Identity/Account/Manage/SupportTickets",
                                TicketId = ticket.Id,
                                CreatedAt = reply.CreatedAt,
                                IsRead = false
                            };
                            _context.UserNotifications.Add(notification);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }

        private UserNotification? GenerateOrderNotification(Order order, string userId)
        {
            string icon, iconColor, title, message;

            switch (order.Status)
            {
                case OrderStatus.Confirmed:
                    icon = "fa-check-circle";
                    iconColor = "text-success";
                    title = "Order Confirmed";
                    message = $"Order #{order.OrderNumber} has been confirmed and is being processed.";
                    break;

                case OrderStatus.Shipped:
                    icon = "fa-shipping-fast";
                    iconColor = "text-primary";
                    title = "Order Shipped";
                    message = $"Order #{order.OrderNumber} is on its way! Track: {order.TrackingNumber ?? "N/A"}";
                    break;

                case OrderStatus.OutForDelivery:
                    icon = "fa-truck";
                    iconColor = "text-warning";
                    title = "Out for Delivery";
                    message = $"Order #{order.OrderNumber} will be delivered today!";
                    break;

                case OrderStatus.Delivered:
                    icon = "fa-box-open";
                    iconColor = "text-success";
                    title = "Order Delivered";
                    message = $"Order #{order.OrderNumber} has been delivered. Enjoy!";
                    break;

                case OrderStatus.Cancelled:
                    icon = "fa-times-circle";
                    iconColor = "text-danger";
                    title = "Order Cancelled";
                    message = $"Order #{order.OrderNumber} has been cancelled.";
                    break;

                case OrderStatus.Refunded:
                    icon = "fa-money-bill-wave";
                    iconColor = "text-info";
                    title = "Refund Processed";
                    message = $"Refund for Order #{order.OrderNumber} has been processed.";
                    break;

                default:
                    return null;
            }

            return new UserNotification
            {
                UserId = userId,
                Type = "order",
                Icon = icon,
                IconColor = iconColor,
                Title = title,
                Message = message,
                Link = "/Identity/Account/Manage/Orders",
                OrderId = order.Id,
                CreatedAt = order.UpdatedAt,
                IsRead = false
            };
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.UserNotifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task MarkAsReadAsync(int notificationId, string userId)
        {
            var notification = await _context.UserNotifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            var unreadNotifications = await _context.UserNotifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<NotificationPreference> GetOrCreatePreferencesAsync(string userId)
        {
            var preferences = await _context.NotificationPreferences
                .FirstOrDefaultAsync(np => np.UserId == userId);

            if (preferences == null)
            {
                preferences = new NotificationPreference
                {
                    UserId = userId,
                    OrderUpdates = true,
                    DeliveryAlerts = true,
                    Promotions = true,
                    SupportReplies = true,
                    PriceDrops = true,
                    Newsletter = false,
                    FestivalOffers = true
                };
                _context.NotificationPreferences.Add(preferences);
                await _context.SaveChangesAsync();
            }

            return preferences;
        }

        public async Task SavePreferencesAsync(string userId, NotificationPreference updatedPreferences)
        {
            var preferences = await _context.NotificationPreferences
                .FirstOrDefaultAsync(np => np.UserId == userId);

            if (preferences == null)
            {
                updatedPreferences.UserId = userId;
                updatedPreferences.CreatedAt = DateTime.UtcNow;
                _context.NotificationPreferences.Add(updatedPreferences);
            }
            else
            {
                preferences.ReceiveEmails = updatedPreferences.ReceiveEmails;
                preferences.OrderUpdates = updatedPreferences.OrderUpdates;
                preferences.DeliveryAlerts = updatedPreferences.DeliveryAlerts;
                preferences.Promotions = updatedPreferences.Promotions;
                preferences.SupportReplies = updatedPreferences.SupportReplies;
                preferences.PriceDrops = updatedPreferences.PriceDrops;
                preferences.Newsletter = updatedPreferences.Newsletter;
                preferences.FestivalOffers = updatedPreferences.FestivalOffers;
                preferences.SellerMessages = updatedPreferences.SellerMessages;
                preferences.FollowedShopNewProducts = updatedPreferences.FollowedShopNewProducts;
                preferences.FollowedShopPriceDrops = updatedPreferences.FollowedShopPriceDrops;
                preferences.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task CreateNotificationAsync(UserNotification notification)
        {
            notification.CreatedAt = DateTime.UtcNow;
            _context.UserNotifications.Add(notification);
            await _context.SaveChangesAsync();

            // Push real-time notification via SignalR
            try
            {
                await _realTimeService.SendToUserAsync(notification.UserId, _realTimeService.ToDto(notification));

                // Update unread count
                var count = await GetUnreadCountAsync(notification.UserId);
                await _realTimeService.UpdateUnreadCountAsync(notification.UserId, count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send real-time notification to user {UserId}", notification.UserId);
                // Don't throw - database notification is saved, real-time is best-effort
            }
        }

        public async Task CreateWelcomeNotificationAsync(string userId)
        {
            // Check if welcome notification already exists
            var exists = await _context.UserNotifications
                .AnyAsync(n => n.UserId == userId && n.Type == "welcome");

            if (!exists)
            {
                // Get site name from settings
                var siteSettings = await _siteSettingsService.GetSiteSettingsAsync();
                var siteName = siteSettings?.SiteName ?? "Bangaliyana";

                var notification = new UserNotification
                {
                    UserId = userId,
                    Type = "welcome",
                    Icon = "fa-gift",
                    IconColor = "text-purple",
                    Title = $"Welcome to {siteName}!",
                    Message = "Thank you for joining us. Explore our products and enjoy shopping!",
                    Link = "/",
                    CreatedAt = DateTime.UtcNow
                };
                await CreateNotificationAsync(notification);
            }
        }

        public async Task CreateFirstOrderPromoAsync(string userId)
        {
            // Check if user already has first order promo
            var existingPromo = await _context.UserNotifications
                .AnyAsync(n => n.UserId == userId && n.Type == "promo" && n.PromoCode != null && n.PromoCode.StartsWith("WELCOME"));

            if (existingPromo)
                return;

            // Check if user has placed any orders
            var hasOrders = await _context.Orders.AnyAsync(o => o.UserId == userId);
            if (hasOrders)
                return;

            // Generate unique promo code
            var promoCode = $"WELCOME{GenerateRandomCode(6)}";

            // Check if coupon already exists with this code
            while (await _context.Coupons.AnyAsync(c => c.Code == promoCode))
            {
                promoCode = $"WELCOME{GenerateRandomCode(6)}";
            }

            // Create coupon
            var coupon = new Coupon
            {
                Code = promoCode,
                Description = "Welcome discount for new users - 10% off first order",
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10,
                MinimumOrderAmount = 500,
                MaximumDiscountAmount = 500,
                UsageLimit = 1,
                UsagePerUserLimit = 1,
                IsFirstOrderOnly = true,
                IsActive = true,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30),
                CreatedAt = DateTime.UtcNow
            };

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            // Create notification
            var notification = new UserNotification
            {
                UserId = userId,
                Type = "promo",
                Icon = "fa-percentage",
                IconColor = "text-danger",
                Title = "Welcome Gift: 10% Off Your First Order!",
                Message = $"Use code {promoCode} to get 10% off your first order. Valid for 30 days. Min order: ৳500",
                Link = "/",
                PromoCode = promoCode,
                CouponId = coupon.Id,
                CreatedAt = DateTime.UtcNow
            };

            await CreateNotificationAsync(notification);
        }

        public async Task CheckAndCreateFestivalNotificationsAsync(string userId)
        {
            var preferences = await GetOrCreatePreferencesAsync(userId);
            if (!preferences.FestivalOffers)
                return;

            var upcomingFestivals = await GetUpcomingFestivalsAsync(7);

            foreach (var festival in upcomingFestivals)
            {
                // Check if notification already sent for this festival this year
                var festivalYear = festival.NextOccurrence?.Year ?? DateTime.UtcNow.Year;
                var notificationKey = $"{festival.Name.Replace(" ", "")}_{festivalYear}";

                var exists = await _context.UserNotifications
                    .AnyAsync(n => n.UserId == userId &&
                                   n.Type == "festival" &&
                                   n.FestivalName == notificationKey);

                if (!exists)
                {
                    await CreateFestivalNotificationAsync(userId, festival, notificationKey);
                }
            }
        }

        private async Task CreateFestivalNotificationAsync(string userId, FestivalDate festival, string festivalKey)
        {
            // Check if there's already a shared festival coupon for this festival
            var codePrefix = festivalKey.ToUpper().Substring(0, Math.Min(8, festivalKey.Length));
            var existingCoupon = await _context.Coupons
                .Where(c => c.Code.StartsWith(codePrefix) && c.IsActive && c.EndDate > DateTime.UtcNow)
                .FirstOrDefaultAsync();

            string promoCode;
            int? couponId = null;

            if (existingCoupon != null)
            {
                // Use existing shared coupon
                promoCode = existingCoupon.Code;
                couponId = existingCoupon.Id;
            }
            else
            {
                // Create shared promo code (expires day after festival)
                promoCode = $"{codePrefix}{DateTime.UtcNow.Year % 100}";

                // Ensure unique
                var suffix = 1;
                var baseCode = promoCode;
                while (await _context.Coupons.AnyAsync(c => c.Code == promoCode))
                {
                    promoCode = $"{baseCode}{suffix++}";
                }

                // Create shared coupon - UsagePerUserLimit = 1 means each user can use only once
                var coupon = new Coupon
                {
                    Code = promoCode,
                    Description = $"{festival.Name} ({festival.NameBangla ?? ""}) Festival Offer - {festival.DiscountPercentage}% off",
                    DiscountType = DiscountType.Percentage,
                    DiscountValue = festival.DiscountPercentage,
                    MinimumOrderAmount = 300,
                    MaximumDiscountAmount = 500,
                    UsageLimit = null, // No total limit - shared code for everyone
                    UsagePerUserLimit = 1, // Each user can use only once
                    IsActive = true,
                    StartDate = DateTime.UtcNow,
                    EndDate = festival.NextOccurrence?.AddDays(1).Date ?? DateTime.UtcNow.AddDays(1), // Expires day after festival
                    CreatedAt = DateTime.UtcNow
                };

                _context.Coupons.Add(coupon);
                await _context.SaveChangesAsync();
                couponId = coupon.Id;
            }

            var banglaName = !string.IsNullOrEmpty(festival.NameBangla) ? $" ({festival.NameBangla})" : "";

            var notification = new UserNotification
            {
                UserId = userId,
                Type = "festival",
                Icon = "fa-star",
                IconColor = "text-warning",
                Title = $"{festival.Name}{banglaName} Offer!",
                Message = $"Celebrate {festival.Name} with {festival.DiscountPercentage}% off! Use code: {promoCode}. Valid until festival day.",
                Link = "/",
                PromoCode = promoCode,
                CouponId = couponId,
                FestivalName = festivalKey,
                CreatedAt = DateTime.UtcNow
            };

            await CreateNotificationAsync(notification);
        }

        public async Task<List<FestivalDate>> GetUpcomingFestivalsAsync(int daysAhead = 7)
        {
            var today = DateTime.UtcNow.Date;
            var targetDate = today.AddDays(daysAhead);
            var festivals = new List<FestivalDate>();

            var allFestivals = await _context.FestivalDates
                .Where(f => f.IsActive)
                .ToListAsync();

            foreach (var festival in allFestivals)
            {
                DateTime? festivalDate = null;

                // For fixed date festivals
                if (festival.FixedMonth.HasValue && festival.FixedDay.HasValue)
                {
                    var thisYear = new DateTime(today.Year, festival.FixedMonth.Value, festival.FixedDay.Value);
                    if (thisYear < today)
                    {
                        thisYear = thisYear.AddYears(1);
                    }
                    festivalDate = thisYear;
                }
                // For variable date festivals (Eid, Puja etc.)
                else if (festival.NextOccurrence.HasValue)
                {
                    festivalDate = festival.NextOccurrence.Value;
                }

                if (festivalDate.HasValue)
                {
                    var notifyFrom = festivalDate.Value.AddDays(-festival.NotifyDaysBefore);

                    // Check if we should notify (within the notification window)
                    if (today >= notifyFrom && today <= festivalDate.Value)
                    {
                        festival.NextOccurrence = festivalDate;
                        festivals.Add(festival);
                    }
                }
            }

            return festivals;
        }

        public async Task SeedFestivalDatesAsync()
        {
            if (await _context.FestivalDates.AnyAsync())
                return;

            var festivals = new List<FestivalDate>
            {
                // Fixed date festivals
                new FestivalDate
                {
                    Name = "English New Year",
                    NameBangla = "ইংরেজি নববর্ষ",
                    FixedMonth = 1,
                    FixedDay = 1,
                    NotifyDaysBefore = 7,
                    DiscountPercentage = 5,
                    IsActive = true
                },
                new FestivalDate
                {
                    Name = "Bangla New Year",
                    NameBangla = "পহেলা বৈশাখ",
                    FixedMonth = 4,
                    FixedDay = 14,
                    NotifyDaysBefore = 7,
                    DiscountPercentage = 5,
                    IsActive = true
                },
                new FestivalDate
                {
                    Name = "Victory Day",
                    NameBangla = "বিজয় দিবস",
                    FixedMonth = 12,
                    FixedDay = 16,
                    NotifyDaysBefore = 7,
                    DiscountPercentage = 5,
                    IsActive = true
                },
                new FestivalDate
                {
                    Name = "Independence Day",
                    NameBangla = "স্বাধীনতা দিবস",
                    FixedMonth = 3,
                    FixedDay = 26,
                    NotifyDaysBefore = 7,
                    DiscountPercentage = 5,
                    IsActive = true
                },
                // Variable date festivals - These need yearly updates
                // Using approximate 2025 dates
                new FestivalDate
                {
                    Name = "Eid ul-Fitr",
                    NameBangla = "ঈদুল ফিতর",
                    NextOccurrence = new DateTime(2025, 3, 31), // Approximate date for 2025
                    NotifyDaysBefore = 7,
                    DiscountPercentage = 5,
                    IsActive = true
                },
                new FestivalDate
                {
                    Name = "Eid ul-Adha",
                    NameBangla = "ঈদুল আযহা",
                    NextOccurrence = new DateTime(2025, 6, 7), // Approximate date for 2025
                    NotifyDaysBefore = 7,
                    DiscountPercentage = 5,
                    IsActive = true
                },
                new FestivalDate
                {
                    Name = "Durga Puja",
                    NameBangla = "দুর্গা পূজা",
                    NextOccurrence = new DateTime(2025, 10, 1), // Approximate date for 2025
                    NotifyDaysBefore = 7,
                    DiscountPercentage = 5,
                    IsActive = true
                },
                new FestivalDate
                {
                    Name = "Diwali",
                    NameBangla = "দীপাবলি",
                    NextOccurrence = new DateTime(2025, 10, 20), // Approximate date for 2025
                    NotifyDaysBefore = 7,
                    DiscountPercentage = 5,
                    IsActive = true
                },
                new FestivalDate
                {
                    Name = "Christmas",
                    NameBangla = "বড়দিন",
                    FixedMonth = 12,
                    FixedDay = 25,
                    NotifyDaysBefore = 7,
                    DiscountPercentage = 5,
                    IsActive = true
                }
            };

            _context.FestivalDates.AddRange(festivals);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Festival dates seeded successfully");
        }

        private string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
