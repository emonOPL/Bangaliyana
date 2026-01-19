using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableLowStockEmailAlerts",
                table: "Sellers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "EnableLowStockInAppAlerts",
                table: "Sellers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LowStockThreshold",
                table: "Sellers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RestockAlerts",
                table: "NotificationPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "RestockSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: true),
                    SubscribedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsNotified = table.Column<bool>(type: "bit", nullable: false),
                    NotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GuestEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestockSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RestockSubscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RestockSubscriptions_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RestockSubscriptions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ProductVariantId = table.Column<int>(type: "int", nullable: true),
                    OldStock = table.Column<int>(type: "int", nullable: false),
                    NewStock = table.Column<int>(type: "int", nullable: false),
                    QuantityChanged = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockHistories_AspNetUsers_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StockHistories_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StockHistories_ProductVariants_ProductVariantId",
                        column: x => x.ProductVariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StockHistories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RestockSubscriptions_ProductId",
                table: "RestockSubscriptions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_RestockSubscriptions_ProductId_ProductVariantId_IsNotified",
                table: "RestockSubscriptions",
                columns: new[] { "ProductId", "ProductVariantId", "IsNotified" });

            migrationBuilder.CreateIndex(
                name: "IX_RestockSubscriptions_ProductVariantId",
                table: "RestockSubscriptions",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_RestockSubscriptions_UserId",
                table: "RestockSubscriptions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RestockSubscriptions_UserId_ProductId_ProductVariantId",
                table: "RestockSubscriptions",
                columns: new[] { "UserId", "ProductId", "ProductVariantId" },
                unique: true,
                filter: "[UserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_StockHistories_ChangedAt",
                table: "StockHistories",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StockHistories_ChangedByUserId",
                table: "StockHistories",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockHistories_OrderId",
                table: "StockHistories",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_StockHistories_ProductId",
                table: "StockHistories",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockHistories_ProductId_ChangedAt",
                table: "StockHistories",
                columns: new[] { "ProductId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StockHistories_ProductVariantId",
                table: "StockHistories",
                column: "ProductVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_StockHistories_Reason",
                table: "StockHistories",
                column: "Reason");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestockSubscriptions");

            migrationBuilder.DropTable(
                name: "StockHistories");

            migrationBuilder.DropColumn(
                name: "EnableLowStockEmailAlerts",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "EnableLowStockInAppAlerts",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "LowStockThreshold",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "RestockAlerts",
                table: "NotificationPreferences");
        }
    }
}
