# AGENT.MD — LoopGame (SHIFT) Engineering Constitution & Subsystem Mandate

> **Target Audience**: Any AI Coding Agent, LLM Assistant, or Software Engineer contributing to the **LoopGame (SHIFT)** codebase.  
> **Authority**: Absolute. These rules supersede convenience, speed, or superficial passing tests.

---

## 1. Prime Directives: The Non-Negotiables

### 1.1. Absolute Zero-Tolerance for "Cheap-Tape" Fixes
A **cheap-tape fix** is any patch that suppresses an error, test failure, or build issue without understanding and resolving its underlying root cause.

* **NEVER add random null-checks** (`?.`, `?? default`, `?? new()`) to hide uninitialized or missing data. Investigate *why* the data was null.
* **NEVER use fake authorization bypasses** (e.g., `if (!User.Identity.IsAuthenticated) return true;`). If an endpoint requires authentication, enforce it properly with claims and tests.
* **NEVER mock away bugs** in tests. If a test fails, fixing the mock to return happy data without fixing the production invariant is strictly forbidden.
* **NEVER widen access modifiers** or add backdoor methods (e.g. `SetBalanceForTest()`) to production entities just to make a test easier to write.
* **NEVER silence compiler or linter warnings** through blanket `#pragma warning disable` without explicit architectural justification.
* **Identify the root-cause layer**:
  * Is it an invalid business rule? $\rightarrow$ Fix it in **Domain**.
  * Is it missing data integrity / race condition? $\rightarrow$ Fix it in **Infrastructure / PostgreSQL**.
  * Is it an orchestration or transaction mismatch? $\rightarrow$ Fix it in **Application**.
  * Is it an authorization / input boundary issue? $\rightarrow$ Fix it in **Presentation (Controllers)**.

### 1.2. Never Trust Code Just Because It Passes Tests or Compiles
Passing tests, successful compilation, or a `200 OK` response **is NOT proof of correctness**.
* An in-memory test does NOT prove PostgreSQL CHECK constraints or unique indexes exist.
* An in-memory test does NOT prove `SELECT ... FOR UPDATE` acquires an exclusive lock.
* A single-threaded test does NOT prove immunity to concurrency race conditions or double-spending.
* Every critical business invariant must be enforced **in code (Domain)** AND **in storage (PostgreSQL Database)**.

### 1.3. Strict Subsystem Scope Boundaries
LoopGame is divided into modular subsystems (Economy & Progression, Learning & Assessment, Narrative & Story, Code Execution / Sandbox, Auth & Identity, Side Tasks).

* **Work ONLY inside your assigned subsystem.**
* **NEVER modify files belonging to another subsystem** without explicit user instruction.
* If an external subsystem has a bug or blocks your feature (e.g., missing middleware in `Program.cs`, broken external DTOs, or missing seed data):
  1. **DO NOT hot-patch the external subsystem.**
  2. Document the blocker clearly with file paths and line numbers.
  3. Keep your subsystem resilient, decoupled, and cleanly tested.

---

## 2. Architecture & Layering Rules (Clean Architecture + DDD)

LoopGame follows Clean Architecture with Domain-Driven Design (DDD) principles:

```
┌─────────────────────────────────────────────────────────────┐
│                 Presentation (LoopGame API)                 │
│         Controllers, Middleware, Filters, DTO Mappings      │
└──────────────────────────────┬──────────────────────────────┘
                               │ depends on
┌──────────────────────────────▼──────────────────────────────┐
│             Application (LoopGame.Application)              │
│       Use Cases, Orchestration, IUnitOfWork, Result<T>      │
└──────────────────────────────┬──────────────────────────────┘
                               │ depends on
┌──────────────────────────────▼──────────────────────────────┐
│                 Domain (LoopGame.Domain)                    │
│    Entities, Value Objects, Domain Errors, Invariants, Enums│
└──────────────────────────────▲──────────────────────────────┘
                               │ implements interfaces from
┌──────────────────────────────┴──────────────────────────────┐
│           Infrastructure (LoopGame.Infrastructure)          │
│       EF Core, PostgreSQL, Repositories, Migrations, S3     │
└─────────────────────────────────────────────────────────────┘
```

