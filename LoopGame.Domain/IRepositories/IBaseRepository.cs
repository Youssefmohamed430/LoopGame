namespace Domain.IRepositries;

    public interface IBaseRepository<T> where T : class
    {
        T GetByUserId(string id);
        T GetById(int id);
        IQueryable<TDto> GetAll<TDto>();
        T Find(Expression<Func<T, bool>> criteria, string[] includes = null);
        Task<T> FindAsync(Expression<Func<T, bool>> criteria, string[] includes = null);
        T FindWithTracking(Expression<Func<T, bool>> criteria, string[] includes = null);
        Task<TResult> FindWithAttributesAsync<TResult>(Expression<Func<T, bool>> criteria,Expression<Func<T, TResult>> selector,string[] includes = null);
        TDto Find<TDto>(Expression<Func<T, bool>> criteria, string[] includes = null);
        IQueryable<T> FindAll(Expression<Func<T, bool>> criteria, string[] includes = null);
        IQueryable<TDto> FindAll<TDto>(Expression<Func<T, bool>> criteria, string[] includes = null);
        Task<IQueryable<TDto>> FindAllAsync<TDto>(Expression<Func<T, bool>> criteria, string[] includes = null);
        Task<T> AddAsync(T entity);
        Task<T> UpdateAsync(T entity);
        void DeleteAsync(T entity);
    }

