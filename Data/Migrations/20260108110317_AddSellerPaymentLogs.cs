using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerPaymentLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SellerPaymentLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogType = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SellerPaymentId = table.Column<int>(type: "int", nullable: true),
                    SellerId = table.Column<int>(type: "int", nullable: true),
                    TriggeredByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TotalSellersProcessed = table.Column<int>(type: "int", nullable: true),
                    SuccessCount = table.Column<int>(type: "int", nullable: true),
                    FailCount = table.Column<int>(type: "int", nullable: true),
                    TotalAmountProcessed = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerPaymentLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerPaymentLogs_AspNetUsers_TriggeredByUserId",
                        column: x => x.TriggeredByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SellerPaymentLogs_SellerPayments_SellerPaymentId",
                        column: x => x.SellerPaymentId,
                        principalTable: "SellerPayments",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SellerPaymentLogs_Sellers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Sellers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellerPaymentLogs_SellerId",
                table: "SellerPaymentLogs",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerPaymentLogs_SellerPaymentId",
                table: "SellerPaymentLogs",
                column: "SellerPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerPaymentLogs_TriggeredByUserId",
                table: "SellerPaymentLogs",
                column: "TriggeredByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerPaymentLogs");
        }
    }
}
