using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LoopGame.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "ApplicationRole",
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
                    table.PrimaryKey("PK_ApplicationRole", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationUser",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Shift",
                schema: "public",
                columns: table => new
                {
                    ShiftId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShiftNumber = table.Column<int>(type: "integer", nullable: false),
                    ChapterNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsCapstone = table.Column<bool>(type: "boolean", nullable: false),
                    unlock_condition = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shift", x => x.ShiftId);
                });

            migrationBuilder.CreateTable(
                name: "ShopItem",
                schema: "public",
                columns: table => new
                {
                    ItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ItemKey = table.Column<string>(type: "varchar(100)", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "varchar(30)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    RankRequired = table.Column<string>(type: "varchar(30)", nullable: true),
                    IsOneWay = table.Column<bool>(type: "boolean", nullable: false),
                    AssetKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopItem", x => x.ItemId);
                    table.CheckConstraint("CHK_ShopItem_Category", "\"Category\" IN ('sahm_tier','camera','desk_item','workspace')");
                    table.CheckConstraint("CHK_ShopItem_Price", "\"Price\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "SideTaskTemplate",
                schema: "public",
                columns: table => new
                {
                    TemplateId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TemplateKey = table.Column<string>(type: "varchar(100)", nullable: false),
                    ConceptTag = table.Column<string>(type: "varchar(50)", nullable: false),
                    RankRequired = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Intern"),
                    TitleTemplate = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    DescriptionTemplate = table.Column<string>(type: "text", nullable: false),
                    SlotsSchema = table.Column<string>(type: "jsonb", nullable: false),
                    EgpMin = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false, defaultValue: 500m),
                    EgpMax = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false, defaultValue: 3000m),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SideTaskTemplate", x => x.TemplateId);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationRoleClaim",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationRoleClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationRoleClaim_ApplicationRole_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "public",
                        principalTable: "ApplicationRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationUserClaim",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserClaim", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationUserClaim_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationUserLogin",
                schema: "public",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserLogin", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_ApplicationUserLogin_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationUserRole",
                schema: "public",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserRole", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_ApplicationUserRole_ApplicationRole_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "public",
                        principalTable: "ApplicationRole",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicationUserRole_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApplicationUserToken",
                schema: "public",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationUserToken", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_ApplicationUserToken_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                schema: "public",
                columns: table => new
                {
                    TokenId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    TokenHash = table.Column<string>(type: "character(64)", nullable: false),
                    IssuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(45)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.TokenId);
                    table.ForeignKey(
                        name: "FK_RefreshToken_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Player",
                schema: "public",
                columns: table => new
                {
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    StudentIdHash = table.Column<string>(type: "character(64)", nullable: false),
                    Rank = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Intern"),
                    CurrentShiftId = table.Column<int>(type: "integer", nullable: true),
                    TotalPlayTimeSec = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Player", x => x.PlayerId);
                    table.ForeignKey(
                        name: "FK_Player_ApplicationUser_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Player_Shift_CurrentShiftId",
                        column: x => x.CurrentShiftId,
                        principalSchema: "public",
                        principalTable: "Shift",
                        principalColumn: "ShiftId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PracticeTask",
                schema: "public",
                columns: table => new
                {
                    TaskId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShiftId = table.Column<int>(type: "integer", nullable: false),
                    TaskOrder = table.Column<byte>(type: "smallint", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    StarterCode = table.Column<string>(type: "text", nullable: true),
                    ConceptTag = table.Column<string>(type: "varchar(50)", nullable: false),
                    Difficulty = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Standard"),
                    MaxAttempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    EgpReward = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false, defaultValue: 0m),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeTask", x => x.TaskId);
                    table.CheckConstraint("CHK_PracticeTask_Difficulty", "\"Difficulty\" IN ('SpacedRetrieval', 'Standard', 'Challenge')");
                    table.ForeignKey(
                        name: "FK_PracticeTask_Shift_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "public",
                        principalTable: "Shift",
                        principalColumn: "ShiftId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StoryBeat",
                schema: "public",
                columns: table => new
                {
                    BeatId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ShiftId = table.Column<int>(type: "integer", nullable: false),
                    BeatKey = table.Column<string>(type: "varchar(100)", nullable: false),
                    beat_type = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "narrative"),
                    SequenceOrder = table.Column<int>(type: "integer", nullable: true),
                    app = table.Column<string>(type: "varchar(50)", nullable: false),
                    SenderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    content_json = table.Column<string>(type: "jsonb", nullable: false),
                    desktop_event = table.Column<string>(type: "jsonb", nullable: true),
                    DelaySeconds = table.Column<decimal>(type: "numeric(5,1)", nullable: false, defaultValue: 0m),
                    HasChoices = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryBeat", x => x.BeatId);
                    table.CheckConstraint("CHK_Beat_SequenceOrder", "(beat_type = 'narrative' AND \"SequenceOrder\" IS NOT NULL) OR (beat_type = 'consequence' AND \"SequenceOrder\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_StoryBeat_Shift_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "public",
                        principalTable: "Shift",
                        principalColumn: "ShiftId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiGenerationLog",
                schema: "public",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PromptText = table.Column<string>(type: "text", nullable: false),
                    RawResponse = table.Column<string>(type: "text", nullable: true),
                    ParsedSlots = table.Column<string>(type: "jsonb", nullable: true),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    ValidationError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LatencyMs = table.Column<int>(type: "integer", nullable: false),
                    EstimatedCostUsd = table.Column<decimal>(type: "numeric(8,6)", precision: 8, scale: 6, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() + INTERVAL '2 years'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiGenerationLog", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_AiGenerationLog_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiGenerationLog_SideTaskTemplate_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "public",
                        principalTable: "SideTaskTemplate",
                        principalColumn: "TemplateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentEvent",
                schema: "public",
                columns: table => new
                {
                    EventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "varchar(50)", nullable: false),
                    ConceptTag = table.Column<string>(type: "varchar(50)", nullable: true),
                    Tier = table.Column<string>(type: "varchar(20)", nullable: true),
                    Payload = table.Column<string>(type: "jsonb", nullable: true),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentEvent", x => x.EventId);
                    table.CheckConstraint("CHK_Assessment_EventType", "\"EventType\" IN ('choice_submission','practice_attempt','hint_request','side_task_submission','desktop_interaction','consequence_trigger','gate_cleared','shift_completed')");
                    table.ForeignKey(
                        name: "FK_AssessmentEvent_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLog",
                schema: "public",
                columns: table => new
                {
                    AuditId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    PlayerId = table.Column<int>(type: "integer", nullable: true),
                    Action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(50)", nullable: true),
                    EntityId = table.Column<int>(type: "integer", nullable: true),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(45)", nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLog", x => x.AuditId);
                    table.ForeignKey(
                        name: "FK_AuditLog_ApplicationUser_UserId",
                        column: x => x.UserId,
                        principalSchema: "public",
                        principalTable: "ApplicationUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AuditLog_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ConceptMasterySnapshot",
                schema: "public",
                columns: table => new
                {
                    SnapshotId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ShiftId = table.Column<int>(type: "integer", nullable: false),
                    ConceptTag = table.Column<string>(type: "varchar(50)", nullable: false),
                    MasteryScore = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    EvidenceCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    SnapshottedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConceptMasterySnapshot", x => x.SnapshotId);
                    table.CheckConstraint("CHK_Mastery_Score", "\"MasteryScore\" BETWEEN 0 AND 1");
                    table.ForeignKey(
                        name: "FK_ConceptMasterySnapshot_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ConceptMasterySnapshot_Shift_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "public",
                        principalTable: "Shift",
                        principalColumn: "ShiftId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerEconomy",
                schema: "public",
                columns: table => new
                {
                    EconomyId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false, defaultValue: 0m),
                    SalaryTier = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    TotalEarned = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    TotalSpent = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerEconomy", x => x.EconomyId);
                    table.CheckConstraint("CHK_Economy_Balance", "\"Balance\" >= 0");
                    table.CheckConstraint("CHK_Economy_SalaryTier", "\"SalaryTier\" BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_PlayerEconomy_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerInventory",
                schema: "public",
                columns: table => new
                {
                    InventoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    PurchasedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    EgpPaid = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerInventory", x => x.InventoryId);
                    table.ForeignKey(
                        name: "FK_PlayerInventory_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerInventory_ShopItem_ItemId",
                        column: x => x.ItemId,
                        principalSchema: "public",
                        principalTable: "ShopItem",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSave",
                schema: "public",
                columns: table => new
                {
                    SaveId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    SlotNumber = table.Column<byte>(type: "smallint", nullable: false),
                    SaveLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    desktop_state = table.Column<string>(type: "jsonb", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSave", x => x.SaveId);
                    table.CheckConstraint("CHK_PlayerSave_SlotNumber", "\"SlotNumber\" IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_PlayerSave_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerShiftProgress",
                schema: "public",
                columns: table => new
                {
                    ProgressId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ShiftId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "in_progress"),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GateAttempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerShiftProgress", x => x.ProgressId);
                    table.CheckConstraint("CHK_ShiftProgress_Status", "\"Status\" IN ('in_progress', 'gate_pending', 'completed')");
                    table.ForeignKey(
                        name: "FK_PlayerShiftProgress_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerShiftProgress_Shift_ShiftId",
                        column: x => x.ShiftId,
                        principalSchema: "public",
                        principalTable: "Shift",
                        principalColumn: "ShiftId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SahmSubscription",
                schema: "public",
                columns: table => new
                {
                    SubscriptionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Free"),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DailyHintLimit = table.Column<byte>(type: "smallint", nullable: false, defaultValue: (byte)3),
                    HintsUsedToday = table.Column<byte>(type: "smallint", nullable: false, defaultValue: (byte)0),
                    LastHintReset = table.Column<DateOnly>(type: "date", nullable: false, defaultValueSql: "CURRENT_DATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SahmSubscription", x => x.SubscriptionId);
                    table.CheckConstraint("CHK_Sahm_Tier", "\"Tier\" IN ('Free','Pro','Team','Enterprise')");
                    table.ForeignKey(
                        name: "FK_SahmSubscription_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                schema: "public",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    transaction_type = table.Column<string>(type: "varchar(30)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReferenceId = table.Column<int>(type: "integer", nullable: true),
                    BalanceAfter = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaction", x => x.TransactionId);
                    table.CheckConstraint("CHK_Transaction_Type", "transaction_type IN ('salary','bonus','side_task','purchase','penalty','bug_bounty')");
                    table.ForeignKey(
                        name: "FK_Transaction_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticeAttempt",
                schema: "public",
                columns: table => new
                {
                    AttemptId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    TaskId = table.Column<int>(type: "integer", nullable: false),
                    SubmittedCode = table.Column<string>(type: "text", nullable: false),
                    Tier = table.Column<string>(type: "varchar(20)", nullable: false),
                    TestResults = table.Column<string>(type: "jsonb", nullable: false),
                    TimeSpentSec = table.Column<int>(type: "integer", nullable: false),
                    HintUsed = table.Column<bool>(type: "boolean", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeAttempt", x => x.AttemptId);
                    table.CheckConstraint("CHK_PracticeAttempt_Tier", "\"Tier\" IN ('Ideal', 'Acceptable', 'Debt', 'Mistake')");
                    table.ForeignKey(
                        name: "FK_PracticeAttempt_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PracticeAttempt_PracticeTask_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "public",
                        principalTable: "PracticeTask",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TestCase",
                schema: "public",
                columns: table => new
                {
                    TestCaseId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaskId = table.Column<int>(type: "integer", nullable: true),
                    TemplateId = table.Column<int>(type: "integer", nullable: true),
                    TestInput = table.Column<string>(type: "text", nullable: false),
                    ExpectedOutput = table.Column<string>(type: "text", nullable: false),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCase", x => x.TestCaseId);
                    table.CheckConstraint("CHK_TestCase_Parent", "(\"TaskId\" IS NOT NULL AND \"TemplateId\" IS NULL) OR (\"TaskId\" IS NULL AND \"TemplateId\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_TestCase_PracticeTask_TaskId",
                        column: x => x.TaskId,
                        principalSchema: "public",
                        principalTable: "PracticeTask",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TestCase_SideTaskTemplate_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "public",
                        principalTable: "SideTaskTemplate",
                        principalColumn: "TemplateId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Consequence",
                schema: "public",
                columns: table => new
                {
                    ConsequenceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BeatId = table.Column<int>(type: "integer", nullable: false),
                    InjectPosition = table.Column<string>(type: "varchar(10)", nullable: false, defaultValue: "start")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consequence", x => x.ConsequenceId);
                    table.CheckConstraint("CHK_Consequence_InjectPosition", "\"InjectPosition\" IN ('start', 'end')");
                    table.ForeignKey(
                        name: "FK_Consequence_StoryBeat_BeatId",
                        column: x => x.BeatId,
                        principalSchema: "public",
                        principalTable: "StoryBeat",
                        principalColumn: "BeatId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerSideTask",
                schema: "public",
                columns: table => new
                {
                    SideTaskId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    TemplateId = table.Column<int>(type: "integer", nullable: false),
                    AiLogId = table.Column<int>(type: "integer", nullable: true),
                    ResolvedTitle = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ResolvedDescription = table.Column<string>(type: "text", nullable: false),
                    FilledSlots = table.Column<string>(type: "jsonb", nullable: false),
                    EgpReward = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "active"),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    DeadlineAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerSideTask", x => x.SideTaskId);
                    table.CheckConstraint("CHK_PlayerSideTask_Status", "\"Status\" IN ('active', 'submitted', 'abandoned', 'expired')");
                    table.ForeignKey(
                        name: "FK_PlayerSideTask_AiGenerationLog_AiLogId",
                        column: x => x.AiLogId,
                        principalSchema: "public",
                        principalTable: "AiGenerationLog",
                        principalColumn: "LogId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PlayerSideTask_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerSideTask_SideTaskTemplate_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "public",
                        principalTable: "SideTaskTemplate",
                        principalColumn: "TemplateId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Choice",
                schema: "public",
                columns: table => new
                {
                    ChoiceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BeatId = table.Column<int>(type: "integer", nullable: false),
                    ChoiceIndex = table.Column<byte>(type: "smallint", nullable: false),
                    ChoiceText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Tier = table.Column<string>(type: "varchar(20)", nullable: false),
                    ConsequenceId = table.Column<int>(type: "integer", nullable: true),
                    ImmediateFeedback = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Choice", x => x.ChoiceId);
                    table.CheckConstraint("CHK_Choice_Index", "\"ChoiceIndex\" BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_Choice_Consequence_ConsequenceId",
                        column: x => x.ConsequenceId,
                        principalSchema: "public",
                        principalTable: "Consequence",
                        principalColumn: "ConsequenceId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Choice_StoryBeat_BeatId",
                        column: x => x.BeatId,
                        principalSchema: "public",
                        principalTable: "StoryBeat",
                        principalColumn: "BeatId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsequenceQueue",
                schema: "public",
                columns: table => new
                {
                    QueueId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    ConsequenceId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "pending"),
                    QueuedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    FiredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsequenceQueue", x => x.QueueId);
                    table.CheckConstraint("CHK_Queue_Status", "\"Status\" IN ('pending', 'fired', 'dismissed')");
                    table.ForeignKey(
                        name: "FK_ConsequenceQueue_Consequence_ConsequenceId",
                        column: x => x.ConsequenceId,
                        principalSchema: "public",
                        principalTable: "Consequence",
                        principalColumn: "ConsequenceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsequenceQueue_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SideTaskHint",
                schema: "public",
                columns: table => new
                {
                    HintId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SideTaskId = table.Column<int>(type: "integer", nullable: false),
                    HintLevel = table.Column<short>(type: "smallint", nullable: false),
                    HintText = table.Column<string>(type: "text", nullable: false),
                    EgpCost = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false, defaultValue: 0m),
                    IsUnlocked = table.Column<bool>(type: "boolean", nullable: false),
                    UnlockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SideTaskHint", x => x.HintId);
                    table.CheckConstraint("CHK_SideTaskHint_Level", "\"HintLevel\" BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_SideTaskHint_PlayerSideTask_SideTaskId",
                        column: x => x.SideTaskId,
                        principalSchema: "public",
                        principalTable: "PlayerSideTask",
                        principalColumn: "SideTaskId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SideTaskSubmission",
                schema: "public",
                columns: table => new
                {
                    SubmissionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SideTaskId = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    SubmittedCode = table.Column<string>(type: "text", nullable: false),
                    Tier = table.Column<string>(type: "varchar(20)", nullable: false),
                    TestResults = table.Column<string>(type: "jsonb", nullable: false),
                    SahmHintsUsed = table.Column<byte>(type: "smallint", nullable: false, defaultValue: (byte)0),
                    TimeSpentSec = table.Column<int>(type: "integer", nullable: false),
                    EgpEarned = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: false, defaultValue: 0m),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SideTaskSubmission", x => x.SubmissionId);
                    table.CheckConstraint("CHK_SideTaskSubmission_Tier", "\"Tier\" IN ('Ideal', 'Acceptable', 'Debt', 'Mistake')");
                    table.ForeignKey(
                        name: "FK_SideTaskSubmission_PlayerSideTask_SideTaskId",
                        column: x => x.SideTaskId,
                        principalSchema: "public",
                        principalTable: "PlayerSideTask",
                        principalColumn: "SideTaskId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SideTaskSubmission_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlayerChoice",
                schema: "public",
                columns: table => new
                {
                    PlayerChoiceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerId = table.Column<int>(type: "integer", nullable: false),
                    BeatId = table.Column<int>(type: "integer", nullable: false),
                    ChoiceId = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<string>(type: "varchar(20)", nullable: false),
                    ChosenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    SessionContext = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerChoice", x => x.PlayerChoiceId);
                    table.ForeignKey(
                        name: "FK_PlayerChoice_Choice_ChoiceId",
                        column: x => x.ChoiceId,
                        principalSchema: "public",
                        principalTable: "Choice",
                        principalColumn: "ChoiceId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlayerChoice_Player_PlayerId",
                        column: x => x.PlayerId,
                        principalSchema: "public",
                        principalTable: "Player",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlayerChoice_StoryBeat_BeatId",
                        column: x => x.BeatId,
                        principalSchema: "public",
                        principalTable: "StoryBeat",
                        principalColumn: "BeatId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiGenerationLog_PlayerId",
                schema: "public",
                table: "AiGenerationLog",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_AiGenerationLog_TemplateId",
                schema: "public",
                table: "AiGenerationLog",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_AiLog_Expiry",
                schema: "public",
                table: "AiGenerationLog",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "public",
                table: "ApplicationRole",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRoleClaim_RoleId",
                schema: "public",
                table: "ApplicationRoleClaim",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "public",
                table: "ApplicationUser",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "public",
                table: "ApplicationUser",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserClaim_UserId",
                schema: "public",
                table: "ApplicationUserClaim",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserLogin_UserId",
                schema: "public",
                table: "ApplicationUserLogin",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationUserRole_RoleId",
                schema: "public",
                table: "ApplicationUserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Assessment_Player_Type",
                schema: "public",
                table: "AssessmentEvent",
                columns: new[] { "PlayerId", "EventType", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_PlayerId",
                schema: "public",
                table: "AuditLog",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId",
                schema: "public",
                table: "AuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Choice_ConsequenceId",
                schema: "public",
                table: "Choice",
                column: "ConsequenceId");

            migrationBuilder.CreateIndex(
                name: "UQ_Choice_Beat_Index",
                schema: "public",
                table: "Choice",
                columns: new[] { "BeatId", "ChoiceIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConceptMasterySnapshot_PlayerId",
                schema: "public",
                table: "ConceptMasterySnapshot",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptMasterySnapshot_ShiftId",
                schema: "public",
                table: "ConceptMasterySnapshot",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Consequence_Beat",
                schema: "public",
                table: "Consequence",
                column: "BeatId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConsequenceQueue_ConsequenceId",
                schema: "public",
                table: "ConsequenceQueue",
                column: "ConsequenceId");

            migrationBuilder.CreateIndex(
                name: "IX_Queue_Player_Status",
                schema: "public",
                table: "ConsequenceQueue",
                columns: new[] { "PlayerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_Queue_Player_Consequence",
                schema: "public",
                table: "ConsequenceQueue",
                columns: new[] { "PlayerId", "ConsequenceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Player_CurrentShiftId",
                schema: "public",
                table: "Player",
                column: "CurrentShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Player_StudentIdHash",
                schema: "public",
                table: "Player",
                column: "StudentIdHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Player_User",
                schema: "public",
                table: "Player",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Choice_Player_Beat",
                schema: "public",
                table: "PlayerChoice",
                columns: new[] { "PlayerId", "BeatId" });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerChoice_BeatId",
                schema: "public",
                table: "PlayerChoice",
                column: "BeatId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerChoice_ChoiceId",
                schema: "public",
                table: "PlayerChoice",
                column: "ChoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerEconomy_PlayerId",
                schema: "public",
                table: "PlayerEconomy",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInventory_ItemId",
                schema: "public",
                table: "PlayerInventory",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "UQ_PlayerInventory",
                schema: "public",
                table: "PlayerInventory",
                columns: new[] { "PlayerId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_PlayerSave",
                schema: "public",
                table: "PlayerSave",
                columns: new[] { "PlayerId", "SlotNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerShiftProgress_ShiftId",
                schema: "public",
                table: "PlayerShiftProgress",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "UQ_PlayerShift",
                schema: "public",
                table: "PlayerShiftProgress",
                columns: new[] { "PlayerId", "ShiftId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSideTask_AiLogId",
                schema: "public",
                table: "PlayerSideTask",
                column: "AiLogId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSideTask_PlayerId",
                schema: "public",
                table: "PlayerSideTask",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerSideTask_TemplateId",
                schema: "public",
                table: "PlayerSideTask",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Attempt_Player_Task",
                schema: "public",
                table: "PracticeAttempt",
                columns: new[] { "PlayerId", "TaskId", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeAttempt_TaskId",
                schema: "public",
                table: "PracticeAttempt",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeTask_ShiftId",
                schema: "public",
                table: "PracticeTask",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_TokenHash",
                schema: "public",
                table: "RefreshToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_User_Expiry",
                schema: "public",
                table: "RefreshToken",
                columns: new[] { "UserId", "ExpiresAt" },
                filter: "\"RevokedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_SahmSubscription_PlayerId",
                schema: "public",
                table: "SahmSubscription",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "UQ_Shift_Number",
                schema: "public",
                table: "Shift",
                columns: new[] { "ChapterNumber", "ShiftNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShopItem_ItemKey",
                schema: "public",
                table: "ShopItem",
                column: "ItemKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SideTaskHint_Task_Level",
                schema: "public",
                table: "SideTaskHint",
                columns: new[] { "SideTaskId", "HintLevel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SideTaskSubmission_PlayerId",
                schema: "public",
                table: "SideTaskSubmission",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_SideTaskSubmission_SideTaskId",
                schema: "public",
                table: "SideTaskSubmission",
                column: "SideTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SideTaskTemplate_TemplateKey",
                schema: "public",
                table: "SideTaskTemplate",
                column: "TemplateKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Beat_Shift_Seq",
                schema: "public",
                table: "StoryBeat",
                columns: new[] { "ShiftId", "beat_type", "SequenceOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_StoryBeat_BeatKey",
                schema: "public",
                table: "StoryBeat",
                column: "BeatKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TestCase_TaskId",
                schema: "public",
                table: "TestCase",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_TestCase_TemplateId",
                schema: "public",
                table: "TestCase",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_Player_Date",
                schema: "public",
                table: "Transaction",
                columns: new[] { "PlayerId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationRoleClaim",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ApplicationUserClaim",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ApplicationUserLogin",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ApplicationUserRole",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ApplicationUserToken",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AssessmentEvent",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AuditLog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ConceptMasterySnapshot",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ConsequenceQueue",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PlayerChoice",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PlayerEconomy",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PlayerInventory",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PlayerSave",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PlayerShiftProgress",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PracticeAttempt",
                schema: "public");

            migrationBuilder.DropTable(
                name: "RefreshToken",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SahmSubscription",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SideTaskHint",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SideTaskSubmission",
                schema: "public");

            migrationBuilder.DropTable(
                name: "TestCase",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Transaction",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ApplicationRole",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Choice",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ShopItem",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PlayerSideTask",
                schema: "public");

            migrationBuilder.DropTable(
                name: "PracticeTask",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Consequence",
                schema: "public");

            migrationBuilder.DropTable(
                name: "AiGenerationLog",
                schema: "public");

            migrationBuilder.DropTable(
                name: "StoryBeat",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Player",
                schema: "public");

            migrationBuilder.DropTable(
                name: "SideTaskTemplate",
                schema: "public");

            migrationBuilder.DropTable(
                name: "ApplicationUser",
                schema: "public");

            migrationBuilder.DropTable(
                name: "Shift",
                schema: "public");
        }
    }
}