### 2.1. Domain Layer (`LoopGame.Domain`)
The core domain model must remain pure, isolated, and completely free of external dependencies (no EF Core attributes, no HTTP context, no external packages).

1. **Entity Encapsulation**:
   * All state-changing properties must have `private set;` or `protected set;`.
   * State mutations MUST occur through expressive domain methods (e.g., `Credit()`, `TryDebit()`, `Promote()`, `CompleteShift()`).
   * Never expose raw collections; expose `IReadOnlyCollection<T>`.

2. **Semantic Type & Sign Validation**:
   * Methods must validate semantic correctness (e.g., `Credit` only accepts positive amounts and credit-specific transaction types like `Salary`, `Bonus`, `SideTask`; `Debit` only accepts debit-specific types like `Purchase`, `Penalty`).
   * Reject negative or zero amounts on standard credit/debit operations.

3. **Numeric Precision & Overflow Protection**:
   * Financial / currency properties (`decimal`) must be rounded explicitly to 2 decimal places using `Math.Round(amount, 2, MidpointRounding.AwayFromZero)` to eliminate fractional piaster/cent drift against PostgreSQL's `numeric(10, 2)`.
   * Enforce upper boundary limits (e.g., `MaxTransactionAmount = 99_999_999.99m`) to prevent database numeric overflow exceptions (`numeric field overflow`).

4. **Domain Errors & Result Pattern**:
   * Return `Result` or `Result<T>` using static domain error definitions (e.g., `EconomyErrors.InsufficientFunds`, `EconomyErrors.InvalidTransactionType`).
   * Do not throw exceptions for predictable business domain failures.

### 2.2. Infrastructure Layer (`LoopGame.Infrastructure`)
The infrastructure layer is responsible for persistence, database constraints, and external integrations.

1. **Database-First Defense in Depth**:
   * Application-level validations can fail under concurrent load. The database MUST act as the final, immutable line of defense.
   * **CHECK Constraints**: Every numeric boundary must be backed by a PostgreSQL CHECK constraint (e.g., `CHK_Economy_Balance` ensuring `Balance >= 0`, `CHK_Economy_SalaryTier` ensuring `SalaryTier BETWEEN 1 AND 5`).
   * **Unique Filtered Indexes**: Prevent duplicate events (e.g., `UX_Transaction_SalaryPerShift` on `(PlayerId, ReferenceId)` WHERE `TransactionType = 'Salary'`).
   * **Composite Unique Indexes**: Prevent duplicate relations (e.g., `UQ_PlayerInventory` on `(PlayerId, ItemId)`).

2. **Concurrency Control & Row-Level Locking**:
   * Sensitive financial or progression rows subject to concurrent updates (e.g., balance debit, inventory purchase) MUST be fetched with exclusive locks:
     ```csharp
     // Native PostgreSQL row lock
     SELECT * FROM "PlayerEconomy" WHERE "PlayerId" = @p0 FOR UPDATE;
     ```
   * Never rely solely on C# `lock` or in-memory semaphores; they fail when multiple API instances run in production.

3. **Repository Integrity**:
   * Repositories must encapsulate complex queries and locking semantics (`GetForUpdateAsync`).
   * Repositories must expose `HasActiveTransaction()` so caller services know whether an outer transaction already exists.

### 2.3. Application Layer (`LoopGame.Application`)
The application layer orchestrates domain entities, repositories, and cross-aggregate business transactions.

