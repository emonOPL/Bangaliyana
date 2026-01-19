using Bangaliyana.Data;
using Bangaliyana.Models;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Services
{
    public interface IStockAlertService
    {
        // Stock History
        Task RecordStockChangeAsync(int productId, int? variantId, int oldStock, int newStock,
            StockChangeReason reason, string? notes, int? orderId, string? changedByUserId);
        Task<List<StockHistory>> GetStockHistoryAsync(int productId, int? variantId = null, int count = 20);
        Task<List<StockHistory>> GetRecentStockChangesAsync(int count = 50);

        // Restock Subscriptions
        Task<bool> SubscribeToRestockAsync(string userId, int productId, int? variantId = null);
        Task<bool> UnsubscribeFromRestockAsync(string userId, int productId, int? variantId = null);
        Task<bool> IsSubscribedAsync(string userId, int productId, int? variantId = null);
        Task<int> GetSubscriberCountAsync(int productId, int? variantId = null);
        Task ProcessRestockNotificationsAsync(int productId, int? variantId = null);

        // Low Stock Alerts
        Task CheckAndSendLowStockAlertsAsync();
        Task<List<Products>> GetLowStockProductsAsync(int? sellerId = null);
        Task<List<Products>> GetOutOfStockProductsAsync(int? sellerId = null);

        // Stock Adjustment
        Task<bool> AdjustStockAsync(int productId, int? variantId, int newStock,
            string reason, string? notes, string changedByUserId);

        // Inventory Stats
        Task<InventoryStatsViewModel> GetInventoryStatsAsync(int? sellerId = null);
    }

    public class InventoryStatsViewModel
    {
        public int TotalProducts { get; set; }
        public int InStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int TotalStockUnits { get; set; }
        public decimal TotalStockValue { get; set; }
    }

    public class StockAlertService : IStockAlertService
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IRealTimeNotificationService _realTimeNotificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<StockAlertService> _logger;

        public StockAlertService(
            ApplicationDbContext context,
            INotificationService notificationService,
            IRealTimeNotificationService realTimeNotificationService,
            IEmailService emailService,
            ILogger<StockAlertService> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _realTimeNotificationService = realTimeNotificationService;
            _emailService = emailService;
            _logger = logger;
        }

        #region Stock History

        public async Task RecordStockChangeAsync(int productId, int? variantId, int oldStock, int newStock,
            StockChangeReason reason, string? notes, int? orderId, string? changedByUserId)
        {
            if (oldStock == newStock) return;

            var stockHistory = new StockHistory
            {
                ProductId = productId,
                ProductVariantId = variantId,
                OldStock = oldStock,
                NewStock = newStock,
                QuantityChanged = newStock - oldStock,
                Reason = reason,
                Notes = notes,
                OrderId = orderId,
                ChangedByUserId = changedByUserId,
                ChangedAt = DateTime.UtcNow
            };

            _context.StockHistories.Add(stockHistory);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Stock change recorded for Product {ProductId} (Variant: {VariantId}): {OldStock} -> {NewStock} ({Reason})",
                productId, variantId, oldStock, newStock, reason);

            // Check if restocked (was 0 or below, now positive)
            if (oldStock <= 0 && newStock > 0)
            {
                await ProcessRestockNotificationsAsync(productId, variantId);
            }
        }

        public async Task<List<StockHistory>> GetStockHistoryAsync(int productId, int? variantId = null, int count = 20)
        {
            var query = _context.StockHistories
                .Include(sh => sh.Order)
                .Include(sh => sh.ChangedBy)
                .Where(sh => sh.ProductId == productId);

            if (variantId.HasValue)
            {
                query = query.Where(sh => sh.ProductVariantId == variantId);
            }

            return await query
                .OrderByDescending(sh => sh.ChangedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<List<StockHistory>> GetRecentStockChangesAsync(int count = 50)
        {
            return await _context.StockHistories
                .Include(sh => sh.Product)
                .Include(sh => sh.ProductVariant)
                .Include(sh => sh.Order)
                .Include(sh => sh.ChangedBy)
                .OrderByDescending(sh => sh.ChangedAt)
                .Take(count)
                .ToListAsync();
        }

        #endregion

        #region Restock Subscriptions

        public async Task<bool> SubscribeToRestockAsync(string userId, int productId, int? variantId = null)
        {
            // Check if already subscribed
            var existing = await _context.RestockSubscriptions
                .FirstOrDefaultAsync(rs => rs.UserId == userId &&
                                          rs.ProductId == productId &&
                                          rs.ProductVariantId == variantId &&
                                          !rs.IsNotified);

            if (existing != null)
            {
                return false; // Already subscribed
            }

            var subscription = new RestockSubscription
            {
                UserId = userId,
                ProductId = productId,
                ProductVariantId = variantId,
                SubscribedAt = DateTime.UtcNow,
                IsNotified = false
            };

            _context.RestockSubscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} subscribed to restock for Product {ProductId} (Variant: {VariantId})",
                userId, productId, variantId);

            return true;
        }

        public async Task<bool> UnsubscribeFromRestockAsync(string userId, int productId, int? variantId = null)
        {
            var subscription = await _context.RestockSubscriptions
                .FirstOrDefaultAsync(rs => rs.UserId == userId &&
                                          rs.ProductId == productId &&
                                          rs.ProductVariantId == variantId &&
                                          !rs.IsNotified);

            if (subscription == null)
            {
                return false;
            }

            _context.RestockSubscriptions.Remove(subscription);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} unsubscribed from restock for Product {ProductId} (Variant: {VariantId})",
                userId, productId, variantId);

            return true;
        }

        public async Task<bool> IsSubscribedAsync(string userId, int productId, int? variantId = null)
        {
            return await _context.RestockSubscriptions
                .AnyAsync(rs => rs.UserId == userId &&
                               rs.ProductId == productId &&
                               rs.ProductVariantId == variantId &&
                               !rs.IsNotified);
        }

        public async Task<int> GetSubscriberCountAsync(int productId, int? variantId = null)
        {
            return await _context.RestockSubscriptions
                .Where(rs => rs.ProductId == productId &&
                            rs.ProductVariantId == variantId &&
                            !rs.IsNotified)
                .CountAsync();
        }

        public async Task ProcessRestockNotificationsAsync(int productId, int? variantId = null)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return;

            ProductVariant? variant = null;
            if (variantId.HasValue)
            {
                variant = await _context.ProductVariants
                    .FirstOrDefaultAsync(pv => pv.Id == variantId);
            }

            // Get all pending subscriptions
            var subscriptions = await _context.RestockSubscriptions
                .Include(rs => rs.User)
                .Where(rs => rs.ProductId == productId &&
                            rs.ProductVariantId == variantId &&
                            !rs.IsNotified)
                .ToListAsync();

            if (!subscriptions.Any()) return;

            _logger.LogInformation("Processing {Count} restock notifications for Product {ProductId} (Variant: {VariantId})",
                subscriptions.Count, productId, variantId);

            foreach (var subscription in subscriptions)
            {
                // Check user notification preferences
                var preference = await _context.NotificationPreferences
                    .FirstOrDefaultAsync(np => np.UserId == subscription.UserId);

                if (preference != null && !preference.RestockAlerts)
                {
                    continue;
                }

                var variantInfo = variant != null ? $" ({variant.Size} / {variant.Color})" : "";
                var productName = product.Name + variantInfo;
                var productImage = product.Images?.FirstOrDefault()?.ImageUrl ?? product.ImageUrl;

                // Create in-app notification
                var userNotification = new UserNotification
                {
                    UserId = subscription.UserId,
                    Type = "restock",
                    Icon = "fa-box-open",
                    IconColor = "text-success",
                    Title = "Back in Stock!",
                    Message = $"{productName} is now available. Get it before it sells out again!",
                    Link = $"/Details/{product.Slug ?? product.Id.ToString()}",
                    ProductId = productId
                };
                await _notificationService.CreateNotificationAsync(userNotification);

                // Send real-time notification
                await _realTimeNotificationService.SendToUserAsync(
                    subscription.UserId,
                    new NotificationDto
                    {
                        Type = "restock",
                        Icon = "fa-box-open",
                        IconColor = "text-success",
                        Title = "Back in Stock!",
                        Message = $"{productName} is now available!",
                        Link = $"/Details/{product.Slug ?? product.Id.ToString()}"
                    });

                // Send email notification if user has email
                if (subscription.User?.Email != null && preference?.ReceiveEmails != false)
                {
                    try
                    {
                        await SendRestockEmail(subscription.User.Email, productName, product.Slug ?? product.Id.ToString(), productImage);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send restock email to {Email}", subscription.User.Email);
                    }
                }

                // Mark subscription as notified
                subscription.IsNotified = true;
                subscription.NotifiedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed {Count} restock notifications for Product {ProductId}",
                subscriptions.Count, productId);
        }

        private async Task SendRestockEmail(string email, string productName, string productSlug, string? productImage)
        {
            var subject = $"Good News! {productName} is Back in Stock";
            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #22c55e;'>Back in Stock Alert!</h2>
                    <p>Great news! The product you were waiting for is now available:</p>

                    <div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        {(productImage != null ? $"<img src='{productImage}' style='width: 100px; height: 100px; object-fit: cover; border-radius: 4px;' />" : "")}
                        <h3 style='margin: 10px 0;'>{productName}</h3>
                    </div>

                    <p>Hurry! Popular items sell out fast.</p>

                    <a href='/Details/{productSlug}'
                       style='display: inline-block; background: #f97316; color: white; padding: 12px 24px;
                              text-decoration: none; border-radius: 6px; margin-top: 10px;'>
                        Shop Now
                    </a>

                    <p style='color: #666; font-size: 12px; margin-top: 30px;'>
                        You received this email because you subscribed to restock notifications for this product.
                    </p>
                </div>";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        #endregion

        #region Low Stock Alerts

        public async Task CheckAndSendLowStockAlertsAsync()
        {
            _logger.LogInformation("Starting daily low stock alert check...");

            // Get all sellers with their products and alert settings
            var sellers = await _context.Sellers
                .Include(s => s.User)
                .Include(s => s.Products)
                .Where(s => s.Status == SellerStatus.Approved &&
                           (s.EnableLowStockEmailAlerts || s.EnableLowStockInAppAlerts))
                .ToListAsync();

            foreach (var seller in sellers)
            {
                var threshold = seller.LowStockThreshold > 0 ? seller.LowStockThreshold : 5;

                var lowStockProducts = seller.Products
                    .Where(p => p.Status == ProductStatus.Active && p.Stock > 0 && p.Stock <= threshold)
                    .ToList();

                var outOfStockProducts = seller.Products
                    .Where(p => p.Status == ProductStatus.Active && p.Stock <= 0)
                    .ToList();

                if (!lowStockProducts.Any() && !outOfStockProducts.Any())
                {
                    continue;
                }

                // Send in-app notification
                if (seller.EnableLowStockInAppAlerts && seller.UserId != null)
                {
                    var message = "";
                    if (outOfStockProducts.Any())
                    {
                        message += $"{outOfStockProducts.Count} products are out of stock. ";
                    }
                    if (lowStockProducts.Any())
                    {
                        message += $"{lowStockProducts.Count} products have low stock (below {threshold} units).";
                    }

                    await _notificationService.CreateNotificationAsync(new UserNotification
                    {
                        UserId = seller.UserId,
                        Type = "inventory",
                        Icon = "fa-exclamation-triangle",
                        IconColor = "text-warning",
                        Title = "Inventory Alert",
                        Message = message,
                        Link = "/Seller/Inventory"
                    });

                    await _realTimeNotificationService.SendToUserAsync(
                        seller.UserId,
                        new NotificationDto
                        {
                            Type = "inventory",
                            Icon = "fa-exclamation-triangle",
                            IconColor = "text-warning",
                            Title = "Inventory Alert",
                            Message = message,
                            Link = "/Seller/Inventory"
                        });
                }

                // Send email notification
                if (seller.EnableLowStockEmailAlerts && seller.User?.Email != null)
                {
                    try
                    {
                        await SendLowStockEmail(seller.User.Email, seller.ShopName ?? "Your Shop",
                            lowStockProducts, outOfStockProducts, threshold);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send low stock email to seller {SellerId}", seller.Id);
                    }
                }
            }

            _logger.LogInformation("Low stock alert check completed for {Count} sellers", sellers.Count);
        }

        private async Task SendLowStockEmail(string email, string shopName, List<Products> lowStock, List<Products> outOfStock, int threshold)
        {
            var subject = $"Inventory Alert for {shopName}";

            var outOfStockHtml = outOfStock.Any()
                ? $@"<h3 style='color: #dc2626;'>Out of Stock ({outOfStock.Count})</h3>
                     <ul>{string.Join("", outOfStock.Take(10).Select(p => $"<li>{p.Name}</li>"))}</ul>
                     {(outOfStock.Count > 10 ? $"<p>...and {outOfStock.Count - 10} more</p>" : "")}"
                : "";

            var lowStockHtml = lowStock.Any()
                ? $@"<h3 style='color: #f59e0b;'>Low Stock ({lowStock.Count})</h3>
                     <ul>{string.Join("", lowStock.Take(10).Select(p => $"<li>{p.Name} - {p.Stock} units left</li>"))}</ul>
                     {(lowStock.Count > 10 ? $"<p>...and {lowStock.Count - 10} more</p>" : "")}"
                : "";

            var body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #f59e0b;'>Inventory Alert</h2>
                    <p>This is your daily inventory summary for <strong>{shopName}</strong>:</p>

                    {outOfStockHtml}
                    {lowStockHtml}

                    <p>Current low stock threshold: <strong>{threshold} units</strong></p>

                    <a href='/Seller/Inventory'
                       style='display: inline-block; background: #f97316; color: white; padding: 12px 24px;
                              text-decoration: none; border-radius: 6px; margin-top: 10px;'>
                        Manage Inventory
                    </a>

                    <p style='color: #666; font-size: 12px; margin-top: 30px;'>
                        You can update your alert preferences in the seller dashboard settings.
                    </p>
                </div>";

            await _emailService.SendEmailAsync(email, subject, body);
        }

        public async Task<List<Products>> GetLowStockProductsAsync(int? sellerId = null)
        {
            var query = _context.Products
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Where(p => p.Status == ProductStatus.Active);

            if (sellerId.HasValue)
            {
                var seller = await _context.Sellers.FindAsync(sellerId.Value);
                var threshold = seller?.LowStockThreshold > 0 ? seller.LowStockThreshold : 5;
                query = query.Where(p => p.SellerId == sellerId && p.Stock > 0 && p.Stock <= threshold);
            }
            else
            {
                query = query.Where(p => p.Stock > 0 && p.Stock <= 5);
            }

            return await query.OrderBy(p => p.Stock).ToListAsync();
        }

        public async Task<List<Products>> GetOutOfStockProductsAsync(int? sellerId = null)
        {
            var query = _context.Products
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Include(p => p.Seller)
                .Where(p => p.Status == ProductStatus.Active && p.Stock <= 0);

            if (sellerId.HasValue)
            {
                query = query.Where(p => p.SellerId == sellerId);
            }

            return await query.OrderBy(p => p.Name).ToListAsync();
        }

        #endregion

        #region Stock Adjustment

        public async Task<bool> AdjustStockAsync(int productId, int? variantId, int newStock,
            string reason, string? notes, string changedByUserId)
        {
            if (variantId.HasValue)
            {
                var variant = await _context.ProductVariants.FindAsync(variantId.Value);
                if (variant == null) return false;

                var oldStock = variant.Stock;
                variant.Stock = newStock;

                await RecordStockChangeAsync(productId, variantId, oldStock, newStock,
                    StockChangeReason.ManualAdjustment, notes ?? reason, null, changedByUserId);
            }
            else
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null) return false;

                var oldStock = product.Stock;
                product.Stock = newStock;

                await RecordStockChangeAsync(productId, null, oldStock, newStock,
                    StockChangeReason.ManualAdjustment, notes ?? reason, null, changedByUserId);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Inventory Stats

        public async Task<InventoryStatsViewModel> GetInventoryStatsAsync(int? sellerId = null)
        {
            var query = _context.Products
                .Where(p => p.Status == ProductStatus.Active);

            if (sellerId.HasValue)
            {
                query = query.Where(p => p.SellerId == sellerId);
            }

            var products = await query.ToListAsync();

            var lowStockThreshold = 5;
            if (sellerId.HasValue)
            {
                var seller = await _context.Sellers.FindAsync(sellerId.Value);
                if (seller != null && seller.LowStockThreshold > 0)
                {
                    lowStockThreshold = seller.LowStockThreshold;
                }
            }

            return new InventoryStatsViewModel
            {
                TotalProducts = products.Count,
                InStockProducts = products.Count(p => p.Stock > lowStockThreshold),
                LowStockProducts = products.Count(p => p.Stock > 0 && p.Stock <= lowStockThreshold),
                OutOfStockProducts = products.Count(p => p.Stock <= 0),
                TotalStockUnits = products.Sum(p => p.Stock),
                TotalStockValue = products.Sum(p => p.Stock * p.EffectivePrice)
            };
        }

        #endregion
    }
}
