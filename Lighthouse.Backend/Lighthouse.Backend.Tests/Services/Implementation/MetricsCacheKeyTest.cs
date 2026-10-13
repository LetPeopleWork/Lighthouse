using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// Teams and Portfolios are numbered in separate tables, so Team 3 and Portfolio 3 both exist and
    /// both have metrics remembered in the same in-process cache. Every metric below is asked of both
    /// owners under the same name, so if the owner's kind is missing from the key, one owner is served
    /// the other's numbers and clearing one owner clears the other too.
    ///
    /// Every family listed is one both owners cache under a name built from the same id and metric.
    /// A family added to both goes through the same builder, which is the point of having one.
    ///
    /// Two of the tests hold whether or not the owner's kind is part of the name: clearing an owner
    /// reaches all of its own metrics, and clearing Team 1 leaves Team 11 alone.
    /// </summary>
    [TestFixture]
    [Category("story-6251-team-feature-size")]
    [Category("slice-00")]
    [Category("us-00")]
    public class MetricsCacheKeyTest
    {
        private static readonly string[] MetricsBothOwnersRemember =
        [
            "Throughput_2026-08-18_2026-09-16",
            "StartedItems_2026-08-18_2026-09-16",
            "ForecastPredictabilityScore_2026-08-18_2026-09-16",
            "CycleTimePercentiles_2026-08-18_2026-09-16",
            "WorkItemAgePercentiles_2026-09-16",
            "NamedCycleTimePercentiles_2026-08-18_2026-09-16_Def_7",
            "AgeInStatePercentiles_2026-08-18_2026-09-16",
            "CumulativeStateTime_2026-08-18_2026-09-16",
            "FlowEfficiencyInfo_2026-08-18_2026-09-16",
            "CumulativeStateTime_Items_In Progress_2026-08-18_2026-09-16",
            "CumulativeStateTime_Candidates_2026-08-18_2026-09-16",
            "EstimationVsCycleTime_2026-08-18_2026-09-16",
            "TotalWorkItemAge_2026-09-16",
            "WipSnapshot_2026-09-16",
            "ThroughputInfo_2026-08-18_2026-09-16",
            "ArrivalsInfo_2026-08-18_2026-09-16",
            "WipOverviewInfo_2026-08-18_2026-09-16",
            "TotalWorkItemAgeInfo_2026-08-18_2026-09-16",
            "PredictabilityScoreInfo_2026-08-18_2026-09-16",
            "CycleTimePercentilesInfo_2026-08-18_2026-09-16",
            "CycleTimePercentilesInfo_2026-08-18_2026-09-16_Def_7",
        ];

        // Ids that share digits, so a prefix that forgot where the id ends would reach a neighbour.
        private static readonly int[] Ids = [1, 3, 10, 11, 42];

        [TestCaseSource(nameof(MetricsBothOwnersRemember))]
        [Category("error")]
        [Category("contract-shape:pure-function")]
        public void A_Team_and_a_Portfolio_with_the_same_id_never_share_a_remembered_metric(string metric)
        {
            foreach (var id in Ids)
            {
                Assert.That(
                    TeamKey(id, metric),
                    Is.Not.EqualTo(PortfolioKey(id, metric)),
                    $"Team {id} and Portfolio {id} would be served each other's {metric}.");
            }
        }

        [TestCaseSource(nameof(MetricsBothOwnersRemember))]
        [Category("error")]
        [Category("contract-shape:bounded-change")]
        public void Clearing_one_owner_reaches_none_of_the_other_owner_s_metrics(string metric)
        {
            foreach (var id in Ids)
            {
                var teamKey = TeamKey(id, metric);
                var portfolioKey = PortfolioKey(id, metric);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(portfolioKey, Does.Not.StartWith(TeamPrefix(id)),
                        $"Clearing Team {id} would also clear Portfolio {id}'s {metric}.");
                    Assert.That(teamKey, Does.Not.StartWith(PortfolioPrefix(id)),
                        $"Clearing Portfolio {id} would also clear Team {id}'s {metric}.");
                }
            }
        }

        [TestCaseSource(nameof(MetricsBothOwnersRemember))]
        [Category("contract-shape:unbounded-preservation")]
        public void Clearing_an_owner_reaches_every_one_of_its_own_metrics(string metric)
        {
            foreach (var id in Ids)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(TeamKey(id, metric), Does.StartWith(TeamPrefix(id)),
                        $"Clearing Team {id} would leave its {metric} behind.");
                    Assert.That(PortfolioKey(id, metric), Does.StartWith(PortfolioPrefix(id)),
                        $"Clearing Portfolio {id} would leave its {metric} behind.");
                }
            }
        }

        // Team 11's id starts with Team 1's, so a prefix that does not mark where the id ends would
        // clear both.
        [Test]
        [Category("error")]
        [Category("contract-shape:unbounded-preservation")]
        public void Clearing_one_owner_reaches_no_other_owner_of_the_same_kind()
        {
            const string metric = "ThroughputInfo_2026-08-18_2026-09-16";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TeamKey(11, metric), Does.Not.StartWith(TeamPrefix(1)));
                Assert.That(PortfolioKey(11, metric), Does.Not.StartWith(PortfolioPrefix(1)));
            }
        }

        // The owner's kind travels in the type argument, so each owner is named as its own type, the
        // way the metrics services pass it.
        private static string TeamKey(int id, string metric) => MetricsCacheKey.For<Team>(ATeam(id), metric);

        private static string PortfolioKey(int id, string metric) => MetricsCacheKey.For<Portfolio>(APortfolio(id), metric);

        private static string TeamPrefix(int id) => MetricsCacheKey.PrefixFor<Team>(ATeam(id));

        private static string PortfolioPrefix(int id) => MetricsCacheKey.PrefixFor<Portfolio>(APortfolio(id));

        private static Team ATeam(int id) => new() { Id = id, Name = $"Team {id}" };

        private static Portfolio APortfolio(int id) => new() { Id = id, Name = $"Portfolio {id}" };
    }
}
