namespace Infrastructure;

public class UnitOfWork(AppDbContext _context) : IUnitOfWork
{
    private readonly ConcurrentDictionary<string, object> _repositories = new();
    public IDbContextTransaction? _transaction;

    public IBaseRepository<TEntity> GetRepository<TEntity>() where TEntity : class
    {
        var key = typeof(TEntity).Name;

        var repo = (IBaseRepository<TEntity>)_repositories.GetOrAdd(key, _ =>
          new BaseRepository<TEntity>(_context));

        return repo;
    }
    public async Task<int> SaveAsync()
    {
        return _context.SaveChangesAsync().Result;
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    public void BeginTransaction()
    {
        if (_transaction == null)
            _transaction = _context.Database.BeginTransaction();
    }

    public void Commit()
    {
        try
        {
            _context.SaveChanges();
            _transaction?.Commit();
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction?.Dispose();
        _transaction = null;
    }
}
