using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundStatusTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedRefundAmount",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReferredToAccounting",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReferredToAccountingAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferredToAccountingName",
                table: "Orders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundAdminNotes",
                table: "Orders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefundStatus",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundStatusChangedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedRefundAmount",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReferredToAccounting",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReferredToAccountingAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReferredToAccountingName",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundAdminNotes",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundStatus",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RefundStatusChangedAt",
                table: "Orders");
        }
    }
}
