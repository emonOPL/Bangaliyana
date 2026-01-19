using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardPointsSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_UserId",
                table: "ProductReviews");

            migrationBuilder.AddColumn<bool>(
                name: "PointsAwarded",
                table: "Testimonials",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PointsAwardedAt",
                table: "Testimonials",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PointsAwarded",
                table: "ProductReviews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PointsAwardedAt",
                table: "ProductReviews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryPointsAmount",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "DeliveryPointsAwarded",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryPointsAwardedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasUsedReferralCode",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReferralCode",
                table: "AspNetUsers",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReferralCodeGeneratedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferredByUserId",
                table: "AspNetUsers",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalRewardPoints",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "RewardPointsTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    BalanceAfter = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    ProductReviewId = table.Column<int>(type: "int", nullable: true),
                    TestimonialId = table.Column<int>(type: "int", nullable: true),
                    ReferredUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardPointsTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RewardPointsTransactions_AspNetUsers_ReferredUserId",
                        column: x => x.ReferredUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RewardPointsTransactions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RewardPointsTransactions_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RewardPointsTransactions_ProductReviews_ProductReviewId",
                        column: x => x.ProductReviewId,
                        principalTable: "ProductReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RewardPointsTransactions_Testimonials_TestimonialId",
                        column: x => x.TestimonialId,
                        principalTable: "Testimonials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_UserId_ProductId",
                table: "ProductReviews",
                columns: new[] { "UserId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_ReferralCode",
                table: "AspNetUsers",
                column: "ReferralCode",
                unique: true,
                filter: "[ReferralCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RewardPointsTransactions_CreatedAt",
                table: "RewardPointsTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RewardPointsTransactions_OrderId",
                table: "RewardPointsTransactions",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardPointsTransactions_ProductReviewId",
                table: "RewardPointsTransactions",
                column: "ProductReviewId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardPointsTransactions_ReferredUserId",
                table: "RewardPointsTransactions",
                column: "ReferredUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardPointsTransactions_TestimonialId",
                table: "RewardPointsTransactions",
                column: "TestimonialId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardPointsTransactions_TransactionType",
                table: "RewardPointsTransactions",
                column: "TransactionType");

            migrationBuilder.CreateIndex(
                name: "IX_RewardPointsTransactions_UserId",
                table: "RewardPointsTransactions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RewardPointsTransactions");

            migrationBuilder.DropIndex(
                name: "IX_ProductReviews_UserId_ProductId",
                table: "ProductReviews");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_ReferralCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PointsAwarded",
                table: "Testimonials");

            migrationBuilder.DropColumn(
                name: "PointsAwardedAt",
                table: "Testimonials");

            migrationBuilder.DropColumn(
                name: "PointsAwarded",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "PointsAwardedAt",
                table: "ProductReviews");

            migrationBuilder.DropColumn(
                name: "DeliveryPointsAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryPointsAwarded",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "DeliveryPointsAwardedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "HasUsedReferralCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ReferralCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ReferralCodeGeneratedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ReferredByUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TotalRewardPoints",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_ProductReviews_UserId",
                table: "ProductReviews",
                column: "UserId");
        }
    }
}
