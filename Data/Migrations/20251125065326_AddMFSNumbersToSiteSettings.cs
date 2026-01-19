using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMFSNumbersToSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BkashNumber",
                table: "SiteSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NagadNumber",
                table: "SiteSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RocketNumber",
                table: "SiteSettings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BkashNumber",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "NagadNumber",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "RocketNumber",
                table: "SiteSettings");
        }
    }
}
