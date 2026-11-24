using System.Reflection;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Platform.DomainEvents;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The dispatcher runs the handlers of an event one after the other, in the order the start-up code
    /// registers them, so that order is behaviour: the metrics cache of a team or a portfolio must be dropped
    /// before the recorders read the metrics again, or they record what the previous refresh knew. Moving a
    /// registration, or gathering several into one extension method, can change the order without any
    /// other test noticing. The expected orders below are written out by hand on purpose: a list computed
    /// from the registrations would follow any reordering and could never fail.
    /// </summary>
    [TestFixture]
    public class DomainEventHandlerOrderPinTest
    {
        private static readonly string[] TeamDataRefreshedOrder =
        [
            "TeamDataRefreshedTeamMetricsUpdateHandler",
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

        private static readonly string[] FeatureOrderingPolicyChangedOrder =
        [
            "FeatureOrderingPolicyChangedForecastTriggerHandler",
        ];

        private static readonly string[] FeatureRankChangedOrder =
        [
            "FeatureRankChangedForecastTriggerHandler",
        ];

        private static readonly string[] TeamDeletedOrder =
        [
            "TeamDeletedRefreshLogCleanupHandler",
            "TeamDeletedForecastRetriggerHandler",
        ];

        private static readonly string[] PortfolioDeletedOrder =
        [
            "PortfolioDeletedOrphanedFeatureCleanupHandler",
        ];

        private static readonly string[] PortfolioForecastsUpdatedOrder =
        [
            "DeliveryMetricSnapshotRecordingHandler",
            "DeliveryForecastPublishingHandler",
        ];

        private StartUpDomainEventHandlers handlers = null!;

        [OneTimeSetUp]
        public void ReadTheStartUpRegistrations()
        {
            handlers = StartUpDomainEventHandlers.Read(
                typeof(TeamDataRefreshed),
                typeof(PortfolioFeaturesRefreshed),
                typeof(FeatureOrderingPolicyChanged),
                typeof(FeatureRankChanged),
                typeof(TeamDeleted),
                typeof(PortfolioForecastsUpdated),
                typeof(PortfolioDeleted));
        }

        [Test]
        public void HandlersOfATeamRefresh_UpdateTheTeamMetricsBeforeAnyOtherReaction()
        {
            var handedOut = HandlersInTheOrderTheContainerHandsThemOut<TeamDataRefreshed>();

            Assert.That(handedOut, Is.EqualTo(TeamDataRefreshedOrder),
                "The handlers of a team refresh do not come in the order written out here, with the Flow metrics reaction that updates the team's metrics first. " +
                "That reaction drops the team's metrics cache and saves its update time and Feature WIP; a reaction that runs before it, such as a snapshot recorder, " +
                "reads the metrics the previous refresh knew and records them as today's. " +
                "Register the team metrics reaction on its own line immediately before the other team refresh handlers and keep their order; " +
                "if another order is intended, change the list in this test in the same commit and say why.");
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

        [Test]
        public void HandlersOfAFeatureOrderingPolicyChange_AreTheForecastTriggerAlone()
        {
            var handedOut = HandlersInTheOrderTheContainerHandsThemOut<FeatureOrderingPolicyChanged>();

            Assert.That(handedOut, Is.EqualTo(FeatureOrderingPolicyChangedOrder),
                "The handlers of a feature ordering policy change are no longer the forecast trigger alone, as the start-up code registers them today. " +
                "Without that registration, changing the ordering policy no longer retriggers the forecasts. " +
                "Put the registration back, or, if the change is intended, change the list in this test in the same commit and say why.");
        }

        [Test]
        public void HandlersOfAFeatureRankChange_AreTheForecastTriggerAlone()
        {
            var handedOut = HandlersInTheOrderTheContainerHandsThemOut<FeatureRankChanged>();

            Assert.That(handedOut, Is.EqualTo(FeatureRankChangedOrder),
                "The handlers of a feature rank change are no longer the forecast trigger alone, as the start-up code registers them today. " +
                "Without that registration, ranking a feature no longer retriggers the forecasts. " +
                "Put the registration back, or, if the change is intended, change the list in this test in the same commit and say why.");
        }

        [Test]
        public void HandlersOfATeamDeletion_ForgetTheRefreshLogBeforeTheForecastsRetrigger()
        {
            var handedOut = HandlersInTheOrderTheContainerHandsThemOut<TeamDeleted>();

            Assert.That(handedOut, Is.EqualTo(TeamDeletedOrder),
                "The handlers of a team deletion no longer come in the order the start-up code registers them today. " +
                "The dispatcher runs them in this order, so a new order changes what the forecast retrigger finds after a team is deleted. " +
                "Put the registrations back in their old order, or, if the new order is intended, change the list in this test in the same commit and say why.");
        }

        [Test]
        public void HandlersOfAPortfolioForecastUpdate_RecordTheDeliverySnapshotBeforePublishingTheForecasts()
        {
            var handedOut = HandlersInTheOrderTheContainerHandsThemOut<PortfolioForecastsUpdated>();

            Assert.That(handedOut, Is.EqualTo(PortfolioForecastsUpdatedOrder),
                "The handlers of a portfolio forecast update no longer come in the order the start-up code registers them today: the Delivery metric snapshot recording, then the Delivery forecast publishing. " +
                "The dispatcher runs them in this order, so publishing first would publish forecasts before the snapshot of the Delivery metrics is recorded, and a missing handler stops its reaction altogether. " +
                "Put the registrations back in their old order, or, if the new order is intended, change the list in this test in the same commit and say why.");
        }

        [Test]
        public void HandlersOfAPortfolioDeletion_AreTheOrphanedFeatureCleanupAlone()
        {
            var handedOut = HandlersInTheOrderTheContainerHandsThemOut<PortfolioDeleted>();

            Assert.That(handedOut, Is.EqualTo(PortfolioDeletedOrder),
                "The handlers of a Portfolio deletion are not the orphaned-Feature clean-up alone. " +
                "Without that registration, deleting a Portfolio leaves its Features on the Team's Features tab until the next refresh or start. " +
                "Register the clean-up reaction for PortfolioDeleted beside the Team deletion handlers, or, if another handler is intended, change the list in this test in the same commit and say why.");
        }

        private List<string> HandlersInTheOrderTheContainerHandsThemOut<TEvent>() where TEvent : IDomainEvent
            => [.. handlers.Of<TEvent>().Select(handler => handler.Name)];
    }

    /// <summary>
    /// The domain-event handlers the start-up code registers, per event, in the order the container hands them out,
    /// which is the order they were registered in. No web host is started: the start-up code's own registration
    /// method fills a service collection. A handler registered through a factory is asked for its type by running
    /// that factory. The handlers themselves are not built: their dependencies reach the database and the key ring,
    /// which only a started host sets up, and the order does not depend on them.
    /// </summary>
    internal sealed class StartUpDomainEventHandlers
    {
        private readonly Dictionary<Type, IReadOnlyList<Type>> handlersByEvent;

        private StartUpDomainEventHandlers(Dictionary<Type, IReadOnlyList<Type>> handlersByEvent)
        {
            this.handlersByEvent = handlersByEvent;
        }

        public IReadOnlyList<Type> Of<TEvent>() where TEvent : IDomainEvent
            => handlersByEvent.TryGetValue(typeof(TEvent), out var handlers)
                ? handlers
                : throw new InvalidOperationException($"The handlers of {typeof(TEvent).Name} were not read. Name the event when reading the start-up registrations.");

        public static StartUpDomainEventHandlers Read(params Type[] events)
        {
            var registrations = TheStartUpRegistrations();
            using var services = registrations.BuildServiceProvider();
            using var scope = services.CreateScope();

            var handlersByEvent = new Dictionary<Type, IReadOnlyList<Type>>();
            foreach (var domainEvent in events)
            {
                var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent);
                handlersByEvent[domainEvent] = [.. registrations
                    .Where(registration => registration.ServiceType == handlerType)
                    .Select(registration => HandlerTypeOf(registration, scope.ServiceProvider))];
            }

            return new StartUpDomainEventHandlers(handlersByEvent);
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
