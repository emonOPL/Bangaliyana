using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerPaymentAccountsFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MobileBankingAccountName",
                table: "Sellers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MobileBankingNumber",
                table: "Sellers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MobileBankingProvider",
                table: "Sellers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SellerBankAccountChangeRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SellerId = table.Column<int>(type: "int", nullable: false),
                    ExistingBankAccountId = table.Column<int>(type: "int", nullable: true),
                    NewAccountType = table.Column<int>(type: "int", nullable: false),
                    NewBankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NewBranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    NewAccountHolderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NewAccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NewRoutingNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NewCheckbookPhotoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerBankAccountChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerBankAccountChangeRequests_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SellerBankAccountChangeRequests_SellerBankAccounts_ExistingBankAccountId",
                        column: x => x.ExistingBankAccountId,
                        principalTable: "SellerBankAccounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SellerBankAccountChangeRequests_Sellers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Sellers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellerBankAccountChangeRequests_ExistingBankAccountId",
                table: "SellerBankAccountChangeRequests",
                column: "ExistingBankAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerBankAccountChangeRequests_ReviewedByUserId",
                table: "SellerBankAccountChangeRequests",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerBankAccountChangeRequests_SellerId",
                table: "SellerBankAccountChangeRequests",
                column: "SellerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerBankAccountChangeRequests");

            migrationBuilder.DropColumn(
                name: "MobileBankingAccountName",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "MobileBankingNumber",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "MobileBankingProvider",
                table: "Sellers");
        }
    }
}
