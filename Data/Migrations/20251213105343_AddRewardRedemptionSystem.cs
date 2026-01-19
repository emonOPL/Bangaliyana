using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRewardRedemptionSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppliedFreeShippingRewardId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AppliedRewardId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FreeShippingApplied",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPremiumOrder",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "PremiumDiscountAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RewardDiscountAmount",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WalletAmountUsed",
                table: "Orders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsPremiumMember",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PremiumDiscountExpiresAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PremiumExpiresAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WalletBalance",
                table: "AspNetUsers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "UserRedeemedRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    RewardType = table.Column<int>(type: "int", nullable: false),
                    RewardName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MinimumOrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PointsSpent = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UsedOnOrderId = table.Column<int>(type: "int", nullable: true),
                    RedeemedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRedeemedRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRedeemedRewards_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRedeemedRewards_Orders_UsedOnOrderId",
                        column: x => x.UsedOnOrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AppliedFreeShippingRewardId",
                table: "Orders",
                column: "AppliedFreeShippingRewardId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_AppliedRewardId",
                table: "Orders",
                column: "AppliedRewardId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRedeemedRewards_ExpiresAt",
                table: "UserRedeemedRewards",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserRedeemedRewards_Status",
                table: "UserRedeemedRewards",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_UserRedeemedRewards_UsedOnOrderId",
                table: "UserRedeemedRewards",
                column: "UsedOnOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRedeemedRewards_UserId",
                table: "UserRedeemedRewards",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_UserRedeemedRewards_AppliedFreeShippingRewardId",
                table: "Orders",
                column: "AppliedFreeShippingRewardId",
                principalTable: "UserRedeemedRewards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_UserRedeemedRewards_AppliedRewardId",
                table: "Orders",
                column: "AppliedRewardId",
                principalTable: "UserRedeemedRewards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_UserRedeemedRewards_AppliedFreeShippingRewardId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_UserRedeemedRewards_AppliedRewardId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "UserRedeemedRewards");

            migrationBuilder.DropIndex(
                name: "IX_Orders_AppliedFreeShippingRewardId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_AppliedRewardId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AppliedFreeShippingRewardId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "AppliedRewardId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FreeShippingApplied",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsPremiumOrder",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PremiumDiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RewardDiscountAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "WalletAmountUsed",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsPremiumMember",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PremiumDiscountExpiresAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PremiumExpiresAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "WalletBalance",
                table: "AspNetUsers");
        }
    }
}