1. **Atomic Cross-Aggregate Transactions**:
   * Any operation mutating more than one table/entity (e.g., debiting balance + inserting transaction ledger + inserting inventory) MUST execute within an explicit database transaction via `IUnitOfWork`.
   * Transaction pattern:
     ```csharp
     await _unitOfWork.BeginTransactionAsync(cancellationToken);
     try
     {
         // 1. Lock and mutate aggregate A
         // 2. Mutate aggregate B
         // 3. Save changes
         await _unitOfWork.SaveChangesAsync(cancellationToken);
         await _unitOfWork.CommitAsync(cancellationToken);
         return Result.Success();
     }
     catch
     {
         await _unitOfWork.RollbackAsync(cancellationToken);
         throw;
     }
     ```

2. **Transaction Ownership Rule**:
   * A service method that can be called standalone OR as part of a larger caller-managed workflow must check if a transaction is already active before calling `BeginTransactionAsync()`.
   * Never commit or roll back a transaction that was created by a parent caller.

3. **Front-Line Guards**:
   * Validate request parameters, IDs, and business pre-conditions before opening database connections or starting transactions to minimize database pool exhaustion.

4. **Lifecycle & Cascading Consistency**:
   * Reset or delete operations must cleanly handle all dependent records (e.g., resetting player economy must clean up `PlayerInventory`, `Transaction`, and `SahmSubscription`).

### 2.4. Presentation Layer (`LoopGame` API)
Controllers must remain thin, secure, and purely focused on HTTP translation and security.

1. **Mandatory Authentication**:
   * Every controller accessing player state must be decorated with `[Authorize]`.
   * Never allow anonymous callers to reach sensitive endpoints.

2. **Strict IDOR (Insecure Direct Object Reference) Prevention**:
   * **NEVER trust a `playerId` parameter passed in routes, query strings, or request bodies.**
   * Always extract the authenticated user's ID from claims:
     ```csharp
     var claimId = User.FindFirstValue(ClaimTypes.NameIdentifier);
     ```
   * Check whether the authenticated user owns the resource or has the `Admin` role:
     ```csharp
     protected bool IsAuthorizedForPlayer(int playerId)
     {
         if (User?.Identity?.IsAuthenticated != true) return false;
         if (User.IsInRole("Admin")) return true;
         
         var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
         return int.TryParse(claim, out var authId) && authId == playerId;
     }
     ```
   * If unauthorized, immediately return `Forbid()` (403) or `Unauthorized()` (401).

3. **Explicit HTTP Status Mapping**:
   * Map `Result<T>` cleanly:
     * `IsSuccess` $\rightarrow$ `Ok(value)` or `CreatedAtAction(...)`
     * `Error.Type == NotFound` $\rightarrow$ `NotFound(new { error = error.Description })`
     * `Error.Type == Conflict` $\rightarrow$ `Conflict(new { error = error.Description })`
     * `Error.Type == Validation` $\rightarrow$ `BadRequest(new { error = error.Description })`
     * Unauthorized $\rightarrow$ `Forbid()` / `Unauthorized()`

---

## 3. SOLID Principles: Operational Checklist

Before declaring any change complete, verify compliance against the SOLID checklist:

| Principle | Meaning in LoopGame | Violations to Avoid |
|---|---|---|
| **S — Single Responsibility** | Each class has one reason to change. Separate Shop purchasing from Salary calculation, Sahm investments, and Ledger auditing. | Monolithic services handling shop, salary, inventory, assessment, and coding runners in one class. |
| **O — Open/Closed** | Systems are open for extension via policies, strategies, or configurations, but closed for ad-hoc modification. | Hardcoded switch statements for calculating tier multipliers or salary bonuses scattered across services. Use `SalaryPolicy` or `SahmTierPolicy`. |
| **L — Liskov Substitution** | Derived classes or interface implementations must honor their contract without throwing unexpected `NotSupportedException`. | Repositories that throw exceptions on standard methods or mock repositories that behave differently from PostgreSQL. |
| **I — Interface Segregation** | Fine-grained, role-focused interfaces (`IEconomyService`, `IShopService`, `ISahmService`). | Huge kitchen-sink interfaces forcing callers to depend on methods they never use. |
| **D — Dependency Inversion** | Depend on domain abstractions (`IRepository`, `IUnitOfWork`), not concrete database contexts. | Controllers or services directly injecting `AppDbContext` and writing raw LINQ queries instead of using repositories. |

