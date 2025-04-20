using System.Linq.Expressions;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    public interface IRepository<T> where T : class
    {
        void Add(T item);

        IEnumerable<T> GetAll();

        T? GetById(int id);

        T? GetByPredicate(Func<T, bool> predicate);

        IQueryable<T> GetAllByPredicate(Expression<Func<T, bool>> predicate);

        void Remove(int id);

        void Update(T item);

        Task Save();

        bool Exists(int id);

        bool Exists(Func<T, bool> predicate);
    }
}