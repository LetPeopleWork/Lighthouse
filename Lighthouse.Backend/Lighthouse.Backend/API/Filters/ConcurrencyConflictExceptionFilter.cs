using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.API.Filters
{
    public sealed class ConcurrencyConflictExceptionFilter : IExceptionFilter
    {
        internal const string ConflictCode = "concurrency-conflict";

        private const string ConflictMessage =
            "This item was changed by someone else since you loaded it. Reload and re-apply your change.";

        private readonly ILogger<ConcurrencyConflictExceptionFilter> logger;

        public ConcurrencyConflictExceptionFilter(ILogger<ConcurrencyConflictExceptionFilter> logger)
        {
            this.logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not DbUpdateConcurrencyException)
            {
                return;
            }

            logger.LogInformation("Optimistic concurrency conflict on {Path}; returning 409.",
                context.HttpContext.Request.Path);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Concurrency conflict",
                Detail = ConflictMessage,
                Type = ConflictCode,
            };
            problemDetails.Extensions["code"] = ConflictCode;

            context.Result = new ConflictObjectResult(problemDetails);
            context.ExceptionHandled = true;
        }
    }
}
