using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerApplicationSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApplicationStatus",
                table: "Sellers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ApplicationStep",
                table: "Sellers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BusinessTypeId",
                table: "Sellers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentNumber",
                table: "Sellers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentPhotoUrl",
                table: "Sellers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocumentType",
                table: "Sellers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FatherName",
                table: "Sellers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FullName",
                table: "Sellers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModificationNotes",
                table: "Sellers",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModificationRequestedAt",
                table: "Sellers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotherName",
                table: "Sellers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PassportPhotoUrl",
                table: "Sellers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PermanentAddress",
                table: "Sellers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PresentAddress",
                table: "Sellers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckbookPhotoUrl",
                table: "SellerBankAccounts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BusinessTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IconClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessTypes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Sellers_BusinessTypeId",
                table: "Sellers",
                column: "BusinessTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Sellers_DocumentNumber",
                table: "Sellers",
                column: "DocumentNumber",
                unique: true,
                filter: "[DocumentNumber] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Sellers_BusinessTypes_BusinessTypeId",
                table: "Sellers",
                column: "BusinessTypeId",
                principalTable: "BusinessTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sellers_BusinessTypes_BusinessTypeId",
                table: "Sellers");

            migrationBuilder.DropTable(
                name: "BusinessTypes");

            migrationBuilder.DropIndex(
                name: "IX_Sellers_BusinessTypeId",
                table: "Sellers");

            migrationBuilder.DropIndex(
                name: "IX_Sellers_DocumentNumber",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "ApplicationStatus",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "ApplicationStep",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "BusinessTypeId",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "DocumentNumber",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "DocumentPhotoUrl",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "FatherName",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "FullName",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "ModificationNotes",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "ModificationRequestedAt",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "MotherName",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "PassportPhotoUrl",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "PermanentAddress",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "PresentAddress",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "CheckbookPhotoUrl",
                table: "SellerBankAccounts");
        }
    }
}
