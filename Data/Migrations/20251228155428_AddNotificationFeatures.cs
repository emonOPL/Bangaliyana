using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastNotifiedAt",
                table: "Wishlists",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LastNotifiedPrice",
                table: "Wishlists",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnPriceDrop",
                table: "Wishlists",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceWhenAdded",
                table: "Wishlists",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CampaignId",
                table: "UserNotifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConversationId",
                table: "UserNotifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "UserNotifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SellerMessages",
                table: "NotificationPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "PriceHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    OldPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OldDiscountPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewDiscountPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    OldEffectivePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewEffectivePrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    PercentageChange = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceHistories_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PriceHistories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PromotionalCampaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IconClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IconColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionText = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TargetAudience = table.Column<int>(type: "int", nullable: false),
                    MinOrderCount = table.Column<int>(type: "int", nullable: true),
                    MinTotalSpent = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CouponId = table.Column<int>(type: "int", nullable: true),
                    PromoCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DiscountDescription = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SendEmail = table.Column<bool>(type: "bit", nullable: false),
                    EmailSubject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    EmailSent = table.Column<bool>(type: "bit", nullable: false),
                    ScheduledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsImmediate = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalRecipients = table.Column<int>(type: "int", nullable: false),
                    DeliveredCount = table.Column<int>(type: "int", nullable: false),
                    ReadCount = table.Column<int>(type: "int", nullable: false),
                    ClickCount = table.Column<int>(type: "int", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotionalCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotionalCampaigns_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PromotionalCampaigns_Coupons_CouponId",
                        column: x => x.CouponId,
                        principalTable: "Coupons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SellerConversations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SellerId = table.Column<int>(type: "int", nullable: false),
                    BuyerId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnreadCountBuyer = table.Column<int>(type: "int", nullable: false),
                    UnreadCountSeller = table.Column<int>(type: "int", nullable: false),
                    IsClosedByBuyer = table.Column<bool>(type: "bit", nullable: false),
                    IsClosedBySeller = table.Column<bool>(type: "bit", nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerConversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerConversations_AspNetUsers_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SellerConversations_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SellerConversations_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SellerConversations_Sellers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Sellers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampaignRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NotificationId = table.Column<int>(type: "int", nullable: true),
                    EmailSent = table.Column<bool>(type: "bit", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClickedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignRecipients_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignRecipients_PromotionalCampaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "PromotionalCampaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignRecipients_UserNotifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "UserNotifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SellerMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    SenderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsSentBySeller = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttachmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerMessages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SellerMessages_SellerConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "SellerConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_CampaignId",
                table: "UserNotifications",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_ConversationId",
                table: "UserNotifications",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_ProductId",
                table: "UserNotifications",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignRecipients_CampaignId",
                table: "CampaignRecipients",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignRecipients_NotificationId",
                table: "CampaignRecipients",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignRecipients_UserId",
                table: "CampaignRecipients",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_ChangedByUserId",
                table: "PriceHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PriceHistories_ProductId_ChangedAt",
                table: "PriceHistories",
                columns: new[] { "ProductId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PromotionalCampaigns_CouponId",
                table: "PromotionalCampaigns",
                column: "CouponId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionalCampaigns_CreatedByUserId",
                table: "PromotionalCampaigns",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionalCampaigns_ScheduledAt",
                table: "PromotionalCampaigns",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_PromotionalCampaigns_Status",
                table: "PromotionalCampaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SellerConversations_BuyerId",
                table: "SellerConversations",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerConversations_LastMessageAt",
                table: "SellerConversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_SellerConversations_OrderId",
                table: "SellerConversations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerConversations_ProductId",
                table: "SellerConversations",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerConversations_SellerId",
                table: "SellerConversations",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerMessages_ConversationId",
                table: "SellerMessages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerMessages_CreatedAt",
                table: "SellerMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SellerMessages_SenderId",
                table: "SellerMessages",
                column: "SenderId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserNotifications_Products_ProductId",
                table: "UserNotifications",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserNotifications_PromotionalCampaigns_CampaignId",
                table: "UserNotifications",
                column: "CampaignId",
                principalTable: "PromotionalCampaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserNotifications_SellerConversations_ConversationId",
                table: "UserNotifications",
                column: "ConversationId",
                principalTable: "SellerConversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserNotifications_Products_ProductId",
                table: "UserNotifications");

            migrationBuilder.DropForeignKey(
                name: "FK_UserNotifications_PromotionalCampaigns_CampaignId",
                table: "UserNotifications");

            migrationBuilder.DropForeignKey(
                name: "FK_UserNotifications_SellerConversations_ConversationId",
                table: "UserNotifications");

            migrationBuilder.DropTable(
                name: "CampaignRecipients");

            migrationBuilder.DropTable(
                name: "PriceHistories");

            migrationBuilder.DropTable(
                name: "SellerMessages");

            migrationBuilder.DropTable(
                name: "PromotionalCampaigns");

            migrationBuilder.DropTable(
                name: "SellerConversations");

            migrationBuilder.DropIndex(
                name: "IX_UserNotifications_CampaignId",
                table: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "IX_UserNotifications_ConversationId",
                table: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "IX_UserNotifications_ProductId",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "LastNotifiedAt",
                table: "Wishlists");

            migrationBuilder.DropColumn(
                name: "LastNotifiedPrice",
                table: "Wishlists");

            migrationBuilder.DropColumn(
                name: "NotifyOnPriceDrop",
                table: "Wishlists");

            migrationBuilder.DropColumn(
                name: "PriceWhenAdded",
                table: "Wishlists");

            migrationBuilder.DropColumn(
                name: "CampaignId",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "SellerMessages",
                table: "NotificationPreferences");
        }
    }
}
