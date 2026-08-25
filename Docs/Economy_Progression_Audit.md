# Economy & Progression Services — Implementation & Audit Report

**Project:** SHIFT / LoopGame — Helwan University CS Graduate Project
**Module:** Economy & Progression Services (Economy, Shop, Sahm AI)
**Author:** **Mohamed Saba**
**Date:** 2026-08-25
**Branch:** `master`
**Status:** ✅ Complete (Phases 0–4) — build clean, 58/58 tests passing, live concurrency race test passed

---

## Table of Contents

1. [Scope & Architecture](#1-scope--architecture)
2. [Hard Rules Compliance](#2-hard-rules-compliance)
3. [Phase 0 — Foundations](#3-phase-0--foundations)
4. [Phase 1 — EconomyService](#4-phase-1--economyservice)
5. [Phase 2 — ShopService](#5-phase-2--shopservice)
6. [Phase 3 — SahmService](#6-phase-3--sahmservice)
7. [Phase 4 — API Wiring](#7-phase-4--api-wiring)
8. [Concurrency Design Explained](#8-concurrency-design-explained)
9. [Domain Business Rules Explained](#9-domain-business-rules-explained)
10. [Quality Assurance Results](#10-quality-assurance-results)
11. [Known Risks & Cross-Team Dependencies](#11-known-risks--cross-team-dependencies)
12. [Complete File Inventory](#12-complete-file-inventory)

---

## 1. Scope & Architecture

This module implements the entire money and progression backbone of the SHIFT game:

| Service | Responsibility | Use Cases |
|---|---|---|
| `EconomyService` | Balance reads, transaction ledger paging, the single sanctioned money pipeline, shift salaries, economy reset | UC-ECO-01..05, part of UC-GAME-11 |
| `ShopService` | Catalogue browsing, item purchases, Sahm tier upgrades via shop, owned inventory | UC-ECO-06/08/09 |
| `SahmService` | Daily hint limits, lazy counter reset, subscription status, telemetry emission | UC-SAHM-02/03/04/06/07 |

The solution follows Clean Architecture:

```
LoopGame.Domain          → entities, enums, constants, pure policies, Result/Error, repo interfaces
LoopGame.Application     → services + DTOs (no DB access; EF Core only for async LINQ operators)
LoopGame.Infrastructure  → EF Core (Npgsql/PostgreSQL), repositories, UnitOfWork, DI
LoopGame                 → ASP.NET Core API host: controllers, OpenAPI, HTTP mapping
LoopGame.Tests           → xUnit suite (58 tests) over Domain + Application services
```

Database is **PostgreSQL (Supabase)**. Connection details come from a gitignored `.env`
file loaded by DotNetEnv (`ConnectionStrings__DefaultConnection`).

---

## 2. Hard Rules Compliance

| # | Rule | Status |
|---|---|---|
| 1 | `EconomyService` is the ONLY writer of `PlayerEconomy.Balance`; all other money effects go through `ApplyEgpDeltaAsync` | ✅ Enforced with `private set` + domain methods |
| 2 | Every balance change inserts exactly ONE immutable `Transaction` ledger row (signed Amount + `BalanceAfter`) in the SAME DB transaction | ✅ Verified on every path |
| 3 | Business failures return typed `Result.Failure(Error)`; exceptions only for infrastructure faults / caller bugs | ✅ |
| 4 | Reads are AsNoTracking server-side projections; writes do exactly ONE SaveChanges per use case | ✅ `CommitAsync` is a pure commit (no hidden save) |
| 5 | Money transactions lock the economy row FIRST (`SELECT … FOR UPDATE`) and stay short — no HTTP/AI/timer calls inside | ✅ Proven by live race test |
| 6 | AssessmentEvent telemetry never written inside money transactions; fire-and-forget emitter only | ✅ `IAssessmentEventEmitter` stub |
| 7 | Existing namespaces/folder conventions preserved (including the historic `Domain.IRepositries` spelling) | ✅ |
| 8 | All public async methods accept `CancellationToken ct = default` | ✅ Catch blocks deliberately use `CancellationToken.None` for rollback so cleanup can never be cancelled mid-way |
| 9 | Other groups' services/controllers untouched | ✅ Only shared plumbing touched: `Program.cs` (+1 line), `GlobalUsings.cs` (+usings), nullability fixes in `BaseRepository` |
| 10 | No `EnableRetryOnFailure`, no sync-over-async anywhere | ✅ Grep-audited |

An independent code review round (findings F-1..F-9) was completed before Phase 2;
all MAJOR/MINOR findings were fixed and re-verified.

---

## 3. Phase 0 — Foundations

### Async unit of work
`IUnitOfWork` was converted from sync to fully async:
`BeginTransactionAsync` / `CommitAsync` / `RollbackAsync(CancellationToken)` plus an
optional token on `SaveAsync`. The old Infrastructure `UnitOfWork` contained real
sync-over-async bugs (`SaveChangesAsync().Result`, sync `SaveChanges()` inside commit);
these were eliminated. `CommitAsync` is now a **pure transaction commit** — callers must
persist explicitly via `SaveAsync` first, guaranteeing exactly one SaveChanges per use case.

### Rich domain model — `PlayerEconomy`
`Balance`, `TotalEarned`, `TotalSpent`, `UpdatedAt` now have **private setters**, making it
impossible for application code to corrupt accounting state directly. All mutation flows
through four domain methods, each of which also produces the immutable ledger row:

| Method | Behavior | Ledger row |
|---|---|---|
| `Credit(amount > 0, type, desc, refId)` | Adds to balance + TotalEarned; throws `ArgumentOutOfRangeException` on non-positive input (caller bug, not business failure) | signed `+amount` |
| `TryDebit(amount, type, desc, refId)` | Fails with `InsufficientBalance` when amount ≤ 0 or balance insufficient; never goes negative | signed `-amount` |
| `ApplyPenalty(amount, desc, refId)` | Debits `MIN(Balance, amount)` — clamped at zero, never negative | signed `-applied` |
| `Reset()` | Zeroes balance and both totals (UC-GAME-11 new-game flow) | none (ledger wiped separately) |

### Error catalogs
Typed, static catalogs following the existing `Error` record pattern:
`EconomyErrors` (InvalidAmount, InvalidPagination, PlayerNotFound, PlayerEconomyNotFound,
InsufficientBalance, SalaryAlreadyPaid), `ShopErrors` (ItemNotFoundOrUnavailable,
RankNotMet, AlreadyOwned), `SahmErrors` (DailyHintLimitReached, InvalidTierUpgrade).

### Pure domain policies (fully deterministic, no I/O)
- **`SalaryPolicy.BaseSalary(rank)`** — maps each rank to its tier from `SalaryTiers`
  (Intern 2000 → Lead 12000 EGP).
- **`SalaryPolicy.ComputeShiftBonus(baseSalary, tierCounts)`** — shift performance bonus:
  `bonus = base × (idealShare × 0.20 + acceptableShare × 0.10)`, rounded to 2 decimals
  (AwayFromZero). Debt/Mistake choices contribute nothing; empty shifts earn no bonus.
- **`SahmTierPolicy`** — parses shop item keys (`sahm_pro` → `SahmTier.Pro`) and maps tiers
  to daily hint limits. Note: Enterprise's "unlimited" is stored as byte `255` because
  `HintLimits.Enterprise = int.MaxValue` cannot fit the smallint/byte column
  (255 is the documented sentinel).

### SahmTier enum + persistence
`SahmSubscription.Tier` changed from raw `string` to the `SahmTier` enum, persisted through
a value converter using static helper methods (same pattern as TransactionType). Store
representation is identical (`varchar(20)` holding `Free/Pro/Team/Enterprise`) — zero schema change.

### Row-lock repository
`IPlayerEconomyRepository.GetForUpdateAsync(playerId)` loads the economy row as a TRACKED
entity with an exclusive lock held until commit:

```sql
SELECT * FROM "PlayerEconomy" WHERE "PlayerId" = @playerId FOR UPDATE
```

(`FOR UPDATE` is PostgreSQL's equivalent of SQL Server's `WITH (UPDLOCK, ROWLOCK)`.)

### Test project
`LoopGame.Tests` (xUnit) created with domain tests for `PlayerEconomy` behavior
(clamp-at-zero, insufficient balance, ledger `BalanceAfter`, totals) and `SalaryPolicy` math.

---

## 4. Phase 1 — EconomyService

| Method | What it does |
|---|---|
| `GetBalanceAsync` | No-tracking single-row projection; `SalaryTier` int (stored 1-based) mapped to rank name in memory — note `PlayerRank` enum is 0-based, hence `(tier - 1)` |
| `GetTransactionHistoryAsync` | Server-side paging over `IX_Transaction_Player_Date` (`OrderByDescending(CreatedAt)` + Skip/Take); `HasNext` detected via `Take(pageSize + 1)`; guards use `InvalidPagination` |
| `ApplyEgpDeltaAsync` | THE money pipeline other groups must call. Flow: begin tx → lock economy → route delta (`>0` Credit · `<0 && Penalty` ApplyPenalty · `<0` TryDebit · `0` InvalidAmount) → insert ledger → save → commit; catch rolls back (non-cancellable) and rethrows |
| `PayShiftSalaryAsync` | Lock-first tx containing: idempotency check (existing Salary row w/ ReferenceId = shiftId ⇒ `SalaryAlreadyPaid`) → player rank read → server-side `GroupBy(Tier)` count of the shift's `PlayerChoice`s → `total = BaseSalary + ComputeShiftBonus` → Credit → ledger → save → commit. Concurrency backstop: filtered unique index `UX_Transaction_SalaryPerShift` |
| `ResetEconomyAsync` | One transaction: lock → `Reset()` → delete all PlayerInventory + Transaction rows (PO decision: full clean slate). Not exposed via HTTP — the game-progress group's UC-GAME-11 orchestrator calls the service |

---

## 5. Phase 2 — ShopService

**Catalog** (`GetCatalogAsync`): all available items with a per-player `IsOwned` badge
(translated to an EXISTS subquery), ordered by SortOrder. Rank-locked items remain visible
so the UI can badge them ("requires Senior"); unavailable items are hidden.
`RankRequired` is projected raw and converted to its name string in memory
(enum `.ToString()` does not translate to SQL).

**Purchase** (`PurchaseItemAsync`) — one lock-first transaction, guard order per
Architecture doc §5.9:

1. Item exists AND `IsAvailable` → else 404 `ItemNotFoundOrUnavailable`
2. Player rank ≥ `RankRequired` (explicit int casts; enums don't support `<` in C#) → else `RankNotMet`
3. `Balance >= Price` (read-only pre-check preserving error order) → else 402 `InsufficientBalance`
4. For `sahm_tier` items: parse key via `SahmTierPolicy`, load active tier (latest
   `SahmSubscription` row by ActivatedAt, default Free); target must be strictly higher
   (upgrades are one-way) → else `InvalidTierUpgrade`. Validated BEFORE any mutation so a
   rejected upgrade charges nothing
5. Not already owned (UNIQUE constraint is the DB backstop) → else 409 `AlreadyOwned`

Then the single mutation: `TryDebit(price, Purchase, …, refItemId)` → persist the returned
ledger row → insert `PlayerInventory(EgpPaid = price)` → insert new `SahmSubscription`
history row (for sahm_tier items) with the tier's hint limit → save → commit.

> ⚠️ A bug of this exact class was caught by tests during development: an early draft
> debited via `TryDebit` but never persisted the returned ledger row. Fixed and now covered
> by explicit assertions (exactly one Purchase ledger row with correct `BalanceAfter`).

**Inventory** (`GetInventoryAsync`): join to ShopItem details, newest first.

---

## 6. Phase 3 — SahmService

- **Lazy daily reset**: on the first hint request of a new UTC day, `HintsUsedToday` resets
  to 0 and `LastHintReset` moves to today (double-safety with the midnight scheduler).
- **Limit enforcement**: `HintsUsedToday >= DailyHintLimit` ⇒ `DailyHintLimitReached`
  (HTTP 429). Rejected requests emit NO telemetry and consume nothing.
- **Counter increment** persists via a tracked entity load (the shared repository's
  `FindAll` is AsNoTracking — IDs are resolved via projection, then loaded tracked).
- **First-ever request lazily creates** the implicit Free subscription.
- **Hint level mapping**: Free → `ConceptualNudge`, Pro → `StructuralGuidance`,
  Team/Enterprise → `CodeSnippet`.
- **AI hint text is out of scope**: `IAiOrchestrationService` belongs to the AI-pipeline
  group; `HintText` returns null until they land. Everything around it (limits, counters,
  telemetry) is complete.
- **Telemetry**: `hint_request` events go through `IAssessmentEventEmitter.Emit` —
  fire-and-forget, called AFTER persistence. Current implementation is a deliberate no-op
  stub until the Assessment group delivers their channel worker.
- **Midnight job support**: `ResetExpiredCountersAsync` bulk-resets stale rows and returns
  the affected count — ready to be wired into a hosted scheduler in a later sprint.

---

## 7. Phase 4 — API Wiring

Three controllers expose the module over REST (all async, cancellation forwarded from
`HttpContext.RequestAborted`):

| Route | Purpose |
|---|---|
| `GET /api/economy/{pid}/balance` | UC-ECO-01 |
| `GET /api/economy/{pid}/transactions?page=&pageSize=` | UC-ECO-05 |
| `POST /api/economy/{pid}/delta` | Sanctioned gateway for other groups' money effects |
| `POST /api/economy/{pid}/salary/{shiftId}` | Shift salary payment |
| `GET /api/shop/{pid}/catalog` · `/inventory` · `POST /purchase/{itemId}` | UC-ECO-06/08/09 |
| `GET /api/sahm/{pid}/status` · `POST /hint` | UC-SAHM-04/07, 02/03 |

`ResultHttpMapping` converts typed errors to status codes:
402 InsufficientBalance · 429 DailyHintLimitReached · 409 SalaryAlreadyPaid/AlreadyOwned ·
404 *NotFound · 400 everything else.

`{playerId}` currently comes from the route — marked with `TODO(identity)` until the auth
pipeline lands. Reset has no endpoint by decision (UC-GAME-11 belongs to the orchestrator).

---

## 8. Concurrency Design Explained

Every money operation follows the same discipline:

```
BeginTransactionAsync
  └─ GetForUpdateAsync(playerId)        ← SELECT … FOR UPDATE: exclusive row lock,
  └─ guards / reads / idempotency check    held until commit; concurrent writers BLOCK here
  └─ mutate via PlayerEconomy domain methods
  └─ AddAsync(ledger row with BalanceAfter)
  └─ SaveAsync()                        ← exactly one SaveChanges
  └─ CommitAsync()                      ← pure commit; lock released
catch → RollbackAsync(CancellationToken.None) → rethrow
```

Under PostgreSQL READ COMMITTED this serializes concurrent operations per player:
the second waiter unblocks after the first commits and then sees fresh data — which is how
the salary idempotency check and the balance pre-check become race-free.

**Live proof (executed against Supabase, Phase 4):** two parallel `-100 EGP` debits on a
100 EGP balance produced exactly one `200 OK` (balance → 0.00) and one
`402 Payment Required` — with exactly one ledger row and consistent totals afterwards.
Scratch data was removed after the test.

Sahm hint counters intentionally do NOT take row locks: they cap non-financial daily usage,
a microsecond race could over-consume by at most one hint, and the docs mandate no locking
there. If hints ever cost EGP, switch them to the locked pattern above.

---

## 9. Domain Business Rules Explained

| Rule | Where | Detail |
|---|---|---|
| Balance can never go negative | `PlayerEconomy` + DB CHECK | `TryDebit` fails instead; `ApplyPenalty` clamps at zero |
| Penalties clamp | `ApplyPenalty` | Abandoning a task with 30 EGP applies only −30, not the full penalty |
| Salary = base + performance bonus | `SalaryPolicy` | e.g. Intern base 2000 with 50% Ideal + 30% Acceptable choices ⇒ bonus rate 0.13 ⇒ 2260 total |
| Salaries are idempotent per shift | `UX_Transaction_SalaryPerShift` | Filtered unique index `(PlayerId, ReferenceId) WHERE transaction_type='salary' AND reference_id IS NOT NULL`; service checks first, DB backstops races |
| Sahm upgrades are one-way | `ShopService` | Buying `sahm_pro` while on Team fails with `InvalidTierUpgrade`, charging nothing; active tier = latest subscription row |
| Hint limits per tier | `SahmTierPolicy` | Free 3 · Pro 10 · Team 25 · Enterprise 255-as-unlimited (byte column sentinel) |
| Ledger is append-only history | `Transaction` entity | Signed Amount, post-change `BalanceAfter`, immutable once inserted |

---

## 10. Quality Assurance Results

| Check | Result |
|---|---|
| Clean rebuild (`--no-incremental`) | ✅ 0 errors, 0 warnings |
| xUnit suite | ✅ 58 / 58 passing (20 domain/policy + 38 service/integration-style) |
| Sync-over-async grep audit | ✅ none (`.Result` / `.Wait()` / `GetAwaiter().GetResult()`) |
| Retry-strategy audit | ✅ no `EnableRetryOnFailure` |
| Secrets audit of tracked files | ✅ clean (connection string only in gitignored `.env`) |
| Boot check against live Supabase DB | ✅ DI graph resolves; OpenAPI lists all routes |
| Read endpoints vs real data | ✅ balance/status/catalog/inventory/ledger verified |
| **Live concurrency race test** | ✅ **exactly one of two parallel debits succeeded (200 / 402)** |
| Independent code review (F-1..F-9) | ✅ all findings fixed and re-verified |

Test strategy notes: service tests run against the real `BaseRepository` over the EF Core
InMemory provider (async LINQ actually executes), with `IUnitOfWork` mocked so `SaveAsync`
delegates to the real context. Assertions query the database fresh after clearing the
change tracker — they verify persistence, not just tracked state. Transaction/lock semantics
cannot be exercised on InMemory and are covered by the manual/live race test instead
(see `Docs/Economy_RaceTest_Manual.md`).

---

## 11. Known Risks & Cross-Team Dependencies

1. **Migration drift (coordinate!):** `UX_Transaction_SalaryPerShift` exists in the EF model
   (`TransactionConfiguration`) but NOT yet in the database or migrations. The schema owner
   must create it — and until their migration lands, anyone running
   `dotnet ef migrations add` will generate it into their own migration.
2. **Identity/auth:** controllers accept `{playerId}` from the route pending the auth
   middleware; every action carries a `TODO(identity)` marker.
3. **Assessment telemetry:** `NoopAssessmentEventEmitter` must be replaced by the
   Assessment group's channel worker when ready (interface already matches §5.11 shape).
4. **AI hint text:** `HintResponseDto.HintText` stays null until the AI-pipeline group's
   orchestration service exists.
5. **Enterprise hint limit:** stored as byte 255 ("unlimited" sentinel) because
   `int.MaxValue` cannot fit the column — confirm with product owner.
6. **Reset deletes ALL ledger rows** (product-owner decision: clean slate). If financial
   auditing ever matters, revisit to preserve salary history.
7. **Rotate the Supabase password** — it was shared in plaintext chat during setup; the
   `.env` file itself is gitignored and safe.

---

## 12. Complete File Inventory

**Created — Domain:** `Abstractions/{EconomyErrors,ShopErrors,SahmErrors}.cs`,
`Constants/{SalaryPolicy,SahmTierPolicy}.cs`, `Enums/SahmTier.cs`,
`IRepositories/IPlayerEconomyRepository.cs`

**Created — Application:** `DependencyInjection.cs`,
`Services/{IEconomyService,EconomyService,IShopService,ShopService,ISahmService,SahmService,IAssessmentEventEmitter,NoopAssessmentEventEmitter}.cs`,
`Dtos/{BalanceDto,TransactionDto,PagedResult,ShopItemDto,PurchaseResultDto,InventoryItemDto,HintRequestDto,HintResponseDto,SahmStatusDto,AssessmentEventDto}.cs`

**Created — Infrastructure:** `Repositories/PlayerEconomyRepository.cs`

**Created — API:** `Controllers/{EconomyController,ShopController,SahmController}.cs`,
`Extensions/ResultHttpMapping.cs`

**Created — Tests:** `LoopGame.Tests/` project with
`Entities/PlayerEconomyTests.cs`, `Constants/SalaryPolicyTests.cs`,
`Services/{EconomyServiceTests,ShopServiceTests,SahmServiceTests}.cs`

**Created — Docs:** `Docs/Economy_RaceTest_Manual.md`, this document

**Modified:** `IUnitOfWork.cs`, `UnitOfWork.cs`, `PlayerEconomy.cs`,
`SahmSubscription.cs`, `IBaseRepository.cs` (nullable + `Delete` rename),
`BaseRepository.cs`, `TransactionConfiguration.cs` (salary index),
`SahmSubscriptionConfiguration.cs` (enum converter), `DependencyInjection.cs` (both layers),
`Program.cs` (+1 line), API/Application/Infrastructure `GlobalUsings.cs`,
Application `.csproj` (+EF Core, DI.Abstractions), `LoopGame.sln` (+test project),
deleted placeholder `Class1.cs`.

---

*Prepared by **Mohamed Saba** — Economy & Progression Services group.*
*For questions about any design decision above, see the referenced docs
(`SHIFT_Backend_Architecture.md` §5.8–5.10, `SHIFT_UseCases.md`, `SHIFT_ER_Diagram.md`)
or contact the author.*
