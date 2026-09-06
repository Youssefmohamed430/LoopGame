using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoopGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_PlayerSideTask_Status",
                schema: "public",
                table: "PlayerSideTask");

            migrationBuilder.AddColumn<int>(
                name: "Difficulty",
                schema: "public",
                table: "PlayerSideTask",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddCheckConstraint(
                name: "CHK_PlayerSideTask_Status",
                schema: "public",
                table: "PlayerSideTask",
                sql: "\"Status\" IN ('active', 'queued', 'submitted', 'abandoned', 'expired')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CHK_PlayerSideTask_Status",
                schema: "public",
                table: "PlayerSideTask");

            migrationBuilder.DropColumn(
                name: "Difficulty",
                schema: "public",
                table: "PlayerSideTask");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_PlayerSideTask_Status",
                schema: "public",
                table: "PlayerSideTask",
                sql: "\"Status\" IN ('active', 'submitted', 'abandoned', 'expired')");
        }
    }
}
