using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Services.Implementation.Authorization
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class RbacGuardAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public RbacGuardAttribute()
        {
        }

        public RbacGuardAttribute(RbacGuardRequirement requirement)
        {
            Requirement = requirement;
        }

        public RbacGuardRequirement Requirement { get; set; } = RbacGuardRequirement.SystemAdmin;

        public string? ScopeIdRouteKey { get; set; }

        public bool Check { get; set; } = true;

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            if (!Check)
            {
                return;
            }

            var rbacAdministrationService = context.HttpContext?.RequestServices
                .GetService<IRbacAdministrationService>();
            if (rbacAdministrationService is null)
            {
                return;
            }

            int? scopeId = null;
            if (RequiresScope() && !TryResolveScopeId(context, out scopeId))
            {
                context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
                return;
            }

            var canAccess = await rbacAdministrationService.CanSatisfyRequirementAsync(
                context.HttpContext.User,
                Requirement,
                scopeId,
                context.HttpContext?.RequestAborted ?? default);

            if (canAccess)
            {
                return;
            }

            context.Result = IsReadRequirement()
                ? new NotFoundResult()
                : new ForbidResult();
        }

        private bool RequiresScope()
        {
            return Requirement is RbacGuardRequirement.TeamRead
                or RbacGuardRequirement.TeamWrite
                or RbacGuardRequirement.PortfolioRead
                or RbacGuardRequirement.PortfolioWrite;
        }

        private bool IsReadRequirement()
        {
            return Requirement is RbacGuardRequirement.TeamRead
                or RbacGuardRequirement.PortfolioRead;
        }

        private bool TryResolveScopeId(AuthorizationFilterContext context, out int? scopeId)
        {
            scopeId = null;

            if (string.IsNullOrWhiteSpace(ScopeIdRouteKey))
            {
                return false;
            }

            if (!context.RouteData.Values.TryGetValue(ScopeIdRouteKey, out var routeValue) || routeValue is null)
            {
                return false;
            }

            if (routeValue is int intValue)
            {
                scopeId = intValue;
                return true;
            }

            if (int.TryParse(routeValue.ToString(), out var parsedValue))
            {
                scopeId = parsedValue;
                return true;
            }

            return false;
        }
    }
}