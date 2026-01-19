using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPlatformReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ReviewText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    CachedUserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CachedUserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CachedUserPhone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CachedUserAvatar = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CachedUserIsVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformReviews");
        }
    }
}
