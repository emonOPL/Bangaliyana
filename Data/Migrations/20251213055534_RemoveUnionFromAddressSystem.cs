using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnionFromAddressSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Unions_UnionId",
                table: "AspNetUsers");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Unions_UnionId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Unions_Upazilas_UpazilaId",
                table: "Unions");

            migrationBuilder.DropForeignKey(
                name: "FK_UserAddresses_Unions_UnionId",
                table: "UserAddresses");

            migrationBuilder.DropIndex(
                name: "IX_UserAddresses_UnionId",
                table: "UserAddresses");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UnionId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_UnionId",
                table: "AspNetUsers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Unions",
                table: "Unions");

            migrationBuilder.DropColumn(
                name: "UnionId",
                table: "UserAddresses");

            migrationBuilder.DropColumn(
                name: "UnionId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OtherUnion",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UnionId",
                table: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "Unions",
                newName: "Union");

            migrationBuilder.RenameIndex(
                name: "IX_Unions_UpazilaId",
                table: "Union",
                newName: "IX_Union_UpazilaId");

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "UserAddresses",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "UserAddresses",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "Orders",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Orders",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "AspNetUsers",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Union",
                table: "Union",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Union_Upazilas_UpazilaId",
                table: "Union",
                column: "UpazilaId",
                principalTable: "Upazilas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Union_Upazilas_UpazilaId",
                table: "Union");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Union",
                table: "Union");

            migrationBuilder.RenameTable(
                name: "Union",
                newName: "Unions");

            migrationBuilder.RenameIndex(
                name: "IX_Union_UpazilaId",
                table: "Unions",
                newName: "IX_Unions_UpazilaId");

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "UserAddresses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "UserAddresses",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(11)",
                oldMaxLength: 11);

            migrationBuilder.AddColumn<int>(
                name: "UnionId",
                table: "UserAddresses",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Phone",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(11)",
                oldMaxLength: 11);

            migrationBuilder.AddColumn<int>(
                name: "UnionId",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PostalCode",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(10)",
                oldMaxLength: 10,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtherUnion",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnionId",
                table: "AspNetUsers",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Unions",
                table: "Unions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserAddresses_UnionId",
                table: "UserAddresses",
                column: "UnionId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UnionId",
                table: "Orders",
                column: "UnionId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_UnionId",
                table: "AspNetUsers",
                column: "UnionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Unions_UnionId",
                table: "AspNetUsers",
                column: "UnionId",
                principalTable: "Unions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Unions_UnionId",
                table: "Orders",
                column: "UnionId",
                principalTable: "Unions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Unions_Upazilas_UpazilaId",
                table: "Unions",
                column: "UpazilaId",
                principalTable: "Upazilas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAddresses_Unions_UnionId",
                table: "UserAddresses",
                column: "UnionId",
                principalTable: "Unions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
