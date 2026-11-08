using System.Reflection;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The dispatcher runs the handlers of an event one after the other, in the order the start-up code
    /// registers them, so that order is behaviour: the metrics cache of a portfolio must be dropped before
    /// the recorders read the metrics again, or they record what the previous refresh knew. Moving a
    /// registration, or gathering several into one extension method, can change the order without any
    /// other test noticing. The expected orders below are written out by hand on purpose: a list computed
    /// from the registrations would follow any reordering and could never fail.
    ///
    /// No web host is started. The start-up code's own registration method fills a service collection,
    /// and the handlers of an event are read from it in the order the container hands them out, which is
    /// the order they were registered in. A handler registered through a factory is asked for its type by
    /// running that factory. The handlers themselves are not built: their dependencies reach the database
    /// and the key ring, which only a started host sets up, and the order does not depend on them.
    /// </summary>
    [TestFixture]
    public class DomainEventHandlerOrderPinTest
    {
        private static readonly string[] TeamDataRefreshedOrder =
        [
            "ReconstructionMemo",
            "TeamDataRefreshedForecastTriggerHandler",
            "BlockedCountSnapshotRecordingHandler",
            "PercentilesOverTimeRecordingHandler",
            "ProcessBehaviorRecordingHandler",
            "DemoBlockedHistoryBackfillHandler",
            "DemoPercentilesBackfillHandler",
        ];

        private static readonly string[] PortfolioFeaturesRefreshedOrder =
        [
            "ReconstructionMemo",
            "PortfolioFeaturesRefreshedMetricsInvalidationHandler",
            "BlockedCountSnapshotRecordingHandler",
            "PercentilesOverTimeRecordingHandler",
            "ProcessBehaviorRecordingHandler",
            "DemoBlockedHistoryBackfillHandler",
            "DemoPercentilesBackfillHandler",
        ];

        private IServiceCollection registrations = null!;
        private ServiceProvider services = null!;

        [OneTimeSetUp]
        public void FillTheStartUpRegistrations()
        {
            registrations = TheStartUpRegistrations();
            services = registrations.BuildServiceProvider();
        }

        [OneTimeTearDown]
        public void DisposeTheRegistrations()
        {
            services.Dispose();
        }

        [Test]
        public void HandlersOfATeamRefresh_RunInTheRegisteredOrder()
        {
            var handedOut = HandlersInTheOrderTheContainerHandsThemOut<TeamDataRefreshed>();

            Assert.That(handedOut, Is.EqualTo(TeamDataRefreshedOrder),
                "The handlers of a team refresh no longer come in the order the start-up code registers them today. " +
                "The dispatcher runs them in this order, so a new order changes what happens after every team refresh. " +
                "Put the registrations back in their old order, or, if the new order is intended, change the list in this test in the same commit and say why.");
        }

        [Test]
        public void HandlersOfAPortfolioRefresh_DropTheMetricsCacheBeforeTheRecordersRead()
        {
            var handedOut = HandlersInTheOrderTheContainerHandsThemOut<PortfolioFeaturesRefreshed>();

            Assert.That(handedOut, Is.EqualTo(PortfolioFeaturesRefreshedOrder),
                "The handlers of a portfolio refresh no longer come in the order the start-up code registers them today. " +
                "The metrics cache must be dropped before the three recorders read the metrics, or they record what the previous refresh knew. " +
                "Put the registrations back in their old order, or, if the new order is intended, change the list in this test in the same commit and say why.");
        }

        private List<string> HandlersInTheOrderTheContainerHandsThemOut<TEvent>() where TEvent : IDomainEvent
        {
            using var scope = services.CreateScope();
            return [.. registrations
                .Where(registration => registration.ServiceType == typeof(IDomainEventHandler<TEvent>))
                .Select(registration => HandlerTypeOf(registration, scope.ServiceProvider).Name)];
        }

        private static Type HandlerTypeOf(ServiceDescriptor registration, IServiceProvider scope)
            => registration.ImplementationType
               ?? registration.ImplementationInstance?.GetType()
               ?? registration.ImplementationFactory!(scope).GetType();

        private static IServiceCollection TheStartUpRegistrations()
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });

            var registerServices = typeof(Program).GetMethod("RegisterServices", BindingFlags.NonPublic | BindingFlags.Static, [typeof(WebApplicationBuilder)])
                ?? throw new InvalidOperationException(
                    "The start-up code has no RegisterServices(WebApplicationBuilder) any more, so this test cannot read the handler registrations. " +
                    "Point it at the method that now registers the domain-event handlers.");
            registerServices.Invoke(null, [builder]);

            return builder.Services;
        }
    }
}
