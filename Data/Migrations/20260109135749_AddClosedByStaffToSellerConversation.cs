using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClosedByStaffToSellerConversation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClosedByUserId",
                table: "SellerConversations",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsClosedByStaff",
                table: "SellerConversations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SellerConversations_ClosedByUserId",
                table: "SellerConversations",
                column: "ClosedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_SellerConversations_AspNetUsers_ClosedByUserId",
                table: "SellerConversations",
                column: "ClosedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SellerConversations_AspNetUsers_ClosedByUserId",
                table: "SellerConversations");

            migrationBuilder.DropIndex(
                name: "IX_SellerConversations_ClosedByUserId",
                table: "SellerConversations");

            migrationBuilder.DropColumn(
                name: "ClosedByUserId",
                table: "SellerConversations");

            migrationBuilder.DropColumn(
                name: "IsClosedByStaff",
                table: "SellerConversations");
        }
    }
}
