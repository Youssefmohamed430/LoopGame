namespace Infrastructure.Repositories;

public class PlayerEconomyRepository(AppDbContext _context) : IPlayerEconomyRepository
{
    // SELECT ... FOR UPDATE takes an exclusive row lock until commit/rollback
    // (PostgreSQL equivalent of SQL Server's WITH (UPDLOCK, ROWLOCK)).
    // The query is tracked by design: the locked row is the one we mutate.
    public async Task<PlayerEconomy?> GetForUpdateAsync(int playerId, CancellationToken ct = default)
        => await _context.PlayerEconomies
            .FromSqlInterpolated($"SELECT * FROM \"PlayerEconomy\" WHERE \"PlayerId\" = {playerId} FOR UPDATE")
            .FirstOrDefaultAsync(ct);
}
