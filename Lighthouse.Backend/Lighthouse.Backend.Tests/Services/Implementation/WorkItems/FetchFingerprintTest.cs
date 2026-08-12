using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    /// <summary>
    /// Epic #5687 AC-5.1 / ADR-140. The fingerprint decides whether the next cycle downloads everything,
    /// so the two failure modes it has to be tested against are opposites: too sensitive means every
    /// cycle is full again and the epic bought nothing, too blunt means delta serves stale records with
    /// every other test still green.
    /// </summary>
    public class FetchFingerprintTest
    {
        [Test]
        public void For_ATeam_ReturnsAFingerprint()
        {
            var fingerprint = FetchFingerprint.For(ATeam());

            Assert.That(fingerprint, Is.Not.Empty);
        }

        [Test]
        public void For_APortfolio_ReturnsAFingerprint()
        {
            var fingerprint = FetchFingerprint.For(APortfolio());

            Assert.That(fingerprint, Is.Not.Empty);
        }

        /// <summary>
        /// The column is <c>string?</c> with no declared length, so the output has to be a digest rather
        /// than the rendered input - a portfolio with a long query would otherwise write an unbounded row.
        /// </summary>
        [Test]
        public void For_AnOwnerWithAVerboseQuery_StaysAsShortAsAnyOtherOwner()
        {
            var terse = APortfolio();
            var verbose = APortfolio();
            verbose.DataRetrievalValue = new string('x', 5000);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(FetchFingerprint.For(verbose), Has.Length.EqualTo(FetchFingerprint.For(terse).Length));
                Assert.That(FetchFingerprint.For(verbose), Has.Length.LessThanOrEqualTo(64));
            }
        }

        /// <summary>
        /// Two structurally identical owners are the same fetch. A digest over object identity would pass
        /// every "changes the hash" case below and fail only here.
        /// </summary>
        [Test]
        public void For_TwoSeparatelyBuiltButIdenticalOwners_ProducesTheSameFingerprint()
        {
            var onePortfolio = APortfolio();
            var anIdenticalPortfolio = APortfolio();

            Assert.That(FetchFingerprint.For(onePortfolio), Is.EqualTo(FetchFingerprint.For(anIdenticalPortfolio)));
        }

        /// <summary>
        /// ADR-140: order carries no meaning in any of these collections, and EF hands them back in
        /// whatever order the query produced - so an order-sensitive hash would make cycles full at random.
        /// </summary>
        [Test]
        public void For_EveryCollectionReversed_ProducesTheSameFingerprint()
        {
            Assert.That(
                FetchFingerprint.For(APortfolio(everyCollectionReversed: true)),
                Is.EqualTo(FetchFingerprint.For(APortfolio())));
        }

        [TestCaseSource(nameof(EveryRegisteredProperty))]
        public void For_ARegisteredPropertyChanges_ProducesADifferentFingerprint(string property, Action<Portfolio> anOperatorEdit)
        {
            var portfolio = APortfolio();
            var before = FetchFingerprint.For(portfolio);

            anOperatorEdit(portfolio);

            Assert.That(FetchFingerprint.For(portfolio), Is.Not.EqualTo(before), $"{property} shapes a fetch but the fingerprint did not move.");
        }

        /// <summary>
        /// Keeps the table below honest against the registry the guard test pins: a property registered
        /// but never exercised here is a property nothing proves the hash actually reads.
        /// </summary>
        [Test]
        public void EveryRegisteredPropertyHasACaseInThisFile()
        {
            var exercised = EveryRegisteredProperty().Select(testCase => (string)testCase.Arguments[0]!);

            Assert.That(exercised, Is.EquivalentTo(FetchFingerprint.RegisteredProperties));
        }

        [TestCaseSource(nameof(ARepresentativeOfEveryExcludedGroup))]
        public void For_AnExcludedPropertyChanges_ProducesTheSameFingerprint(string property, Action<Portfolio> anOperatorEdit)
        {
            var portfolio = APortfolio();
            var before = FetchFingerprint.For(portfolio);

            anOperatorEdit(portfolio);

            Assert.That(FetchFingerprint.For(portfolio), Is.EqualTo(before), $"{property} costs nothing remote, so hashing it makes every cycle full for free.");
        }

        [Test]
        public void For_ATeamOnlyExcludedPropertyChanges_ProducesTheSameFingerprint()
        {
            var team = ATeam();
            var before = FetchFingerprint.For(team);

            team.FeatureWIP = 42;
            team.ForecastFilterRuleSetJson = "{}";

            Assert.That(FetchFingerprint.For(team), Is.EqualTo(before));
        }

        /// <summary>
        /// The function reads the connection the owner points at, not only the owner: field definitions
        /// are connection-scoped, so nothing on the portfolio moves when an operator adds one.
        /// </summary>
        [Test]
        public void For_AFieldDefinitionAddedToTheConnection_ProducesADifferentFingerprintWithoutTouchingTheOwner()
        {
            var portfolio = APortfolio();
            var before = FetchFingerprint.For(portfolio);

            portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(
                new AdditionalFieldDefinition { Id = 9, DisplayName = "Risk", Reference = "customfield_10009" });

            Assert.That(FetchFingerprint.For(portfolio), Is.Not.EqualTo(before));
        }

        /// <summary>
        /// An owner whose connection was never loaded still has to hash. The connection is a navigation
        /// property, so any query that does not include it hands back null - and throwing there would take
        /// the whole update cycle down instead of costing one full fetch.
        /// </summary>
        [Test]
        public void For_AnOwnerWhoseConnectionWasNotLoaded_StillProducesAFingerprintAndADifferentOne()
        {
            var portfolio = APortfolio();
            var withTheConnectionLoaded = FetchFingerprint.For(portfolio);

            portfolio.WorkTrackingSystemConnection = null!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(FetchFingerprint.For(portfolio), Is.Not.Empty);
                Assert.That(FetchFingerprint.For(portfolio), Is.Not.EqualTo(withTheConnectionLoaded));
            }
        }

        /// <summary>No repository, no clock, no service graph to stand up.</summary>
        [Test]
        public void FetchFingerprintIsAPureStatic()
        {
            var type = typeof(FetchFingerprint);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(type.IsAbstract && type.IsSealed, Is.True, "FetchFingerprint must stay a static class.");
                Assert.That(type.GetMethod(nameof(FetchFingerprint.For))!.IsStatic, Is.True);
            }
        }

        private static IEnumerable<TestCaseData> EveryRegisteredProperty()
        {
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.DataRetrievalValue), p => p.DataRetrievalValue = "project = SOMETHING-ELSE");
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.WorkItemTypes), p => p.WorkItemTypes = ["Epic"]);
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.DoneItemsCutoffDays), p => p.DoneItemsCutoffDays = 90);
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.ToDoStates), p => p.ToDoStates = ["New", "Proposed", "Backlog"]);
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.DoingStates), p => p.DoingStates = ["Active"]);
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.DoneStates), p => p.DoneStates = ["Done"]);
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.StateMappings), p => p.StateMappings[0].States.Add("Testing"));
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.ParentOverrideAdditionalFieldDefinitionId), p => p.ParentOverrideAdditionalFieldDefinitionId = 2);
            yield return Case(nameof(Portfolio.FeatureOwnerAdditionalFieldDefinitionId), p => p.FeatureOwnerAdditionalFieldDefinitionId = null);
            yield return Case(nameof(Portfolio.SizeEstimateAdditionalFieldDefinitionId), p => p.SizeEstimateAdditionalFieldDefinitionId = 3);
            yield return Case(nameof(WorkTrackingSystemConnection.AdditionalFieldDefinitions), p => p.WorkTrackingSystemConnection.AdditionalFieldDefinitions[0].Reference = "customfield_99999");
            yield return Case(nameof(WorkTrackingSystemConnection.WorkTrackingSystem), p => p.WorkTrackingSystemConnection.WorkTrackingSystem = WorkTrackingSystems.AzureDevOps);
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.WorkTrackingSystemConnectionId), p => p.WorkTrackingSystemConnectionId = 8);
        }

        private static IEnumerable<TestCaseData> ARepresentativeOfEveryExcludedGroup()
        {
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.Name), p => p.Name = "Renamed");
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.UpdateTime), p => p.RefreshUpdateTime());
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.FetchFingerprint), p => p.FetchFingerprint = "whatever the last cycle wrote");
            yield return Case(nameof(WorkTrackingSystemConnection.AuthenticationMethodKey), p => p.WorkTrackingSystemConnection.AuthenticationMethodKey = "RotatedToken");
            yield return Case(nameof(WorkTrackingSystemConnection.Options), p => p.WorkTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "ApiToken", Value = "rotated" }));
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.StalenessThresholdDays), p => p.StalenessThresholdDays = 14);
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.BlockedRuleSetJson), p => p.BlockedRuleSetJson = "{\"any\":true}");
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.WaitStates), p => p.WaitStates = ["Review"]);
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.CycleTimeDefinitions), p => p.CycleTimeDefinitions.Add(new CycleTimeDefinition { Name = "Dev", StartState = "Active", EndState = "Done" }));
            yield return Case(nameof(WorkTrackingSystemOptionsOwner.EstimationAdditionalFieldDefinitionId), p => p.EstimationAdditionalFieldDefinitionId = 3);
            yield return Case(nameof(Portfolio.DefaultAmountOfWorkItemsPerFeature), p => p.DefaultAmountOfWorkItemsPerFeature = 99);
            yield return Case(nameof(Portfolio.OwningTeamId), p => p.OwningTeamId = 5);
        }

        private static TestCaseData Case(string property, Action<Portfolio> anOperatorEdit)
            => new TestCaseData(property, anOperatorEdit).SetArgDisplayNames(property);

        private static Team ATeam()
            => new()
            {
                Id = 4,
                Name = "Team",
                DataRetrievalValue = "project = LH AND type = Story",
                WorkItemTypes = ["User Story", "Bug"],
                WorkTrackingSystemConnectionId = 7,
                WorkTrackingSystemConnection = AConnection(),
            };

        private static Portfolio APortfolio(bool everyCollectionReversed = false)
        {
            IEnumerable<T> InOperatorOrder<T>(params T[] items) => everyCollectionReversed ? items.Reverse() : items;

            var connection = AConnection();
            connection.AdditionalFieldDefinitions.AddRange(InOperatorOrder(
                new AdditionalFieldDefinition { Id = 1, DisplayName = "Owner", Reference = "customfield_10001" },
                new AdditionalFieldDefinition { Id = 2, DisplayName = "Size", Reference = "customfield_10002" }));

            return new Portfolio
            {
                Id = 3,
                Name = "Portfolio",
                DataRetrievalValue = "project = LH AND type = Epic",
                WorkItemTypes = [.. InOperatorOrder("Epic", "Feature")],
                ToDoStates = [.. InOperatorOrder("New", "Proposed")],
                DoingStates = [.. InOperatorOrder("Active", "Committed")],
                DoneStates = [.. InOperatorOrder("Done", "Closed")],
                StateMappings =
                [
                    .. InOperatorOrder(
                        new StateMapping { Name = "Active", States = [.. InOperatorOrder("In Progress", "Review")] },
                        new StateMapping { Name = "Done", States = [.. InOperatorOrder("Released", "Shipped")] }),
                ],
                DoneItemsCutoffDays = 365,
                ParentOverrideAdditionalFieldDefinitionId = 1,
                FeatureOwnerAdditionalFieldDefinitionId = 1,
                SizeEstimateAdditionalFieldDefinitionId = 2,
                WorkTrackingSystemConnectionId = 7,
                WorkTrackingSystemConnection = connection,
            };
        }

        private static WorkTrackingSystemConnection AConnection()
            => new() { Id = 7, Name = "Tracker", WorkTrackingSystem = WorkTrackingSystems.Jira };
    }
}
