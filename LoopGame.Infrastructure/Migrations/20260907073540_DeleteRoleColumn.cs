using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoopGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeleteRoleColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Role",
                schema: "public",
                table: "ApplicationUser");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                schema: "public",
                table: "ApplicationUser",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
