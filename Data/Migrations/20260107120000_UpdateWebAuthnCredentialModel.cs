using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWebAuthnCredentialModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new byte array columns for proper WebAuthn storage (nullable for backward compatibility)
            migrationBuilder.AddColumn<byte[]>(
                name: "CredentialIdBytes",
                table: "WebAuthnCredentials",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "PublicKeyBytes",
                table: "WebAuthnCredentials",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "UserHandle",
                table: "WebAuthnCredentials",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AaGuidValue",
                table: "WebAuthnCredentials",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CredentialType",
                table: "WebAuthnCredentials",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "public-key");

            migrationBuilder.AddColumn<DateTime>(
                name: "RegDate",
                table: "WebAuthnCredentials",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()");

            // For existing credentials, copy CreatedAt to RegDate
            migrationBuilder.Sql(@"
                UPDATE WebAuthnCredentials
                SET RegDate = CreatedAt
                WHERE RegDate = '0001-01-01'
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CredentialIdBytes",
                table: "WebAuthnCredentials");

            migrationBuilder.DropColumn(
                name: "PublicKeyBytes",
                table: "WebAuthnCredentials");

            migrationBuilder.DropColumn(
                name: "UserHandle",
                table: "WebAuthnCredentials");

            migrationBuilder.DropColumn(
                name: "AaGuidValue",
                table: "WebAuthnCredentials");

            migrationBuilder.DropColumn(
                name: "CredentialType",
                table: "WebAuthnCredentials");

            migrationBuilder.DropColumn(
                name: "RegDate",
                table: "WebAuthnCredentials");
        }
    }
}
