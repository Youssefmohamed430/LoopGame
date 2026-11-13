# Task 1 — Content Ownership, Narrative Generation Log, Session Context, and Query Isolation — Implementation Report

- **Author:** Mohamed Saba
- **Date:** 2026-11-13
- **Task:** Task 1 — Content Ownership, Narrative Generation Log, Session Context, and Query Isolation
- **Related ADR:** `Docs/ADR-001-Task1-Narrative-Ownership-And-Context.md`
- **EF Core Migration:** `20261113131912_AddNarrativeOwnershipAndContext`
- **Build Status:** `Build succeeded — 0 Warning(s), 0 Error(s)` (`dotnet build --no-restore`)
- **Test Status:** `63/63` Task 1 unit & service/security tests passing (`0` failed, `0` skipped)

---

## 1. Overview & Task 1 Invariants

Task 1 establishes the database ownership model, operational LLM audit logging, rolling per-shift session memory, query isolation, and endpoint authorization required to support **per-player AI-generated narrative branches and deferred consequences** alongside **global human-authored curriculum beats** without breaking existing gameplay, assessment, or economy behavior.

### Core Invariants Enforced

1. **Invariant 1 — Beat Ownership & Cross-Player Query Isolation:**
   - Human-authored (canonical) beats remain global and unowned (`PlayerId = null`, `IsAiGenerated = false`).
   - AI-generated beats are strictly owned by a single player (`IsAiGenerated = true`, `PlayerId != null`), enforced at both the PostgreSQL constraint layer (`CHK_Beat_AiOwnership`) and the application query layer (`PlayerId == null || PlayerId == playerId`).
   - Canonical admin CRUD endpoints (`NarrativeService` admin methods, `ChoiceService.AddChoice` / `UpdateChoice`, and `ShiftDetailDto` Mapster projections) never expose, mutate, move, or delete dynamic/player-owned beats (`IsAiGenerated || PlayerId != null`), and canonical `SequenceOrder` uniqueness checks ignore private AI beats.
   - All runtime player narrative endpoints in `NarrativeController` enforce caller identity via `IsAuthorizedForPlayer(playerId)` to close cross-player IDOR access.

2. **Invariant 2 — Curriculum, Assessment, and Economy Integrity:**
   - Pre-authored canonical curriculum beats and shifts remain unchanged and protected by existing `DeleteBehavior.Restrict` safeguards (`PlayerSave`, `PlayerChoice`, `ConsequenceQueue`).
   - Choices belonging to AI-generated beats **never** affect stealth assessment or gate progression (`ChoiceService.SubmitChoice` computes `isEvaluateable = choice.IsEvaluateable && !choice.Beat.IsAiGenerated`, gating `PlayerShiftProgress.GateAttempts` increments and `GameEventDto` / `choice_submission` telemetry emission).
   - `EconomyService.PayShiftSalaryAsync` filters `PlayerChoice` aggregations with `!pc.Beat.IsAiGenerated` so choices on AI beats never alter shift salary bonuses.
   - Critical pre-existing runtime bugs discovered during auditing (such as `ChoiceService.SubmitChoice` skipping `SaveAsync()` when `IsEvaluateable == false`, `NarrativeService.EndShift` implicitly wrapping `Result` failures inside `Result<object>` with `IsSuccess = true`, and `NarrativeService.StartShift` firing consequences on mid-shift resume and calling `UpdateAsync` on untracked `Include` graphs) are resolved.

---

## 2. Files Created

