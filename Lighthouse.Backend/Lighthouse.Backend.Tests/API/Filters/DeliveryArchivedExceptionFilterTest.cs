using Lighthouse.Backend.API.Filters;
using Lighthouse.Backend.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.API.Filters
{
    public class DeliveryArchivedExceptionFilterTest
    {
        private DeliveryArchivedExceptionFilter subject;

        [SetUp]
        public void Setup()
        {
            subject = new DeliveryArchivedExceptionFilter(Mock.Of<ILogger<DeliveryArchivedExceptionFilter>>());
        }

        [Test]
        public void OnException_RefusalToChangeAnArchivedDelivery_BecomesConflictCarryingTheReason()
        {
            var context = ExceptionContextFor(DeliveryArchivedException.AlreadyArchived(42));

            subject.OnException(context);

            var conflict = context.Result as ConflictObjectResult;
            var problemDetails = conflict?.Value as ProblemDetails;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.ExceptionHandled, Is.True);
                Assert.That(conflict, Is.Not.Null);
                Assert.That(problemDetails, Is.Not.Null);
                Assert.That(problemDetails?.Status, Is.EqualTo(StatusCodes.Status409Conflict));
                Assert.That(problemDetails?.Detail, Is.EqualTo("Delivery 42 is already archived."));
                Assert.That(problemDetails?.Extensions["code"], Is.EqualTo("delivery-archived"));
            }
        }

        [Test]
        public void OnException_AnyOtherFailure_IsLeftForSomebodyElse()
        {
            var context = ExceptionContextFor(new InvalidOperationException("unrelated"));

            subject.OnException(context);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(context.ExceptionHandled, Is.False);
                Assert.That(context.Result, Is.Null);
            }
        }

        private static ExceptionContext ExceptionContextFor(Exception exception)
        {
            var actionContext = new ActionContext(
                new DefaultHttpContext(),
                new RouteData(),
                new ActionDescriptor());

            return new ExceptionContext(actionContext, [])
            {
                Exception = exception,
            };
        }
    }
}
