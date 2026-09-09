using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoopGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConceptTagAsEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConceptTag",
                schema: "public",
                table: "Shift",
                type: "varchar(50)",
                nullable: false,
                defaultValue: "Basics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConceptTag",
                schema: "public",
                table: "Shift");
        }
    }
}