| # | File | Purpose |
|---|------|---------|
| 1 | `LoopGame.Domain/Entities/Audit/NarrativeGenerationLog.cs` | Dedicated audit entity for narrative LLM calls (`OperationType`, `ModelName`, `ResponseTimeMs`, validation/repair/fallback flags, redacted `FailureReason`, 90-day `RetentionDays`). |
| 2 | `LoopGame.Infrastructure/Persistence/Configurations/NarrativeGenerationLogConfiguration.cs` | EF Core configuration for `"NarrativeGenerationLog"` table, FKs (`Player` Cascade, `Shift` Restrict), and indexes (`IX_NarrativeGenerationLog_Player_CreatedAt`, `IX_NarrativeGenerationLog_CreatedAt`). |
| 3 | `LoopGame.Infrastructure/Migrations/20261113131912_AddNarrativeOwnershipAndContext.cs` | EF Core migration adding `StoryBeat` ownership columns/indexes/`CHK_Beat_AiOwnership`, `PlayerShiftProgress.SessionContext`, and the `NarrativeGenerationLog` table (`Up` and `Down`). |
| 4 | `LoopGame.Infrastructure/Migrations/20261113131912_AddNarrativeOwnershipAndContext.Designer.cs` | EF Core migration designer snapshot paired with `20261113131912_AddNarrativeOwnershipAndContext`. |
| 5 | `Docs/ADR-001-Task1-Narrative-Ownership-And-Context.md` | Architecture Decision Record documenting `NarrativeGenerationLog` vs `AiGenerationLog`, `Player` deletion cascades, PostgreSQL 16 verification, and `SessionContext` lifecycle/bounds. |
| 6 | `Docs/task1_narrative_ownership_implementation_report.md` | This Task 1 implementation report. |
| 7 | `LoopGame.Tests/Entities/StoryBeatTests.cs` | Tier 1 unit tests (4 tests) verifying `StoryBeat` defaults, backward compatibility, AI ownership fields, and `GenerationLogId` nullification behavior. |
| 8 | `LoopGame.Tests/Entities/PlayerShiftProgressTests.cs` | Tier 1 unit tests (15 test cases) verifying `SessionContext` default, constants, whitespace normalization, 2-sentence limit, 600-char cap, and high-surrogate boundary handling. |
| 9 | `LoopGame.Tests/Entities/NarrativeGenerationLogTests.cs` | Tier 1 unit tests (10 test cases) verifying `NarrativeGenerationLog` defaults, length/retention constants, and `SetFailureReason` sanitization/truncation. |
| 10 | `LoopGame.Tests/Services/NarrativeServiceTests.cs` | Tier 2 service tests (14 tests) verifying `StartShift`, `Save`, `EndShift`, and Admin CRUD isolation/bug fixes. |
| 11 | `LoopGame.Tests/Services/ChoiceServiceTests.cs` | Tier 2 service tests (11 tests) verifying `GetChoices`, `SubmitChoice` null-safety/ownership/deduplication/`IsEvaluateable == false` persistence/Invariant 2, and `AddChoice`/`UpdateChoice` guards. |
| 12 | `LoopGame.Tests/Services/NarrativeSecurityTests.cs` | Tier 2 security & economy tests (9 tests) verifying `NarrativeController` IDOR/role checks, `ResultHttpMapping` status codes, and `EconomyService.PayShiftSalaryAsync` AI-choice exclusion. |

---

## 3. Files Modified

