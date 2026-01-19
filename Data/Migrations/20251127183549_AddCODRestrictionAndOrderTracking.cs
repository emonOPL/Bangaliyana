using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCODRestrictionAndOrderTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CODPenaltyApplied",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CustomerRefusedDelivery",
                table: "Orders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReturnReason",
                table: "Orders",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnedAt",
                table: "Orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CODRestrictedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CODRestrictionReason",
                table: "AspNetUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FailedCODOrdersCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsCODAllowed",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReturnedOrdersCount",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CODPenaltyApplied",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CustomerRefusedDelivery",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReturnReason",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReturnedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CODRestrictedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "CODRestrictionReason",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FailedCODOrdersCount",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "IsCODAllowed",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ReturnedOrdersCount",
                table: "AspNetUsers");
        }
    }
}
