using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportCenterFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContactInquiries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    AdminResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RespondedById = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactInquiries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactInquiries_AspNetUsers_RespondedById",
                        column: x => x.RespondedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContactInquiries_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupportChatSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    VisitorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    VisitorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    InitialQuery = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Department = table.Column<int>(type: "int", nullable: false),
                    AgentId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CustomerConnectionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgentConnectionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AgentJoinedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportChatSessions_AspNetUsers_AgentId",
                        column: x => x.AgentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupportChatSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupportChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SenderId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsAgentMessage = table.Column<bool>(type: "bit", nullable: false),
                    IsSystemMessage = table.Column<bool>(type: "bit", nullable: false),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttachmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AttachmentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportChatMessages_AspNetUsers_SenderId",
                        column: x => x.SenderId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SupportChatMessages_SupportChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "SupportChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContactInquiries_RespondedById",
                table: "ContactInquiries",
                column: "RespondedById");

            migrationBuilder.CreateIndex(
                name: "IX_ContactInquiries_UserId",
                table: "ContactInquiries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportChatMessages_SenderId",
                table: "SupportChatMessages",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportChatMessages_SessionId",
                table: "SupportChatMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportChatSessions_AgentId",
                table: "SupportChatSessions",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportChatSessions_UserId",
                table: "SupportChatSessions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContactInquiries");

            migrationBuilder.DropTable(
                name: "SupportChatMessages");

            migrationBuilder.DropTable(
                name: "SupportChatSessions");
        }
    }
}
