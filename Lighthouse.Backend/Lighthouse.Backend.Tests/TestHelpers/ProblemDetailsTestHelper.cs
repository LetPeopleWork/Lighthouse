using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    public static class ProblemDetailsTestHelper
    {
        /// <summary>Lets a controller built outside the web host answer with ProblemDetails, as the host would.</summary>
        public static TController AnsweringWithProblemDetails<TController>(this TController controller)
            where TController : ControllerBase
        {
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.ProblemDetailsFactory = new PlainProblemDetailsFactory();
            return controller;
        }

        public static ProblemDetails? TheProblem(this ActionResult? result) => (result as ObjectResult)?.Value as ProblemDetails;

        private sealed class PlainProblemDetailsFactory : ProblemDetailsFactory
        {
            public override ProblemDetails CreateProblemDetails(
                HttpContext httpContext, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
                => new() { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };

            public override ValidationProblemDetails CreateValidationProblemDetails(
                HttpContext httpContext, ModelStateDictionary modelStateDictionary, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
                => new(modelStateDictionary) { Status = statusCode, Title = title };
        }
    }
}
