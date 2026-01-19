using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    /// <summary>
    /// Background service that runs daily to:
    /// 1. Generate shared festival promo codes 7 days before festivals
    /// 2. Notify all users about upcoming festival promos
    /// 3. Expire festival codes the day after the festival
    /// </summary>
    public class FestivalPromoBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FestivalPromoBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6); // Check every 6 hours

        public FestivalPromoBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<FestivalPromoBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Festival Promo Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessFestivalPromosAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing festival promos");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task ProcessFestivalPromosAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateTime.UtcNow.Date;

            // First, expire ALL festival coupons whose EndDate has passed
            // This handles the case where a festival has passed and the coupon should be expired
            await ExpireAllPastFestivalCouponsAsync(context);

            // Get all active festivals
            var festivals = await context.FestivalDates
                .Where(f => f.IsActive)
                .ToListAsync();

            foreach (var festival in festivals)
            {
                var festivalDate = GetFestivalDate(festival, today);
                if (festivalDate == null) continue;

                var notifyFrom = festivalDate.Value.AddDays(-festival.NotifyDaysBefore);
                var festivalYear = festivalDate.Value.Year;
                var festivalKey = $"{festival.Name.Replace(" ", "")}_{festivalYear}";

                // Check if we should create promo for this festival (within notification window)
                if (today >= notifyFrom && today <= festivalDate.Value)
                {
                    // Check if shared promo already exists for this festival
                    var existingCoupon = await context.Coupons
                        .FirstOrDefaultAsync(c => c.Code.StartsWith(festivalKey.Substring(0, Math.Min(10, festivalKey.Length))));

                    if (existingCoupon == null)
                    {
                        // Create shared festival promo code
                        await CreateSharedFestivalPromoAsync(context, festival, festivalDate.Value, festivalKey);
                        _logger.LogInformation("Created shared festival promo for {Festival}", festival.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Expires all festival coupons whose EndDate has passed, regardless of festival key.
        /// This ensures coupons are properly expired even after the festival date calculation rolls over to next year.
        /// </summary>
        private async Task ExpireAllPastFestivalCouponsAsync(ApplicationDbContext context)
        {
            // Find all active coupons that have expired based on their EndDate
            // Festival coupons typically have descriptions containing "Festival Offer"
            var expiredCoupons = await context.Coupons
                .Where(c => c.IsActive &&
                           c.EndDate.HasValue &&
                           c.EndDate < DateTime.UtcNow &&
                           c.Description != null &&
                           c.Description.Contains("Festival"))
                .ToListAsync();

            foreach (var coupon in expiredCoupons)
            {
                coupon.IsActive = false;
                coupon.UpdatedAt = DateTime.UtcNow;
            }

            if (expiredCoupons.Any())
            {
                await context.SaveChangesAsync();
                _logger.LogInformation("Expired {Count} past festival coupons", expiredCoupons.Count);
            }
        }

        private DateTime? GetFestivalDate(FestivalDate festival, DateTime today)
        {
            // For fixed date festivals
            if (festival.FixedMonth.HasValue && festival.FixedDay.HasValue)
            {
                var thisYear = new DateTime(today.Year, festival.FixedMonth.Value, festival.FixedDay.Value);
                if (thisYear < today.AddDays(-1)) // Already passed this year
                {
                    thisYear = thisYear.AddYears(1);
                }
                return thisYear;
            }

            // For variable date festivals (Eid, Puja etc.)
            if (festival.NextOccurrence.HasValue)
            {
                return festival.NextOccurrence.Value;
            }

            return null;
        }

        private async Task CreateSharedFestivalPromoAsync(
            ApplicationDbContext context,
            FestivalDate festival,
            DateTime festivalDate,
            string festivalKey)
        {
            // Generate promo code
            var promoCode = $"{festivalKey.ToUpper().Substring(0, Math.Min(8, festivalKey.Length))}{DateTime.UtcNow.Year % 100}";

            // Ensure unique
            var suffix = 1;
            var baseCode = promoCode;
            while (await context.Coupons.AnyAsync(c => c.Code == promoCode))
            {
                promoCode = $"{baseCode}{suffix++}";
            }

            // Create shared coupon - expires the day after the festival
            var coupon = new Coupon
            {
                Code = promoCode,
                Description = $"{festival.Name} ({festival.NameBangla ?? ""}) Festival Offer - {festival.DiscountPercentage}% off",
                DiscountType = DiscountType.Percentage,
                DiscountValue = festival.DiscountPercentage,
                MinimumOrderAmount = 300,
                MaximumDiscountAmount = 500,
                UsageLimit = null, // No total limit - anyone can use
                UsagePerUserLimit = 1, // Each user can use only once
                IsActive = true,
                StartDate = DateTime.UtcNow,
                EndDate = festivalDate.AddDays(1).Date, // Expires the day after festival
                CreatedAt = DateTime.UtcNow
            };

            context.Coupons.Add(coupon);
            await context.SaveChangesAsync();

            // Notify all users about this festival promo
            await SendFestivalNotificationsToAllUsersAsync(context, festival, coupon, festivalKey);
        }

        private async Task SendFestivalNotificationsToAllUsersAsync(
            ApplicationDbContext context,
            FestivalDate festival,
            Coupon coupon,
            string festivalKey)
        {
            // Get all users who have festival offers enabled
            var usersWithPrefs = await context.NotificationPreferences
                .Where(np => np.FestivalOffers)
                .Select(np => np.UserId)
                .ToListAsync();

            // Also get users without preferences (default is enabled)
            var allUserIds = await context.Users
                .Select(u => u.Id)
                .ToListAsync();

            // Users with prefs disabled
            var usersWithFestivalDisabled = await context.NotificationPreferences
                .Where(np => !np.FestivalOffers)
                .Select(np => np.UserId)
                .ToListAsync();

            // Final list: all users except those who disabled festival offers
            var targetUsers = allUserIds.Except(usersWithFestivalDisabled).ToList();

            var banglaName = !string.IsNullOrEmpty(festival.NameBangla) ? $" ({festival.NameBangla})" : "";

            var notifications = new List<UserNotification>();
            foreach (var userId in targetUsers)
            {
                // Check if notification already exists for this user for this festival
                var exists = await context.UserNotifications
                    .AnyAsync(n => n.UserId == userId &&
                                   n.Type == "festival" &&
                                   n.FestivalName == festivalKey);

                if (!exists)
                {
                    notifications.Add(new UserNotification
                    {
                        UserId = userId,
                        Type = "festival",
                        Icon = "fa-star",
                        IconColor = "text-warning",
                        Title = $"{festival.Name}{banglaName} Offer!",
                        Message = $"Celebrate {festival.Name} with {festival.DiscountPercentage}% off! Use code: {coupon.Code}. Valid until festival day.",
                        Link = "/",
                        PromoCode = coupon.Code,
                        CouponId = coupon.Id,
                        FestivalName = festivalKey,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            if (notifications.Any())
            {
                context.UserNotifications.AddRange(notifications);
                await context.SaveChangesAsync();
                _logger.LogInformation("Sent {Count} festival notifications for {Festival}",
                    notifications.Count, festival.Name);
            }
        }

    }
}
