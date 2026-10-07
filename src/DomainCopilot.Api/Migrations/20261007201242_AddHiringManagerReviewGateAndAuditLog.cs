using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DomainCopilot.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHiringManagerReviewGateAndAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogEntries_AspNetUsers_PerformedByUserId",
                table: "AuditLogEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogEntries_CandidateEvaluations_CandidateEvaluationId",
                table: "AuditLogEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogEntries",
                table: "AuditLogEntries");

            migrationBuilder.RenameTable(
                name: "AuditLogEntries",
                newName: "AuditLogs");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogEntries_PerformedByUserId",
                table: "AuditLogs",
                newName: "IX_AuditLogs_PerformedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogEntries_CandidateEvaluationId",
                table: "AuditLogs",
                newName: "IX_AuditLogs_CandidateEvaluationId");

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "CandidateEvaluations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReviewedBy",
                table: "CandidateEvaluations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerformedBy",
                table: "AuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "Timestamp",
                table: "AuditLogs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_AspNetUsers_PerformedByUserId",
                table: "AuditLogs",
                column: "PerformedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_CandidateEvaluations_CandidateEvaluationId",
                table: "AuditLogs",
                column: "CandidateEvaluationId",
                principalTable: "CandidateEvaluations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_AspNetUsers_PerformedByUserId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_CandidateEvaluations_CandidateEvaluationId",
                table: "AuditLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "CandidateEvaluations");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "CandidateEvaluations");

            migrationBuilder.DropColumn(
                name: "PerformedBy",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "AuditLogEntries");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_PerformedByUserId",
                table: "AuditLogEntries",
                newName: "IX_AuditLogEntries_PerformedByUserId");

            migrationBuilder.RenameIndex(
                name: "IX_AuditLogs_CandidateEvaluationId",
                table: "AuditLogEntries",
                newName: "IX_AuditLogEntries_CandidateEvaluationId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogEntries",
                table: "AuditLogEntries",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogEntries_AspNetUsers_PerformedByUserId",
                table: "AuditLogEntries",
                column: "PerformedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogEntries_CandidateEvaluations_CandidateEvaluationId",
                table: "AuditLogEntries",
                column: "CandidateEvaluationId",
                principalTable: "CandidateEvaluations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
