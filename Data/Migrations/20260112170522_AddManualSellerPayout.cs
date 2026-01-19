using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddManualSellerPayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ManualSellerPayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayoutNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SellerId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankAccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BankAccountHolderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankBranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BankRoutingNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BankTransactionReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    MobileBankingProvider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SenderMobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReceiverMobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MobileTransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProcessedById = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ProcessedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalSales = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualSellerPayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManualSellerPayouts_AspNetUsers_ProcessedById",
                        column: x => x.ProcessedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManualSellerPayouts_Sellers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Sellers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ManualSellerPayouts_ProcessedById",
                table: "ManualSellerPayouts",
                column: "ProcessedById");

            migrationBuilder.CreateIndex(
                name: "IX_ManualSellerPayouts_SellerId",
                table: "ManualSellerPayouts",
                column: "SellerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ManualSellerPayouts");
        }
    }
}