---

## 4. Verification & Testing Standards

Every feature, bugfix, or architectural improvement must be accompanied by comprehensive tests across three distinct tiers:

### Tier 1: Pure Domain Unit Tests
* **Target**: Entities, Value Objects, Domain Policies (`LoopGame.Tests/Entities/`).
* **Environment**: Fast, in-memory, zero database, zero mocking.
* **Coverage**: Valid mutations, invalid transitions, boundary values, currency rounding, null checks, overflow guards.

### Tier 2: Application & Security Tests
* **Target**: Application Services & API Controllers (`LoopGame.Tests/Services/`).
* **Environment**: Unit tests with in-memory DB or isolated mocks for external systems.
* **Mandatory Security Scenarios**:
  * Unauthenticated caller $\rightarrow$ Returns 401 or 403 `ForbidResult` without calling the underlying service.
  * Cross-tenant player ID mismatch (IDOR attempt) $\rightarrow$ Returns 403 `ForbidResult`.
  * Admin caller with cross-tenant ID $\rightarrow$ Successfully executes.
  * Legitimate authenticated owner $\rightarrow$ Successfully executes.

### Tier 3: Live PostgreSQL / Supabase Integration Tests
* **Target**: Physical constraints, concurrency, and transactions (`SupabaseEconomyIntegrationTests.cs`).
* **Tagging**: Must be decorated with `[Trait("Category", "Integration")]` so offline/sandboxed test runs can exclude them cleanly:
  ```bash
  # Run offline unit/security suite
  dotnet test --filter "Category!=Integration"
  
  # Run live Supabase integration suite
  dotnet test --filter "Category=Integration"
  ```
* **Required Assertions for Integration Tests**:
  * Physical CHECK constraints (e.g. `Assert.Equal("23514", ex.SqlState)` for negative balance or invalid tier).
  * Unique indexes (e.g. `Assert.Equal("23505", ex.SqlState)` for duplicate payouts or duplicate item ownership).
  * Native row locking (`GetForUpdateAsync` executes `SELECT ... FOR UPDATE` natively without syntax error).
  * Rollback behavior (uncommitted transactions leave zero rows in the live database).
  * **Test Cleanliness**: Any test writing to live databases must seed isolated data and clean it up completely in `DisposeAsync`.

---

## 5. Git & Collaboration Workflow

1. **Check Scope Before Touching Code**:
   * Run `git status` before making changes to understand existing state.
   * Never stage or commit files outside your assigned subsystem.

2. **Keep Builds and Tests 100% Green**:
   * Build the solution: `dotnet build` must result in 0 errors.
   * Run unit tests: `dotnet test --filter "Category!=Integration"` must pass 100%.

3. **Rebase Before Pushing**:
   * Always fetch remote master: `git fetch origin master`.
   * Rebase your changes: `git rebase origin/master`.
   * Never merge commits that create messy merge bubbles if a clean fast-forward/rebase is possible.
   * Never force-push (`git push --force`) to shared branches.

4. **Conventional Commit Format**:
   * Use structured commit messages:
     ```text
     feat(<subsystem>): <concise summary>

     - <layer>: <specific change>
     - <layer>: <specific change>
     - Tests: <tests added and verified>
     ```

---

## 6. Summary Checklist for Any Subsystem Task

When working on this repository, ask yourself these questions before considering the task done:

- [ ] **Did I find and fix the root cause, or did I apply a cheap-tape patch?**
- [ ] **Are my entity properties properly encapsulated with private setters?**
- [ ] **Are monetary / numerical values explicitly rounded to prevent fractional drift?**
- [ ] **Is there a physical PostgreSQL constraint backing the business invariant?**
- [ ] **Are multi-table mutations enclosed in an explicit `IUnitOfWork` transaction?**
- [ ] **Are sensitive rows locked with `FOR UPDATE` under concurrent load?**
- [ ] **Are controller endpoints protected by `[Authorize]` and immune to IDOR?**
- [ ] **Do unauthenticated and cross-tenant requests get rejected with 401/403?**
- [ ] **Did I modify ONLY my assigned subsystem and leave other subsystems untouched?**
- [ ] **Did all tests pass with 0 failures?**
- [ ] **Did I rebase cleanly onto `origin/master` before pushing?**

---

## 7. Mandatory Forensic Review Protocol

When reviewing, auditing, or modifying an existing subsystem, the agent MUST assume that existing code and previous AI-generated fixes may be incorrect.
The agent MUST NOT begin with the assumption that the current architecture or previous implementation is sound.

### 7.1. Evidence Before Verdict
Every claim of correctness MUST be supported by concrete evidence from the repository.
The agent must distinguish between:
* `PROVEN` — directly demonstrated by source code and appropriate tests.
* `NOT PROVEN` — plausible, but the available evidence is insufficient.
* `VIOLATED` — a concrete path exists that breaks the invariant.

The agent MUST NOT convert `NOT PROVEN` into `PROVEN` merely because:
* Tests pass
* The code looks correct
* A method has a reassuring name
* A comment claims the behavior
* An interface suggests the behavior
* An attribute exists
* A mock returns the expected result

---

### 7.2. Trace Critical Operations End-to-End
For every critical Economy & Progression operation, trace the COMPLETE execution path.
At minimum:
```text
HTTP Request
    ↓
Controller
    ↓
Authorization / Identity
    ↓
Application Service
    ↓
Domain Entity
    ↓
Repository
    ↓
EF Core
    ↓
PostgreSQL Transaction
    ↓
Database Constraints
    ↓
Commit
```

For each operation, identify:
* Where input enters the system
* Where authorization occurs
* Where validation occurs
* Where the aggregate is loaded
* Whether it is loaded with the required lock
* Where the state is mutated
* Where the ledger entry is created
* Where SaveChanges occurs
* Which transaction owns the operation
* Who commits
* Who rolls back
* Which database constraints protect the invariant
* What happens if any step fails

Do not evaluate individual methods in isolation when correctness depends on the complete execution path.

---

### 7.3. Critical Operation Inventory
The agent MUST identify every operation capable of changing economic or progression state.
At minimum inspect:
* Credit
* Debit
* Penalty
* Salary payout
* Salary tier change
* Shop purchase
* Inventory acquisition caused by purchase
* Subscription purchase
* Subscription renewal
* Hint purchase
* Reward
* Side-task economic reward
* Economy reset
* Refund
* Administrative balance modification
* Any background job capable of modifying economy
* Any endpoint capable of indirectly causing an economic mutation

Search the entire repository for callers of the underlying domain methods.
Do not assume that the obvious service/controller is the only caller.

---

### 7.4. Mutation Surface Audit
Search for ALL direct mutations of economy-related state.
Examples include:
```text
Balance =
Balance +=
Balance -=
SalaryTier =
Inventory.Add
Inventory.Remove
Transaction.Add
DbSet.Add
DbSet.Update
ExecuteUpdate
ExecuteSql
Raw SQL
```

Also search for reflection, serialization, mapping, or EF mechanisms that could bypass intended domain encapsulation.
Every discovered mutation path MUST be classified as:
* Authorized
* Intentionally internal
* Test-only
* Unsafe
* Unknown

An entity having private setters does NOT prove that its state cannot be bypassed elsewhere.

---

### 7.5. Transaction Ownership Must Be Proven
`HasActiveTransaction()` or checking:
```csharp
Database.CurrentTransaction != null
```
is NOT by itself proof of transaction ownership.

