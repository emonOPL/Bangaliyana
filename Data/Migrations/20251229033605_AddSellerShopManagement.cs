using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerShopManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationNote",
                table: "Sellers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AppliedAt",
                table: "Sellers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BusinessCategory",
                table: "Sellers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "Sellers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramUrl",
                table: "Sellers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OperatingHours",
                table: "Sellers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Sellers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnPolicy",
                table: "Sellers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingInfo",
                table: "Sellers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShopPolicies",
                table: "Sellers",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppNumber",
                table: "Sellers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationNote",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "AppliedAt",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "BusinessCategory",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "InstagramUrl",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "OperatingHours",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "ReturnPolicy",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "ShippingInfo",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "ShopPolicies",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "WhatsAppNumber",
                table: "Sellers");
        }
    }
}
