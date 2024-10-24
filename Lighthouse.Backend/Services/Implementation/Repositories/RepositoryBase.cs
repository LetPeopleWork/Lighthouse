using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.History;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Lighthouse.Backend.Services.Implementation.Repositories
{
    public abstract class RepositoryBase<T> : IRepository<T> where T : class, IEntity
    {
        private readonly Func<LighthouseAppContext, DbSet<T>> dbSetGetter;
        private readonly ILogger<RepositoryBase<T>> logger;

        protected RepositoryBase(LighthouseAppContext context, Func<LighthouseAppContext, DbSet<T>> dbSetGetter, ILogger<RepositoryBase<T>> logger)
        {
            Context = context;
            this.dbSetGetter = dbSetGetter;
            this.logger = logger;
        }

        protected LighthouseAppContext Context { get; private set; }


        public void Add(T item)
        {
            logger.LogDebug("Adding item {Item_Id}", item.Id);
            dbSetGetter(Context).Add(item);
        }

        public bool Exists(int id)
        {
            logger.LogDebug("Check if item exists with id {id}", id);
            return dbSetGetter(Context).Find(id) != null;
        }

        public bool Exists(Func<T, bool> predicate)
        {
            return GetByPredicate(predicate) != null;
        }

        public virtual IEnumerable<T> GetAll()
        {
            return dbSetGetter(Context).ToList();
        }

        public virtual T? GetById(int id)
        {
            return dbSetGetter(Context).SingleOrDefault(t => t.Id == id);
        }

        public virtual T? GetByPredicate(Func<T, bool> predicate)
        {
            return dbSetGetter(Context).SingleOrDefault(predicate);
        }

        public virtual IQueryable<T> GetAllByPredicate(Expression<Func<T, bool>> predicate)
        {
            return dbSetGetter(Context).Where(predicate);
        }

        public virtual void Remove(int id)
        {
            logger.LogInformation("Removing item with {id}", id);
            var itemToRemove = dbSetGetter(Context).Find(id);

            Remove(itemToRemove);
        }

        public void Remove(T? item)
        {
            if (item != null)
            {
                dbSetGetter(Context).Remove(item);
            }
        }

        public async Task Save()
        {
            logger.LogDebug("Saving");
            await Context.SaveChangesAsync();
        }

        public void SaveSync()
        {
            Context.SaveChanges();
        }

        public void Update(T item)
        {
            logger.LogDebug("Updating item {Item_Id}", item.Id);
            dbSetGetter(Context).Update(item);
        }
    }
}