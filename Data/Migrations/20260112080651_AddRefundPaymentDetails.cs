using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundPaymentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RefundPaidAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundPaidFromAccount",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundPaymentDate",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefundPaymentMethod",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundPaymentNotes",
                table: "Orders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReceiverNumber",
                table: "Orders",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundTransactionId",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundPaidAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundPaidFromAccount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundPaymentDate",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundPaymentMethod",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundPaymentNotes",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundReceiverNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundTransactionId",
                table: "Orders");
        }
    }
}
