using Lighthouse.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Lighthouse.Backend.API.Filters
{
    /// <summary>
    /// An archived Delivery refuses to change, and that refusal is about the state of the Delivery,
    /// not about who is asking - so it reaches the caller as a conflict rather than a denial or a
    /// crash. The reason travels with it, because "already archived" and "not archived" need
    /// different words on screen.
    /// </summary>
    public sealed class DeliveryArchivedExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<DeliveryArchivedExceptionFilter> logger;

        public DeliveryArchivedExceptionFilter(ILogger<DeliveryArchivedExceptionFilter> logger)
        {
            this.logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.Exception is not DeliveryArchivedException archived)
            {
                return;
            }

            logger.LogInformation(
                "Delivery {DeliveryId} refused a change because of its archived state on {Path}; returning 409.",
                archived.DeliveryId, context.HttpContext.Request.Path);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Delivery archived",
                Detail = archived.Message,
                Type = archived.Code,
            };
            problemDetails.Extensions["code"] = archived.Code;
            problemDetails.Extensions["deliveryId"] = archived.DeliveryId;

            context.Result = new ConflictObjectResult(problemDetails);
            context.ExceptionHandled = true;
        }
    }
}