The agent MUST determine:
> Who created the current transaction?

A service that discovers an existing transaction must NOT commit or roll it back unless it owns that transaction.
For every transaction-sensitive workflow, explicitly document:
```text
Transaction creator:
Transaction owner:
Nested caller:
Commit owner:
Rollback owner:
SaveChanges owner:
```

If ownership cannot be reliably determined, mark the transaction design:
`NOT PROVEN`

Do not treat "an active transaction exists" as equivalent to "my transaction exists."

---

### 7.6. Concurrency Must Be Analyzed as Interleavings
Do not merely state that row locking exists.
For every concurrent financial mutation, model at least:
```text
Request A:
Read
Lock
Validate
Mutate
Save
Commit

Request B:
Read
Lock
Validate
Mutate
Save
Commit
```

Determine what happens under every possible ordering.
At minimum analyze:
* Two simultaneous debits
* Debit + credit
* Two simultaneous purchases
* Purchase + inventory mutation
* Two salary payouts
* Salary payout + purchase
* Two hint purchases
* Two subscription operations

If the system depends on PostgreSQL locking, verify that the lock occurs:
1. Inside the transaction
2. Before the critical read
3. On the correct row
4. For the entire critical section
5. Until commit/rollback

A lock applied after the balance has already been read is insufficient.

---

### 7.7. Idempotency Audit
For every externally-triggered economic mutation, answer:
> What happens if the exact same request executes twice?

The agent MUST identify whether duplicate execution is:
* Impossible by design
* Prevented by a database constraint
* Prevented by an idempotency key
* Prevented by a unique business reference
* Intentionally allowed
* Not addressed

If duplicate execution can produce unauthorized economic gain or loss, classify it as a finding.
Do not solve duplicate execution with an in-memory flag or static variable.

---

### 7.8. Database Reality Check
Do not assume that EF configuration equals database reality.
For every critical constraint, determine whether it exists in:
1. EF model configuration
2. Migration
3. Actual PostgreSQL schema, when integration access is available

If only #1 exists:
`DATABASE ENFORCEMENT = NOT PROVEN`

If migrations exist but have not been applied:
`DATABASE ENFORCEMENT = NOT PROVEN`

The agent MUST NOT claim that PostgreSQL protects an invariant solely because an EF configuration appears to define it.

---

### 7.9. Test Adequacy Audit
For every critical invariant, identify the strongest test that proves it.
Use this hierarchy:
```text
Pure domain invariant
        ↓
Application behavior
        ↓
Persistence behavior
        ↓
Transaction behavior
        ↓
Concurrency behavior
        ↓
Real PostgreSQL constraint behavior
```

A lower-level test MUST NOT be presented as proof of a higher-level invariant.
Examples:
* Domain unit test ≠ PostgreSQL constraint proof
* Mocked transaction test ≠ real transaction proof
* Controller unit test ≠ ASP.NET authentication proof
* Single-threaded debit test ≠ concurrency proof
* EF model test ≠ physical database schema proof

---

### 7.10. Adversarial Testing Requirement
For every critical operation, the agent MUST attempt to construct a failure scenario.
Examples:
```text
What if the request is repeated?
What if two requests arrive simultaneously?
What if SaveChanges fails?
What if the inventory insert fails?
What if the transaction already exists?
What if the transaction does not exist?
What if the player does not exist?
What if the player belongs to another user?
What if the database rejects the write?
What if the process fails after one mutation but before another?
What if the client retries after a timeout?
What if the request is malformed?
What if the amount is zero?
What if the amount is negative?
What if the amount exceeds the database precision?
```

The agent MUST record which scenarios were actually tested and which remain untested.

---

### 7.11. Previous AI Fix Audit
If the subsystem contains code previously generated or modified by another AI agent, every major fix MUST be independently reviewed.
For each previous fix:
```text
Previous Problem
↓
Previous Fix
↓
Actual Root Cause
↓
Does the Fix Address the Root Cause?
↓
Can the Original Bug Still Be Triggered?
↓
Did the Fix Introduce a New Failure?
↓
Evidence
```