| # | File | Change Summary |
|---|------|----------------|
| 1 | `LoopGame.Domain/Entities/Narrative/StoryBeat.cs` | Added nullable `PlayerId`, `IsAiGenerated` (default `false`), nullable `GenerationLogId`, and `Player?` / `NarrativeGenerationLog?` navigations. |
| 2 | `LoopGame.Domain/Entities/Player/Player.cs` | Added `StoryBeats` and `NarrativeGenerationLogs` navigation collections. |
| 3 | `LoopGame.Domain/Entities/Player/PlayerShiftProgress.cs` | Added `MaxSessionContextLength` (`600`), `MaxSessionContextSentences` (`2`), `SessionContext` (default `string.Empty`), `UpdateSessionContext(string?)`, and `NormalizeSessionContext(string?)`. |
| 4 | `LoopGame.Domain/Abstractions/NarrativeErrors.cs` | Added `BeatAccessForbidden` (`"Forbidden.Access"`) and `DynamicBeatForbidden` (`"Narrative.DynamicBeatForbidden"`). |
| 5 | `LoopGame.Domain/Abstractions/ChoiceErrors.cs` | Added `ForbiddenAccess` (`"Forbidden.Access"`), `DynamicBeatForbidden` (`"Choice.DynamicBeatForbidden"`), `DuplicateChoiceSubmission` (`"Choice.DuplicateSubmission"`), and `ShiftProgressNotFound` (`"Choice.ShiftProgressNotFound"`). |
| 6 | `LoopGame.Infrastructure/Persistence/AppDbContext.cs` | Added `DbSet<NarrativeGenerationLog> NarrativeGenerationLogs`. |
| 7 | `LoopGame.Infrastructure/Persistence/Configurations/StoryBeatConfiguration.cs` | Configured `IsAiGenerated`, `CHK_Beat_AiOwnership`, preserved `CHK_Beat_SequenceOrder`, FKs `PlayerId -> Player` (`Cascade`) and `GenerationLogId -> NarrativeGenerationLog` (`SetNull`), and `IX_Beat_Shift_Player`. |
| 8 | `LoopGame.Infrastructure/Persistence/Configurations/PlayerShiftProgressConfiguration.cs` | Configured `SessionContext` with `HasMaxLength(600)`, `HasDefaultValue(string.Empty)`, and `IsRequired()`. |
| 9 | `LoopGame.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` | Updated model snapshot with `NarrativeGenerationLog`, `StoryBeat` ownership/log FKs/indexes/`CHK_Beat_AiOwnership`, `PlayerShiftProgress.SessionContext`, and `Player` navigations. |
| 10 | `LoopGame.Application/Dtos/NarrativeDtos/BeatDto.cs` | Added `PlayerId` (`int?`) and `IsAiGenerated` (`bool`) properties. |
| 11 | `LoopGame.Application/Mapping/MapsterConfiguration.cs` | Scoped `Shift -> ShiftDetailDto` (`NarrativeBeats` and `ConsequenceBeats`) to canonical beats (`b.PlayerId == null && !b.IsAiGenerated`). |
| 12 | `LoopGame.Application/Services/LearningAndContentServices/NarrativeService.cs` | Added player scoping in `StartShift`/`Save`/`GetNarrativeBeats`/`GetPendingConsequences`, fixed resume/queue tracking/`EndShift` failure-wrapping, and guarded admin CRUD against dynamic beats. |
| 13 | `LoopGame.Application/Services/LearningAndContentServices/ChoiceService.cs` | Added null-safety, ownership, shift, and duplicate guards in `GetChoices`/`SubmitChoice`; moved `SaveAsync()` outside `if (isEvaluateable)`; enforced Invariant 2; blocked dynamic beats/consequences in `AddChoice`/`UpdateChoice`. |
| 14 | `LoopGame.Application/Services/EconomyAndProgressionServices/EconomyService.cs` | Added `!pc.Beat.IsAiGenerated` filter to `PayShiftSalaryAsync` `PlayerChoice` tier aggregation. |
| 15 | `LoopGame/Controllers/NarrativeController.cs` | Enforced `IsAuthorizedForPlayer(playerId)` IDOR checks across all 5 endpoints and routed `EndShift` through `Handle` (`ResultHttpMapping`). |
| 16 | `LoopGame/Extensions/ResultHttpMapping.cs` | Added `403`, `404`, and `409` status mappings for new ownership, dynamic beat, duplicate submission, shift progress, and save player errors. |

---

## 4. Detailed Technical Changes by Layer

### 4.1 Domain & Database Schema

#### `StoryBeat` Ownership & Constraints
- **`LoopGame.Domain/Entities/Narrative/StoryBeat.cs`**:
  - Added `int? PlayerId`, `bool IsAiGenerated { get; set; } = false`, and `int? GenerationLogId`.
  - Added optional navigations `Player.Player? Player` and `NarrativeGenerationLog? GenerationLog` (paired with `Player.StoryBeats`). Existing object initializers without these properties continue to default to global canonical beats (`PlayerId = null`, `IsAiGenerated = false`, `GenerationLogId = null`).
- **`StoryBeatConfiguration.cs`**:
  - `IsAiGenerated`: required boolean with DB default `false`.
  - `CHK_Beat_AiOwnership`: `"IsAiGenerated" = false OR "PlayerId" IS NOT NULL` — prevents AI-generated beats from entering shared (`PlayerId IS NULL`) scope at the PostgreSQL constraint level while keeping existing pre-authored rows valid.
  - `CHK_Beat_SequenceOrder`: preserved verbatim as `(beat_type = 'narrative' AND "SequenceOrder" IS NOT NULL) OR (beat_type = 'consequence' AND "SequenceOrder" IS NULL)`.
  - Indexes: added `IX_Beat_Shift_Player` on `(ShiftId, PlayerId)` for shift + player isolation queries, `IX_StoryBeat_PlayerId` on `PlayerId`, and unique `IX_StoryBeat_GenerationLogId` on `GenerationLogId` (PostgreSQL treats multiple `NULL` values as distinct, so unlimited pre-authored or fallback beats with `GenerationLogId = NULL` coexist without conflict). Preserved `IX_Beat_Shift_Seq` and unique `BeatKey`.

