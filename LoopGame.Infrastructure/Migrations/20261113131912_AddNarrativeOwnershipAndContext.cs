using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LoopGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNarrativeOwnershipAndContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GenerationLogId",
                schema: "public",
                table: "StoryBeat",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAiGenerated",
                schema: "public",
                table: "StoryBeat",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PlayerId",
                schema: "public",
                table: "StoryBeat",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionContext",
                schema: "public",
                table: "PlayerShiftProgress",
                type: "character varying(600)",
                maxLength: 600,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "NarrativeGenerationLog",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ShiftId = table.Column<int>(type: "integer", nullable: false),
                    OperationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ResponseTimeMs = table.Column<int>(type: "integer", nullable: false),
                    ValidationPassed = table.Column<bool>(type: "boolean", nullable: false),
                    RepairAttempted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RepairSucceeded = table.Column<bool>(type: "boolean", nullable: true),
                    UsedFallback = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NarrativeGenerationLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NarrativeGenerationLog_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NarrativeGenerationLog_Shift_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "public",
                        principalTable: "Shift",
                        principalColumn: "ShiftId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beat_Shift_Player",
                schema: "public",
                table: "StoryBeat",
                columns: new[] { "ShiftId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_StoryBeat_GenerationLogId",
                schema: "public",
                table: "StoryBeat",
                column: "GenerationLogId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoryBeat_PlayerId",
                schema: "public",
                table: "StoryBeat",
                column: "PlayerId");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_Beat_AiOwnership",
                schema: "public",
                table: "StoryBeat",
                sql: "\"IsAiGenerated\" = false OR \"PlayerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeGenerationLog_CreatedAt",
                schema: "public",
                table: "NarrativeGenerationLog",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeGenerationLog_Player_CreatedAt",
                schema: "public",
                table: "NarrativeGenerationLog",
                columns: new[] { "PlayerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NarrativeGenerationLog_ShiftId",
                schema: "public",
                table: "NarrativeGenerationLog",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_StoryBeat_NarrativeGenerationLog_GenerationLogId",
                schema: "public",
                table: "StoryBeat",
                column: "GenerationLogId",
                principalSchema: "public",
                principalTable: "NarrativeGenerationLog",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StoryBeat_Player_PlayerId",
                schema: "public",
                table: "StoryBeat",
                column: "PlayerId",
                principalSchema: "public",
                principalTable: "Player",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StoryBeat_NarrativeGenerationLog_GenerationLogId",
                schema: "public",
                table: "StoryBeat");

            migrationBuilder.DropForeignKey(
                name: "FK_StoryBeat_Player_PlayerId",
                schema: "public",
                table: "StoryBeat");

            migrationBuilder.DropTable(
                name: "NarrativeGenerationLog",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_Beat_Shift_Player",
                schema: "public",
                table: "StoryBeat");

            migrationBuilder.DropIndex(
                name: "IX_StoryBeat_GenerationLogId",
                schema: "public",
                table: "StoryBeat");

            migrationBuilder.DropIndex(
                name: "IX_StoryBeat_PlayerId",
                schema: "public",
                table: "StoryBeat");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_Beat_AiOwnership",
                schema: "public",
                table: "StoryBeat");

            migrationBuilder.DropColumn(
                name: "GenerationLogId",
                schema: "public",
                table: "StoryBeat");

            migrationBuilder.DropColumn(
                name: "IsAiGenerated",
                schema: "public",
                table: "StoryBeat");

            migrationBuilder.DropColumn(
                name: "PlayerId",
                schema: "public",
                table: "StoryBeat");

            migrationBuilder.DropColumn(
                name: "SessionContext",
                schema: "public",
                table: "PlayerShiftProgress");
        }
    }
}