The agent MUST explicitly classify the previous fix:
* ROOT-CAUSE FIX
* PARTIAL FIX
* CHEAP-TAPE FIX
* INCORRECT FIX
* UNVERIFIABLE

Passing regression tests does not automatically classify a fix as a ROOT-CAUSE FIX.

---

### 7.12. No Self-Certification
An agent MUST NOT use its own implementation as evidence of correctness.
For example:
> "I added a lock, therefore concurrency is safe."
is invalid reasoning.

The correct reasoning is:
> "The critical row is acquired with `FOR UPDATE` inside the transaction before the balance is read; concurrent transactions therefore serialize on that row; integration test X demonstrates the behavior against PostgreSQL."

The evidence, not the implementation claim, determines the verdict.

---

### 7.13. Modification vs Audit Separation
When performing an audit:
1. First inspect the existing implementation.
2. Record findings.
3. Identify root causes.
4. Only then modify code.
5. Re-run the audit against the modified implementation.
6. Verify that the fix did not introduce a new invariant violation.

Do not continuously patch individual failures without periodically re-evaluating the entire invariant.

---

### 7.14. Required Final Evidence Table
Every completed Economy & Progression audit MUST finish with:

| Invariant                                        | Status                         | Evidence        | Test Level                 | Remaining Risk |
| ------------------------------------------------ | ------------------------------ | --------------- | -------------------------- | -------------- |
| Balance cannot become negative                   | PROVEN / NOT PROVEN / VIOLATED | Exact mechanism | Domain / App / Integration | ...            |
| Unauthorized users cannot mutate economy         | ...                            | ...             | ...                        | ...            |
| Every balance mutation has correct ledger entry  | ...                            | ...             | ...                        | ...            |
| Ledger is immutable                              | ...                            | ...             | ...                        | ...            |
| Purchases are atomic                             | ...                            | ...             | ...                        | ...            |
| Concurrent spending is safe                      | ...                            | ...             | ...                        | ...            |
| Salary cannot duplicate unexpectedly             | ...                            | ...             | ...                        | ...            |
| Progression tiers remain valid                   | ...                            | ...             | ...                        | ...            |
| Sahm economic operations are atomic              | ...                            | ...             | ...                        | ...            |
| Database constraints enforce critical invariants | ...                            | ...             | ...                        | ...            |

A final `SAFE` verdict is forbidden if any critical invariant is `VIOLATED`.
A final `SAFE` verdict should normally be avoided when critical invariants remain `NOT PROVEN`; use `SAFE WITH KNOWN LIMITATIONS` or `CANNOT VERIFY` instead.

---

## 8. Change Control
Before modifying any file, the agent MUST identify:
```text
File:
Subsystem:
Layer:
Reason for change:
Root cause addressed:
Invariant protected:
Tests proving the change:
```

If the file belongs to another subsystem, do not modify it.
If an external file is required for correctness, document it as an external blocker.

---

## 9. Anti-Regression Requirement
After implementing a fix, the agent MUST verify both:

### Original invariant
The original defect is no longer reproducible.

### Neighboring invariants
The fix did not break:
* Authorization
* Transaction ownership
* Rollback
* Concurrency
* Ledger consistency
* Inventory consistency
* Progression consistency
* Existing valid behavior

A fix is incomplete if it repairs one invariant while violating another.

---

## 10. Final Rule
The agent's responsibility is not to produce a reassuring report.
The agent's responsibility is to discover the truth of the system.

When evidence is insufficient, the correct answer is:  
**NOT PROVEN.**

When the implementation violates an invariant, the correct answer is:  
**VIOLATED.**

When the implementation genuinely enforces the invariant and the appropriate test demonstrates it, the correct answer is:  
**PROVEN.**

