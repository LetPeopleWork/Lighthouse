namespace Lighthouse.Services.Interfaces
{
    public interface IRepository<T> where T : class
    {
        void Add(T item);

        IEnumerable<T> GetAll();

        T? GetById(int id);

        T? GetByPredicate(Func<T, bool> predicate);

        void Remove(int id);

        void Update(T item);

        Task Save();

        bool Exists(int id);

        bool Exists(Func<T, bool> predicate);
    }
}