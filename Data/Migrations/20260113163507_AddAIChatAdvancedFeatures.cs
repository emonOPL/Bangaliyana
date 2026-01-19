using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAIChatAdvancedFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIChatAnalytics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalSessions = table.Column<int>(type: "int", nullable: false),
                    NewSessions = table.Column<int>(type: "int", nullable: false),
                    ReturningUsers = table.Column<int>(type: "int", nullable: false),
                    GuestSessions = table.Column<int>(type: "int", nullable: false),
                    AuthenticatedSessions = table.Column<int>(type: "int", nullable: false),
                    TotalMessages = table.Column<int>(type: "int", nullable: false),
                    UserMessages = table.Column<int>(type: "int", nullable: false),
                    AIMessages = table.Column<int>(type: "int", nullable: false),
                    AgentMessages = table.Column<int>(type: "int", nullable: false),
                    SessionsResolved = table.Column<int>(type: "int", nullable: false),
                    SessionsHandedOff = table.Column<int>(type: "int", nullable: false),
                    SessionsAbandoned = table.Column<int>(type: "int", nullable: false),
                    AverageResponseTimeMs = table.Column<double>(type: "float", nullable: false),
                    AverageSessionDuration = table.Column<double>(type: "float", nullable: false),
                    AverageMessagesPerSession = table.Column<double>(type: "float", nullable: false),
                    AverageRating = table.Column<double>(type: "float", nullable: false),
                    TotalRatings = table.Column<int>(type: "int", nullable: false),
                    PositiveFeedback = table.Column<int>(type: "int", nullable: false),
                    NegativeFeedback = table.Column<int>(type: "int", nullable: false),
                    TopIntents = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TopTopics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BengaliMessages = table.Column<int>(type: "int", nullable: false),
                    EnglishMessages = table.Column<int>(type: "int", nullable: false),
                    BanglishMessages = table.Column<int>(type: "int", nullable: false),
                    CartAdditions = table.Column<int>(type: "int", nullable: false),
                    WishlistAdditions = table.Column<int>(type: "int", nullable: false),
                    OrdersTracked = table.Column<int>(type: "int", nullable: false),
                    ProductsSearched = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIChatAnalytics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIChatSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    GuestSessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GuestName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InitialQuery = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DetectedLanguage = table.Column<int>(type: "int", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AssignedAgentId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    HandoffRequestedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HandoffAcceptedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HandoffReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TotalMessages = table.Column<int>(type: "int", nullable: false),
                    AIResponses = table.Column<int>(type: "int", nullable: false),
                    UserMessages = table.Column<int>(type: "int", nullable: false),
                    AverageResponseTime = table.Column<double>(type: "float", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: true),
                    Feedback = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WasHelpful = table.Column<bool>(type: "bit", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeviceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ContextData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Topics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetectedIntents = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastActivityAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIChatSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIChatSessions_AspNetUsers_AssignedAgentId",
                        column: x => x.AssignedAgentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AIChatSessions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProactiveChatTriggers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TriggerType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TriggerValue = table.Column<int>(type: "int", nullable: true),
                    PageUrlPattern = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TargetGuests = table.Column<bool>(type: "bit", nullable: false),
                    TargetAuthenticated = table.Column<bool>(type: "bit", nullable: false),
                    TargetNewUsers = table.Column<bool>(type: "bit", nullable: false),
                    TargetReturningUsers = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MessageBengali = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuickReplies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DelaySeconds = table.Column<int>(type: "int", nullable: false),
                    CooldownMinutes = table.Column<int>(type: "int", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    TimesTriggered = table.Column<int>(type: "int", nullable: false),
                    TimesClicked = table.Column<int>(type: "int", nullable: false),
                    TimesConverted = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProactiveChatTriggers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAIChatPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PreferredLanguage = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AutoDetectLanguage = table.Column<bool>(type: "bit", nullable: false),
                    EnableChatNotifications = table.Column<bool>(type: "bit", nullable: false),
                    EnableEmailNotifications = table.Column<bool>(type: "bit", nullable: false),
                    EnableSoundNotifications = table.Column<bool>(type: "bit", nullable: false),
                    EnablePersonalizedResponses = table.Column<bool>(type: "bit", nullable: false),
                    RememberConversationHistory = table.Column<bool>(type: "bit", nullable: false),
                    ShowProductRecommendations = table.Column<bool>(type: "bit", nullable: false),
                    InterestedCategories = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BrowsingBehavior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PurchasePreferences = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TotalSessions = table.Column<int>(type: "int", nullable: false),
                    TotalMessages = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAIChatPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAIChatPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIChatHandoffQueues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    AssignedAgentId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsAssigned = table.Column<bool>(type: "bit", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EstimatedWaitTime = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIChatHandoffQueues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIChatHandoffQueues_AIChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AIChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AIChatHandoffQueues_AspNetUsers_AssignedAgentId",
                        column: x => x.AssignedAgentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AIChatHandoffQueues_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sender = table.Column<int>(type: "int", nullable: false),
                    MessageType = table.Column<int>(type: "int", nullable: false),
                    RichContent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuickReplies = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DetectedIntent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IntentConfidence = table.Column<double>(type: "float", nullable: true),
                    Sentiment = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Language = table.Column<int>(type: "int", nullable: false),
                    ResponseTimeMs = table.Column<int>(type: "int", nullable: true),
                    WasHelpful = table.Column<bool>(type: "bit", nullable: true),
                    UserFeedback = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttachmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AttachmentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AgentId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIChatMessages_AIChatSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "AIChatSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AIChatMessages_AspNetUsers_AgentId",
                        column: x => x.AgentId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIChatAnalytics_Date",
                table: "AIChatAnalytics",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIChatHandoffQueues_AssignedAgentId",
                table: "AIChatHandoffQueues",
                column: "AssignedAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AIChatHandoffQueues_IsAssigned",
                table: "AIChatHandoffQueues",
                column: "IsAssigned");

            migrationBuilder.CreateIndex(
                name: "IX_AIChatHandoffQueues_IsResolved",
                table: "AIChatHandoffQueues",
                column: "IsResolved");

            migrationBuilder.CreateIndex(
                name: "IX_AIChatHandoffQueues_Priority",
                table: "AIChatHandoffQueues",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_AIChatHandoffQueues_SessionId",
                table: "AIChatHandoffQueues",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AIChatHandoffQueues_UserId",
                table: "AIChatHandoffQueues",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AIChatMessages_AgentId",
                table: "AIChatMessages",
                column: "AgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AIChatMessages_CreatedAt",
                table: "AIChatMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AIChatMessages_DetectedIntent",
                table: "AIChatMessages",
                column: "DetectedIntent");

            migrationBuilder.CreateIndex(
                name: "IX_AIChatMessages_SessionId",
                table: "AIChatMessages",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AIChatSessions_AssignedAgentId",
                table: "AIChatSessions",
                column: "AssignedAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_AIChatSessions_CreatedAt",
                table: "AIChatSessions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AIChatSessions_GuestSessionId",
                table: "AIChatSessions",
                column: "GuestSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AIChatSessions_SessionCode",
                table: "AIChatSessions",
                column: "SessionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIChatSessions_Status",
                table: "AIChatSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AIChatSessions_UserId",
                table: "AIChatSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProactiveChatTriggers_IsActive",
                table: "ProactiveChatTriggers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProactiveChatTriggers_TriggerType",
                table: "ProactiveChatTriggers",
                column: "TriggerType");

            migrationBuilder.CreateIndex(
                name: "IX_UserAIChatPreferences_UserId",
                table: "UserAIChatPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIChatAnalytics");

            migrationBuilder.DropTable(
                name: "AIChatHandoffQueues");

            migrationBuilder.DropTable(
                name: "AIChatMessages");

            migrationBuilder.DropTable(
                name: "ProactiveChatTriggers");

            migrationBuilder.DropTable(
                name: "UserAIChatPreferences");

            migrationBuilder.DropTable(
                name: "AIChatSessions");
        }
    }
}