#### `NarrativeGenerationLog` (Audit Schema, PII Redaction, and Retention)
- **`LoopGame.Domain/Entities/Audit/NarrativeGenerationLog.cs`** & **`NarrativeGenerationLogConfiguration.cs`**:
  - Created as a separate entity/table (`public."NarrativeGenerationLog"`) instead of overloading `AiGenerationLog` (which belongs to side-task generation via `PlayerSideTask.AiLogId`).
  - Columns:
    - `Id` (`int`, identity PK via `UseIdentityByDefaultColumn`)
    - `PlayerId` (`int`, FK -> `Player.PlayerId`, `DeleteBehavior.Cascade`)
    - `ShiftId` (`int`, FK -> `Shift.ShiftId`, `DeleteBehavior.Restrict`)
    - `OperationType` (`varchar(50)`, `MaxOperationTypeLength = 50`, required — e.g., `ImmediateBranch`, `DeferredConsequence`, `Repair`)
    - `ModelName` (`varchar(100)`, `MaxModelNameLength = 100`, required)
    - `ResponseTimeMs` (`int`, required)
    - `ValidationPassed` (`bool`, required)
    - `RepairAttempted` (`bool`, required, default `false`)
    - `RepairSucceeded` (`bool?`)
    - `UsedFallback` (`bool`, required, default `false`)
    - `FailureReason` (`varchar(500)`, nullable, `MaxFailureReasonLength = 500`)
    - `CreatedAt` (`timestamp with time zone`, default `NOW()`)
  - Single-FK 1:0..1 relationship owned by `StoryBeat.GenerationLogId -> NarrativeGenerationLog.Id` (`DeleteBehavior.SetNull`), preventing circular FKs and supporting single-`SaveChangesAsync` persistence.
  - **PII Redaction & 90-Day Retention:** Stores operational telemetry only — no raw prompts, player inputs, or generated JSON payloads. `SetFailureReason(string? reason)` replaces `\r` and `\n` with spaces, trims, maps blank inputs to `null`, and caps length at `500` characters (callers must pass redacted diagnostic codes/summaries only). `RetentionDays = 90` is supported by `IX_NarrativeGenerationLog_CreatedAt` for batched pruning (`WHERE "CreatedAt" < NOW() - INTERVAL '90 days'`) and `IX_NarrativeGenerationLog_Player_CreatedAt` on `(PlayerId, CreatedAt)` for per-player lookups.

#### `PlayerShiftProgress.SessionContext`
- **`LoopGame.Domain/Entities/Player/PlayerShiftProgress.cs`** & **`PlayerShiftProgressConfiguration.cs`**:
  - Added `string SessionContext { get; set; } = string.Empty;` configured with `HasMaxLength(600)` (`character varying(600)`), `HasDefaultValue("")`, and `IsRequired()`.
  - Constants: `MaxSessionContextLength = 600` (~120 tokens) and `MaxSessionContextSentences = 2`.
  - `UpdateSessionContext(string? context)` invokes `NormalizeSessionContext(string? context)`: returns `string.Empty` for null/whitespace, collapses contiguous whitespace (`CollapseWhitespace`), caps at 2 sentence boundaries (`.`, `!`, `?` only when at end-of-string or followed by whitespace via `FindNthSentenceEnd`, preserving mid-token periods like `main.c` or `v1.2`), and enforces `<= 600` characters via `TruncateSafely` (avoiding splitting UTF-16 surrogate pairs via `char.IsHighSurrogate` and trimming to the last word boundary when `lastSpace > maxLength / 2`).

#### Cascade / SetNull Behavior on `Player` Deletion & Migration `Up`/`Down`
- **Deletion Graph (`Player` -> Narrative Tables):**
  - `StoryBeat.PlayerId -> Player.PlayerId`: `DeleteBehavior.Cascade` (deletes player-owned AI beats while leaving shared `PlayerId = NULL` beats untouched; child `Choice.BeatId` and `Consequence.BeatId` cascade from `StoryBeat`; `Choice.ConsequenceId` uses `SetNull`).
  - `NarrativeGenerationLog.PlayerId -> Player.PlayerId`: `DeleteBehavior.Cascade`.
  - `StoryBeat.GenerationLogId -> NarrativeGenerationLog.Id`: `DeleteBehavior.SetNull` (90-day log pruning preserves beats).
  - `PlayerSave.PlayerId`, `PlayerChoice.PlayerId`, `ConsequenceQueue.PlayerId`, `PlayerShiftProgress.PlayerId`: `DeleteBehavior.Cascade`.
  - `PlayerSave.BeatId`, `PlayerChoice.BeatId`, `PlayerChoice.ChoiceId`, `ConsequenceQueue.ConsequenceId`: remain `DeleteBehavior.Restrict` to guard canonical shared content against accidental direct deletion (raising SQLSTATE `23503`).
