using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    public interface IPromotionalCampaignService
    {
        Task<List<PromotionalCampaign>> GetAllCampaignsAsync();
        Task<PromotionalCampaign?> GetCampaignAsync(int id);
        Task<PromotionalCampaign> CreateCampaignAsync(PromotionalCampaign campaign);
        Task UpdateCampaignAsync(PromotionalCampaign campaign);
        Task DeleteCampaignAsync(int id);
        Task<int> SendCampaignAsync(int campaignId);
        Task<CampaignAnalytics> GetCampaignAnalyticsAsync(int campaignId);
        Task<int> GetTargetUserCountAsync(TargetAudience audience, int? minOrderCount = null, decimal? minTotalSpent = null);
        Task ProcessScheduledCampaignsAsync();
        Task RecordNotificationReadAsync(int notificationId);
        Task RecordNotificationClickAsync(int notificationId);
    }

    public class CampaignAnalytics
    {
        public int TotalRecipients { get; set; }
        public int DeliveredCount { get; set; }
        public int ReadCount { get; set; }
        public int ClickCount { get; set; }
        public decimal ReadRate { get; set; }
        public decimal ClickRate { get; set; }
        public int EmailsSent { get; set; }
        public List<CampaignRecipient> Recipients { get; set; } = new();
        public Dictionary<DateTime, int> ReadsByDate { get; set; } = new();
    }

    public class PromotionalCampaignService : IPromotionalCampaignService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<PromotionalCampaignService> _logger;

        public PromotionalCampaignService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IEmailService emailService,
            ILogger<PromotionalCampaignService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<List<PromotionalCampaign>> GetAllCampaignsAsync()
        {
            return await _context.PromotionalCampaigns
                .Include(pc => pc.Coupon)
                .Include(pc => pc.CreatedBy)
                .OrderByDescending(pc => pc.CreatedAt)
                .ToListAsync();
        }

        public async Task<PromotionalCampaign?> GetCampaignAsync(int id)
        {
            return await _context.PromotionalCampaigns
                .Include(pc => pc.Coupon)
                .Include(pc => pc.CreatedBy)
                .Include(pc => pc.Recipients)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(pc => pc.Id == id);
        }

        public async Task<PromotionalCampaign> CreateCampaignAsync(PromotionalCampaign campaign)
        {
            campaign.CreatedAt = DateTime.UtcNow;
            campaign.UpdatedAt = DateTime.UtcNow;
            campaign.Status = campaign.IsImmediate ? CampaignStatus.Draft : CampaignStatus.Scheduled;

            _context.PromotionalCampaigns.Add(campaign);
            await _context.SaveChangesAsync();

            return campaign;
        }

        public async Task UpdateCampaignAsync(PromotionalCampaign campaign)
        {
            campaign.UpdatedAt = DateTime.UtcNow;
            _context.PromotionalCampaigns.Update(campaign);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCampaignAsync(int id)
        {
            var campaign = await _context.PromotionalCampaigns.FindAsync(id);
            if (campaign != null)
            {
                _context.PromotionalCampaigns.Remove(campaign);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<int> SendCampaignAsync(int campaignId)
        {
            var campaign = await _context.PromotionalCampaigns
                .Include(pc => pc.Coupon)
                .FirstOrDefaultAsync(pc => pc.Id == campaignId);

            if (campaign == null)
            {
                throw new ArgumentException("Campaign not found");
            }

            if (campaign.Status == CampaignStatus.Sent)
            {
                throw new InvalidOperationException("Campaign has already been sent");
            }

            campaign.Status = CampaignStatus.Sending;
            await _context.SaveChangesAsync();

            try
            {
                // Get target users
                var targetUsers = await GetTargetUsersAsync(campaign.TargetAudience, campaign.MinOrderCount, campaign.MinTotalSpent);

                var recipientCount = 0;
                var emailsSent = 0;

                foreach (var user in targetUsers)
                {
                    // Check user preference for promotions
                    var preference = await _context.NotificationPreferences
                        .FirstOrDefaultAsync(np => np.UserId == user.Id);

                    if (preference != null && !preference.Promotions)
                    {
                        continue;
                    }

                    // Create notification
                    var notification = new UserNotification
                    {
                        UserId = user.Id,
                        Type = "promo",
                        Icon = campaign.IconClass,
                        IconColor = campaign.IconColor,
                        Title = campaign.Title,
                        Message = campaign.Message,
                        Link = campaign.ActionUrl,
                        PromoCode = campaign.PromoCode,
                        CouponId = campaign.CouponId,
                        CampaignId = campaign.Id,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _notificationService.CreateNotificationAsync(notification);

                    // Create recipient record
                    var recipient = new CampaignRecipient
                    {
                        CampaignId = campaign.Id,
                        UserId = user.Id,
                        NotificationId = notification.Id,
                        SentAt = DateTime.UtcNow
                    };

                    _context.CampaignRecipients.Add(recipient);
                    recipientCount++;

                    // Send email if enabled
                    if (campaign.SendEmail && !string.IsNullOrEmpty(user.Email))
                    {
                        try
                        {
                            await SendPromotionalEmailAsync(user.Email, campaign);
                            recipient.EmailSent = true;
                            recipient.EmailSentAt = DateTime.UtcNow;
                            emailsSent++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send promotional email to {Email}", user.Email);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Update campaign stats
                campaign.Status = CampaignStatus.Sent;
                campaign.SentAt = DateTime.UtcNow;
                campaign.TotalRecipients = recipientCount;
                campaign.DeliveredCount = recipientCount;
                campaign.EmailSent = emailsSent > 0;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Campaign {CampaignId} sent to {RecipientCount} recipients, {EmailsSent} emails sent",
                    campaignId, recipientCount, emailsSent);

                return recipientCount;
            }
            catch (Exception ex)
            {
                campaign.Status = CampaignStatus.Draft;
                await _context.SaveChangesAsync();

                _logger.LogError(ex, "Failed to send campaign {CampaignId}", campaignId);
                throw;
            }
        }

        private async Task SendPromotionalEmailAsync(string email, PromotionalCampaign campaign)
        {
            var subject = campaign.EmailSubject ?? campaign.Title;
            var body = $@"
                <h2>{campaign.Title}</h2>
                <p>{campaign.Message}</p>
                {(campaign.ImageUrl != null ? $"<img src='{campaign.ImageUrl}' alt='Promo' style='max-width: 100%;'>" : "")}
                {(campaign.PromoCode != null ? $"<p><strong>Use code:</strong> {campaign.PromoCode}</p>" : "")}
                {(campaign.DiscountDescription != null ? $"<p>{campaign.DiscountDescription}</p>" : "")}
                {(campaign.ActionUrl != null ? $"<p><a href='{campaign.ActionUrl}' style='background: #007bff; color: white; padding: 10px 20px; text-decoration: none; border-radius: 5px;'>{campaign.ActionText ?? "Shop Now"}</a></p>" : "")}
            ";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        private async Task<List<ApplicationUser>> GetTargetUsersAsync(TargetAudience audience, int? minOrderCount, decimal? minTotalSpent)
        {
            var query = _context.Users.AsQueryable();

            switch (audience)
            {
                case TargetAudience.NewUsers:
                    // Users registered in the last 30 days
                    var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
                    query = query.Where(u => u.CreatedAt >= thirtyDaysAgo);
                    break;

                case TargetAudience.ReturningUsers:
                    // Users with at least minOrderCount orders
                    var minOrders = minOrderCount ?? 1;
                    var userIdsWithOrders = await _context.Orders
                        .Where(o => o.UserId != null)
                        .GroupBy(o => o.UserId)
                        .Where(g => g.Count() >= minOrders)
                        .Select(g => g.Key)
                        .ToListAsync();
                    query = query.Where(u => userIdsWithOrders.Contains(u.Id));
                    break;

                case TargetAudience.PremiumMembers:
                    query = query.Where(u => u.IsPremiumMember);
                    break;

                case TargetAudience.InactiveUsers:
                    // Users who haven't ordered in the last 60 days
                    var sixtyDaysAgo = DateTime.UtcNow.AddDays(-60);
                    var activeUserIds = await _context.Orders
                        .Where(o => o.UserId != null && o.OrderDate >= sixtyDaysAgo)
                        .Select(o => o.UserId)
                        .Distinct()
                        .ToListAsync();
                    query = query.Where(u => !activeUserIds.Contains(u.Id));
                    break;

                case TargetAudience.HighSpenders:
                    // Users who have spent more than minTotalSpent
                    var minSpent = minTotalSpent ?? 10000m;
                    var highSpenderIds = await _context.Orders
                        .Where(o => o.UserId != null && o.Status == OrderStatus.Delivered)
                        .GroupBy(o => o.UserId)
                        .Where(g => g.Sum(o => o.TotalAmount) >= minSpent)
                        .Select(g => g.Key)
                        .ToListAsync();
                    query = query.Where(u => highSpenderIds.Contains(u.Id));
                    break;

                case TargetAudience.All:
                default:
                    // All users - no filter
                    break;
            }

            return await query.ToListAsync();
        }

        public async Task<int> GetTargetUserCountAsync(TargetAudience audience, int? minOrderCount = null, decimal? minTotalSpent = null)
        {
            var users = await GetTargetUsersAsync(audience, minOrderCount, minTotalSpent);
            return users.Count;
        }

        public async Task<CampaignAnalytics> GetCampaignAnalyticsAsync(int campaignId)
        {
            var campaign = await _context.PromotionalCampaigns
                .Include(pc => pc.Recipients)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(pc => pc.Id == campaignId);

            if (campaign == null)
            {
                throw new ArgumentException("Campaign not found");
            }

            var readsByDate = campaign.Recipients
                .Where(r => r.ReadAt.HasValue)
                .GroupBy(r => r.ReadAt!.Value.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            return new CampaignAnalytics
            {
                TotalRecipients = campaign.TotalRecipients,
                DeliveredCount = campaign.DeliveredCount,
                ReadCount = campaign.ReadCount,
                ClickCount = campaign.ClickCount,
                ReadRate = campaign.ReadRate,
                ClickRate = campaign.ClickRate,
                EmailsSent = campaign.Recipients.Count(r => r.EmailSent),
                Recipients = campaign.Recipients.OrderByDescending(r => r.SentAt).Take(100).ToList(),
                ReadsByDate = readsByDate
            };
        }

        public async Task ProcessScheduledCampaignsAsync()
        {
            var now = DateTime.UtcNow;

            var scheduledCampaigns = await _context.PromotionalCampaigns
                .Where(pc => pc.Status == CampaignStatus.Scheduled &&
                            pc.ScheduledAt != null &&
                            pc.ScheduledAt <= now)
                .ToListAsync();

            foreach (var campaign in scheduledCampaigns)
            {
                try
                {
                    await SendCampaignAsync(campaign.Id);
                    _logger.LogInformation("Scheduled campaign {CampaignId} sent successfully", campaign.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send scheduled campaign {CampaignId}", campaign.Id);
                }
            }
        }

        public async Task RecordNotificationReadAsync(int notificationId)
        {
            var recipient = await _context.CampaignRecipients
                .Include(cr => cr.Campaign)
                .FirstOrDefaultAsync(cr => cr.NotificationId == notificationId);

            if (recipient != null && !recipient.ReadAt.HasValue)
            {
                recipient.ReadAt = DateTime.UtcNow;
                if (recipient.Campaign != null)
                {
                    recipient.Campaign.ReadCount++;
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task RecordNotificationClickAsync(int notificationId)
        {
            var recipient = await _context.CampaignRecipients
                .Include(cr => cr.Campaign)
                .FirstOrDefaultAsync(cr => cr.NotificationId == notificationId);

            if (recipient != null && !recipient.ClickedAt.HasValue)
            {
                recipient.ClickedAt = DateTime.UtcNow;
                if (recipient.Campaign != null)
                {
                    recipient.Campaign.ClickCount++;
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
