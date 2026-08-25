namespace Infrastructure;

public class UnitOfWork(AppDbContext _context) : IUnitOfWork, IDisposable
{
    private readonly ConcurrentDictionary<string, object> _repositories = new();
    private IDbContextTransaction? _transaction;

    public IBaseRepository<TEntity> GetRepository<TEntity>() where TEntity : class
    {
        var key = typeof(TEntity).Name;

        var repo = (IBaseRepository<TEntity>)_repositories.GetOrAdd(key, _ =>
          new BaseRepository<TEntity>(_context));

        return repo;
    }

    public async Task<int> SaveAsync(CancellationToken ct = default)
    {
        return await _context.SaveChangesAsync(ct);
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
            _transaction = await _context.Database.BeginTransactionAsync(ct);
    }

    /// <summary>Pure transaction commit — does NOT save changes.
    /// Callers must persist via <see cref="SaveAsync"/> before committing.</summary>
    public async Task CommitAsync(CancellationToken ct = default)
    {
        try
        {
            if (_transaction != null)
                await _transaction.CommitAsync(ct);
        }
        finally
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        try
        {
            if (_transaction != null)
                await _transaction.RollbackAsync(ct);
        }
        finally
        {
            if (_transaction != null)
                await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
