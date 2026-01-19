using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageSharingFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentType",
                table: "SellerMessages",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MessageType",
                table: "SellerMessages",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SharedOrderId",
                table: "SellerMessages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SharedProductId",
                table: "SellerMessages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerMessages_SharedOrderId",
                table: "SellerMessages",
                column: "SharedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SellerMessages_SharedProductId",
                table: "SellerMessages",
                column: "SharedProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_SellerMessages_Orders_SharedOrderId",
                table: "SellerMessages",
                column: "SharedOrderId",
                principalTable: "Orders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_SellerMessages_Products_SharedProductId",
                table: "SellerMessages",
                column: "SharedProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SellerMessages_Orders_SharedOrderId",
                table: "SellerMessages");

            migrationBuilder.DropForeignKey(
                name: "FK_SellerMessages_Products_SharedProductId",
                table: "SellerMessages");

            migrationBuilder.DropIndex(
                name: "IX_SellerMessages_SharedOrderId",
                table: "SellerMessages");

            migrationBuilder.DropIndex(
                name: "IX_SellerMessages_SharedProductId",
                table: "SellerMessages");

            migrationBuilder.DropColumn(
                name: "AttachmentType",
                table: "SellerMessages");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "SellerMessages");

            migrationBuilder.DropColumn(
                name: "SharedOrderId",
                table: "SellerMessages");

            migrationBuilder.DropColumn(
                name: "SharedProductId",
                table: "SellerMessages");
        }
    }
}