- **Migration `20261113131912_AddNarrativeOwnershipAndContext`:**
  - `Up()` adds `GenerationLogId`, `IsAiGenerated` (default `false`), and `PlayerId` to `public."StoryBeat"`; adds `SessionContext` (default `""`, max `600`) to `public."PlayerShiftProgress"`; creates `public."NarrativeGenerationLog"` with PK and FKs; creates all 6 new indexes (`IX_Beat_Shift_Player`, `IX_StoryBeat_GenerationLogId`, `IX_StoryBeat_PlayerId`, `IX_NarrativeGenerationLog_CreatedAt`, `IX_NarrativeGenerationLog_Player_CreatedAt`, `IX_NarrativeGenerationLog_ShiftId`); adds `CHK_Beat_AiOwnership`; and adds `FK_StoryBeat_NarrativeGenerationLog_GenerationLogId` (`SetNull`) and `FK_StoryBeat_Player_PlayerId` (`Cascade`).
  - `Down()` reverses every operation in dependency order (drops both new `StoryBeat` FKs, drops `NarrativeGenerationLog`, drops the 3 new `StoryBeat` indexes and `CHK_Beat_AiOwnership`, and drops the 3 `StoryBeat` columns and `PlayerShiftProgress.SessionContext`).

---

### 4.2 Application Layer Query Isolation & Bug Fixes

#### `NarrativeService` & `MapsterConfiguration`
- **Query Scoping Helpers:**
  - `GetNarrativeBeats(int shiftId, int playerId)` filters `sb.ShiftId == shiftId && sb.BeatType == BeatType.Narrative && (sb.PlayerId == null || sb.PlayerId == playerId)`, orders by `SequenceOrder`, adapts to `BeatDto`, and sorts nested choices by `ChoiceIndex` via `OrderBeatChoices`.
  - `GetPendingConsequences(int playerId, int shiftId)` filters `cq.PlayerId == playerId && cq.Status == ConsequenceStatus.pending && cq.Consequence.Beat.ShiftId == shiftId && (cq.Consequence.Beat.PlayerId == null || cq.Consequence.Beat.PlayerId == playerId)`, ordered by `QueueId`.
- **`StartShift(int playerId)`:**
  - Validates player and current shift via `ValidatePlayerAccess` and `GetShiftEntityForRuntime`.
  - **First shift start (`playerShiftProgress == null`):** Fires pending consequences for the shift, merges `[startConsequences] -> narrativeBeats -> [endConsequences]` via `MergeBeats`, inserts `PlayerShiftProgress`, and safely creates/updates `PlayerSave` only when `resultBeats.Count > 0` (preventing `InvalidOperationException` on empty shifts).
  - **Tracking Bug Fix in `CategorizeConsequencesByInjectPosition`:** Replaced `UpdateAsync(cq)` on an untracked `Include` graph (which attached and marked the entire `Consequence`/`StoryBeat`/`Choice` graph as `Modified`) with a tracked scalar lookup (`FindAsync(q => q.QueueId == cq.QueueId)`) updating only `Status = ConsequenceStatus.fired` and `FiredAt`.
  - **Mid-shift resume (`playerShiftProgress != null`):** Skips firing pending consequences; queries `PlayerSave` scoped by `Beat.ShiftId == shift.ShiftId && (Beat.PlayerId == null || Beat.PlayerId == playerId)` ordered by `SavedAt` / `SaveId` descending, and constructs the remaining flow via `BuildResumeBeats(savedBeat, narrativeBeats)` (correcting the previous bug where `OrderBy` placed null `SequenceOrder` items ahead of `Skip` on an unsorted list).
