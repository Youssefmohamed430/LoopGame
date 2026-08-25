namespace Domain.IRepositries;

    public interface IPlayerEconomyRepository
    {
        /// <summary>
        /// Loads the player's economy row as a TRACKED entity with an exclusive
        /// row lock held until the surrounding transaction commits/rolls back
        /// (SELECT ... FOR UPDATE). Must be called first in every money transaction.
        /// </summary>
        Task<PlayerEconomy?> GetForUpdateAsync(int playerId, CancellationToken ct = default);
    }
