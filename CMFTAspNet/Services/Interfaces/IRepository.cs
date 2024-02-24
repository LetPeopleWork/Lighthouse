namespace CMFTAspNet.Services.Interfaces
{
    public interface IRepository<T> where T : class
    {
        void Add(T newItem);

        IEnumerable<T> GetAll();

        T? GetById(int id);

        void Remove(int id);

        void Update(T item);

        Task Save();
    }
}