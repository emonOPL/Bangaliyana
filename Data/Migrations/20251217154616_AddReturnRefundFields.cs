using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnRefundFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredRefundMethod",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PrefersReplacement",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RefundAccountHolderName",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundAccountNumber",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundBankName",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnImages",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredRefundMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PrefersReplacement",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundAccountHolderName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundAccountNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundBankName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReturnImages",
                table: "Orders");
        }
    }
}
