namespace LoopGame.Domain.Abstractions;

    public interface IUnitOfWork
    {
        IBaseRepository<TEntity> GetRepository<TEntity>() where TEntity : class;

        Task<int> SaveAsync();
        void BeginTransaction();
        void Commit();
        void Rollback();
    }

