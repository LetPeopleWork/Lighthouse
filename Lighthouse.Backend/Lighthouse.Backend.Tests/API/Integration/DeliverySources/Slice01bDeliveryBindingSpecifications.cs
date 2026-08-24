using System.Net;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.DeliverySources
{
    /// <summary>
    /// Step definitions for creating a Delivery from a Release, editing one that follows a Release, and
    /// releasing it again - all over HTTP. The backend-observable contract: a bound Delivery says what
    /// the Release says and nothing a payload asks for, and releasing it hands the last synced name,
    /// date and Features back as its own.
    /// </summary>
    public partial class Slice01bDeliveryBindingTest : DeliverySourcesAcceptanceTest
    {
        private const string TheRelease = "10007";
        private const string TheReleaseName = "Release 3.0";
        private const string TheWorkTheReleaseCarries = "LGH-1";
        private const string TheWorkNobodyTaggedAgainstIt = "LGH-2";

        private const string ANameSomebodyTyped = "Renamed By Hand";

        private static readonly DateTime TheReleaseDate = new(2027, 3, 18, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime ADateSomebodyPicked = new(2027, 9, 30, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime AReleaseDateThatHasAlreadyPassed = new(2024, 2, 6, 0, 0, 0, DateTimeKind.Utc);

        private static readonly List<WorkItemRuleCondition> ARuleSomebodyWrote =
        [
            new WorkItemRuleCondition { FieldKey = "type", Operator = "equals", Value = "Epic" },
        ];

        /// <summary>
        /// The four edits that leave the Delivery following the Release. Each names one of the writes
        /// the aggregate refuses while bound, so a controller that stopped discarding the payload would
        /// show up here as the Delivery wearing the typed value instead of the Release's.
        /// </summary>
        private static readonly TestCaseData[] EveryHandEditToWhatTheReleaseOwns =
        [
            new TestCaseData(HandEdit.Rename).SetName("{m}(a name somebody typed)"),
            new TestCaseData(HandEdit.Reschedule).SetName("{m}(a date somebody picked)"),
            new TestCaseData(HandEdit.ReplaceTheFeatures).SetName("{m}(a Feature list somebody assembled)"),
            new TestCaseData(HandEdit.SelectFeaturesByRule).SetName("{m}(a rule somebody wrote)"),
        ];

        public enum HandEdit
        {
            Rename,
            Reschedule,
            ReplaceTheFeatures,
            SelectFeaturesByRule,
        }

        private int untrackedFeatureId;

        // --- Given ---

        private int GivenAJiraPortfolioWhoseReleaseCarriesWorkItTracks()
        {
            var portfolioId = SeedPortfolioOn(WorkTrackingSystems.Jira);

            TheJiraConnectionOffersItsReleases();
            SeedTrackedFeature(portfolioId, TheWorkTheReleaseCarries, "Ship the thing");
            untrackedFeatureId = SeedTrackedFeature(portfolioId, TheWorkNobodyTaggedAgainstIt, "Ship the other thing");

            TheReleaseIsDated(TheReleaseDate);

            return portfolioId;
        }

        private void GivenTheReleaseShippedLongAgo() => TheReleaseIsDated(AReleaseDateThatHasAlreadyPassed);

        private void TheReleaseIsDated(DateTime date)
        {
            TheRemoteSays(TheRelease, new DeliverySourceResolution.Resolved(
                new DeliverySourceSnapshot(TheReleaseName, date, [TheWorkTheReleaseCarries])));
        }

        private void GivenTheCallerMayOnlyReadThisPortfolio(int portfolioId) => Client.AsPortfolioViewer(portfolioId);

        private void GivenTheCallerMayChangeThisPortfolio(int portfolioId) => Client.AsPortfolioAdmin(portfolioId);

        private void GivenTheInstanceIsNotLicensed() => TheInstanceIsNotLicensedForPremium();

        // --- When ---

        private Task<HttpResponseMessage> WhenTheDeliveryIsCreatedFromTheRelease(string prefix, int portfolioId)
            => PostTheDelivery(prefix, portfolioId, ARequestToFollowTheRelease());

        private Task<HttpResponseMessage> WhenTheDeliveryIsCreatedByHand(string prefix, int portfolioId)
            => PostTheDelivery(prefix, portfolioId, new UpdateDeliveryRequest
            {
                Name = ANameSomebodyTyped,
                Date = ADateSomebodyPicked,
                FeatureIds = [],
                SelectionMode = DeliverySelectionMode.Manual,
            });

        private Task<HttpResponseMessage> WhenTheDeliveryIsEditedByHandWhileStillFollowingTheRelease(
            string prefix, int deliveryId, HandEdit edit)
        {
            var request = ARequestToFollowTheRelease();

            switch (edit)
            {
                case HandEdit.Rename:
                    request.Name = ANameSomebodyTyped;
                    break;
                case HandEdit.Reschedule:
                    request.Date = ADateSomebodyPicked;
                    break;
                case HandEdit.ReplaceTheFeatures:
                    request.FeatureIds = [untrackedFeatureId];
                    break;
                default:
                    request.Rules = ARuleSomebodyWrote;
                    request.Mode = WorkItemRuleSet.ModeOr;
                    break;
            }

            return PutTheDelivery(prefix, deliveryId, request);
        }

        /// <summary>
        /// One request carrying both halves of the tension: it asks to choose the Features by hand,
        /// which is the fifth write a bound Delivery refuses, and it asks to stop following the Release,
        /// which must go through. Sending them apart would prove neither.
        /// </summary>
        private Task<HttpResponseMessage> WhenTheDeliveryIsTakenBackByHand(
            string prefix, int deliveryId, DateTime dateInThePayload)
            => PutTheDelivery(prefix, deliveryId, new UpdateDeliveryRequest
            {
                Name = ANameSomebodyTyped,
                Date = dateInThePayload,
                FeatureIds = [untrackedFeatureId],
                SelectionMode = DeliverySelectionMode.Manual,
            });

        private Task<HttpResponseMessage> WhenTheDeliveryIsPointedAtTheReleaseAgain(string prefix, int deliveryId)
            => PutTheDelivery(prefix, deliveryId, ARequestToFollowTheRelease());

        private static UpdateDeliveryRequest ARequestToFollowTheRelease()
            => new()
            {
                Name = TheReleaseName,
                Date = TheReleaseDate,
                FeatureIds = [],
                SelectionMode = DeliverySelectionMode.SourceBound,
                SourceKey = JiraReleaseSourceKey,
                SourceReference = TheRelease,
            };

        private async Task<DeliveryRow> TheOnlyDeliveryOf(string prefix, int portfolioId)
        {
            var response = await GetTheDeliveriesOfPortfolio(prefix, portfolioId);
            var body = await DeliveriesIn(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(body.Active, Has.Count.EqualTo(1));
            }

            return body.Active[0];
        }

        private async Task<int> TheNumberOfDeliveriesShownFor(string prefix, int portfolioId)
        {
            var body = await DeliveriesIn(await GetTheDeliveriesOfPortfolio(prefix, portfolioId));

            return body.Active.Count;
        }

        // --- Then ---

        private static void ThenTheAnswerIs(HttpResponseMessage response, HttpStatusCode expected)
            => Assert.That(response.StatusCode, Is.EqualTo(expected));

        private void ThenTheDeliverySaysWhatTheReleaseSays(DeliveryRow delivery, DateTime expectedDate)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.SourceBound));
                Assert.That(delivery.SourceKey, Is.EqualTo(JiraReleaseSourceKey));
                Assert.That(delivery.SourceReference, Is.EqualTo(TheRelease));
                Assert.That(delivery.Name, Is.EqualTo(TheReleaseName));
                Assert.That(delivery.Date, Is.EqualTo(expectedDate));
                Assert.That(delivery.Features, Does.Not.Contain(untrackedFeatureId));
                Assert.That(delivery.Features, Has.Count.EqualTo(1));
            }
        }

        private void ThenTheDeliveryIsItsOwnAgainCarryingWhatTheReleaseLeft(DeliveryRow delivery, DateTime expectedDate)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
                Assert.That(delivery.SourceKey, Is.Null);
                Assert.That(delivery.SourceReference, Is.Null);
                Assert.That(delivery.Name, Is.EqualTo(TheReleaseName),
                    "the last synced name is why somebody releases a Delivery rather than deleting it.");
                Assert.That(delivery.Date, Is.EqualTo(expectedDate));
                Assert.That(delivery.Features, Does.Not.Contain(untrackedFeatureId));
                Assert.That(delivery.Features, Has.Count.EqualTo(1));
            }
        }
    }
}
