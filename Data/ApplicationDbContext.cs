using Bangaliyana.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bangaliyana.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Product Management
        public DbSet<Products> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Subcategory> Subcategories { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }

        // Order Management
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<PersistentCart> PersistentCarts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        // User Management
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<UserPaymentMethod> UserPaymentMethods { get; set; }
        public DbSet<Wishlist> Wishlists { get; set; }

        // Reviews
        public DbSet<ProductReview> ProductReviews { get; set; }
        public DbSet<ReviewImage> ReviewImages { get; set; }
        public DbSet<ReviewReply> ReviewReplies { get; set; }

        // Q&A
        public DbSet<ProductQuestion> ProductQuestions { get; set; }

        // Size Guides
        public DbSet<SizeGuide> SizeGuides { get; set; }
        public DbSet<SizeGuideEntry> SizeGuideEntries { get; set; }

        // Seller/Marketplace
        public DbSet<Seller> Sellers { get; set; }
        public DbSet<SellerBankAccount> SellerBankAccounts { get; set; }
        public DbSet<SellerBankAccountChangeRequest> SellerBankAccountChangeRequests { get; set; }
        public DbSet<SellerTransaction> SellerTransactions { get; set; }
        public DbSet<BusinessType> BusinessTypes { get; set; }
        public DbSet<ShopFollower> ShopFollowers { get; set; }

        // Seller Payment System
        public DbSet<SellerPayment> SellerPayments { get; set; }
        public DbSet<SellerPaymentMessage> SellerPaymentMessages { get; set; }
        public DbSet<SellerPaymentMessageAttachment> SellerPaymentMessageAttachments { get; set; }
        public DbSet<SellerPaymentSettings> SellerPaymentSettings { get; set; }
        public DbSet<SellerPaymentLog> SellerPaymentLogs { get; set; }
        public DbSet<ManualSellerPayout> ManualSellerPayouts { get; set; }

        // Monthly Seller Payment Reports
        public DbSet<SellerMonthlyReport> SellerMonthlyReports { get; set; }
        public DbSet<MonthlyReportOrderItem> MonthlyReportOrderItems { get; set; }

        // Seller Subscriptions
        public DbSet<SellerSubscriptionPlan> SellerSubscriptionPlans { get; set; }
        public DbSet<SellerSubscription> SellerSubscriptions { get; set; }

        // Coupons
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<CouponUsage> CouponUsages { get; set; }

        // Admin/Roles
        public DbSet<AdminRole> AdminRoles { get; set; }
        public DbSet<AdminPermission> AdminPermissions { get; set; }
        public DbSet<AdminRoleAssignment> AdminRoleAssignments { get; set; }

        // Audit Logs
        public DbSet<AuditLog> AuditLogs { get; set; }

        // Support
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketMessage> TicketMessages { get; set; }
        public DbSet<TicketAttachment> TicketAttachments { get; set; }

        // Contact Inquiries
        public DbSet<ContactInquiry> ContactInquiries { get; set; }

        // Live Support Chat
        public DbSet<SupportChatSession> SupportChatSessions { get; set; }
        public DbSet<SupportChatMessage> SupportChatMessages { get; set; }

        // Bangladesh Administrative Divisions
        public DbSet<Division> Divisions { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Upazila> Upazilas { get; set; }
        public DbSet<DistrictDeliveryCharge> DistrictDeliveryCharges { get; set; }

        // Dynamic Content Management
        public DbSet<SiteSettings> SiteSettings { get; set; }
        public DbSet<SocialLink> SocialLinks { get; set; }
        public DbSet<Feature> Features { get; set; }
        public DbSet<Banner> Banners { get; set; }
        public DbSet<PageContent> PageContents { get; set; }
        public DbSet<Testimonial> Testimonials { get; set; }
        public DbSet<FAQ> FAQs { get; set; }
        public DbSet<FAQFeedback> FAQFeedbacks { get; set; }
        public DbSet<NewsletterSubscription> NewsletterSubscriptions { get; set; }

        // Blog / Fashion Tips System
        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<BlogCategory> BlogCategories { get; set; }

        // Styling Guides
        public DbSet<StylingGuide> StylingGuides { get; set; }

        // Seasonal Collections
        public DbSet<SeasonalCollection> SeasonalCollections { get; set; }

        // Flash Sales
        public DbSet<FlashSale> FlashSales { get; set; }
        public DbSet<FlashSaleProduct> FlashSaleProducts { get; set; }

        // Category Promotions
        public DbSet<CategoryPromotion> CategoryPromotions { get; set; }

        // Inventory Management
        public DbSet<RestockSubscription> RestockSubscriptions { get; set; }
        public DbSet<StockHistory> StockHistories { get; set; }

        // AI Search & Recommendations
        public DbSet<SearchHistory> SearchHistories { get; set; }
        public DbSet<TrendingSearch> TrendingSearches { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<ProductView> ProductViews { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<AISearchSettings> AISearchSettings { get; set; }
        public DbSet<SearchAnalytics> SearchAnalytics { get; set; }
        public DbSet<RecommendationAnalytics> RecommendationAnalytics { get; set; }
        public DbSet<PromotedSearch> PromotedSearches { get; set; }
        public DbSet<SearchSynonym> SearchSynonyms { get; set; }
        public DbSet<BlockedSearchTerm> BlockedSearchTerms { get; set; }

        // Authentication & OTP
        public DbSet<OtpCode> OtpCodes { get; set; }

        // Dynamic Menu System
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<MenuPermission> MenuPermissions { get; set; }
        public DbSet<UserMenuPermission> UserMenuPermissions { get; set; }

        // WebAuthn/Biometric Authentication
        public DbSet<WebAuthnCredential> WebAuthnCredentials { get; set; }
        public DbSet<WebAuthnChallenge> WebAuthnChallenges { get; set; }

        // Reward Points System
        public DbSet<RewardPointsTransaction> RewardPointsTransactions { get; set; }
        public DbSet<UserRedeemedReward> UserRedeemedRewards { get; set; }

        // Notification System
        public DbSet<UserNotification> UserNotifications { get; set; }
        public DbSet<NotificationPreference> NotificationPreferences { get; set; }
        public DbSet<FestivalDate> FestivalDates { get; set; }

        // Seller Messaging System
        public DbSet<SellerConversation> SellerConversations { get; set; }
        public DbSet<SellerMessage> SellerMessages { get; set; }

        // Price History & Alerts
        public DbSet<PriceHistory> PriceHistories { get; set; }

        // Promotional Campaigns
        public DbSet<PromotionalCampaign> PromotionalCampaigns { get; set; }
        public DbSet<CampaignRecipient> CampaignRecipients { get; set; }

        // AI Chat System
        public DbSet<AIChatSession> AIChatSessions { get; set; }
        public DbSet<AIChatMessage> AIChatMessages { get; set; }
        public DbSet<AIChatAnalytics> AIChatAnalytics { get; set; }
        public DbSet<AIChatHandoffQueue> AIChatHandoffQueues { get; set; }
        public DbSet<UserAIChatPreference> UserAIChatPreferences { get; set; }
        public DbSet<ProactiveChatTrigger> ProactiveChatTriggers { get; set; }
        public DbSet<AILearningRecord> AILearningRecords { get; set; }
        public DbSet<AIFeedbackAnalysis> AIFeedbackAnalyses { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser address relationships
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Division)
                .WithMany()
                .HasForeignKey(u => u.DivisionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.District)
                .WithMany()
                .HasForeignKey(u => u.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Upazila)
                .WithMany()
                .HasForeignKey(u => u.UpazilaId)
                .OnDelete(DeleteBehavior.Restrict);

            // ApplicationUser - Seller one-to-one
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Seller)
                .WithOne(s => s.User)
                .HasForeignKey<Seller>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Order address relationships
            builder.Entity<Order>()
                .HasOne(o => o.Division)
                .WithMany()
                .HasForeignKey(o => o.DivisionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .HasOne(o => o.District)
                .WithMany()
                .HasForeignKey(o => o.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .HasOne(o => o.Upazila)
                .WithMany()
                .HasForeignKey(o => o.UpazilaId)
                .OnDelete(DeleteBehavior.Restrict);

            // UserAddress relationships
            builder.Entity<UserAddress>()
                .HasOne(ua => ua.Division)
                .WithMany()
                .HasForeignKey(ua => ua.DivisionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserAddress>()
                .HasOne(ua => ua.District)
                .WithMany()
                .HasForeignKey(ua => ua.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserAddress>()
                .HasOne(ua => ua.Upazila)
                .WithMany()
                .HasForeignKey(ua => ua.UpazilaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Wishlist unique constraint
            builder.Entity<Wishlist>()
                .HasIndex(w => new { w.UserId, w.ProductId })
                .IsUnique();

            // Category self-referencing hierarchy
            builder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Product-Category relationship (hierarchical categories)
            builder.Entity<Products>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // Product-Seller relationship
            builder.Entity<Products>()
                .HasOne(p => p.Seller)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SellerId)
                .OnDelete(DeleteBehavior.SetNull);

            // OrderItem-Seller relationship
            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Seller)
                .WithMany()
                .HasForeignKey(oi => oi.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order-Seller relationship
            builder.Entity<Order>()
                .HasOne(o => o.Seller)
                .WithMany()
                .HasForeignKey(o => o.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order-Coupon relationship
            builder.Entity<Order>()
                .HasOne(o => o.Coupon)
                .WithMany()
                .HasForeignKey(o => o.CouponId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ticket-AssignedTo relationship
            builder.Entity<Ticket>()
                .HasOne(t => t.AssignedTo)
                .WithMany()
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            // Ticket-Order relationship
            builder.Entity<Ticket>()
                .HasOne(t => t.Order)
                .WithMany()
                .HasForeignKey(t => t.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // Ticket-User relationship
            builder.Entity<Ticket>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // TicketMessage relationships
            builder.Entity<TicketMessage>()
                .HasOne(tm => tm.Ticket)
                .WithMany(t => t.Messages)
                .HasForeignKey(tm => tm.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TicketMessage>()
                .HasOne(tm => tm.Sender)
                .WithMany()
                .HasForeignKey(tm => tm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // TicketAttachment relationships
            builder.Entity<TicketAttachment>()
                .HasOne(ta => ta.Ticket)
                .WithMany(t => t.Attachments)
                .HasForeignKey(ta => ta.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TicketAttachment>()
                .HasOne(ta => ta.TicketMessage)
                .WithMany()
                .HasForeignKey(ta => ta.MessageId)
                .OnDelete(DeleteBehavior.Restrict);

            // ProductReview relationships
            builder.Entity<ProductReview>()
                .HasOne(pr => pr.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProductReview>()
                .HasOne(pr => pr.Order)
                .WithMany(o => o.Reviews)
                .HasForeignKey(pr => pr.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            // ProductQuestion relationships
            builder.Entity<ProductQuestion>()
                .HasOne(pq => pq.Product)
                .WithMany(p => p.Questions)
                .HasForeignKey(pq => pq.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductQuestion>()
                .HasOne(pq => pq.User)
                .WithMany()
                .HasForeignKey(pq => pq.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ProductQuestion>()
                .HasOne(pq => pq.AnsweredBy)
                .WithMany()
                .HasForeignKey(pq => pq.AnsweredById)
                .OnDelete(DeleteBehavior.Restrict);

            // SizeGuide relationships
            builder.Entity<SizeGuideEntry>()
                .HasOne(sge => sge.SizeGuide)
                .WithMany(sg => sg.Entries)
                .HasForeignKey(sge => sge.SizeGuideId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Products>()
                .HasOne(p => p.SizeGuide)
                .WithMany(sg => sg.Products)
                .HasForeignKey(p => p.SizeGuideId)
                .OnDelete(DeleteBehavior.SetNull);

            // CouponUsage relationships
            builder.Entity<CouponUsage>()
                .HasOne(cu => cu.User)
                .WithMany()
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CouponUsage>()
                .HasOne(cu => cu.Order)
                .WithMany()
                .HasForeignKey(cu => cu.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Transaction relationships
            builder.Entity<Transaction>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Transaction>()
                .HasOne(t => t.Order)
                .WithMany(o => o.Transactions)
                .HasForeignKey(t => t.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Transaction>()
                .HasOne(t => t.Seller)
                .WithMany()
                .HasForeignKey(t => t.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            // AdminRoleAssignment relationships
            builder.Entity<AdminRoleAssignment>()
                .HasOne(ara => ara.User)
                .WithMany()
                .HasForeignKey(ara => ara.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure decimal precision for currency fields
            builder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.SubTotal)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.DeliveryCharge)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.UnitPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.DiscountPrice)
                .HasPrecision(18, 2);

            builder.Entity<OrderItem>()
                .Property(oi => oi.TotalPrice)
                .HasPrecision(18, 2);

            builder.Entity<Products>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Entity<Products>()
                .Property(p => p.DiscountPrice)
                .HasPrecision(18, 2);

            builder.Entity<Products>()
                .Property(p => p.Weight)
                .HasPrecision(18, 2);

            builder.Entity<ProductVariant>()
                .Property(pv => pv.AdditionalPrice)
                .HasPrecision(18, 2);

            builder.Entity<Seller>()
                .Property(s => s.CommissionRate)
                .HasPrecision(5, 2);

            builder.Entity<Seller>()
                .Property(s => s.TotalSales)
                .HasPrecision(18, 2);

            builder.Entity<Seller>()
                .Property(s => s.Rating)
                .HasPrecision(3, 2);

            builder.Entity<Seller>()
                .Property(s => s.AccountBalance)
                .HasPrecision(18, 2);

            // SellerTransaction configurations
            builder.Entity<SellerTransaction>()
                .HasOne(st => st.Seller)
                .WithMany(s => s.Transactions)
                .HasForeignKey(st => st.SellerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SellerTransaction>()
                .HasOne(st => st.Order)
                .WithMany()
                .HasForeignKey(st => st.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<SellerTransaction>()
                .HasIndex(st => st.SellerId);

            builder.Entity<SellerTransaction>()
                .HasIndex(st => st.CreatedAt);

            builder.Entity<SellerTransaction>()
                .Property(st => st.Amount)
                .HasPrecision(18, 2);

            builder.Entity<SellerTransaction>()
                .Property(st => st.BalanceAfter)
                .HasPrecision(18, 2);

            // SellerPayment configurations
            builder.Entity<SellerPayment>()
                .HasOne(sp => sp.Seller)
                .WithMany()
                .HasForeignKey(sp => sp.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SellerPayment>()
                .HasOne(sp => sp.ProcessedBy)
                .WithMany()
                .HasForeignKey(sp => sp.ProcessedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SellerPayment>()
                .HasIndex(sp => sp.PaymentNumber)
                .IsUnique();

            builder.Entity<SellerPayment>()
                .HasIndex(sp => sp.SellerId);

            builder.Entity<SellerPayment>()
                .HasIndex(sp => sp.Status);

            builder.Entity<SellerPayment>()
                .HasIndex(sp => sp.CreatedAt);

            builder.Entity<SellerPayment>()
                .Property(sp => sp.Amount)
                .HasPrecision(18, 2);

            // SellerPaymentMessage configurations
            builder.Entity<SellerPaymentMessage>()
                .HasOne(spm => spm.SellerPayment)
                .WithMany(sp => sp.Messages)
                .HasForeignKey(spm => spm.SellerPaymentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SellerPaymentMessage>()
                .HasOne(spm => spm.Sender)
                .WithMany()
                .HasForeignKey(spm => spm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SellerPaymentMessage>()
                .HasIndex(spm => spm.SellerPaymentId);

            builder.Entity<SellerPaymentMessage>()
                .HasIndex(spm => spm.CreatedAt);

            // SellerPaymentMessageAttachment configurations
            builder.Entity<SellerPaymentMessageAttachment>()
                .HasOne(spma => spma.Message)
                .WithMany(spm => spm.Attachments)
                .HasForeignKey(spma => spma.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            // SellerPaymentSettings configurations
            builder.Entity<SellerPaymentSettings>()
                .Property(sps => sps.MinimumPayoutAmount)
                .HasPrecision(18, 2);

            // SellerMonthlyReport configurations
            builder.Entity<SellerMonthlyReport>()
                .HasOne(r => r.Seller)
                .WithMany()
                .HasForeignKey(r => r.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SellerMonthlyReport>()
                .HasOne(r => r.FinalizedBy)
                .WithMany()
                .HasForeignKey(r => r.FinalizedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SellerMonthlyReport>()
                .HasOne(r => r.ManualSellerPayout)
                .WithMany()
                .HasForeignKey(r => r.ManualSellerPayoutId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<SellerMonthlyReport>()
                .HasIndex(r => new { r.SellerId, r.Year, r.Month })
                .IsUnique();

            builder.Entity<SellerMonthlyReport>()
                .HasIndex(r => r.ReportNumber)
                .IsUnique();

            builder.Entity<SellerMonthlyReport>()
                .HasIndex(r => r.Status);

            builder.Entity<SellerMonthlyReport>()
                .HasIndex(r => new { r.Year, r.Month });

            builder.Entity<SellerMonthlyReport>()
                .Property(r => r.TotalSales).HasPrecision(18, 2);

            builder.Entity<SellerMonthlyReport>()
                .Property(r => r.CommissionRate).HasPrecision(5, 2);

            builder.Entity<SellerMonthlyReport>()
                .Property(r => r.CommissionAmount).HasPrecision(18, 2);

            builder.Entity<SellerMonthlyReport>()
                .Property(r => r.RefundDeductions).HasPrecision(18, 2);

            builder.Entity<SellerMonthlyReport>()
                .Property(r => r.ReturnDeductions).HasPrecision(18, 2);

            builder.Entity<SellerMonthlyReport>()
                .Property(r => r.Adjustments).HasPrecision(18, 2);

            builder.Entity<SellerMonthlyReport>()
                .Property(r => r.CarryForwardFromPrevious).HasPrecision(18, 2);

            builder.Entity<SellerMonthlyReport>()
                .Property(r => r.CarryForwardToNext).HasPrecision(18, 2);

            builder.Entity<SellerMonthlyReport>()
                .Property(r => r.NetPayable).HasPrecision(18, 2);

            builder.Entity<SellerMonthlyReport>()
                .Property(r => r.PayoutAmount).HasPrecision(18, 2);

            // MonthlyReportOrderItem configurations
            builder.Entity<MonthlyReportOrderItem>()
                .HasOne(roi => roi.MonthlyReport)
                .WithMany(r => r.ReportOrderItems)
                .HasForeignKey(roi => roi.MonthlyReportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MonthlyReportOrderItem>()
                .HasOne(roi => roi.OrderItem)
                .WithMany()
                .HasForeignKey(roi => roi.OrderItemId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MonthlyReportOrderItem>()
                .HasOne(roi => roi.Order)
                .WithMany()
                .HasForeignKey(roi => roi.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MonthlyReportOrderItem>()
                .HasOne(roi => roi.DeductionReport)
                .WithMany()
                .HasForeignKey(roi => roi.DeductionReportId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MonthlyReportOrderItem>()
                .HasIndex(roi => new { roi.MonthlyReportId, roi.OrderItemId })
                .IsUnique();

            builder.Entity<MonthlyReportOrderItem>()
                .HasIndex(roi => roi.OrderId);

            builder.Entity<MonthlyReportOrderItem>()
                .HasIndex(roi => roi.Status);

            builder.Entity<MonthlyReportOrderItem>()
                .Property(roi => roi.ItemAmount).HasPrecision(18, 2);

            builder.Entity<MonthlyReportOrderItem>()
                .Property(roi => roi.CommissionRate).HasPrecision(5, 2);

            builder.Entity<MonthlyReportOrderItem>()
                .Property(roi => roi.CommissionAmount).HasPrecision(18, 2);

            builder.Entity<MonthlyReportOrderItem>()
                .Property(roi => roi.NetAmount).HasPrecision(18, 2);

            builder.Entity<MonthlyReportOrderItem>()
                .Property(roi => roi.DeductionAmount).HasPrecision(18, 2);

            builder.Entity<Transaction>()
                .Property(t => t.Amount)
                .HasPrecision(18, 2);

            builder.Entity<Coupon>()
                .Property(c => c.DiscountValue)
                .HasPrecision(18, 2);

            builder.Entity<Coupon>()
                .Property(c => c.MinimumOrderAmount)
                .HasPrecision(18, 2);

            builder.Entity<Coupon>()
                .Property(c => c.MaximumDiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<CouponUsage>()
                .Property(cu => cu.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<SiteSettings>()
                .Property(s => s.DefaultDeliveryCharge)
                .HasPrecision(18, 2);

            builder.Entity<SiteSettings>()
                .Property(s => s.FreeDeliveryThreshold)
                .HasPrecision(18, 2);

            // Configure unique indexes
            builder.Entity<PageContent>()
                .HasIndex(p => p.Slug)
                .IsUnique();

            builder.Entity<SocialLink>()
                .HasIndex(s => s.PlatformName);

            builder.Entity<Feature>()
                .HasIndex(f => f.Section);

            builder.Entity<Banner>()
                .HasIndex(b => b.Location);

            // BlogPost configurations
            builder.Entity<BlogPost>()
                .HasIndex(bp => bp.Slug)
                .IsUnique();

            builder.Entity<BlogPost>()
                .HasIndex(bp => bp.IsActive);

            builder.Entity<BlogPost>()
                .HasIndex(bp => bp.IsFeatured);

            builder.Entity<BlogPost>()
                .HasIndex(bp => bp.PublishedAt);

            builder.Entity<BlogPost>()
                .HasOne(bp => bp.Category)
                .WithMany(bc => bc.Posts)
                .HasForeignKey(bp => bp.BlogCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // BlogCategory configurations
            builder.Entity<BlogCategory>()
                .HasIndex(bc => bc.Slug)
                .IsUnique();

            builder.Entity<BlogCategory>()
                .HasIndex(bc => bc.IsActive);

            // StylingGuide configurations
            builder.Entity<StylingGuide>()
                .HasIndex(sg => sg.Slug)
                .IsUnique();

            builder.Entity<StylingGuide>()
                .HasIndex(sg => sg.IsActive);

            builder.Entity<StylingGuide>()
                .HasIndex(sg => sg.IsFeatured);

            builder.Entity<StylingGuide>()
                .HasIndex(sg => sg.GuideType);

            // SeasonalCollection configurations
            builder.Entity<SeasonalCollection>()
                .HasIndex(sc => sc.Slug)
                .IsUnique();

            builder.Entity<SeasonalCollection>()
                .HasIndex(sc => sc.IsActive);

            builder.Entity<SeasonalCollection>()
                .HasIndex(sc => sc.IsFeatured);

            builder.Entity<SeasonalCollection>()
                .HasIndex(sc => sc.SeasonType);

            builder.Entity<SeasonalCollection>()
                .HasIndex(sc => new { sc.StartDate, sc.EndDate });

            builder.Entity<Category>()
                .HasIndex(c => c.Slug)
                .IsUnique();

            builder.Entity<Subcategory>()
                .HasIndex(sc => sc.Slug);

            builder.Entity<Products>()
                .HasIndex(p => p.Slug);

            builder.Entity<Products>()
                .HasIndex(p => p.SKU);

            builder.Entity<Seller>()
                .HasIndex(s => s.ShopSlug)
                .IsUnique();

            // Unique constraint on DocumentNumber (one document = one shop)
            builder.Entity<Seller>()
                .HasIndex(s => s.DocumentNumber)
                .IsUnique()
                .HasFilter("[DocumentNumber] IS NOT NULL");

            // BusinessType relationship
            builder.Entity<Seller>()
                .HasOne(s => s.BusinessTypeEntity)
                .WithMany(bt => bt.Sellers)
                .HasForeignKey(s => s.BusinessTypeId)
                .OnDelete(DeleteBehavior.SetNull);

            // SellerBankAccountChangeRequest relationships
            builder.Entity<SellerBankAccountChangeRequest>()
                .HasOne(r => r.Seller)
                .WithMany(s => s.BankAccountChangeRequests)
                .HasForeignKey(r => r.SellerId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<SellerBankAccountChangeRequest>()
                .HasOne(r => r.ExistingBankAccount)
                .WithMany()
                .HasForeignKey(r => r.ExistingBankAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<SellerBankAccountChangeRequest>()
                .HasOne(r => r.ReviewedBy)
                .WithMany()
                .HasForeignKey(r => r.ReviewedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // ShopFollower configurations
            builder.Entity<ShopFollower>()
                .HasOne(sf => sf.User)
                .WithMany()
                .HasForeignKey(sf => sf.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ShopFollower>()
                .HasOne(sf => sf.Seller)
                .WithMany(s => s.Followers)
                .HasForeignKey(sf => sf.SellerId)
                .OnDelete(DeleteBehavior.NoAction);

            // Unique constraint - user can only follow a shop once
            builder.Entity<ShopFollower>()
                .HasIndex(sf => new { sf.UserId, sf.SellerId })
                .IsUnique();

            builder.Entity<Coupon>()
                .HasIndex(c => c.Code)
                .IsUnique();

            builder.Entity<Ticket>()
                .HasIndex(t => t.TicketNumber)
                .IsUnique();

            builder.Entity<Order>()
                .HasIndex(o => o.OrderNumber);

            // AI Search & Recommendations configurations
            builder.Entity<SearchHistory>()
                .HasOne(sh => sh.User)
                .WithMany()
                .HasForeignKey(sh => sh.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SearchHistory>()
                .HasOne(sh => sh.ClickedProduct)
                .WithMany()
                .HasForeignKey(sh => sh.ClickedProductId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<SearchHistory>()
                .HasIndex(sh => sh.UserId);

            builder.Entity<SearchHistory>()
                .HasIndex(sh => sh.SessionId);

            builder.Entity<SearchHistory>()
                .HasIndex(sh => sh.SearchTerm);

            builder.Entity<SearchHistory>()
                .HasIndex(sh => sh.CreatedAt);

            builder.Entity<TrendingSearch>()
                .HasIndex(ts => ts.NormalizedTerm)
                .IsUnique();

            builder.Entity<TrendingSearch>()
                .HasIndex(ts => ts.TrendScore);

            builder.Entity<UserPreference>()
                .HasOne(up => up.User)
                .WithMany()
                .HasForeignKey(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserPreference>()
                .HasIndex(up => up.UserId)
                .IsUnique();

            builder.Entity<UserPreference>()
                .Property(up => up.MinPricePreference)
                .HasPrecision(18, 2);

            builder.Entity<UserPreference>()
                .Property(up => up.MaxPricePreference)
                .HasPrecision(18, 2);

            builder.Entity<ProductView>()
                .HasOne(pv => pv.Product)
                .WithMany()
                .HasForeignKey(pv => pv.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductView>()
                .HasOne(pv => pv.User)
                .WithMany()
                .HasForeignKey(pv => pv.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ProductView>()
                .HasIndex(pv => pv.UserId);

            builder.Entity<ProductView>()
                .HasIndex(pv => pv.SessionId);

            builder.Entity<ProductView>()
                .HasIndex(pv => pv.ViewedAt);

            // UserSession configurations
            builder.Entity<UserSession>()
                .HasOne(us => us.User)
                .WithMany()
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserSession>()
                .HasIndex(us => us.SessionId);

            builder.Entity<UserSession>()
                .HasIndex(us => us.UserId);

            builder.Entity<UserSession>()
                .HasIndex(us => us.StartTime);

            builder.Entity<UserSession>()
                .Property(us => us.ConversionValue)
                .HasPrecision(18, 2);

            // AISearchSettings configurations
            builder.Entity<AISearchSettings>()
                .HasIndex(s => s.SettingKey)
                .IsUnique();

            builder.Entity<AISearchSettings>()
                .HasIndex(s => s.Category);

            builder.Entity<AISearchSettings>()
                .Property(s => s.MinValue)
                .HasPrecision(18, 4);

            builder.Entity<AISearchSettings>()
                .Property(s => s.MaxValue)
                .HasPrecision(18, 4);

            // SearchAnalytics configurations
            builder.Entity<SearchAnalytics>()
                .HasIndex(sa => new { sa.Date, sa.Period });

            builder.Entity<SearchAnalytics>()
                .Property(sa => sa.ConversionRevenue)
                .HasPrecision(18, 2);

            // RecommendationAnalytics configurations
            builder.Entity<RecommendationAnalytics>()
                .HasIndex(ra => new { ra.Date, ra.RecommendationType, ra.Placement });

            builder.Entity<RecommendationAnalytics>()
                .Property(ra => ra.Revenue)
                .HasPrecision(18, 2);

            // PromotedSearch configurations
            builder.Entity<PromotedSearch>()
                .HasOne(ps => ps.PromotedProduct)
                .WithMany()
                .HasForeignKey(ps => ps.PromotedProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PromotedSearch>()
                .HasOne(ps => ps.PromotedCategory)
                .WithMany()
                .HasForeignKey(ps => ps.PromotedCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PromotedSearch>()
                .HasIndex(ps => ps.SearchTerm);

            builder.Entity<PromotedSearch>()
                .HasIndex(ps => ps.IsActive);

            // SearchSynonym configurations
            builder.Entity<SearchSynonym>()
                .HasIndex(ss => ss.SourceTerm);

            builder.Entity<SearchSynonym>()
                .HasIndex(ss => ss.IsActive);

            // BlockedSearchTerm configurations
            builder.Entity<BlockedSearchTerm>()
                .HasIndex(bs => bs.Term);

            builder.Entity<BlockedSearchTerm>()
                .HasIndex(bs => bs.IsActive);

            // TrendingSearch - PrimaryCategory relationship
            builder.Entity<TrendingSearch>()
                .HasOne(ts => ts.PrimaryCategory)
                .WithMany()
                .HasForeignKey(ts => ts.PrimaryCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // OtpCode configurations
            builder.Entity<OtpCode>()
                .HasIndex(o => new { o.Identifier, o.Type, o.IsUsed });

            builder.Entity<OtpCode>()
                .HasIndex(o => o.ExpiresAt);

            // MenuItem configurations
            builder.Entity<MenuItem>()
                .HasOne(m => m.Parent)
                .WithMany(m => m.Children)
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<MenuItem>()
                .HasIndex(m => new { m.Location, m.DisplayOrder });

            builder.Entity<MenuItem>()
                .HasIndex(m => m.IsActive);

            // MenuPermission configurations
            builder.Entity<MenuPermission>()
                .HasOne(mp => mp.MenuItem)
                .WithMany(m => m.Permissions)
                .HasForeignKey(mp => mp.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<MenuPermission>()
                .HasIndex(mp => new { mp.MenuItemId, mp.RoleName })
                .IsUnique();

            // Testimonial configurations (user-submitted reviews)
            builder.Entity<Testimonial>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Testimonial>()
                .HasIndex(t => t.UserId);

            builder.Entity<Testimonial>()
                .HasIndex(t => new { t.IsActive, t.IsApproved, t.Rating });

            builder.Entity<Testimonial>()
                .HasIndex(t => t.IsUserSubmitted);

            // WebAuthn configurations
            builder.Entity<WebAuthnCredential>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<WebAuthnCredential>()
                .HasIndex(w => w.UserId);

            builder.Entity<WebAuthnCredential>()
                .HasIndex(w => w.CredentialId)
                .IsUnique();

            builder.Entity<WebAuthnChallenge>()
                .HasIndex(w => w.Challenge);

            builder.Entity<WebAuthnChallenge>()
                .HasIndex(w => w.ExpiresAt);

            // RewardPointsTransaction configurations
            builder.Entity<RewardPointsTransaction>()
                .HasOne(rpt => rpt.User)
                .WithMany(u => u.PointsTransactions)
                .HasForeignKey(rpt => rpt.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RewardPointsTransaction>()
                .HasOne(rpt => rpt.Order)
                .WithMany()
                .HasForeignKey(rpt => rpt.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RewardPointsTransaction>()
                .HasOne(rpt => rpt.ProductReview)
                .WithMany()
                .HasForeignKey(rpt => rpt.ProductReviewId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RewardPointsTransaction>()
                .HasOne(rpt => rpt.Testimonial)
                .WithMany()
                .HasForeignKey(rpt => rpt.TestimonialId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RewardPointsTransaction>()
                .HasOne(rpt => rpt.ReferredUser)
                .WithMany()
                .HasForeignKey(rpt => rpt.ReferredUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RewardPointsTransaction>()
                .HasIndex(rpt => rpt.UserId);

            builder.Entity<RewardPointsTransaction>()
                .HasIndex(rpt => rpt.TransactionType);

            builder.Entity<RewardPointsTransaction>()
                .HasIndex(rpt => rpt.CreatedAt);

            // Unique constraint for ProductReview - one user can review each product only once
            builder.Entity<ProductReview>()
                .HasIndex(pr => new { pr.UserId, pr.ProductId })
                .IsUnique();

            // ReviewReply configurations - one-to-one with ProductReview
            builder.Entity<ReviewReply>()
                .HasOne(rr => rr.ProductReview)
                .WithOne(pr => pr.Reply)
                .HasForeignKey<ReviewReply>(rr => rr.ProductReviewId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ReviewReply>()
                .HasOne(rr => rr.User)
                .WithMany()
                .HasForeignKey(rr => rr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReviewReply>()
                .HasIndex(rr => rr.ProductReviewId)
                .IsUnique();

            // Unique index for ApplicationUser ReferralCode
            builder.Entity<ApplicationUser>()
                .HasIndex(u => u.ReferralCode)
                .IsUnique()
                .HasFilter("[ReferralCode] IS NOT NULL");

            // UserRedeemedReward configurations
            builder.Entity<UserRedeemedReward>()
                .HasOne(urr => urr.User)
                .WithMany(u => u.RedeemedRewards)
                .HasForeignKey(urr => urr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserRedeemedReward>()
                .HasOne(urr => urr.UsedOnOrder)
                .WithMany()
                .HasForeignKey(urr => urr.UsedOnOrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<UserRedeemedReward>()
                .HasIndex(urr => urr.UserId);

            builder.Entity<UserRedeemedReward>()
                .HasIndex(urr => urr.Status);

            builder.Entity<UserRedeemedReward>()
                .HasIndex(urr => urr.ExpiresAt);

            builder.Entity<UserRedeemedReward>()
                .Property(urr => urr.DiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<UserRedeemedReward>()
                .Property(urr => urr.MinimumOrderAmount)
                .HasPrecision(18, 2);

            // Order reward voucher relationships
            builder.Entity<Order>()
                .HasOne(o => o.AppliedReward)
                .WithMany()
                .HasForeignKey(o => o.AppliedRewardId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .HasOne(o => o.AppliedFreeShippingReward)
                .WithMany()
                .HasForeignKey(o => o.AppliedFreeShippingRewardId)
                .OnDelete(DeleteBehavior.Restrict);

            // Order new decimal field precision
            builder.Entity<Order>()
                .Property(o => o.RewardDiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.WalletAmountUsed)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.PremiumDiscountAmount)
                .HasPrecision(18, 2);

            // ApplicationUser wallet balance precision
            builder.Entity<ApplicationUser>()
                .Property(u => u.WalletBalance)
                .HasPrecision(18, 2);

            // UserNotification configurations
            builder.Entity<UserNotification>()
                .HasOne(un => un.User)
                .WithMany()
                .HasForeignKey(un => un.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserNotification>()
                .HasOne(un => un.Order)
                .WithMany()
                .HasForeignKey(un => un.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<UserNotification>()
                .HasOne(un => un.Ticket)
                .WithMany()
                .HasForeignKey(un => un.TicketId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<UserNotification>()
                .HasOne(un => un.Coupon)
                .WithMany()
                .HasForeignKey(un => un.CouponId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<UserNotification>()
                .HasIndex(un => un.UserId);

            builder.Entity<UserNotification>()
                .HasIndex(un => un.IsRead);

            builder.Entity<UserNotification>()
                .HasIndex(un => un.CreatedAt);

            // NotificationPreference configurations
            builder.Entity<NotificationPreference>()
                .HasOne(np => np.User)
                .WithMany()
                .HasForeignKey(np => np.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<NotificationPreference>()
                .HasIndex(np => np.UserId)
                .IsUnique();

            // FestivalDate configurations
            builder.Entity<FestivalDate>()
                .HasIndex(fd => fd.Name)
                .IsUnique();

            builder.Entity<FestivalDate>()
                .HasIndex(fd => fd.IsActive);

            // SellerConversation configurations
            builder.Entity<SellerConversation>()
                .HasOne(sc => sc.Seller)
                .WithMany()
                .HasForeignKey(sc => sc.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SellerConversation>()
                .HasOne(sc => sc.Buyer)
                .WithMany()
                .HasForeignKey(sc => sc.BuyerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SellerConversation>()
                .HasOne(sc => sc.Order)
                .WithMany()
                .HasForeignKey(sc => sc.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<SellerConversation>()
                .HasOne(sc => sc.Product)
                .WithMany()
                .HasForeignKey(sc => sc.ProductId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<SellerConversation>()
                .HasIndex(sc => sc.SellerId);

            builder.Entity<SellerConversation>()
                .HasIndex(sc => sc.BuyerId);

            builder.Entity<SellerConversation>()
                .HasIndex(sc => sc.LastMessageAt);

            // SellerMessage configurations
            builder.Entity<SellerMessage>()
                .HasOne(sm => sm.Conversation)
                .WithMany(sc => sc.Messages)
                .HasForeignKey(sm => sm.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SellerMessage>()
                .HasOne(sm => sm.Sender)
                .WithMany()
                .HasForeignKey(sm => sm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SellerMessage>()
                .HasIndex(sm => sm.ConversationId);

            builder.Entity<SellerMessage>()
                .HasIndex(sm => sm.CreatedAt);

            // PriceHistory configurations
            builder.Entity<PriceHistory>()
                .HasOne(ph => ph.Product)
                .WithMany()
                .HasForeignKey(ph => ph.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PriceHistory>()
                .HasOne(ph => ph.ChangedBy)
                .WithMany()
                .HasForeignKey(ph => ph.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<PriceHistory>()
                .HasIndex(ph => new { ph.ProductId, ph.ChangedAt });

            // PromotionalCampaign configurations
            builder.Entity<PromotionalCampaign>()
                .HasOne(pc => pc.Coupon)
                .WithMany()
                .HasForeignKey(pc => pc.CouponId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<PromotionalCampaign>()
                .HasOne(pc => pc.CreatedBy)
                .WithMany()
                .HasForeignKey(pc => pc.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PromotionalCampaign>()
                .HasIndex(pc => pc.Status);

            builder.Entity<PromotionalCampaign>()
                .HasIndex(pc => pc.ScheduledAt);

            // CampaignRecipient configurations
            builder.Entity<CampaignRecipient>()
                .HasOne(cr => cr.Campaign)
                .WithMany(pc => pc.Recipients)
                .HasForeignKey(cr => cr.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CampaignRecipient>()
                .HasOne(cr => cr.User)
                .WithMany()
                .HasForeignKey(cr => cr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CampaignRecipient>()
                .HasOne(cr => cr.Notification)
                .WithMany()
                .HasForeignKey(cr => cr.NotificationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CampaignRecipient>()
                .HasIndex(cr => cr.CampaignId);

            builder.Entity<CampaignRecipient>()
                .HasIndex(cr => cr.UserId);

            // UserNotification - new navigation properties
            builder.Entity<UserNotification>()
                .HasOne(un => un.Conversation)
                .WithMany()
                .HasForeignKey(un => un.ConversationId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<UserNotification>()
                .HasOne(un => un.Campaign)
                .WithMany()
                .HasForeignKey(un => un.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<UserNotification>()
                .HasOne(un => un.Product)
                .WithMany()
                .HasForeignKey(un => un.ProductId)
                .OnDelete(DeleteBehavior.SetNull);

            // Wishlist price tracking precision
            builder.Entity<Wishlist>()
                .Property(w => w.PriceWhenAdded)
                .HasPrecision(18, 2);

            builder.Entity<Wishlist>()
                .Property(w => w.LastNotifiedPrice)
                .HasPrecision(18, 2);

            // ============ AI Chat System Configurations ============

            // AIChatSession - Unique session code
            builder.Entity<AIChatSession>()
                .HasIndex(s => s.SessionCode)
                .IsUnique();

            builder.Entity<AIChatSession>()
                .HasIndex(s => s.UserId);

            builder.Entity<AIChatSession>()
                .HasIndex(s => s.GuestSessionId);

            builder.Entity<AIChatSession>()
                .HasIndex(s => s.Status);

            builder.Entity<AIChatSession>()
                .HasIndex(s => s.CreatedAt);

            // AIChatMessage indexes
            builder.Entity<AIChatMessage>()
                .HasIndex(m => m.SessionId);

            builder.Entity<AIChatMessage>()
                .HasIndex(m => m.CreatedAt);

            builder.Entity<AIChatMessage>()
                .HasIndex(m => m.DetectedIntent);

            // AIChatAnalytics - Unique date
            builder.Entity<AIChatAnalytics>()
                .HasIndex(a => a.Date)
                .IsUnique();

            // AIChatHandoffQueue indexes
            builder.Entity<AIChatHandoffQueue>()
                .HasIndex(h => h.IsAssigned);

            builder.Entity<AIChatHandoffQueue>()
                .HasIndex(h => h.IsResolved);

            builder.Entity<AIChatHandoffQueue>()
                .HasIndex(h => h.Priority);

            // UserAIChatPreference - One per user
            builder.Entity<UserAIChatPreference>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            // ProactiveChatTrigger indexes
            builder.Entity<ProactiveChatTrigger>()
                .HasIndex(t => t.IsActive);

            builder.Entity<ProactiveChatTrigger>()
                .HasIndex(t => t.TriggerType);

            // ============ Flash Sales Configurations ============

            builder.Entity<FlashSale>()
                .HasIndex(fs => fs.IsActive);

            builder.Entity<FlashSale>()
                .HasIndex(fs => new { fs.StartDate, fs.EndDate });

            // FlashSaleProduct configurations
            builder.Entity<FlashSaleProduct>()
                .HasOne(fsp => fsp.FlashSale)
                .WithMany(fs => fs.Products)
                .HasForeignKey(fsp => fsp.FlashSaleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FlashSaleProduct>()
                .HasOne(fsp => fsp.Product)
                .WithMany()
                .HasForeignKey(fsp => fsp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<FlashSaleProduct>()
                .HasIndex(fsp => new { fsp.FlashSaleId, fsp.ProductId })
                .IsUnique();

            builder.Entity<FlashSaleProduct>()
                .Property(fsp => fsp.FlashSalePrice)
                .HasPrecision(18, 2);

            // ============ Category Promotions Configurations ============

            builder.Entity<CategoryPromotion>()
                .HasOne(cp => cp.Category)
                .WithMany()
                .HasForeignKey(cp => cp.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<CategoryPromotion>()
                .HasIndex(cp => cp.IsActive);

            builder.Entity<CategoryPromotion>()
                .HasIndex(cp => cp.IsFeatured);

            builder.Entity<CategoryPromotion>()
                .HasIndex(cp => new { cp.StartDate, cp.EndDate });

            builder.Entity<CategoryPromotion>()
                .Property(cp => cp.DiscountValue)
                .HasPrecision(18, 2);

            builder.Entity<CategoryPromotion>()
                .Property(cp => cp.MaxDiscountAmount)
                .HasPrecision(18, 2);

            builder.Entity<CategoryPromotion>()
                .Property(cp => cp.MinOrderAmount)
                .HasPrecision(18, 2);

            // ============ Inventory Management Configurations ============

            // RestockSubscription configurations
            builder.Entity<RestockSubscription>()
                .HasOne(rs => rs.User)
                .WithMany()
                .HasForeignKey(rs => rs.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RestockSubscription>()
                .HasOne(rs => rs.Product)
                .WithMany()
                .HasForeignKey(rs => rs.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<RestockSubscription>()
                .HasOne(rs => rs.ProductVariant)
                .WithMany()
                .HasForeignKey(rs => rs.ProductVariantId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<RestockSubscription>()
                .HasIndex(rs => rs.UserId);

            builder.Entity<RestockSubscription>()
                .HasIndex(rs => rs.ProductId);

            builder.Entity<RestockSubscription>()
                .HasIndex(rs => new { rs.ProductId, rs.ProductVariantId, rs.IsNotified });

            // Unique constraint - one subscription per user/product/variant combination
            builder.Entity<RestockSubscription>()
                .HasIndex(rs => new { rs.UserId, rs.ProductId, rs.ProductVariantId })
                .IsUnique()
                .HasFilter("[UserId] IS NOT NULL");

            // StockHistory configurations
            builder.Entity<StockHistory>()
                .HasOne(sh => sh.Product)
                .WithMany()
                .HasForeignKey(sh => sh.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<StockHistory>()
                .HasOne(sh => sh.ProductVariant)
                .WithMany()
                .HasForeignKey(sh => sh.ProductVariantId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<StockHistory>()
                .HasOne(sh => sh.Order)
                .WithMany()
                .HasForeignKey(sh => sh.OrderId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<StockHistory>()
                .HasOne(sh => sh.ChangedBy)
                .WithMany()
                .HasForeignKey(sh => sh.ChangedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<StockHistory>()
                .HasIndex(sh => sh.ProductId);

            builder.Entity<StockHistory>()
                .HasIndex(sh => sh.ProductVariantId);

            builder.Entity<StockHistory>()
                .HasIndex(sh => sh.ChangedAt);

            builder.Entity<StockHistory>()
                .HasIndex(sh => sh.Reason);

            builder.Entity<StockHistory>()
                .HasIndex(sh => new { sh.ProductId, sh.ChangedAt });

            // ============ Seller Subscription Configurations ============

            // SellerSubscriptionPlan configurations
            builder.Entity<SellerSubscriptionPlan>()
                .Property(p => p.MonthlyPrice)
                .HasPrecision(18, 2);

            builder.Entity<SellerSubscriptionPlan>()
                .Property(p => p.CommissionRate)
                .HasPrecision(5, 2);

            builder.Entity<SellerSubscriptionPlan>()
                .HasIndex(p => p.IsActive);

            builder.Entity<SellerSubscriptionPlan>()
                .HasIndex(p => p.SortOrder);

            // SellerSubscription configurations
            builder.Entity<SellerSubscription>()
                .Property(s => s.AmountPaid)
                .HasPrecision(18, 2);

            builder.Entity<SellerSubscription>()
                .HasIndex(s => s.SellerId);

            builder.Entity<SellerSubscription>()
                .HasIndex(s => s.Status);

            builder.Entity<SellerSubscription>()
                .HasIndex(s => s.EndDate);

            // Seller.Subscriptions (one-to-many) relationship
            builder.Entity<SellerSubscription>()
                .HasOne(s => s.Seller)
                .WithMany(s => s.Subscriptions)
                .HasForeignKey(s => s.SellerId)
                .OnDelete(DeleteBehavior.NoAction);

            // SellerSubscription.Plan relationship
            builder.Entity<SellerSubscription>()
                .HasOne(s => s.Plan)
                .WithMany(p => p.Subscriptions)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seller.CurrentSubscription (one-to-one) relationship - configured separately
            builder.Entity<Seller>()
                .HasOne(s => s.CurrentSubscription)
                .WithOne()
                .HasForeignKey<Seller>(s => s.CurrentSubscriptionId)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
