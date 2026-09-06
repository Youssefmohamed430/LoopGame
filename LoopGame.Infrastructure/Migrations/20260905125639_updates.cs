using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LoopGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SheetFiles_Shift_ShiftId",
                schema: "public",
                table: "SheetFiles");

            migrationBuilder.DropForeignKey(
                name: "FK_TestCase_SideTaskTemplate_TemplateId",
                schema: "public",
                table: "TestCase");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_TestCase_Parent",
                schema: "public",
                table: "TestCase");

            migrationBuilder.DropIndex(
                name: "IX_SheetFiles_ShiftId",
                schema: "public",
                table: "SheetFiles");

            migrationBuilder.DropColumn(
                name: "DeadlineAt",
                schema: "public",
                table: "PlayerSideTask");

            migrationBuilder.RenameColumn(
                name: "ShiftId",
                schema: "public",
                table: "SheetFiles",
                newName: "Status");

            migrationBuilder.AddColumn<int>(
                name: "SideTaskId",
                schema: "public",
                table: "TestCase",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Concept",
                schema: "public",
                table: "SheetFiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "GateClearedAt",
                schema: "public",
                table: "PlayerShiftProgress",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGateCleared",
                schema: "public",
                table: "PlayerShiftProgress",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PlayerName",
                schema: "public",
                table: "Player",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_TestCase_SideTaskId",
                schema: "public",
                table: "TestCase",
                column: "SideTaskId");

            migrationBuilder.AddCheckConstraint(
                name: "CHK_TestCase_Parent",
                schema: "public",
                table: "TestCase",
                sql: "(\"TaskId\" IS NOT NULL AND \"SideTaskId\" IS NULL) OR (\"TaskId\" IS NULL AND \"SideTaskId\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_TestCase_PlayerSideTask_SideTaskId",
                schema: "public",
                table: "TestCase",
                column: "SideTaskId",
                principalSchema: "public",
                principalTable: "PlayerSideTask",
                principalColumn: "SideTaskId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestCase_SideTaskTemplate_TemplateId",
                schema: "public",
                table: "TestCase",
                column: "TemplateId",
                principalSchema: "public",
                principalTable: "SideTaskTemplate",
                principalColumn: "TemplateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TestCase_PlayerSideTask_SideTaskId",
                schema: "public",
                table: "TestCase");

            migrationBuilder.DropForeignKey(
                name: "FK_TestCase_SideTaskTemplate_TemplateId",
                schema: "public",
                table: "TestCase");

            migrationBuilder.DropIndex(
                name: "IX_TestCase_SideTaskId",
                schema: "public",
                table: "TestCase");

            migrationBuilder.DropCheckConstraint(
                name: "CHK_TestCase_Parent",
                schema: "public",
                table: "TestCase");

            migrationBuilder.DropColumn(
                name: "SideTaskId",
                schema: "public",
                table: "TestCase");

            migrationBuilder.DropColumn(
                name: "Concept",
                schema: "public",
                table: "SheetFiles");

            migrationBuilder.DropColumn(
                name: "GateClearedAt",
                schema: "public",
                table: "PlayerShiftProgress");

            migrationBuilder.DropColumn(
                name: "IsGateCleared",
                schema: "public",
                table: "PlayerShiftProgress");

            migrationBuilder.DropColumn(
                name: "PlayerName",
                schema: "public",
                table: "Player");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "public",
                table: "SheetFiles",
                newName: "ShiftId");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeadlineAt",
                schema: "public",
                table: "PlayerSideTask",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CHK_TestCase_Parent",
                schema: "public",
                table: "TestCase",
                sql: "(\"TaskId\" IS NOT NULL AND \"TemplateId\" IS NULL) OR (\"TaskId\" IS NULL AND \"TemplateId\" IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_SheetFiles_ShiftId",
                schema: "public",
                table: "SheetFiles",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_SheetFiles_Shift_ShiftId",
                schema: "public",
                table: "SheetFiles",
                column: "ShiftId",
                principalSchema: "public",
                principalTable: "Shift",
                principalColumn: "ShiftId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TestCase_SideTaskTemplate_TemplateId",
                schema: "public",
                table: "TestCase",
                column: "TemplateId",
                principalSchema: "public",
                principalTable: "SideTaskTemplate",
                principalColumn: "TemplateId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
