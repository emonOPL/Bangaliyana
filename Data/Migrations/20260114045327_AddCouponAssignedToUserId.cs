using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCouponAssignedToUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedToUserId",
                table: "Coupons",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_AssignedToUserId",
                table: "Coupons",
                column: "AssignedToUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Coupons_AspNetUsers_AssignedToUserId",
                table: "Coupons",
                column: "AssignedToUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Coupons_AspNetUsers_AssignedToUserId",
                table: "Coupons");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_AssignedToUserId",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "Coupons");
        }
    }
}
