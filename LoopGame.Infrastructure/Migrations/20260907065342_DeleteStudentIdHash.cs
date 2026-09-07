using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoopGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeleteStudentIdHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Player_StudentIdHash",
                schema: "public",
                table: "Player");

            migrationBuilder.DropColumn(
                name: "StudentIdHash",
                schema: "public",
                table: "Player");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StudentIdHash",
                schema: "public",
                table: "Player",
                type: "character(64)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Player_StudentIdHash",
                schema: "public",
                table: "Player",
                column: "StudentIdHash",
                unique: true);
        }
    }
}
