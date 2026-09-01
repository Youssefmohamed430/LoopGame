using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LoopGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OtpRecordedFileSheet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationRoleClaim_ApplicationRole_RoleId",
                schema: "public",
                table: "ApplicationRoleClaim");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUserRole_ApplicationRole_RoleId",
                schema: "public",
                table: "ApplicationUserRole");

            migrationBuilder.DropTable(
                name: "ApplicationRole",
                schema: "public");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                schema: "public",
                table: "ApplicationUser",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OtpRecords",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SheetFiles",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShiftId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    S3Key = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploadedByUserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SheetFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SheetFiles_Shift_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "public",
                        principalTable: "Shift",
                        principalColumn: "ShiftId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_Transaction_SalaryPerShift",
                schema: "public",
                table: "Transaction",
                columns: new[] { "PlayerId", "ReferenceId" },
                unique: true,
                filter: "\"transaction_type\" = 'salary' AND \"ReferenceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "public",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SheetFiles_ShiftId",
                schema: "public",
                table: "SheetFiles",
                column: "ShiftId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationRoleClaim_AspNetRoles_RoleId",
                schema: "public",
                table: "ApplicationRoleClaim",
                column: "RoleId",
                principalSchema: "public",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUserRole_AspNetRoles_RoleId",
                schema: "public",
                table: "ApplicationUserRole",
                column: "RoleId",
                principalSchema: "public",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationRoleClaim_AspNetRoles_RoleId",
                schema: "public",
                table: "ApplicationRoleClaim");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationUserRole_AspNetRoles_RoleId",
                schema: "public",
                table: "ApplicationUserRole");

            migrationBuilder.DropTable(
                name: "AspNetRoles",
                schema: "public");

            migrationBuilder.DropTable(
                name: "OtpRecords",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SheetFiles",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "UX_Transaction_SalaryPerShift",
                schema: "public",
                table: "Transaction");

            migrationBuilder.DropColumn(
                name: "Role",
                schema: "public",
                table: "ApplicationUser");

            migrationBuilder.CreateTable(
                name: "ApplicationRole",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationRole", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "public",
                table: "ApplicationRole",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationRoleClaim_ApplicationRole_RoleId",
                schema: "public",
                table: "ApplicationRoleClaim",
                column: "RoleId",
                principalSchema: "public",
                principalTable: "ApplicationRole",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationUserRole_ApplicationRole_RoleId",
                schema: "public",
                table: "ApplicationUserRole",
                column: "RoleId",
                principalSchema: "public",
                principalTable: "ApplicationRole",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