- **`Save(int playerId, int beatId)`:**
  - Guards `beatId <= 0` (`ChoiceErrors.InvalidId`), validates player, and validates `CurrentShiftId` via `GetShiftEntityForRuntime` (removing unsafe `Convert.ToInt32(player.CurrentShiftId)`).
  - Enforces ownership (`beat.PlayerId.HasValue && beat.PlayerId.Value != playerId` -> `NarrativeErrors.BeatAccessForbidden`) and shift match (`beat.ShiftId != shift.ShiftId` -> `ChoiceErrors.ShiftMismatch`).
  - Upserts `PlayerSave` with `SavedAt = DateTime.UtcNow` and returns remaining player-accessible beats via `GetNarrativeBeats` + `BuildResumeBeats`.
- **`EndShift(int playerId)`:**
  - Fixed the critical `Result<object>` failure-wrapping bug where returning `Result<NarrativeFlowDto>` or `Result.Failure(...)` from `Task<Result<object>>` implicitly converted the failure `Result` into `Result<object>.Success(failureResult)`. Now returns explicit `Result.Failure<object>(...)` errors across player, shift, progress, and gate-clearance checks.
  - Explicitly loads `currentShift` (fixing `NullReferenceException` when `player.CurrentShift` was not included), orders the next shift by `(ChapterNumber, ShiftNumber)`, updates `PlayerSave` to the next shift's first **canonical** narrative beat (`b.PlayerId == null`) or removes stale saves if none exist, and sets `player.CurrentShiftId = null` gracefully on the final shift (fixing NRE on the final shift and removing hardcoded `BeatId = 1`).
- **Admin CRUD Guards & DTO/Mapping:**
  - `BeatDto.cs` exposes `int? PlayerId` and `bool IsAiGenerated`.
  - `MapsterConfiguration.cs` filters `Shift -> ShiftDetailDto` (`NarrativeBeats` and `ConsequenceBeats`) with `b.PlayerId == null && !b.IsAiGenerated`, preventing `GetShift` and `UpdateShift` from leaking private AI beats.
  - `GetStoryBeat`, `UpdateStoryBeat`, `DeleteStoryBeat`, and `AssignBeatToShift` reject any beat where `beat.IsAiGenerated || beat.PlayerId != null` with `NarrativeErrors.DynamicBeatForbidden`.
  - `CreateStoryBeat` explicitly initializes `PlayerId = null`, `IsAiGenerated = false`, and `GenerationLogId = null`. Canonical `SequenceOrder` collision checks in `CreateStoryBeat`, `UpdateStoryBeat`, and `AssignBeatToShift` filter by `b.PlayerId == null && !b.IsAiGenerated` so private AI beats never collide with canonical numbering. `UpdateStoryBeat` also ensures `Narrative -> Consequence` type transitions clear `SequenceOrder` and upsert the `Consequence` row.

#### `ChoiceService`
- **`GetChoices(int beatId, int playerId)`:**
  - Guards `beatId <= 0 || playerId <= 0`, missing player (`ChoiceErrors.PlayerNotFound`), missing beat (`ChoiceErrors.BeatNotFound`), beat ownership (`beat.PlayerId.HasValue && beat.PlayerId.Value != playerId` -> `ChoiceErrors.ForbiddenAccess`), and shift match (`!player.CurrentShiftId.HasValue || player.CurrentShiftId.Value != beat.ShiftId` -> `ChoiceErrors.ShiftMismatch`), returning choices ordered by `ChoiceIndex`.
- **`SubmitChoice(int choiceid, int PlayerId)`:**
  - **Null-safety & validation:** Checks `choiceid <= 0 || PlayerId <= 0`, `player == null`, `!player.CurrentShiftId.HasValue || player.CurrentShiftId.Value <= 0`, `choice == null`, and `choice.Beat == null` prior to dereferencing.
  - **Ownership & duplicate protection:** Enforces `(choice.Beat.PlayerId == null || choice.Beat.PlayerId == PlayerId)` (`ChoiceErrors.ForbiddenAccess`), `choice.Beat.ShiftId == player.CurrentShiftId.Value` (`ChoiceErrors.ShiftMismatch`), and checks `PlayerChoice` for an existing `(PlayerId, choice.BeatId)` submission (`ChoiceErrors.DuplicateChoiceSubmission`) to prevent `GateAttempts` inflation and `UQ_Queue_Player_Consequence` violations.
  - **Consequence target check & deduplication:** When `choice.ConsequenceId.HasValue`, verifies `choice.Consequence?.Beat` is global or owned by `PlayerId` (`ChoiceErrors.ForbiddenAccess`) and checks whether `(PlayerId, ConsequenceId)` is already queued in `ConsequenceQueue` before inserting.
  - **`SaveAsync()` fix for `IsEvaluateable == false`:** Moved `await unitOfWork.SaveAsync()` outside the evaluation branch so `PlayerChoice` and `ConsequenceQueue` are persisted for non-evaluateable and AI choices alike.
  - **Invariant 2 defense-in-depth:** Computes `bool isEvaluateable = choice.IsEvaluateable && !choice.Beat.IsAiGenerated;`. Only when `isEvaluateable` is `true` does `SubmitChoice` require `PlayerShiftProgress` (`ChoiceErrors.ShiftProgressNotFound`), increment `playerProgress.GateAttempts`, and publish `GameEventDto` (`AssessmentWeights.EventTypes.ChoiceSubmission`).
