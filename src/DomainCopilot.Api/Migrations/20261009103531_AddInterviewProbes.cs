using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomainCopilot.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddInterviewProbes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InterviewProbes",
                table: "CandidateEvaluations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InterviewProbes",
                table: "CandidateEvaluations");
        }
    }
}
