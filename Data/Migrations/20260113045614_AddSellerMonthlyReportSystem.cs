using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerMonthlyReportSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SellerMonthlyReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SellerId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    ReportNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TotalSales = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalItemsDelivered = table.Column<int>(type: "int", nullable: false),
                    TotalOrders = table.Column<int>(type: "int", nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RefundDeductions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReturnDeductions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Adjustments = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CarryForwardFromPrevious = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetPayable = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CarryForwardToNext = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PayoutAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ManualSellerPayoutId = table.Column<int>(type: "int", nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerMonthlyReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerMonthlyReports_AspNetUsers_FinalizedByUserId",
                        column: x => x.FinalizedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SellerMonthlyReports_ManualSellerPayouts_ManualSellerPayoutId",
                        column: x => x.ManualSellerPayoutId,
                        principalTable: "ManualSellerPayouts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SellerMonthlyReports_Sellers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Sellers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyReportOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MonthlyReportId = table.Column<int>(type: "int", nullable: false),
                    OrderItemId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ItemAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CommissionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DeductionAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DeductionReportId = table.Column<int>(type: "int", nullable: true),
                    DeductionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReturnedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyReportOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyReportOrderItems_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyReportOrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyReportOrderItems_SellerMonthlyReports_DeductionReportId",
                        column: x => x.DeductionReportId,
                        principalTable: "SellerMonthlyReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MonthlyReportOrderItems_SellerMonthlyReports_MonthlyReportId",
                        column: x => x.MonthlyReportId,
                        principalTable: "SellerMonthlyReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReportOrderItems_DeductionReportId",
                table: "MonthlyReportOrderItems",
                column: "DeductionReportId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReportOrderItems_MonthlyReportId_OrderItemId",
                table: "MonthlyReportOrderItems",
                columns: new[] { "MonthlyReportId", "OrderItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReportOrderItems_OrderId",
                table: "MonthlyReportOrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReportOrderItems_OrderItemId",
                table: "MonthlyReportOrderItems",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyReportOrderItems_Status",
                table: "MonthlyReportOrderItems",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SellerMonthlyReports_FinalizedByUserId",
                table: "SellerMonthlyReports",
                column: "FinalizedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerMonthlyReports_ManualSellerPayoutId",
                table: "SellerMonthlyReports",
                column: "ManualSellerPayoutId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerMonthlyReports_ReportNumber",
                table: "SellerMonthlyReports",
                column: "ReportNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerMonthlyReports_SellerId_Year_Month",
                table: "SellerMonthlyReports",
                columns: new[] { "SellerId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerMonthlyReports_Status",
                table: "SellerMonthlyReports",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SellerMonthlyReports_Year_Month",
                table: "SellerMonthlyReports",
                columns: new[] { "Year", "Month" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MonthlyReportOrderItems");

            migrationBuilder.DropTable(
                name: "SellerMonthlyReports");
        }
    }
}