- **`AddChoice` & `UpdateChoice` Guards:**
  - `ValidateBeatsOfChoices` and `UpdateChoice` reject adding or updating choices on beats with `IsAiGenerated || PlayerId != null` (`ChoiceErrors.DynamicBeatForbidden`).
  - `ValidateConsequences` and `UpdateChoice` reject linking consequences whose target `Consequence.Beat` has `IsAiGenerated || PlayerId != null` (`ChoiceErrors.DynamicBeatForbidden`). `UpdateChoice` queries parent beat ownership via scalar projection to avoid attaching extra tracked graphs.

#### `EconomyService`
- **`PayShiftSalaryAsync`** (`LoopGame.Application/Services/EconomyAndProgressionServices/EconomyService.cs`):
  - Updated the `PlayerChoice` aggregation query to include `!pc.Beat.IsAiGenerated` alongside `pc.PlayerId == playerId && pc.Beat.ShiftId == shiftId`, ensuring choices recorded on AI-generated beats never affect `SalaryPolicy.ComputeShiftBonus`.

---

### 4.3 API Layer Security & HTTP Error Mapping

#### `NarrativeController` IDOR Protection & Endpoint Alignment
- **`LoopGame/Controllers/NarrativeController.cs`**:
  - Added `IsAuthorizedForPlayer(int playerId)` across all 5 endpoints (`StartShift`, `Save`, `EndShift`, `GetChoices`, `SubmitChoice`), returning `Forbid()` (`403`) whenever:
    - `User?.Identity?.IsAuthenticated != true`, or
    - the caller is neither in an admin role (`"Admin"`, `"admin"`, `"super_admin"`) nor authenticated with a `ClaimTypes.NameIdentifier` matching `playerId`.
  - Updated `EndShift` to delegate through `Handle(_narrative.EndShift(playerId))` so failures map consistently via `ResultHttpMapping` instead of returning `BadRequest(result.Error)`.

#### Domain Errors & `ResultHttpMapping`
- Added domain errors in `NarrativeErrors.cs` (`BeatAccessForbidden`, `DynamicBeatForbidden`) and `ChoiceErrors.cs` (`ForbiddenAccess`, `DynamicBeatForbidden`, `DuplicateChoiceSubmission`, `ShiftProgressNotFound`).
- Extended `ResultHttpMapping.StatusCodeFor` (`LoopGame/Extensions/ResultHttpMapping.cs`):

| HTTP Status | Mapped Error Codes (New / Updated in Task 1) | Meaning |
|---|---|---|
| `403 Forbidden` | `"Forbidden.Access"`, `"Narrative.DynamicBeatForbidden"`, `"Choice.DynamicBeatForbidden"` | Cross-player beat/choice access or canonical admin mutation attempt on dynamic/AI beats. |
| `404 Not Found` | `"Save.PlayerNotFound"`, `"Choice.ShiftProgressNotFound"` *(plus existing `Narrative.ShiftNotFound`, `Narrative.BeatNotFound`, `Choice.PlayerNotFound`, `Choice.BeatNotFound`, `Choice.ChoiceNotFound`, `Save.SaveNotFound`)* | Missing player, beat, choice, shift, or shift progress record. |
| `409 Conflict` | `"Choice.DuplicateSubmission"` *(plus existing `Choice.ShiftMismatch` and `Narrative.*` conflict codes)* | Duplicate choice submission on a beat, shift mismatch, or canonical sequence/dependency conflict. |
| `400 Bad Request` | Fallback (`_ => 400`, e.g., `Choice.InvalidId`, `Narrative.ShiftNotCompleted`) | Validation and non-conflict domain rule failures. |

