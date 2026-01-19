using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShopFollower : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FollowerCount",
                table: "Sellers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ShopFollowers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SellerId = table.Column<int>(type: "int", nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NotifyOnNewProducts = table.Column<bool>(type: "bit", nullable: false),
                    NotifyOnSales = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopFollowers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopFollowers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopFollowers_Sellers_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Sellers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopFollowers_SellerId",
                table: "ShopFollowers",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopFollowers_UserId_SellerId",
                table: "ShopFollowers",
                columns: new[] { "UserId", "SellerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopFollowers");

            migrationBuilder.DropColumn(
                name: "FollowerCount",
                table: "Sellers");
        }
    }
}
