using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAISearchTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductViews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ViewDuration = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SearchTerm = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ViewedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductViews_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductViews_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchTerm = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    HasClicked = table.Column<bool>(type: "bit", nullable: false),
                    ClickedProductId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchHistories_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SearchHistories_Products_ClickedProductId",
                        column: x => x.ClickedProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TrendingSearches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchTerm = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    NormalizedTerm = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SearchCount = table.Column<int>(type: "int", nullable: false),
                    ClickCount = table.Column<int>(type: "int", nullable: false),
                    TrendScore = table.Column<double>(type: "float", nullable: false),
                    LastSearchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrendingSearches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PreferredCategories = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PreferredBrands = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinPricePreference = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxPricePreference = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PreferredProductTypes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InterestKeywords = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalProductsViewed = table.Column<int>(type: "int", nullable: false),
                    TotalSearches = table.Column<int>(type: "int", nullable: false),
                    TotalPurchases = table.Column<int>(type: "int", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductViews_ProductId",
                table: "ProductViews",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductViews_SessionId",
                table: "ProductViews",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductViews_UserId",
                table: "ProductViews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductViews_ViewedAt",
                table: "ProductViews",
                column: "ViewedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistories_ClickedProductId",
                table: "SearchHistories",
                column: "ClickedProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistories_CreatedAt",
                table: "SearchHistories",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistories_SearchTerm",
                table: "SearchHistories",
                column: "SearchTerm");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistories_SessionId",
                table: "SearchHistories",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistories_UserId",
                table: "SearchHistories",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TrendingSearches_NormalizedTerm",
                table: "TrendingSearches",
                column: "NormalizedTerm",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TrendingSearches_TrendScore",
                table: "TrendingSearches",
                column: "TrendScore");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                table: "UserPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductViews");

            migrationBuilder.DropTable(
                name: "SearchHistories");

            migrationBuilder.DropTable(
                name: "TrendingSearches");

            migrationBuilder.DropTable(
                name: "UserPreferences");
        }
    }
}
