using System.Linq.Expressions;

namespace SonqoAPI.Repositories.IRepositories
{
    public interface IGenericRepository<T> where T : class
    {
        // Obtiene registros con la opción de filtrarlos y de incluir tablas relacionadas (JOINs).
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? filter = null, params Expression<Func<T, object>>[] includes);

        // Obtiene un solo registro por su ID principal.
        Task<T?> GetByIdAsync(int id);

        // Verifica si existe al menos un registro que cumpla con una condición.
        Task<bool> ExistsAsync(Expression<Func<T, bool>> filter);

        // Agrega un nuevo registro al contexto.
        Task AddAsync(T entity);

        // Agrega una lista de registros al contexto.
        Task AddRangeAsync(IEnumerable<T> entities);

        // Marca un registro como modificado.
        void Update(T entity);

        // Marca un registro para ser eliminado.
        void Delete(T entity);

        // Ejecuta los cambios en la base de datos de PostgreSQL.
        Task<bool> SaveChangesAsync();
        IQueryable<T> GetQueryable();
    }
}