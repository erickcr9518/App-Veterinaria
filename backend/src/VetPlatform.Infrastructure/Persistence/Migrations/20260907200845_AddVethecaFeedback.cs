using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetPlatform.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVethecaFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Feedback",
                table: "VethecaSearchLogs",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedbackNote",
                table: "VethecaSearchLogs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Feedback",
                table: "VethecaSearchLogs");

            migrationBuilder.DropColumn(
                name: "FeedbackNote",
                table: "VethecaSearchLogs");
        }
    }
}
