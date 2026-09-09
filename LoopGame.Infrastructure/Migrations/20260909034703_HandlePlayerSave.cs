using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoopGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HandlePlayerSave : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_PlayerSave",
                schema: "public",
                table: "PlayerSave");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_PlayerSave_SlotNumber",
                schema: "public",
                table: "PlayerSave");

            migrationBuilder.DropColumn(
                name: "SlotNumber",
                schema: "public",
                table: "PlayerSave");

            migrationBuilder.DropColumn(
                name: "desktop_state",
                schema: "public",
                table: "PlayerSave");

            migrationBuilder.AddColumn<int>(
                name: "BeatId",
                schema: "public",
                table: "PlayerSave",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSave_BeatId",
                schema: "public",
                table: "PlayerSave",
                column: "BeatId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSave_PlayerId",
                schema: "public",
                table: "PlayerSave",
                column: "PlayerId");

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerSave_StoryBeat_BeatId",
                schema: "public",
                table: "PlayerSave",
                column: "BeatId",
                principalSchema: "public",
                principalTable: "StoryBeat",
                principalColumn: "BeatId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerSave_StoryBeat_BeatId",
                schema: "public",
                table: "PlayerSave");

            migrationBuilder.DropIndex(
                name: "IX_PlayerSave_BeatId",
                schema: "public",
                table: "PlayerSave");

            migrationBuilder.DropIndex(
                name: "IX_PlayerSave_PlayerId",
                schema: "public",
                table: "PlayerSave");

            migrationBuilder.DropColumn(
                name: "BeatId",
                schema: "public",
                table: "PlayerSave");

            migrationBuilder.AddColumn<byte>(
                name: "SlotNumber",
                schema: "public",
                table: "PlayerSave",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<string>(
                name: "desktop_state",
                schema: "public",
                table: "PlayerSave",
                type: "jsonb",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "UQ_PlayerSave",
                schema: "public",
                table: "PlayerSave",
                columns: new[] { "PlayerId", "SlotNumber" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CHK_PlayerSave_SlotNumber",
                schema: "public",
                table: "PlayerSave",
                sql: "\"SlotNumber\" IN (1, 2, 3)");
        }
    }
}
