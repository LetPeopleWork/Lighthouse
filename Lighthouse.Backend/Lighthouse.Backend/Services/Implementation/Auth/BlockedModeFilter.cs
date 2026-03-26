using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    public class BlockedModeFilter : IAsyncActionFilter
    {
        private static readonly HashSet<string> BlockedModeAllowedPaths =
        [
            "/api/auth",
            "/api/license",
            "/api/version",
        ];

        private readonly IAuthModeResolver authModeResolver;
        private readonly ILogger<BlockedModeFilter> logger;

        public BlockedModeFilter(IAuthModeResolver authModeResolver, ILogger<BlockedModeFilter> logger)
        {
            this.authModeResolver = authModeResolver;
            this.logger = logger;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var status = authModeResolver.Resolve();

            if (status.Mode == AuthMode.Blocked)
            {
                var path = context.HttpContext.Request.Path.Value ?? string.Empty;

                var isAllowed = BlockedModeAllowedPaths.Any(
                    allowedPath => path.StartsWith(allowedPath, StringComparison.OrdinalIgnoreCase));

                if (!isAllowed)
                {
                    logger.LogDebug("Blocked-mode access denied for path: {Path}", path);
                    context.Result = new ObjectResult("Access denied: Premium license required for authentication feature.")
                    {
                        StatusCode = StatusCodes.Status403Forbidden,
                    };
                    return;
                }
            }

            await next();
        }
    }
}
