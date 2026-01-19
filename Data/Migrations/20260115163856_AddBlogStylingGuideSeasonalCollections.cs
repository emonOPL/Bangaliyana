using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBlogStylingGuideSeasonalCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlogCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IconClass = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeasonalCollections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Tagline = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    BannerImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MobileBannerImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ThumbnailImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SeasonType = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    PrimaryColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SecondaryColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TextColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProductIds = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CategoryIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CollectionUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ButtonText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShowCountdown = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeasonalCollections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StylingGuides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Subtitle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ThumbnailImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GuideType = table.Column<int>(type: "int", nullable: false),
                    TargetGender = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Occasion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Season = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RelatedProductIds = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetaKeywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StylingGuides", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BlogPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Excerpt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeaturedImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ThumbnailImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AuthorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AuthorImageUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    BlogCategoryId = table.Column<int>(type: "int", nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetaKeywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlogPosts_BlogCategories_BlogCategoryId",
                        column: x => x.BlogCategoryId,
                        principalTable: "BlogCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlogCategories_IsActive",
                table: "BlogCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BlogCategories_Slug",
                table: "BlogCategories",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_BlogCategoryId",
                table: "BlogPosts",
                column: "BlogCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_IsActive",
                table: "BlogPosts",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_IsFeatured",
                table: "BlogPosts",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_PublishedAt",
                table: "BlogPosts",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Slug",
                table: "BlogPosts",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeasonalCollections_IsActive",
                table: "SeasonalCollections",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonalCollections_IsFeatured",
                table: "SeasonalCollections",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonalCollections_SeasonType",
                table: "SeasonalCollections",
                column: "SeasonType");

            migrationBuilder.CreateIndex(
                name: "IX_SeasonalCollections_Slug",
                table: "SeasonalCollections",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeasonalCollections_StartDate_EndDate",
                table: "SeasonalCollections",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StylingGuides_GuideType",
                table: "StylingGuides",
                column: "GuideType");

            migrationBuilder.CreateIndex(
                name: "IX_StylingGuides_IsActive",
                table: "StylingGuides",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_StylingGuides_IsFeatured",
                table: "StylingGuides",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_StylingGuides_Slug",
                table: "StylingGuides",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlogPosts");

            migrationBuilder.DropTable(
                name: "SeasonalCollections");

            migrationBuilder.DropTable(
                name: "StylingGuides");

            migrationBuilder.DropTable(
                name: "BlogCategories");
        }
    }
}
