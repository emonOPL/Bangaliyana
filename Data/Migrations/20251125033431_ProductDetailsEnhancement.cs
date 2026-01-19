using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProductDetailsEnhancement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "ProductVariants",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ProductVariants",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CareInstructions",
                table: "Products",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryOfOrigin",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimatedDeliveryDays",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Has360View",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Highlights",
                table: "Products",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCODAvailable",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsReturnable",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Manufacturer",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReturnDays",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SizeGuideId",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "View360Url",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyInfo",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NewsletterSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SubscribedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UnsubscribedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NewsletterSubscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AnsweredById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsAnswered = table.Column<bool>(type: "bit", nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    HelpfulCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductQuestions_AspNetUsers_AnsweredById",
                        column: x => x.AnsweredById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductQuestions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductQuestions_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SizeGuides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizeGuides", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SizeGuideEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SizeGuideId = table.Column<int>(type: "int", nullable: false),
                    Size = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Chest = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Waist = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Hip = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Length = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Shoulder = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SleeveLength = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Inseam = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Neck = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizeGuideEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SizeGuideEntries_SizeGuides_SizeGuideId",
                        column: x => x.SizeGuideId,
                        principalTable: "SizeGuides",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_SizeGuideId",
                table: "Products",
                column: "SizeGuideId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuestions_AnsweredById",
                table: "ProductQuestions",
                column: "AnsweredById");

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuestions_ProductId",
                table: "ProductQuestions",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductQuestions_UserId",
                table: "ProductQuestions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SizeGuideEntries_SizeGuideId",
                table: "SizeGuideEntries",
                column: "SizeGuideId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_SizeGuides_SizeGuideId",
                table: "Products",
                column: "SizeGuideId",
                principalTable: "SizeGuides",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_SizeGuides_SizeGuideId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "NewsletterSubscriptions");

            migrationBuilder.DropTable(
                name: "ProductQuestions");

            migrationBuilder.DropTable(
                name: "SizeGuideEntries");

            migrationBuilder.DropTable(
                name: "SizeGuides");

            migrationBuilder.DropIndex(
                name: "IX_Products_SizeGuideId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "CareInstructions",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CountryOfOrigin",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "EstimatedDeliveryDays",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Has360View",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Highlights",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsCODAvailable",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsReturnable",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Manufacturer",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ReturnDays",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SizeGuideId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "View360Url",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WarrantyInfo",
                table: "Products");
        }
    }
}
