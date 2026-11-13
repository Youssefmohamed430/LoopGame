# ADR-001: Task 1 — AI Narrative Ownership, Generation Audit Logging, and Session Context

- **Status:** Accepted
- **Author:** Mohamed Saba
- **Date:** 2026-11-13
- **Scope:** `LoopGame.Domain`, `LoopGame.Infrastructure`, EF Core Migration `20261113131912_AddNarrativeOwnershipAndContext`

---

## 1. Decision Summary

Task 1 establishes database and domain foundations for player-owned AI-generated narrative branches and deferred consequences while keeping pre-authored shared beats, side-task generation logs, and existing API/application contracts intact:

1. Introduce a dedicated `NarrativeGenerationLog` entity and table (`"NarrativeGenerationLog"`) for narrative LLM telemetry, failure diagnostics, and 90-day retention pruning, rather than overloading `AiGenerationLog`.
2. Add ownership and lineage columns (`PlayerId`, `IsAiGenerated`, `GenerationLogId`) to `StoryBeat`, enforced at the database level via `CHK_Beat_AiOwnership`, indexed on `(ShiftId, PlayerId)`, and wired into PostgreSQL cascade/set-null delete rules without disrupting shared beats or `CHK_Beat_SequenceOrder`.
3. Add rolling per-shift narrative memory (`SessionContext`, `string`, default `""`, max 600 characters / max 2 sentences) to `PlayerShiftProgress` with deterministic domain normalization via `UpdateSessionContext(string? context)`.

---

## 2. `NarrativeGenerationLog` vs `AiGenerationLog`

### Context
`AiGenerationLog` (`LoopGame.Domain/Entities/Audit/AiGenerationLog.cs`) already exists for the side-task parameterization pipeline (`PlayerSideTask.AiLogId`), carrying side-task-specific schema (`TemplateId`, `GeneratedData` JSON, `ValidationErrors` JSON, `ModelUsed`, `PromptTokens`, `CompletionTokens`, `LatencyMs`, `ExpiresAt`, and `DeleteBehavior.Restrict` on `PlayerId`).

### Decision
Create a separate `NarrativeGenerationLog` entity in `LoopGame.Domain/Entities/Audit/NarrativeGenerationLog.cs` configured via `NarrativeGenerationLogConfiguration` instead of mutating `AiGenerationLog` with nullable narrative-only columns.

### Rationale
- **Distinct domain lifecycle & schema:** Side-task logs are coupled to `PracticeTaskTemplate` (`TemplateId`) and slot-fill JSON (`GeneratedData`), whereas narrative generation requires branching outcome tracking (`OperationType`, `ValidationPassed`, `RepairAttempted`, `RepairSucceeded`, `UsedFallback`), latency (`ResponseTimeMs`), and correlation with `Shift` and `StoryBeat`.
- **Clean single-FK 1:0..1 relationship with `StoryBeat`:** Keeping the FK exclusively on `StoryBeat.GenerationLogId -> NarrativeGenerationLog.Id` (`DeleteBehavior.SetNull`) prevents circular foreign keys between `StoryBeat` and `NarrativeGenerationLog`, enables single-transaction/single-`SaveChangesAsync` persistence of a beat and its generation log, and lets PostgreSQL enforce uniqueness via `IX_StoryBeat_GenerationLogId` (PostgreSQL unique indexes treat multiple `NULL` values as distinct, so unlimited pre-authored or fallback beats with `GenerationLogId = NULL` coexist cleanly).

### Fields & Configuration (`NarrativeGenerationLog`)
- `Id` (`int`, identity PK)
- `PlayerId` (`int`, FK -> `Player.PlayerId`, `DeleteBehavior.Cascade`)
- `ShiftId` (`int`, FK -> `Shift.ShiftId`, `DeleteBehavior.Restrict`)
- `OperationType` (`string`, `HasMaxLength(50)`, e.g., `ImmediateBranch`, `DeferredConsequence`, `Repair`)
- `ModelName` (`string`, `HasMaxLength(100)`)
- `ResponseTimeMs` (`int`)
- `ValidationPassed` (`bool`)
- `RepairAttempted` (`bool`, default `false`)
- `RepairSucceeded` (`bool?`)
- `UsedFallback` (`bool`, default `false`)
- `FailureReason` (`string?`, `HasMaxLength(500)`)
- `CreatedAt` (`DateTime`, `timestamp with time zone`, default `NOW()`)
- Navigations: `Player`, `Shift`, `StoryBeat?`

