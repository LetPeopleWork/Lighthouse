using Lighthouse.Backend.API.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.API.Filters
{
    [TestFixture]
    public class ConcurrencyConflictExceptionFilterTest
    {
        private ConcurrencyConflictExceptionFilter subject;

        [SetUp]
        public void SetUp()
        {
            subject = new ConcurrencyConflictExceptionFilter(Mock.Of<ILogger<ConcurrencyConflictExceptionFilter>>());
        }

        [Test]
        public void OnException_NonConcurrencyException_LeavesExceptionUnhandled()
        {
            var context = CreateContext(new InvalidOperationException("unrelated failure"));

            subject.OnException(context);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.ExceptionHandled, Is.False);
                Assert.That(context.Result, Is.Null);
            }
        }

        [Test]
        public void OnException_ConcurrencyConflict_ReturnsHandled409WithDistinguishableCode()
        {
            var context = CreateContext(new DbUpdateConcurrencyException("stale token"));

            subject.OnException(context);

            var conflict = context.Result as ConflictObjectResult;
            var problemDetails = conflict?.Value as ProblemDetails;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.ExceptionHandled, Is.True);
                Assert.That(conflict, Is.Not.Null);
                Assert.That(conflict!.StatusCode, Is.EqualTo(StatusCodes.Status409Conflict));
                Assert.That(problemDetails, Is.Not.Null);
                Assert.That(problemDetails!.Status, Is.EqualTo(StatusCodes.Status409Conflict));
                Assert.That(problemDetails.Title, Is.Not.Empty);
                Assert.That(problemDetails.Detail, Is.Not.Empty);
                Assert.That(problemDetails.Type, Is.EqualTo(ConcurrencyConflictExceptionFilter.ConflictCode));
                Assert.That(problemDetails.Extensions["code"], Is.EqualTo(ConcurrencyConflictExceptionFilter.ConflictCode));
            }
        }

        private static ExceptionContext CreateContext(Exception exception)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = new PathString("/api/latest/teams/1");

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            return new ExceptionContext(actionContext, [])
            {
                Exception = exception,
            };
        }
    }
}
