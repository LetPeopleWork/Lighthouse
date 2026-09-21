using System.Globalization;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    /// <summary>
    /// Writing back when work on a Feature is expected to begin, so a roadmap bar gets both of its ends
    /// from measured flow.
    ///
    /// The completion sources these sit beside answer one question - what does the simulation say. The
    /// start sources answer two, because a Feature somebody has already started has a real start date
    /// and no need of a forecast. Which one is given is the Feature's decision, not this service's:
    /// the table and the write-back both read it from the same place, so a Jira plan cannot end up
    /// starting on a forecast date while the table beside it shows the day work really began.
    /// </summary>
    public partial class WriteBackTriggerServiceTest
    {
        [Test]
        public void ResolveForecastWriteBackForPortfolio_StartPercentile_WritesTheDayWorkIsExpectedToBegin()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastedStartPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Start85", WriteBackTargetValueType.Date));

            var team = new Team { Id = 1, Name = "Team 1" };
            portfolio.Features.Add(CreateFeatureExpectedToStartIn("F-30", team, workingDaysUntilStart: 6, daysAt85: 20));

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            // Six working days on from the fixed clock of 2026-03-10, with no blackout days configured.
            AssertPlanned(plan, updates =>
                        updates.Count == 1 &&
                        updates[0].WorkItemId == "F-30" &&
                        updates[0].TargetFieldReference == "Custom.Start85" &&
                        updates[0].Value == "2026-03-16");
        }

        [Test]
        public void ResolveForecastWriteBackForPortfolio_StartAsFormattedText_UsesTheSameDateFormatCompletionDoes()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastedStartPercentile50, WriteBackAppliesTo.Portfolio, "Custom.Start50", WriteBackTargetValueType.FormattedText, "MM/dd/yyyy"));

            var team = new Team { Id = 1, Name = "Team 1" };
            portfolio.Features.Add(CreateFeatureExpectedToStartIn("F-31", team, workingDaysUntilStart: 4, daysAt50: 12));

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            AssertPlanned(plan, updates =>
                        updates.Count == 1 &&
                        updates[0].Value == "03/14/2026");
        }

        /// <summary>
        /// AC-3.3. The day work began is a fact, and a fact outranks a forecast. Writing the simulation's
        /// answer here would move the left end of a roadmap bar off the day it actually started, onto a
        /// guess about a day that has already passed.
        /// </summary>
        [Test]
        public void ResolveForecastWriteBackForPortfolio_StartedFeature_WritesTheDayItActuallyBegan()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastedStartPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Start85", WriteBackTargetValueType.Date));

            var team = new Team { Id = 1, Name = "Team 1" };

            portfolio.Features.Add(AStartedFeature("F-32", team, new DateTime(2026, 2, 17, 9, 0, 0, DateTimeKind.Utc)));

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            AssertPlanned(plan, updates =>
                        updates.Count == 1 &&
                        updates[0].WorkItemId == "F-32" &&
                        updates[0].Value == "2026-02-17");
        }

        /// <summary>
        /// Bug #5567 again, at a new boundary. The stored instant is one day in UTC and the next one in the
        /// instance zone, and the instance zone is the one the screen answers in - so sending the tracker the
        /// UTC day would put the roadmap bar one day off the date Lighthouse itself displays.
        ///
        /// Both directions are covered because an off-by-one that only moves one way is usually a sign the
        /// conversion was applied in the wrong place rather than not at all. Expectations are literal: deriving
        /// them would let the test agree with whatever the code does.
        /// </summary>
        [Test]
        [TestCase("Europe/Zurich", "2026-02-17T23:30:00", "2026-02-18", TestName = "AheadOfUtc_TheDayHasAlreadyTurnedLocally")]
        [TestCase("America/Los_Angeles", "2026-02-18T03:00:00", "2026-02-17", TestName = "BehindUtc_TheDayHasNotTurnedYetLocally")]
        public void ResolveForecastWriteBackForPortfolio_StartedFeature_WritesTheDayInTheInstanceZone(
            string zoneId, string startedAtUtc, string expectedDay)
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastedStartPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Start85", WriteBackTargetValueType.Date));

            var team = new Team { Id = 1, Name = "Team 1" };
            var startedOn = DateTime.SpecifyKind(DateTime.Parse(startedAtUtc, CultureInfo.InvariantCulture), DateTimeKind.Utc);

            portfolio.Features.Add(AStartedFeature("F-37", team, startedOn));

            var subject = CreateSubject(TimeZoneInfo.FindSystemTimeZoneById(zoneId));

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            AssertPlanned(plan, updates => updates.Count == 1 && updates[0].Value == expectedDay);
        }

        /// <summary>
        /// A Feature that finished without Lighthouse ever having recorded the day it began. There is no
        /// fact to send, and a forecast of a start that has already happened would be false, so nothing is
        /// written rather than something harmless-looking: a board has no way to say "no answer", and a
        /// field that keeps being rewritten is a field nobody trusts.
        /// </summary>
        [Test]
        public void ResolveForecastWriteBackForPortfolio_ClosedFeatureThatNeverRecordedAStart_WritesNothing()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastedStartPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Start85", WriteBackTargetValueType.Date));

            var team = new Team { Id = 1, Name = "Team 1" };

            // No StartedDate: the helper leaves it unset, and that absence is the whole subject here.
            var done = CreateFeatureExpectedToStartIn("F-33", team, workingDaysUntilStart: 6, daysAt85: 20);
            done.StateCategory = StateCategories.Done;
            done.ClosedDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            var open = CreateFeatureExpectedToStartIn("F-34", team, workingDaysUntilStart: 6, daysAt85: 20);

            portfolio.Features.Add(done);
            portfolio.Features.Add(open);

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            // The open Feature is here so this cannot pass because nothing resolved at all.
            AssertPlanned(plan, updates =>
                        updates.Count == 1 &&
                        updates[0].WorkItemId == "F-34");
        }

        /// <summary>
        /// A Feature that was started and then closed still knows the day work began, and that day is a
        /// fact worth sending. Staying silent is not the neutral choice it looks like: a resolution of
        /// nothing is dropped rather than written as blank, so the forecast that was sent while the
        /// Feature was still open stays in the tracker for good - leaving finished work permanently
        /// labelled as starting on a day that never came.
        /// </summary>
        [Test]
        public void ResolveForecastWriteBackForPortfolio_ClosedFeatureThatHasStarted_WritesTheDayItBegan()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastedStartPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Start85", WriteBackTargetValueType.Date));

            var team = new Team { Id = 1, Name = "Team 1" };

            var closed = AStartedFeature("F-41", team, new DateTime(2026, 2, 17, 9, 0, 0, DateTimeKind.Utc));
            closed.StateCategory = StateCategories.Done;
            closed.ClosedDate = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

            // An open Feature alongside, so the assertion cannot pass because nothing resolved at all.
            var open = CreateFeatureExpectedToStartIn("F-42", team, workingDaysUntilStart: 6, daysAt85: 20);

            portfolio.Features.Add(closed);
            portfolio.Features.Add(open);

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            AssertPlanned(plan, updates =>
                        updates.Count == 2 &&
                        updates.Any(u => u.WorkItemId == "F-41" && u.Value == "2026-02-17") &&
                        updates.Any(u => u.WorkItemId == "F-42"));
        }

        /// <summary>
        /// AC-3.5. A Feature nothing can be said about writes nothing, in both of the ways nothing can be
        /// said: no start rows at all, and start rows with no runs behind them.
        ///
        /// The second is the one worth having. Every percentile off an empty distribution reads as day zero,
        /// and day zero projects to today - so without the guard the field fills with today's date, in the
        /// same shape a real answer arrives in, and nobody can tell the two apart. A fixture with no rows at
        /// all never reaches that code, so on its own it proves nothing about it.
        /// </summary>
        [Test]
        public void ResolveForecastWriteBackForPortfolio_NothingToSayAboutTheStart_WritesNothingRatherThanToday()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastedStartPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Start85", WriteBackTargetValueType.Date));

            var team = new Team { Id = 1, Name = "Team 1" };

            // A completion forecast but no start one: the state a Feature is in between the release that
            // added start rows and the first run that fills them.
            portfolio.Features.Add(CreateFeatureWithForecast("F-35", StateCategories.ToDo, team, daysAt85: 20));

            // Start rows that exist and were never run - an empty histogram, whose every percentile is day
            // zero, which projects to today.
            var neverRun = CreateFeatureWithForecast("F-38", StateCategories.ToDo, team, daysAt85: 20);
            neverRun.SetStartForecasts([new StartForecast(new Dictionary<int, int>())]);
            portfolio.Features.Add(neverRun);

            // A Feature that does have an answer, so neither of the above can pass by nothing resolving.
            portfolio.Features.Add(CreateFeatureExpectedToStartIn("F-39", team, workingDaysUntilStart: 6, daysAt85: 20));

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            AssertPlanned(plan, updates =>
                        updates.Count == 1 &&
                        updates[0].WorkItemId == "F-39");
        }

        /// <summary>
        /// A mapping nobody can resolve costs only itself. The round is caught per portfolio one level up,
        /// so before this a single unusable mapping returned nothing at all - and the completion date that
        /// had been arriving for months simply stopped, with a log line as the only evidence.
        ///
        /// An unusable DateFormat is the reachable way in: the box takes free text. The validator now
        /// refuses one, so this covers the mappings already stored before it did.
        /// </summary>
        [Test]
        public void ResolveForecastWriteBackForPortfolio_OneMappingCannotBeResolved_StillWritesTheOthers()
        {
            var portfolio = CreatePortfolioWithFeatures();

            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastedStartPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Broken", WriteBackTargetValueType.FormattedText, "dd 'of MMM"));

            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Forecast85", WriteBackTargetValueType.Date));

            var team = new Team { Id = 1, Name = "Team 1" };
            portfolio.Features.Add(CreateFeatureExpectedToStartIn("F-40", team, workingDaysUntilStart: 6, daysAt85: 14));

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            AssertPlanned(plan, updates =>
                        updates.Count == 1 &&
                        updates[0].TargetFieldReference == "Custom.Forecast85" &&
                        updates[0].Value == "2026-03-24");
        }

        private static Feature AStartedFeature(string referenceId, Team team, DateTime startedOn)
        {
            // It carries a start forecast too, so that writing the observed day is the resolver preferring a
            // fact over a forecast rather than it having nothing else to write.
            var feature = CreateFeatureExpectedToStartIn(referenceId, team, workingDaysUntilStart: 6, daysAt85: 20);

            feature.StateCategory = StateCategories.Doing;
            feature.StartedDate = startedOn;

            return feature;
        }

        /// <summary>
        /// A Feature the run has recorded a start for. The completion forecast is not decoration: without
        /// one the team counts as unforecastable, which blanks the Feature's start as well, and the test
        /// would then pass for a reason it never meant to assert.
        /// </summary>
        private static Feature CreateFeatureExpectedToStartIn(
            string referenceId, Team team, int workingDaysUntilStart,
            int daysAt50 = -1, int daysAt70 = -1, int daysAt85 = -1, int daysAt95 = -1)
        {
            var feature = CreateFeatureWithForecast(referenceId, StateCategories.ToDo, team, daysAt50, daysAt70, daysAt85, daysAt95);

            // Every trial began work on the same day, so the answer is that day at every percentile and
            // the test does not depend on which one the mapping happens to name.
            feature.SetStartForecasts([new StartForecast(new Dictionary<int, int> { { workingDaysUntilStart, 100 } })]);

            return feature;
        }
    }
}
