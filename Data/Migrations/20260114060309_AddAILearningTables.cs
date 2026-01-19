using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bangaliyana.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAILearningTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIFeedbackAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Intent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AnalysisDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalResponses = table.Column<int>(type: "int", nullable: false),
                    PositiveCount = table.Column<int>(type: "int", nullable: false),
                    NegativeCount = table.Column<int>(type: "int", nullable: false),
                    SatisfactionRate = table.Column<double>(type: "float", nullable: false),
                    CommonIssues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuggestedImprovements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsProcessed = table.Column<bool>(type: "bit", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIFeedbackAnalyses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AILearningRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Intent = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    QueryPattern = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    OriginalResponse = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ImprovedResponse = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PositiveFeedbackCount = table.Column<int>(type: "int", nullable: false),
                    NegativeFeedbackCount = table.Column<int>(type: "int", nullable: false),
                    ConfidenceAdjustment = table.Column<double>(type: "float", nullable: false),
                    AdditionalKeywords = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    LastAppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TimesApplied = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AILearningRecords", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIFeedbackAnalyses");

            migrationBuilder.DropTable(
                name: "AILearningRecords");
        }
    }
}