### PII Avoidance & 90-Day Retention Policy
- **Redacted diagnostics only:** `NarrativeGenerationLog` intentionally omits raw LLM prompts, player inputs, email/chat message bodies, and generated JSON payloads. `FailureReason` (capped at `MaxFailureReasonLength = 500` via `SetFailureReason(string?)`) is restricted to sanitized structural error codes or schema validation summaries and must never persist PII or sensitive prompt text.
- **90-day retention & pruning:** `RetentionDays = 90` defines the audit window. `IX_NarrativeGenerationLog_CreatedAt` supports efficient batched age-based pruning (`WHERE "CreatedAt" < NOW() - INTERVAL '90 days'`), and `IX_NarrativeGenerationLog_Player_CreatedAt` supports per-player telemetry lookups and rate/anomaly queries. Because `FK_StoryBeat_NarrativeGenerationLog_GenerationLogId` uses `DeleteBehavior.SetNull`, pruning expired `NarrativeGenerationLog` rows nullifies `StoryBeat.GenerationLogId` without deleting any `StoryBeat` needed for active saves or replay history.

---

## 3. `StoryBeat` Ownership & `Player` Deletion Cascade Behavior

### Schema & Constraints
`LoopGame.Domain/Entities/Narrative/StoryBeat.cs` and `StoryBeatConfiguration.cs` add:
- `PlayerId` (`int?`, FK -> `Player.PlayerId`, `DeleteBehavior.Cascade`, nullable)
- `IsAiGenerated` (`bool`, default `false`, required)
- `GenerationLogId` (`int?`, FK -> `NarrativeGenerationLog.Id`, `DeleteBehavior.SetNull`, nullable)
- Index `IX_Beat_Shift_Player` on `(ShiftId, PlayerId)` for querying shared (`PlayerId IS NULL`) and player-specific beats within a shift.
- Check constraint `CHK_Beat_AiOwnership`: `"IsAiGenerated" = false OR "PlayerId" IS NOT NULL`.
  - Pre-authored shared beats (`IsAiGenerated = false`, `PlayerId = NULL`) continue to work unchanged, including existing rows during migration.
  - AI-generated beats (`IsAiGenerated = true`) cannot leak into shared scope (`PlayerId = NULL`) because PostgreSQL rejects them at insert/update time.
- Check constraint `CHK_Beat_SequenceOrder` is preserved verbatim: `(beat_type = 'narrative' AND "SequenceOrder" IS NOT NULL) OR (beat_type = 'consequence' AND "SequenceOrder" IS NULL)`.

### Verified Deletion & Cascade Graph (`Player` -> Narrative Entities)
Across the narrative tables (`Player`, `StoryBeat`, `Choice`, `Consequence`, `PlayerSave`, `PlayerChoice`, `ConsequenceQueue`, `NarrativeGenerationLog`, `PlayerShiftProgress`):

| Relationship | DeleteBehavior | Impact on Hard Delete of `Player` | Impact on Direct Delete of Pre-Seeded `StoryBeat` |
|---|---|---|---|
| `StoryBeat.PlayerId` -> `Player.PlayerId` | `Cascade` | Deletes all `Player`-owned AI `StoryBeat` rows; shared (`PlayerId = NULL`) beats are untouched. | N/A |
| `NarrativeGenerationLog.PlayerId` -> `Player.PlayerId` | `Cascade` | Deletes all `NarrativeGenerationLog` rows for the player. | N/A |
| `StoryBeat.GenerationLogId` -> `NarrativeGenerationLog.Id` | `SetNull` | Deleting a `NarrativeGenerationLog` (e.g., 90-day pruning) clears `GenerationLogId` without failing or deleting `StoryBeat`. | N/A |
| `Choice.BeatId` -> `StoryBeat.BeatId` | `Cascade` | Choices attached to deleted AI beats cascade away. | Preserved existing behavior. |
| `Consequence.BeatId` -> `StoryBeat.BeatId` | `Cascade` | Consequences attached to deleted AI consequence beats cascade away. | Preserved existing behavior. |
| `Choice.ConsequenceId` -> `Consequence.ConsequenceId` | `SetNull` | Unlinks optional consequence pointer cleanly without cycles. | Preserved existing behavior. |
| `PlayerSave.PlayerId` -> `Player.PlayerId` | `Cascade` | Deletes player's save records. | Blocked if beat still referenced. |
| `PlayerSave.BeatId` -> `StoryBeat.BeatId` | `Restrict` | Kept as `Restrict` to guard shared beats against accidental deletion. | Direct `DELETE FROM "StoryBeat"` fails with SQLSTATE `23503` if saved. |
| `PlayerChoice.PlayerId` -> `Player.PlayerId` | `Cascade` | Deletes player's choice history. | Blocked if beat/choice referenced. |
| `PlayerChoice.BeatId` / `ChoiceId` -> `StoryBeat` / `Choice` | `Restrict` | Kept as `Restrict` to guard shared beats/choices. | Direct `DELETE FROM "StoryBeat"` fails with SQLSTATE `23503` if referenced. |
| `ConsequenceQueue.PlayerId` -> `Player.PlayerId` | `Cascade` | Deletes player's queued consequences. | Blocked if consequence referenced. |
| `ConsequenceQueue.ConsequenceId` -> `Consequence.ConsequenceId` | `Restrict` | Kept as `Restrict` to guard shared consequences. | Direct `DELETE` fails with SQLSTATE `23503` if queued. |

