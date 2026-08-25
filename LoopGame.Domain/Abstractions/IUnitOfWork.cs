namespace LoopGame.Domain.Abstractions;

    public interface IUnitOfWork
    {
        IBaseRepository<TEntity> GetRepository<TEntity>() where TEntity : class;

        Task<int> SaveAsync(CancellationToken ct = default);

        /// <summary>Opens a database transaction (no-op if one is already active).</summary>
        Task BeginTransactionAsync(CancellationToken ct = default);

        /// <summary>
        /// Commits the active transaction. Does NOT save changes —
        /// call <see cref="SaveAsync"/> first (exactly one Save per use case).
        /// Disposes the transaction in all cases.
        /// </summary>
        Task CommitAsync(CancellationToken ct = default);

        Task RollbackAsync(CancellationToken ct = default);
    }