---

## 5. Verification Summary

### 5.1 Build Verification
```text
dotnet build --no-restore
Build succeeded.
    0 Warning(s)
    0 Error(s)
```
All 6 projects (`LoopGame.Domain`, `CodeRunner`, `LoopGame.Application`, `LoopGame.Infrastructure`, `LoopGame.API`, `LoopGame.Tests`) compiled cleanly with zero warnings and zero errors.

### 5.2 Task 1 Test Suite (`63/63` Passing)

| Test Class | Layer / Tier | Passed | Failed | Skipped | Coverage Scope |
|---|---|---|---|---|---|
| `StoryBeatTests` | Tier 1 (Entity) | 4 | 0 | 0 | Shared vs AI beat defaults, legacy initializers, `PlayerId` / `IsAiGenerated` / `GenerationLogId` state |
| `PlayerShiftProgressTests` | Tier 1 (Entity) | 15 | 0 | 0 | `SessionContext` default, constants, whitespace normalization, 2-sentence & 600-char caps, surrogate safety |
| `NarrativeGenerationLogTests` | Tier 1 (Entity) | 10 | 0 | 0 | Log defaults, length/90-day constants, `SetFailureReason` sanitization and 500-char truncation |
| `NarrativeServiceTests` | Tier 2 (Service) | 14 | 0 | 0 | `StartShift`, `Save`, `EndShift`, Admin CRUD isolation, canonical `SequenceOrder` scoping |
| `ChoiceServiceTests` | Tier 2 (Service) | 11 | 0 | 0 | `GetChoices`, `SubmitChoice` null-safety/ownership/duplicate/`IsEvaluateable == false`/Invariant 2, `AddChoice`/`UpdateChoice` guards |
| `NarrativeSecurityTests` | Tier 2 (Security / Economy) | 9 | 0 | 0 | `NarrativeController` IDOR & role authorization, `ResultHttpMapping`, `EconomyService.PayShiftSalaryAsync` AI exclusion |
| **Total (Task 1)** | **All Task 1 Tests** | **63** | **0** | **0** | **100% passing** |

### 5.3 Broader Non-Integration Regression Run
- Running `dotnet test --no-build --filter "Category!=Integration&FullyQualifiedName!~DockerSandboxIntegrationTests"` yielded **186 passed, 3 failed, 0 skipped (189 total)**.
- All 3 failures are pre-existing in untouched modules (`AssessmentServiceTests.Test4_ProgressionFlow_IdealAttempt_ClearsGate`, `AssessmentServiceTests.Test5_GateCleared_EmittedWithNullConceptTag`, and `EconomyServiceTests.InitializePlayerEconomy_WhenPlayerDoesNotExist_ReturnsPlayerNotFound`). `DockerSandboxIntegrationTests` (3 tests, missing `[Trait("Category", "Integration")]`) requires the `shift-c-runner:latest` Docker image.

### 5.4 PostgreSQL 16 Migration & Constraint Verification
As documented in `Docs/ADR-001-Task1-Narrative-Ownership-And-Context.md` and recorded in the Task 1 PostgreSQL 16 verification pass:
- Live `Migrate()` succeeded over pre-existing canonical `StoryBeat` and `PlayerShiftProgress` rows (`Database.HasPendingModelChanges() == false`).
- `CHK_Beat_AiOwnership` (`"IsAiGenerated" = false OR "PlayerId" IS NOT NULL`) and `CHK_Beat_SequenceOrder` were enforced against PostgreSQL 16.
- 90-day `NarrativeGenerationLog` deletion verified `DeleteBehavior.SetNull` clearing `StoryBeat.GenerationLogId` without deleting beats.
- Hard `DELETE FROM public."Player"` cascaded player-owned `StoryBeat` (and dependent `Choice` / `Consequence`), `NarrativeGenerationLog`, `PlayerSave`, `PlayerChoice`, `ConsequenceQueue`, and `PlayerShiftProgress` rows while preserving shared (`PlayerId IS NULL`) beats, while direct deletion of referenced shared `StoryBeat` rows continued to raise SQLSTATE `23503` via `Restrict`.
- `Down()` migration rollback and Npgsql 9.0.0 LINQ translation for `NarrativeService`, `ChoiceService`, and `EconomyService` (`!pc.Beat.IsAiGenerated`) executed cleanly against PostgreSQL 16.
