using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerPaymentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SellerPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SellerId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BankName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccountHolderName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AccountNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RoutingNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MobileBankingProvider = table.Column<int>(type: "int", nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    MobileAccountName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TransactionReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProcessedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerPayments_AspNetUsers_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SellerPayments_Sellers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Sellers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SellerPaymentSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MinimumPayoutAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentDayOfMonth = table.Column<int>(type: "int", nullable: false),
                    PaymentHour = table.Column<int>(type: "int", nullable: false),
                    AutoProcessPayments = table.Column<bool>(type: "bit", nullable: false),
                    SendEmailNotifications = table.Column<bool>(type: "bit", nullable: false),
                    RequireBankAccountVerification = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerPaymentSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SellerPaymentMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SellerPaymentId = table.Column<int>(type: "int", nullable: false),
                    SenderId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsStaffReply = table.Column<bool>(type: "bit", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerPaymentMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerPaymentMessages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SellerPaymentMessages_SellerPayments_SellerPaymentId",
                        column: x => x.SellerPaymentId,
                        principalTable: "SellerPayments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SellerPaymentMessageAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerPaymentMessageAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerPaymentMessageAttachments_SellerPaymentMessages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "SellerPaymentMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellerPaymentMessageAttachments_MessageId",
                table: "SellerPaymentMessageAttachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerPaymentMessages_CreatedAt",
                table: "SellerPaymentMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SellerPaymentMessages_SellerPaymentId",
                table: "SellerPaymentMessages",
                column: "SellerPaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerPaymentMessages_SenderId",
                table: "SellerPaymentMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerPayments_CreatedAt",
                table: "SellerPayments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SellerPayments_PaymentNumber",
                table: "SellerPayments",
                column: "PaymentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerPayments_ProcessedByUserId",
                table: "SellerPayments",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerPayments_SellerId",
                table: "SellerPayments",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerPayments_Status",
                table: "SellerPayments",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerPaymentMessageAttachments");

            migrationBuilder.DropTable(
                name: "SellerPaymentSettings");

            migrationBuilder.DropTable(
                name: "SellerPaymentMessages");

            migrationBuilder.DropTable(
                name: "SellerPayments");
        }
    }
}
