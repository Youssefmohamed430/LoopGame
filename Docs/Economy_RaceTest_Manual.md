# Manual Race Test — Concurrent Debits on the Same Player

Verifies HARD RULE 5 (economy row locked FIRST via `SELECT ... FOR UPDATE`) using
`IEconomyService.ApplyEgpDeltaAsync`. Automated verification is intentionally manual
because the project's PostgreSQL instance is a shared Supabase dev database.

## Preconditions
- API running with two reachable terminals (Phase 4 wires controllers; until then any
  two processes that call `ApplyEgpDeltaAsync` concurrently, e.g. a small console harness).
- A test player with a known `PlayerEconomy` row and balance `B`.

## Steps
1. Pick a player whose balance is exactly enough for ONE of two debits, e.g. `B = 100`.
2. Prepare the same request in both terminals:
   `ApplyEgpDeltaAsync(playerId, delta: -100, TransactionType.Purchase, "race-test")`
3. Fire both requests at the same moment (two terminal windows, Enter as simultaneously
   as possible; or a script launching both HTTP calls in parallel).

## Expected outcome (exactly one of these)
- **One success, one failure:** first caller commits → new balance `0`;
  second caller blocks on `FOR UPDATE` until the first transaction commits, then reads
  the updated balance and fails with `Result.Failure(EconomyErrors.InsufficientBalance)`.
- **Both succeed is a BUG** (would mean locking is not effective — check that
  `GetForUpdateAsync` is called inside an open transaction).
- Final DB state: exactly ONE new `Transaction` row for this call pair,
  `BalanceAfter = 0`, and `PlayerEconomy.Balance = 0`.

## Verify in SQL (Supabase / psql)
```sql
SELECT balance FROM "PlayerEconomy" WHERE "PlayerId" = @pid;
SELECT amount, transaction_type, balance_after, description, created_at
FROM "Transaction"
WHERE "PlayerId" = @pid AND description = 'race-test'
ORDER BY created_at DESC;
-- expect exactly one row
```

## Note on lock duration
The lock is held only for the duration of the short money transaction
(lock → Credit/TryDebit → insert ledger → SaveChanges → commit). No HTTP/AI calls
happen inside, so contention windows are milliseconds.
