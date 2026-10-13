using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.API.Integration.TeamFeatureSize
{
    /// <summary>
    /// Step definitions for slice 00. Every answer is read off the wire as the dashboard reads it.
    /// </summary>
    public partial class Slice00OwnerScopedMetricsTest : TeamFeatureSizeAcceptanceTest
    {
        private const int TeamThroughput = 3;
        private const int PortfolioThroughput = 5;

        // Cycle times count both ends: the Team's Work Items took 2, 3 and 4 days, and the median of
        // three is the first of them. Every Feature took 21 days.
        private const int TeamMedianCycleTime = 2;
        private const int PortfolioMedianCycleTime = 21;

        private static readonly DateTime PortfolioWindowStart = WindowEnd.AddDays(-59);

        private readonly Dictionary<Owner, int> answers = [];

        private int teamId;
        private int portfolioId;

        public enum Owner
        {
            Team,
            Portfolio,
        }

        public enum RememberedMetric
        {
            TotalThroughput,
            CycleTimePercentiles,
        }

        // --- Given ---

        private void GivenATeamAndAPortfolioWithTheSameNumber()
        {
            teamId = TeamNamed(Gravity);
            portfolioId = PortfolioNamed(OceanExplorer);
        }

        private void GivenATeamWhoseNumberNoPortfolioShares()
        {
            TeamNamed(Gravity);
            portfolioId = PortfolioNamed(OceanExplorer);
            teamId = TeamNamed(Zenith);
        }

        private void GivenTheTeamFinishedThreeWorkItemsAndThePortfolioClosedFiveFeatures()
        {
            SeedFinishedWorkItem(teamId, closedDaysAgo: 3, cycleTimeInDays: 2);
            SeedFinishedWorkItem(teamId, closedDaysAgo: 4, cycleTimeInDays: 3);
            SeedFinishedWorkItem(teamId, closedDaysAgo: 6, cycleTimeInDays: 4);

            for (var feature = 1; feature <= PortfolioThroughput; feature++)
            {
                SeedFeature($"OE-10{feature}", StateCategories.Done, [portfolioId], [], closedDaysAgo: feature + 1);
            }
        }

        // --- When ---

        private async Task WhenBothAreAskedFor(RememberedMetric metric, Owner askedFirst)
        {
            var askedSecond = askedFirst == Owner.Team ? Owner.Portfolio : Owner.Team;

            answers[askedFirst] = await TheAnswerOf(askedFirst, metric);
            answers[askedSecond] = await TheAnswerOf(askedSecond, metric);
        }

        private async Task WhenEachShowsItsTotalThroughputForItsOwnWindow()
        {
            answers[Owner.Team] = await TheTeamsTotalThroughput();
            answers[Owner.Portfolio] = await ThePortfoliosTotalThroughput();
        }

        private void GivenEachHasFinishedOneMore()
        {
            SeedFinishedWorkItem(teamId, closedDaysAgo: 2, cycleTimeInDays: 2);
            SeedFeature("OE-199", StateCategories.Done, [portfolioId], [], closedDaysAgo: 2);
        }

        private void WhenOnlyThisOneIsRefreshed(Owner refreshed)
        {
            if (refreshed == Owner.Team)
            {
                RefreshTheTeam(teamId);
            }
            else
            {
                RefreshThePortfolio(portfolioId);
            }
        }

        // --- Then ---

        /// <summary>
        /// The first Team and the first Portfolio of a fresh instance are both number 1. If that ever
        /// stopped holding here, the collision scenarios would pass without testing anything.
        /// </summary>
        private void ThenTheTeamAndThePortfolioShareANumber()
        {
            Assert.That(portfolioId, Is.EqualTo(teamId),
                "The scenario needs a Team and a Portfolio that share a number, as every instance's first two do.");
        }

        private void ThenTheTeamsNumberIsNotThePortfolios()
        {
            Assert.That(teamId, Is.Not.EqualTo(portfolioId),
                "The scenario needs a Team whose number the only Portfolio does not have.");
        }

        private void ThenEachSawItsOwn(RememberedMetric metric)
        {
            var (team, portfolio) = metric == RememberedMetric.TotalThroughput
                ? (TeamThroughput, PortfolioThroughput)
                : (TeamMedianCycleTime, PortfolioMedianCycleTime);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(answers[Owner.Team], Is.EqualTo(team), $"The Team's {metric}");
                Assert.That(answers[Owner.Portfolio], Is.EqualTo(portfolio), $"The Portfolio's {metric}");
            }
        }

        private void ThenEachShowedWhatItHadFinished()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(answers[Owner.Team], Is.EqualTo(TeamThroughput),
                    "The Team should first show the 3 Work Items it finished.");
                Assert.That(answers[Owner.Portfolio], Is.EqualTo(PortfolioThroughput),
                    "The Portfolio should first show the 5 Features it closed.");
            }
        }

        private async Task ThenTheRefreshedOneCountsTheNewWorkAndTheOtherStillShowsWhatItRemembered(Owner refreshed)
        {
            var team = await TheTeamsTotalThroughput();
            var portfolio = await ThePortfoliosTotalThroughput();

            var (expectedTeam, expectedPortfolio) = refreshed == Owner.Team
                ? (TeamThroughput + 1, PortfolioThroughput)
                : (TeamThroughput, PortfolioThroughput + 1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team, Is.EqualTo(expectedTeam),
                    refreshed == Owner.Team
                        ? "The refreshed Team should count the Work Item it just finished."
                        : "Refreshing the Portfolio should not have made the Team forget what it remembered.");
                Assert.That(portfolio, Is.EqualTo(expectedPortfolio),
                    refreshed == Owner.Portfolio
                        ? "The refreshed Portfolio should count the Feature it just closed."
                        : "Refreshing the Team should not have made the Portfolio forget what it remembered.");
            }
        }

        // --- Helpers ---

        private Task<int> TheAnswerOf(Owner owner, RememberedMetric metric) => (owner, metric) switch
        {
            (Owner.Team, RememberedMetric.TotalThroughput) => TheTeamsTotalThroughput(),
            (Owner.Portfolio, RememberedMetric.TotalThroughput) => TotalThroughputAt(PortfolioMetric(portfolioId, "throughputInfo", WindowStart, WindowEnd)),
            (Owner.Team, _) => MedianCycleTimeAt(TeamMetric(teamId, "cycleTimePercentiles", WindowStart, WindowEnd)),
            _ => MedianCycleTimeAt(PortfolioMetric(portfolioId, "cycleTimePercentiles", WindowStart, WindowEnd)),
        };

        private Task<int> TheTeamsTotalThroughput()
            => TotalThroughputAt(TeamMetric(teamId, "throughputInfo", WindowStart, WindowEnd));

        private Task<int> ThePortfoliosTotalThroughput()
            => TotalThroughputAt(PortfolioMetric(portfolioId, "throughputInfo", PortfolioWindowStart, WindowEnd));

        private async Task<int> TotalThroughputAt(Uri route)
        {
            Client.AsSystemAdmin();
            await Ask(route);
            ThenTheAnswerArrived();

            using var document = JsonDocument.Parse(Body);
            return document.RootElement.GetProperty("total").GetInt32();
        }

        private async Task<int> MedianCycleTimeAt(Uri route)
        {
            Client.AsSystemAdmin();
            await Ask(route);
            ThenTheAnswerArrived();

            using var document = JsonDocument.Parse(Body);
            return document.RootElement
                .EnumerateArray()
                .Single(percentile => percentile.GetProperty("percentile").GetInt32() == 50)
                .GetProperty("value")
                .GetInt32();
        }
    }
}
