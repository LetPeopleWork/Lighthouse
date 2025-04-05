using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Lighthouse.Backend.API
{
    public static class ApiHelpers
    {
        public static ActionResult<TResult> GetEntityByIdAnExecuteAction<TResult, TRepo>(this ControllerBase controller, IRepository<TRepo> repo, int id, Func<TRepo, TResult> action) where TRepo : class, IEntity
        {
            return GetEntityByIdAndExecuteActionInternal(controller, repo, id, entity => Task.FromResult(action(entity))).Result;
        }

        public static async Task<ActionResult<TResult>> GetEntityByIdAnExecuteAction<TResult, TRepo>(this ControllerBase controller, IRepository<TRepo> repo, int id, Func<TRepo, Task<TResult>> action) where TRepo : class, IEntity
        {
            return await GetEntityByIdAndExecuteActionInternal(controller, repo, id, action);
        }

        private static async Task<ActionResult<TResult>> GetEntityByIdAndExecuteActionInternal<TResult, TRepo>(ControllerBase controller, IRepository<TRepo> repo, int id, Func<TRepo, Task<TResult>> action) where TRepo : class, IEntity
        {
            var entity = repo.GetById(id);
            if (entity == null)
            {
                return controller.NotFound();
            }

            var result = await action(entity);

            return controller.Ok(result);
        }
    }
}