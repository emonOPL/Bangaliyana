using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentSubscriptionId",
                table: "Sellers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SellerSubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxProducts = table.Column<int>(type: "int", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    HasPrioritySupport = table.Column<bool>(type: "bit", nullable: false),
                    HasFeaturedListing = table.Column<bool>(type: "bit", nullable: false),
                    HasAdvancedAnalytics = table.Column<bool>(type: "bit", nullable: false),
                    HasPromotionalTools = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    BadgeColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IconClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerSubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SellerSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SellerId = table.Column<int>(type: "int", nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AutoRenew = table.Column<bool>(type: "bit", nullable: false),
                    IsTrial = table.Column<bool>(type: "bit", nullable: false),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerSubscriptions_SellerSubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SellerSubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SellerSubscriptions_Sellers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Sellers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sellers_CurrentSubscriptionId",
                table: "Sellers",
                column: "CurrentSubscriptionId",
                unique: true,
                filter: "[CurrentSubscriptionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SellerSubscriptionPlans_IsActive",
                table: "SellerSubscriptionPlans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SellerSubscriptionPlans_SortOrder",
                table: "SellerSubscriptionPlans",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_SellerSubscriptions_EndDate",
                table: "SellerSubscriptions",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "IX_SellerSubscriptions_PlanId",
                table: "SellerSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerSubscriptions_SellerId",
                table: "SellerSubscriptions",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerSubscriptions_Status",
                table: "SellerSubscriptions",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_Sellers_SellerSubscriptions_CurrentSubscriptionId",
                table: "Sellers",
                column: "CurrentSubscriptionId",
                principalTable: "SellerSubscriptions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sellers_SellerSubscriptions_CurrentSubscriptionId",
                table: "Sellers");

            migrationBuilder.DropTable(
                name: "SellerSubscriptions");

            migrationBuilder.DropTable(
                name: "SellerSubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "IX_Sellers_CurrentSubscriptionId",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "CurrentSubscriptionId",
                table: "Sellers");
        }
    }
}
