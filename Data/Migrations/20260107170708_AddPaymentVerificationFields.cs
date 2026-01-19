using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentVerificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaymentVerified",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentVerificationNotes",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentVerifiedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentVerifiedBy",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderMobileNumber",
                table: "Orders",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPaymentVerified",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentVerificationNotes",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentVerifiedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentVerifiedBy",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SenderMobileNumber",
                table: "Orders");
        }
    }
}
