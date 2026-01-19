using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlashSalesAndCategoryPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoryPromotions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Tagline = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    PromotionType = table.Column<int>(type: "int", nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MinOrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BannerImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MobileBannerImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ThumbnailImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    BackgroundColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TextColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IconClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ButtonText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CustomUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    ShowOnHomepage = table.Column<bool>(type: "bit", nullable: false),
                    ShowCountdown = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryPromotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoryPromotions_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FlashSales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BannerImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MobileBannerImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ShowCountdown = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    PrimaryColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SecondaryColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TextColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashSales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlashSaleProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlashSaleId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    FlashSalePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountPercentage = table.Column<int>(type: "int", nullable: false),
                    FlashSaleStock = table.Column<int>(type: "int", nullable: true),
                    SoldCount = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlashSaleProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FlashSaleProducts_FlashSales_FlashSaleId",
                        column: x => x.FlashSaleId,
                        principalTable: "FlashSales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FlashSaleProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CategoryPromotions_CategoryId",
                table: "CategoryPromotions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryPromotions_IsActive",
                table: "CategoryPromotions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryPromotions_IsFeatured",
                table: "CategoryPromotions",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_CategoryPromotions_StartDate_EndDate",
                table: "CategoryPromotions",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleProducts_FlashSaleId_ProductId",
                table: "FlashSaleProducts",
                columns: new[] { "FlashSaleId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FlashSaleProducts_ProductId",
                table: "FlashSaleProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_FlashSales_IsActive",
                table: "FlashSales",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_FlashSales_StartDate_EndDate",
                table: "FlashSales",
                columns: new[] { "StartDate", "EndDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CategoryPromotions");

            migrationBuilder.DropTable(
                name: "FlashSaleProducts");

            migrationBuilder.DropTable(
                name: "FlashSales");
        }
    }
}
