using Lighthouse.Backend.Models;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.PercentilesOverTime
{
    /// <summary>
    /// DELIVER acceptance scenarios for Slice 03b (US-06, ADO #5564) — the dashboard date range applies to
    /// the two over-time series endpoints. Driving ports: the shipped <c>percentiles-over-time</c> and
    /// <c>process-behavior-over-time</c> read actions on both the team and portfolio metrics controllers,
    /// exercised over real HTTP against real EF. Covers milestone-3b Scenarios 18-21.
    /// Step definitions live in Slice03bDateRangeSpecifications.cs (same partial class).
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5427-percentiles-over-time")]
    [Category("slice-03b")]
    public partial class Slice03bDateRangeTest
    {
        [Test]
        public async Task TeamPercentiles_WindowInsideTheRecordedHistory_IsInclusiveAtBothEnds()
        {
            var teamId = GivenATeam();
            var recordedDays = GivenFiveConsecutiveRecordedDays(teamId, OwnerType.Team);

            var response = await WhenTheTeamPercentilesSeriesIsRequested(teamId, recordedDays[1], recordedDays[3]);

            ThenTheSeriesCoversExactlyTheseDays(response, [recordedDays[1], recordedDays[2], recordedDays[3]]);
        }

        [Test]
        public async Task TeamProcessBehavior_WindowInsideTheRecordedHistory_IsInclusiveAtBothEnds()
        {
            var teamId = GivenATeam();
            var recordedDays = GivenFiveConsecutiveRecordedDays(teamId, OwnerType.Team);

            var response = await WhenTheTeamProcessBehaviorSeriesIsRequested(teamId, recordedDays[1], recordedDays[3]);

            ThenTheSeriesCoversExactlyTheseDays(response, [recordedDays[1], recordedDays[2], recordedDays[3]]);
        }

        [Test]
        public async Task PortfolioPercentiles_WindowInsideTheRecordedHistory_IsInclusiveAtBothEnds()
        {
            var portfolioId = GivenAPortfolio();
            var recordedDays = GivenFiveConsecutiveRecordedDays(portfolioId, OwnerType.Portfolio);

            var response = await WhenThePortfolioPercentilesSeriesIsRequested(portfolioId, recordedDays[1], recordedDays[3]);

            ThenTheSeriesCoversExactlyTheseDays(response, [recordedDays[1], recordedDays[2], recordedDays[3]]);
        }

        [Test]
        public async Task PortfolioProcessBehavior_WindowInsideTheRecordedHistory_IsInclusiveAtBothEnds()
        {
            var portfolioId = GivenAPortfolio();
            var recordedDays = GivenFiveConsecutiveRecordedDays(portfolioId, OwnerType.Portfolio);

            var response = await WhenThePortfolioProcessBehaviorSeriesIsRequested(portfolioId, recordedDays[1], recordedDays[3]);

            ThenTheSeriesCoversExactlyTheseDays(response, [recordedDays[1], recordedDays[2], recordedDays[3]]);
        }

        [Test]
        public async Task TeamPercentiles_StartDateOnly_ReturnsEveryRecordedDayOnOrAfterIt()
        {
            var teamId = GivenATeam();
            var recordedDays = GivenFiveConsecutiveRecordedDays(teamId, OwnerType.Team);

            var response = await WhenTheTeamPercentilesSeriesIsRequested(teamId, recordedDays[3], to: null);

            ThenTheSeriesCoversExactlyTheseDays(response, [recordedDays[3], recordedDays[4]]);
        }

        [Test]
        public async Task TeamPercentiles_EndDateOnly_ReturnsEveryRecordedDayOnOrBeforeIt()
        {
            var teamId = GivenATeam();
            var recordedDays = GivenFiveConsecutiveRecordedDays(teamId, OwnerType.Team);

            var response = await WhenTheTeamPercentilesSeriesIsRequested(teamId, from: null, to: recordedDays[1]);

            ThenTheSeriesCoversExactlyTheseDays(response, [recordedDays[0], recordedDays[1]]);
        }

        [Test]
        public async Task TeamProcessBehavior_StartDateOnly_ReturnsEveryRecordedDayOnOrAfterIt()
        {
            var teamId = GivenATeam();
            var recordedDays = GivenFiveConsecutiveRecordedDays(teamId, OwnerType.Team);

            var response = await WhenTheTeamProcessBehaviorSeriesIsRequested(teamId, recordedDays[3], to: null);

            ThenTheSeriesCoversExactlyTheseDays(response, [recordedDays[3], recordedDays[4]]);
        }

        [Test]
        public async Task TeamProcessBehavior_EndDateOnly_ReturnsEveryRecordedDayOnOrBeforeIt()
        {
            var teamId = GivenATeam();
            var recordedDays = GivenFiveConsecutiveRecordedDays(teamId, OwnerType.Team);

            var response = await WhenTheTeamProcessBehaviorSeriesIsRequested(teamId, from: null, to: recordedDays[1]);

            ThenTheSeriesCoversExactlyTheseDays(response, [recordedDays[0], recordedDays[1]]);
        }

        [Test]
        public async Task TeamPercentiles_NoWindow_StillReturnsTheFullShippedHistory()
        {
            var teamId = GivenATeam();
            var recordedDays = GivenFiveConsecutiveRecordedDays(teamId, OwnerType.Team);

            var response = await WhenTheTeamPercentilesSeriesIsRequested(teamId, from: null, to: null);

            ThenTheSeriesCoversExactlyTheseDays(response, recordedDays);
        }

        [Test]
        public async Task TeamProcessBehavior_NoWindow_StillReturnsTheFullShippedHistory()
        {
            var teamId = GivenATeam();
            var recordedDays = GivenFiveConsecutiveRecordedDays(teamId, OwnerType.Team);

            var response = await WhenTheTeamProcessBehaviorSeriesIsRequested(teamId, from: null, to: null);

            ThenTheSeriesCoversExactlyTheseDays(response, recordedDays);
        }

        [Test]
        public async Task TeamPercentiles_WindowEntirelyBeforeTheRecordedHistory_ReturnsAnEmptySeries()
        {
            var teamId = GivenATeam();
            var recordedDays = GivenFiveConsecutiveRecordedDays(teamId, OwnerType.Team);

            var response = await WhenTheTeamPercentilesSeriesIsRequested(teamId, recordedDays[0].AddDays(-40), recordedDays[0].AddDays(-30));

            ThenTheSeriesIsEmpty(response);
        }

        [Test]
        public async Task TeamProcessBehavior_WindowEntirelyBeforeTheRecordedHistory_ReturnsAnEmptySeries()
        {
            var teamId = GivenATeam();
            var recordedDays = GivenFiveConsecutiveRecordedDays(teamId, OwnerType.Team);

            var response = await WhenTheTeamProcessBehaviorSeriesIsRequested(teamId, recordedDays[0].AddDays(-40), recordedDays[0].AddDays(-30));

            ThenTheSeriesIsEmpty(response);
        }

        // A window whose bounds are EQUAL is a legal single-day request, not an inverted one.
        // Without these the guard could be `>=` and nothing would notice — a caller asking for
        // one specific day would get a 400 instead of that day's row.
        [Test]
        public async Task TeamPercentiles_SingleDayWindow_ReturnsThatDayAndIsNotRejected()
        {
            var teamId = GivenATeam();
            var recordedDays = GivenFiveConsecutiveRecordedDays(teamId, OwnerType.Team);

            var response = await WhenTheTeamPercentilesSeriesIsRequested(teamId, recordedDays[2], recordedDays[2]);

            ThenTheSeriesCoversExactlyTheseDays(response, [recordedDays[2]]);
        }

        [Test]
        public async Task TeamProcessBehavior_SingleDayWindow_ReturnsThatDayAndIsNotRejected()
        {
            var teamId = GivenATeam();
            var recordedDays = GivenFiveConsecutiveRecordedDays(teamId, OwnerType.Team);

            var response = await WhenTheTeamProcessBehaviorSeriesIsRequested(teamId, recordedDays[2], recordedDays[2]);

            ThenTheSeriesCoversExactlyTheseDays(response, [recordedDays[2]]);
        }

        [Test]
        public async Task PortfolioPercentiles_SingleDayWindow_ReturnsThatDayAndIsNotRejected()
        {
            var portfolioId = GivenAPortfolio();
            var recordedDays = GivenFiveConsecutiveRecordedDays(portfolioId, OwnerType.Portfolio);

            var response = await WhenThePortfolioPercentilesSeriesIsRequested(portfolioId, recordedDays[2], recordedDays[2]);

            ThenTheSeriesCoversExactlyTheseDays(response, [recordedDays[2]]);
        }

        [Test]
        public async Task PortfolioProcessBehavior_SingleDayWindow_ReturnsThatDayAndIsNotRejected()
        {
            var portfolioId = GivenAPortfolio();
            var recordedDays = GivenFiveConsecutiveRecordedDays(portfolioId, OwnerType.Portfolio);

            var response = await WhenThePortfolioProcessBehaviorSeriesIsRequested(portfolioId, recordedDays[2], recordedDays[2]);

            ThenTheSeriesCoversExactlyTheseDays(response, [recordedDays[2]]);
        }

        // A LONE bound has nothing to be inverted against, so it must never trip the guard.
        // Asserted on portfolio scope too: the two controllers carry the guard separately and
        // could drift, and the team-only versions above leave the portfolio copy unpinned.
        [Test]
        public async Task PortfolioPercentiles_StartDateOnly_ReturnsEveryRecordedDayOnOrAfterIt()
        {
            var portfolioId = GivenAPortfolio();
            var recordedDays = GivenFiveConsecutiveRecordedDays(portfolioId, OwnerType.Portfolio);

            var response = await WhenThePortfolioPercentilesSeriesIsRequested(portfolioId, recordedDays[3], to: null);

            ThenTheSeriesCoversExactlyTheseDays(response, [recordedDays[3], recordedDays[4]]);
        }

        [Test]
        public async Task PortfolioProcessBehavior_EndDateOnly_ReturnsEveryRecordedDayOnOrBeforeIt()
        {
            var portfolioId = GivenAPortfolio();
            var recordedDays = GivenFiveConsecutiveRecordedDays(portfolioId, OwnerType.Portfolio);

            var response = await WhenThePortfolioProcessBehaviorSeriesIsRequested(portfolioId, from: null, to: recordedDays[1]);

            ThenTheSeriesCoversExactlyTheseDays(response, [recordedDays[0], recordedDays[1]]);
        }

        [Test]
        public async Task TeamPercentiles_InvertedWindow_IsRejected()
        {
            var teamId = GivenATeam();
            var recordedDays = GivenFiveConsecutiveRecordedDays(teamId, OwnerType.Team);

            var response = await WhenTheTeamPercentilesSeriesIsRequested(teamId, recordedDays[3], recordedDays[1]);

            ThenTheWindowIsRejectedAsInverted(response);
        }

        [Test]
        public async Task TeamProcessBehavior_InvertedWindow_IsRejected()
        {
            var teamId = GivenATeam();
            var recordedDays = GivenFiveConsecutiveRecordedDays(teamId, OwnerType.Team);

            var response = await WhenTheTeamProcessBehaviorSeriesIsRequested(teamId, recordedDays[3], recordedDays[1]);

            ThenTheWindowIsRejectedAsInverted(response);
        }

        [Test]
        public async Task PortfolioPercentiles_InvertedWindow_IsRejected()
        {
            var portfolioId = GivenAPortfolio();
            var recordedDays = GivenFiveConsecutiveRecordedDays(portfolioId, OwnerType.Portfolio);

            var response = await WhenThePortfolioPercentilesSeriesIsRequested(portfolioId, recordedDays[3], recordedDays[1]);

            ThenTheWindowIsRejectedAsInverted(response);
        }

        [Test]
        public async Task PortfolioProcessBehavior_InvertedWindow_IsRejected()
        {
            var portfolioId = GivenAPortfolio();
            var recordedDays = GivenFiveConsecutiveRecordedDays(portfolioId, OwnerType.Portfolio);

            var response = await WhenThePortfolioProcessBehaviorSeriesIsRequested(portfolioId, recordedDays[3], recordedDays[1]);

            ThenTheWindowIsRejectedAsInverted(response);
        }
    }
}
