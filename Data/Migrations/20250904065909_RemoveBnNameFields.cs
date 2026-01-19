using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBnNameFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BnName",
                table: "Upazilas");

            migrationBuilder.DropColumn(
                name: "BnName",
                table: "Divisions");

            migrationBuilder.DropColumn(
                name: "BnName",
                table: "Districts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BnName",
                table: "Upazilas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BnName",
                table: "Divisions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BnName",
                table: "Districts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
