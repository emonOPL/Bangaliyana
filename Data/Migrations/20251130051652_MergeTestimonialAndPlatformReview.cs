using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class MergeTestimonialAndPlatformReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformReviews");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "Testimonials",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsUserSubmitted",
                table: "Testimonials",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerifiedUser",
                table: "Testimonials",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Testimonials",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Testimonials_IsActive_IsApproved_Rating",
                table: "Testimonials",
                columns: new[] { "IsActive", "IsApproved", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_Testimonials_IsUserSubmitted",
                table: "Testimonials",
                column: "IsUserSubmitted");

            migrationBuilder.CreateIndex(
                name: "IX_Testimonials_UserId",
                table: "Testimonials",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Testimonials_AspNetUsers_UserId",
                table: "Testimonials",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Testimonials_AspNetUsers_UserId",
                table: "Testimonials");

            migrationBuilder.DropIndex(
                name: "IX_Testimonials_IsActive_IsApproved_Rating",
                table: "Testimonials");

            migrationBuilder.DropIndex(
                name: "IX_Testimonials_IsUserSubmitted",
                table: "Testimonials");

            migrationBuilder.DropIndex(
                name: "IX_Testimonials_UserId",
                table: "Testimonials");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "Testimonials");

            migrationBuilder.DropColumn(
                name: "IsUserSubmitted",
                table: "Testimonials");

            migrationBuilder.DropColumn(
                name: "IsVerifiedUser",
                table: "Testimonials");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Testimonials");

            migrationBuilder.CreateTable(
                name: "PlatformReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CachedUserAvatar = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CachedUserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CachedUserIsVerified = table.Column<bool>(type: "bit", nullable: false),
                    CachedUserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CachedUserPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    ReviewText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlatformReviews_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformReviews_IsActive_IsApproved_Rating",
                table: "PlatformReviews",
                columns: new[] { "IsActive", "IsApproved", "Rating" });

            migrationBuilder.CreateIndex(
                name: "IX_PlatformReviews_UserId",
                table: "PlatformReviews",
                column: "UserId",
                unique: true);
        }
    }
}
