using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lighthouse.Backend.Services.Implementation.Licensing
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class LicenseGuardAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private const int MaxAllowedTeams = 3;

        private const int MaxAllowedProjects = 1;

        public bool RequirePremium { get; set; } = false;

        public bool CheckTeamConstraint { get; set; } = false;

        public int TeamLimitOverride { get; set; } = MaxAllowedTeams;

        public int ProjectLimitOverride { get; set; } = MaxAllowedProjects;

        public bool CheckProjectConstraint { get; set; } = false;

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var licenseService = context.HttpContext.RequestServices.GetService<ILicenseService>();
            if (licenseService == null)
            {
                context.Result = new StatusCodeResult(500);
                return Task.CompletedTask;
            }

            if (!licenseService.CanUsePremiumFeatures())
            {
                if (RequirePremium)
                {
                    context.Result = new ObjectResult("Access denied: Premium Features Required")
                    {
                        StatusCode = StatusCodes.Status403Forbidden,
                    };

                    return Task.CompletedTask;
                }

                if (CheckTeamConstraint)
                {
                    EntityLimitExceeded<Team>(context, TeamLimitOverride);
                }

                if (CheckProjectConstraint)
                {
                    EntityLimitExceeded<Project>(context, ProjectLimitOverride);
                }
            }

            return Task.CompletedTask;
        }

        private bool EntityLimitExceeded<TEntity>(AuthorizationFilterContext context, int maxAllowed)
            where TEntity : class, IEntity
        {
            var repository = context.HttpContext.RequestServices.GetService<IRepository<TEntity>>();
            if (repository == null)
            {
                context.Result = new StatusCodeResult(500);
                return true;
            }

            var count = repository.GetAll().Count();
            if (count > maxAllowed)
            {
                context.Result = new ObjectResult(
                    $"Free users can only use up to {maxAllowed} {typeof(TEntity).Name}.")
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return true;
            }

            return false;
        }
    }
}
