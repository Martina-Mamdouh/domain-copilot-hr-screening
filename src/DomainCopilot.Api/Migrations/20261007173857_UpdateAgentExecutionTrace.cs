using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomainCopilot.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAgentExecutionTrace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderUsed",
                table: "AgentExecutionTraces",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AgentExecutionTraces",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProviderUsed",
                table: "AgentExecutionTraces");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AgentExecutionTraces");
        }
    }
}