#### Verification & Operational Notes
- **PostgreSQL 16 verification:** Tested both the upgraded migration path and trigger order where `StoryBeat -> Player` precedes `PlayerSave -> Player`. In PostgreSQL 16, `DELETE FROM public."Player" WHERE "PlayerId" = ...` cleanly cascades the player's `StoryBeat` (plus dependent `Choice` and `Consequence`), `NarrativeGenerationLog`, `PlayerSave`, `PlayerChoice`, `ConsequenceQueue`, and `PlayerShiftProgress` rows while leaving shared beats (`PlayerId IS NULL`) and other players' data intact. Direct deletion of a referenced `StoryBeat` continues to raise `23503` via `Restrict`.
- **Pre-existing non-narrative `Restrict` FKs & EF ChangeTracker caveat:** The repository's standard deletion mechanism for players is soft-delete (`Player.DeletedAt` / `ApplicationUser.DeletedAt`). Pre-existing non-narrative tables (`AiGenerationLog.PlayerId`, `SideTaskSubmission.PlayerId`, and `ApplicationUser`) retain `DeleteBehavior.Restrict` outside Task 1 scope, and if hard-deleting a `Player` inside EF Core with child narrative entities already tracked in the same `DbContext` `ChangeTracker`, client-side relationship fixup requires either set-based/SQL deletion (`ExecuteDeleteAsync`) or clearing tracked dependent `PlayerSave`/`PlayerChoice`/`ConsequenceQueue` entities first so EF Core does not emit `DELETE FROM "StoryBeat"` ahead of clearing tracked `Restrict` dependents.

---

## 4. `PlayerShiftProgress.SessionContext` Lifecycle & Size Bounds

### Decision
Add `string SessionContext { get; set; } = string.Empty;` to `PlayerShiftProgress` (`MaxLength(600)`, DB default `""`, non-null) alongside `UpdateSessionContext(string? context)` (`NormalizeSessionContext`), rather than repurposing `PlayerChoice.SessionContext` (which is a nullable legacy per-choice field).

### Rationale & Constraints
- **Cardinality:** Narrative continuity state belongs once per player per shift (`UQ_PlayerShift` on `(PlayerId, ShiftId)`), not fragmented per `PlayerChoice` row.
- **Bound (~120 tokens / 2 sentences / 600 characters):** Enforcing both a 2-sentence ceiling (`MaxSessionContextSentences = 2`) and a 600-character ceiling (`MaxSessionContextLength = 600`) guarantees bounded prompt overhead (~120 tokens at ~4–5 chars/token), prevents unbounded column growth, and preserves database safety with an empty-string default (`""`) for all existing `PlayerShiftProgress` rows.
- **Lifecycle:**
  1. **Initialization:** Starts at `string.Empty` when `PlayerShiftProgress` is created for a shift.
  2. **Prompt Read:** Narrative generation services read `PlayerShiftProgress.SessionContext` as a compact summary of recent shift events/decisions when generating immediate branches or deferred consequences.
  3. **Post-Generation Update:** After a choice or consequence resolves, the narrative pipeline calls `progress.UpdateSessionContext(summary)`. Whitespace is normalized, output is capped at at most 2 sentences (`.`, `!`, `?` boundaries), and any overflow is truncated to `<= 600` characters (`...` suffix if needed).
  4. **Reset/Completion:** Remains scoped cleanly to that shift's `PlayerShiftProgress` record and cascades when the `Player` is removed.
