using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomainCopilot.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChunkMetadataAndEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChunkIndex",
                table: "DocumentChunks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PageNumber",
                table: "DocumentChunks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WordCount",
                table: "DocumentChunks",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChunkIndex",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "PageNumber",
                table: "DocumentChunks");

            migrationBuilder.DropColumn(
                name: "WordCount",
                table: "DocumentChunks");
        }
    }
}
