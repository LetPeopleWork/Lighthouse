using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.API
{
    public static class ApiHelpers
    {
        public static async Task<bool> CanExecuteSystemRbacActionAsync(
            this ControllerBase controller,
            CancellationToken cancellationToken = default)
        {
            var rbacAdministrationService = controller.HttpContext?.RequestServices
                .GetService<IRbacAdministrationService>();
            if (rbacAdministrationService is null)
            {
                return true;
            }

            if (!await rbacAdministrationService.IsRbacEnforcedAsync(cancellationToken))
            {
                return true;
            }

            return await rbacAdministrationService.CanManageRbacAsync(controller.User, cancellationToken);
        }

        public static bool CanExecuteSystemRbacAction(this ControllerBase controller)
        {
            return controller
                .CanExecuteSystemRbacActionAsync(controller.HttpContext?.RequestAborted ?? default)
                .GetAwaiter()
                .GetResult();
        }

        public static IReadOnlyList<int> GetReadableTeamIds(this ControllerBase controller, IEnumerable<int> teamIds)
        {
            var rbacAdministrationService = controller.HttpContext?.RequestServices
                .GetService<IRbacAdministrationService>();
            if (rbacAdministrationService is null)
            {
                return teamIds.Distinct().ToArray();
            }

            return rbacAdministrationService
                .GetReadableTeamIdsAsync(controller.User, teamIds, controller.HttpContext?.RequestAborted ?? default)
                .GetAwaiter()
                .GetResult();
        }

        public static IReadOnlyList<int> GetReadablePortfolioIds(this ControllerBase controller, IEnumerable<int> portfolioIds)
        {
            var rbacAdministrationService = controller.HttpContext?.RequestServices
                .GetService<IRbacAdministrationService>();
            if (rbacAdministrationService is null)
            {
                return portfolioIds.Distinct().ToArray();
            }

            return rbacAdministrationService
                .GetReadablePortfolioIdsAsync(controller.User, portfolioIds, controller.HttpContext?.RequestAborted ?? default)
                .GetAwaiter()
                .GetResult();
        }

        public static bool CanWriteTeam(this ControllerBase controller, int teamId)
        {
            var rbacAdministrationService = controller.HttpContext?.RequestServices
                .GetService<IRbacAdministrationService>();
            if (rbacAdministrationService is null)
            {
                return true;
            }

            return rbacAdministrationService
                .CanWriteTeamAsync(controller.User, teamId, controller.HttpContext?.RequestAborted ?? default)
                .GetAwaiter()
                .GetResult();
        }

        public static bool CanWritePortfolio(this ControllerBase controller, int portfolioId)
        {
            var rbacAdministrationService = controller.HttpContext?.RequestServices
                .GetService<IRbacAdministrationService>();
            if (rbacAdministrationService is null)
            {
                return true;
            }

            return rbacAdministrationService
                .CanWritePortfolioAsync(controller.User, portfolioId, controller.HttpContext?.RequestAborted ?? default)
                .GetAwaiter()
                .GetResult();
        }

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
            var method = controller.HttpContext?.Request.Method ?? string.Empty;
            var isRead = HttpMethods.IsGet(method);
            var canAccess = await CanAccessEntityAsync<TRepo>(
                controller,
                id,
                isRead,
                controller.HttpContext?.RequestAborted ?? default);
            if (!canAccess)
            {
                return isRead
                    ? controller.NotFound()
                    : controller.Forbid();
            }

            var entity = repo.GetById(id);
            if (entity == null)
            {
                return controller.NotFound();
            }

            var result = await action(entity);

            return controller.Ok(result);
        }

        private static async Task<bool> CanAccessEntityAsync<TRepo>(
            ControllerBase controller,
            int entityId,
            bool isRead,
            CancellationToken cancellationToken) where TRepo : class, IEntity
        {
            var rbacAdministrationService = controller.HttpContext?.RequestServices
                .GetService<IRbacAdministrationService>();
            if (rbacAdministrationService is null)
            {
                return true;
            }

            if (typeof(TRepo) == typeof(Team))
            {
                return isRead
                    ? await rbacAdministrationService.CanReadTeamAsync(controller.User, entityId, cancellationToken)
                    : await rbacAdministrationService.CanWriteTeamAsync(controller.User, entityId, cancellationToken);
            }

            if (typeof(TRepo) == typeof(Portfolio))
            {
                return isRead
                    ? await rbacAdministrationService.CanReadPortfolioAsync(controller.User, entityId, cancellationToken)
                    : await rbacAdministrationService.CanWritePortfolioAsync(controller.User, entityId, cancellationToken);
            }

            return true;
        }
    }
}