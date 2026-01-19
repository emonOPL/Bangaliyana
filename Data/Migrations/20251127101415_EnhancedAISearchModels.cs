using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedAISearchModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AvgPagesPerSession",
                table: "UserPreferences",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AvgSearchLength",
                table: "UserPreferences",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AvgSessionDuration",
                table: "UserPreferences",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "BrandLoyalty",
                table: "UserPreferences",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "CartHistory",
                table: "UserPreferences",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClickedProducts",
                table: "UserPreferences",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "ConversionScore",
                table: "UserPreferences",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "DayPreferences",
                table: "UserPreferences",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DevicePreferences",
                table: "UserPreferences",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DiscountSensitivity",
                table: "UserPreferences",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EngagementScore",
                table: "UserPreferences",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "ExternalProfiles",
                table: "UserPreferences",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "LifetimeValue",
                table: "UserPreferences",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "PeakActivityTimes",
                table: "UserPreferences",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PriceSensitivity",
                table: "UserPreferences",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SearchToClickRate",
                table: "UserPreferences",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "SearchToPurchaseRate",
                table: "UserPreferences",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "TotalCartAdds",
                table: "UserPreferences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalReviews",
                table: "UserPreferences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalSessions",
                table: "UserPreferences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalWishlistAdds",
                table: "UserPreferences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UserSegment",
                table: "UserPreferences",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ViewedCategories",
                table: "UserPreferences",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AvgClickPosition",
                table: "TrendingSearches",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ClickThroughRate",
                table: "TrendingSearches",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "ConversionCount",
                table: "TrendingSearches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "DailyVelocity",
                table: "TrendingSearches",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "DisplayPriority",
                table: "TrendingSearches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "HourlyVelocity",
                table: "TrendingSearches",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "TrendingSearches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "TrendingSearches",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PrimaryCategoryId",
                table: "TrendingSearches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelatedSearches",
                table: "TrendingSearches",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UniqueUsers",
                table: "TrendingSearches",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "WeeklyVelocity",
                table: "TrendingSearches",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "AppliedFilters",
                table: "SearchHistories",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Browser",
                table: "SearchHistories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClickedPosition",
                table: "SearchHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "SearchHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVoiceSearch",
                table: "SearchHistories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "SearchHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                table: "SearchHistories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponseTimeMs",
                table: "SearchHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SearchSource",
                table: "SearchHistories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeToClick",
                table: "SearchHistories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Browser",
                table: "ProductViews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceType",
                table: "ProductViews",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DidAddToCart",
                table: "ProductViews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DidAddToWishlist",
                table: "ProductViews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DidPurchase",
                table: "ProductViews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DidScrollToBottom",
                table: "ProductViews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DidViewImages",
                table: "ProductViews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DidViewReviews",
                table: "ProductViews",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ExitPage",
                table: "ProductViews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExitTime",
                table: "ProductViews",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "ProductViews",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "ProductViews",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingSystem",
                table: "ProductViews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferrerType",
                table: "ProductViews",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferrerUrl",
                table: "ProductViews",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AISearchSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SettingType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AllowedValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MinValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    MaxValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DefaultValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AISearchSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlockedSearchTerms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Term = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BlockType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AlternativeMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RedirectUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedSearchTerms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PromotedSearches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SearchTerm = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    PromotedProductId = table.Column<int>(type: "int", nullable: true),
                    PromotedCategoryId = table.Column<int>(type: "int", nullable: true),
                    BannerImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BannerText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BannerLink = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    BoostValue = table.Column<int>(type: "int", nullable: false),
                    ShowToNewUsers = table.Column<bool>(type: "bit", nullable: false),
                    ShowToReturningUsers = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Impressions = table.Column<int>(type: "int", nullable: false),
                    Clicks = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromotedSearches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PromotedSearches_Categories_PromotedCategoryId",
                        column: x => x.PromotedCategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PromotedSearches_Products_PromotedProductId",
                        column: x => x.PromotedProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecommendationAnalytics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecommendationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Placement = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Impressions = table.Column<int>(type: "int", nullable: false),
                    UniqueUsersReached = table.Column<int>(type: "int", nullable: false),
                    Clicks = table.Column<int>(type: "int", nullable: false),
                    ClickThroughRate = table.Column<double>(type: "float", nullable: false),
                    AddToCarts = table.Column<int>(type: "int", nullable: false),
                    Purchases = table.Column<int>(type: "int", nullable: false),
                    Revenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ConversionRate = table.Column<double>(type: "float", nullable: false),
                    TopPerformingProducts = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationAnalytics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchAnalytics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TotalSearches = table.Column<int>(type: "int", nullable: false),
                    UniqueSearches = table.Column<int>(type: "int", nullable: false),
                    UniqueUsers = table.Column<int>(type: "int", nullable: false),
                    ZeroResultSearches = table.Column<int>(type: "int", nullable: false),
                    TotalClicks = table.Column<int>(type: "int", nullable: false),
                    AvgClickPosition = table.Column<double>(type: "float", nullable: false),
                    ClickThroughRate = table.Column<double>(type: "float", nullable: false),
                    SearchesToConversion = table.Column<int>(type: "int", nullable: false),
                    ConversionRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ConversionRate = table.Column<double>(type: "float", nullable: false),
                    AvgResponseTimeMs = table.Column<double>(type: "float", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    TopSearches = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopZeroResultSearches = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SearchesByDevice = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SearchesByHour = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchAnalytics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SearchSynonyms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceTerm = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TargetTerm = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SynonymType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RedirectUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TimesUsed = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchSynonyms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DeviceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Browser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OperatingSystem = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    PageViews = table.Column<int>(type: "int", nullable: false),
                    ProductViews = table.Column<int>(type: "int", nullable: false),
                    SearchesPerformed = table.Column<int>(type: "int", nullable: false),
                    ItemsAddedToCart = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false),
                    EntryPage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExitPage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferrerUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReferrerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UtmSource = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UtmMedium = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UtmCampaign = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DidConvert = table.Column<bool>(type: "bit", nullable: false),
                    ConversionValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrendingSearches_PrimaryCategoryId",
                table: "TrendingSearches",
                column: "PrimaryCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AISearchSettings_Category",
                table: "AISearchSettings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_AISearchSettings_SettingKey",
                table: "AISearchSettings",
                column: "SettingKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlockedSearchTerms_IsActive",
                table: "BlockedSearchTerms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedSearchTerms_Term",
                table: "BlockedSearchTerms",
                column: "Term");

            migrationBuilder.CreateIndex(
                name: "IX_PromotedSearches_IsActive",
                table: "PromotedSearches",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_PromotedSearches_PromotedCategoryId",
                table: "PromotedSearches",
                column: "PromotedCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotedSearches_PromotedProductId",
                table: "PromotedSearches",
                column: "PromotedProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PromotedSearches_SearchTerm",
                table: "PromotedSearches",
                column: "SearchTerm");

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationAnalytics_Date_RecommendationType_Placement",
                table: "RecommendationAnalytics",
                columns: new[] { "Date", "RecommendationType", "Placement" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchAnalytics_Date_Period",
                table: "SearchAnalytics",
                columns: new[] { "Date", "Period" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchSynonyms_IsActive",
                table: "SearchSynonyms",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SearchSynonyms_SourceTerm",
                table: "SearchSynonyms",
                column: "SourceTerm");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_SessionId",
                table: "UserSessions",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_StartTime",
                table: "UserSessions",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TrendingSearches_Categories_PrimaryCategoryId",
                table: "TrendingSearches",
                column: "PrimaryCategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TrendingSearches_Categories_PrimaryCategoryId",
                table: "TrendingSearches");

            migrationBuilder.DropTable(
                name: "AISearchSettings");

            migrationBuilder.DropTable(
                name: "BlockedSearchTerms");

            migrationBuilder.DropTable(
                name: "PromotedSearches");

            migrationBuilder.DropTable(
                name: "RecommendationAnalytics");

            migrationBuilder.DropTable(
                name: "SearchAnalytics");

            migrationBuilder.DropTable(
                name: "SearchSynonyms");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropIndex(
                name: "IX_TrendingSearches_PrimaryCategoryId",
                table: "TrendingSearches");

            migrationBuilder.DropColumn(
                name: "AvgPagesPerSession",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "AvgSearchLength",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "AvgSessionDuration",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "BrandLoyalty",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "CartHistory",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ClickedProducts",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ConversionScore",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "DayPreferences",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "DevicePreferences",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "DiscountSensitivity",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "EngagementScore",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ExternalProfiles",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "LifetimeValue",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "PeakActivityTimes",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "PriceSensitivity",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "SearchToClickRate",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "SearchToPurchaseRate",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "TotalCartAdds",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "TotalReviews",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "TotalSessions",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "TotalWishlistAdds",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "UserSegment",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "ViewedCategories",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "AvgClickPosition",
                table: "TrendingSearches");

            migrationBuilder.DropColumn(
                name: "ClickThroughRate",
                table: "TrendingSearches");

            migrationBuilder.DropColumn(
                name: "ConversionCount",
                table: "TrendingSearches");

            migrationBuilder.DropColumn(
                name: "DailyVelocity",
                table: "TrendingSearches");

            migrationBuilder.DropColumn(
                name: "DisplayPriority",
                table: "TrendingSearches");

            migrationBuilder.DropColumn(
                name: "HourlyVelocity",
                table: "TrendingSearches");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "TrendingSearches");

            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "TrendingSearches");

            migrationBuilder.DropColumn(
                name: "PrimaryCategoryId",
                table: "TrendingSearches");

            migrationBuilder.DropColumn(
                name: "RelatedSearches",
                table: "TrendingSearches");

            migrationBuilder.DropColumn(
                name: "UniqueUsers",
                table: "TrendingSearches");

            migrationBuilder.DropColumn(
                name: "WeeklyVelocity",
                table: "TrendingSearches");

            migrationBuilder.DropColumn(
                name: "AppliedFilters",
                table: "SearchHistories");

            migrationBuilder.DropColumn(
                name: "Browser",
                table: "SearchHistories");

            migrationBuilder.DropColumn(
                name: "ClickedPosition",
                table: "SearchHistories");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "SearchHistories");

            migrationBuilder.DropColumn(
                name: "IsVoiceSearch",
                table: "SearchHistories");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "SearchHistories");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "SearchHistories");

            migrationBuilder.DropColumn(
                name: "ResponseTimeMs",
                table: "SearchHistories");

            migrationBuilder.DropColumn(
                name: "SearchSource",
                table: "SearchHistories");

            migrationBuilder.DropColumn(
                name: "TimeToClick",
                table: "SearchHistories");

            migrationBuilder.DropColumn(
                name: "Browser",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "DeviceType",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "DidAddToCart",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "DidAddToWishlist",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "DidPurchase",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "DidScrollToBottom",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "DidViewImages",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "DidViewReviews",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "ExitPage",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "ExitTime",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "OperatingSystem",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "ReferrerType",
                table: "ProductViews");

            migrationBuilder.DropColumn(
                name: "ReferrerUrl",
                table: "ProductViews");
        }
    }
}
