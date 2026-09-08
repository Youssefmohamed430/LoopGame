using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoopGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class HandleTestCaseEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestCase_SideTaskTemplate_TemplateId",
                schema: "public",
                table: "TestCase");

            migrationBuilder.DropIndex(
                name: "IX_TestCase_TemplateId",
                schema: "public",
                table: "TestCase");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                schema: "public",
                table: "TestCase");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TemplateId",
                schema: "public",
                table: "TestCase",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestCase_TemplateId",
                schema: "public",
                table: "TestCase",
                column: "TemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_TestCase_SideTaskTemplate_TemplateId",
                schema: "public",
                table: "TestCase",
                column: "TemplateId",
                principalSchema: "public",
                principalTable: "SideTaskTemplate",
                principalColumn: "TemplateId");
        }
    }
}
