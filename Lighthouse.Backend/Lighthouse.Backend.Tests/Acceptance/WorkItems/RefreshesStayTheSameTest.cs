using Lighthouse.Backend.Modules.FlowMetrics.Contracts;
using Lighthouse.Backend.Modules.OrganisationSetup.Contracts;
using Lighthouse.Backend.Modules.TrackerIntegration.Contracts;
using Lighthouse.Backend.Modules.WorkItems.Application.Policies;
using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Lighthouse.Backend.Platform.UpdateQueue;
using System.Globalization;
using System.Text.Json;
using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Csv;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Lighthouse.Backend.Modules.WorkItems.Domain.Dependencies;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Lighthouse.Backend.Modules.WorkItems.Application.Dependencies;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;
using Lighthouse.Backend.Platform.Host;
using Lighthouse.Backend.Modules.OrganisationSetup.Application;

namespace Lighthouse.Backend.Tests.Acceptance.WorkItems
{
    /// <summary>
    /// Moving the Work Items code to another place must not change what a refresh stores or announces. A refresh
    /// reads a tracker upload, so with the upload and the clock fixed, the same refresh stores the same Work Items
    /// and Features, the same moves between states, the same blocked periods, and announces the same happenings in
    /// the same order. All of that is written out here as plain values: a changed move, a dropped blocked period, a
    /// missing or reordered announcement, or a different stored record turns this red, whatever the code is called
    /// and wherever it lives.
    ///
    /// The refresh is asked of the Team data and Work Item services as the product builds them, over a database of
    /// the scenario's own, with no web host, reading a CSV upload the way a CSV connection does.
    /// </summary>
    [TestFixture]
    public class RefreshesStayTheSameTest
    {
        private static readonly DateTimeOffset FirstRefreshAt = new(2026, 10, 18, 9, 30, 0, TimeSpan.Zero);

        private static readonly DateTimeOffset NextRefreshAt = new(2026, 10, 19, 9, 30, 0, TimeSpan.Zero);

        private const string BlockedState = "Waiting";

        private const string TeamName = "Pinned Team";

        private const string PortfolioName = "Pinned Portfolio";

        private const string TeamUploadHeader = "ID,Name,State,Type,Started Date,Closed Date,Created Date,Parent Reference Id,Tags";

        private static readonly string TeamFirstUpload = string.Join('\n',
            TeamUploadHeader,
            "WI-1,Write the guide,To Do,User Story,,,2026-10-01,FEAT-1,",
            "WI-2,Fix the export,In Progress,Bug,2026-10-05,,2026-10-02,FEAT-1,",
            "WI-3,Wait on legal,Waiting,User Story,2026-10-06,,2026-10-03,FEAT-2,legal",
            "WI-4,Old cleanup,Done,User Story,2026-10-01,2026-10-10,2026-09-30,,");

        /// <summary>A day later: one item started, one finished, the blocked one moves on, one left and one is new.</summary>
        private static readonly string TeamNextUpload = string.Join('\n',
            TeamUploadHeader,
            "WI-1,Write the guide,In Progress,User Story,2026-10-19,,2026-10-01,FEAT-1,",
            "WI-2,Fix the export,Done,Bug,2026-10-05,2026-10-19,2026-10-02,FEAT-1,",
            "WI-3,Wait on legal,In Progress,User Story,2026-10-06,,2026-10-03,FEAT-2,legal",
            "WI-5,Add the audit trail,To Do,User Story,,,2026-10-18,FEAT-2,");

        private const string PortfolioUploadHeader = "ID,Name,State,Type,Started Date,Closed Date,Created Date,Parent Reference Id,Tags,Owning Team,Estimated Size";

        private static readonly string PortfolioFirstUpload = string.Join('\n',
            PortfolioUploadHeader,
            "FEAT-1,Self-service export,In Progress,Feature,2026-10-01,,2026-09-20,EPIC-1,,,5",
            "FEAT-2,Legal sign-off,To Do,Feature,,,2026-09-21,EPIC-1,,,3",
            "FEAT-3,Archive,Done,Feature,2026-09-01,2026-09-30,2026-08-20,,,,2");

        /// <summary>
        /// A day later: the Feature in progress waits, and the one still to do has started. A CSV upload carries no
        /// state history for Features, and the refresh stores no move for them either, so the new states land on
        /// the Features alone.
        /// </summary>
        private static readonly string PortfolioNextUpload = string.Join('\n',
            PortfolioUploadHeader,
            "FEAT-1,Self-service export,Waiting,Feature,2026-10-01,,2026-09-20,EPIC-1,,,5",
            "FEAT-2,Legal sign-off,In Progress,Feature,2026-10-19,,2026-09-21,EPIC-1,,,3",
            "FEAT-3,Archive,Done,Feature,2026-09-01,2026-09-30,2026-08-20,,,,2");

        private ServicePortHarness harness = null!;

        private FakeLighthouseClock clock = null!;

        private FakeTimeProvider timeProvider = null!;

        private EventsPublished eventsPublished = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase()
        {
            clock = new FakeLighthouseClock(FirstRefreshAt);
            timeProvider = new FakeTimeProvider(FirstRefreshAt);
            eventsPublished = new EventsPublished();
            harness = new ServicePortHarness(RegisterTheRefreshAsTheProductBuildsIt);
        }

        [TearDown]
        public void RemoveTheScenariosDatabase()
        {
            harness.Dispose();
        }

        [Test]
        public async Task A_Teams_first_refresh_stores_its_Work_Items_and_opens_the_blocked_period_of_the_waiting_one()
        {
            var teamId = GivenATeamReading(TeamFirstUpload);

            var outcome = await WhenTheTeamIsRefreshed(teamId);

            ThenTheRefreshReports(outcome, "Full sync: 4 scanned, 4 fetched");
            ThenTheTeamHolds(teamId,
                "WI-1 | Write the guide | User Story | To Do | ToDo | started - | closed - | parent FEAT-1 | tags -",
                "WI-2 | Fix the export | Bug | In Progress | Doing | started 2026-10-05 | closed - | parent FEAT-1 | tags -",
                "WI-3 | Wait on legal | User Story | Waiting | Doing | started 2026-10-06 | closed - | parent FEAT-2 | tags legal",
                "WI-4 | Old cleanup | User Story | Done | Done | started 2026-10-01 | closed 2026-10-10 | parent - | tags -");
            ThenTheWorkItemMovesStoredAre(teamId);
            ThenTheWorkItemBlockedPeriodsAre(teamId,
                "WI-3 blocked from 2026-10-18 09:30 until still blocked");
            ThenTheAnnouncementsAre(
                "WI-3 blocked: Waiting",
                "Team Pinned Team refreshed");
        }

        [Test]
        public async Task A_Teams_next_refresh_records_the_moves_closes_the_blocked_period_and_drops_what_left_the_upload()
        {
            var teamId = GivenATeamReading(TeamFirstUpload);
            await GivenTheTeamWasRefreshed(teamId);
            GivenADayLaterTheTeamReads(teamId, TeamNextUpload);

            var outcome = await WhenTheTeamIsRefreshed(teamId);

            ThenTheRefreshReports(outcome, "Full sync: 4 scanned, 4 fetched");
            ThenTheTeamHolds(teamId,
                "WI-1 | Write the guide | User Story | In Progress | Doing | started 2026-10-19 | closed - | parent FEAT-1 | tags -",
                "WI-2 | Fix the export | Bug | Done | Done | started 2026-10-05 | closed 2026-10-19 | parent FEAT-1 | tags -",
                "WI-3 | Wait on legal | User Story | In Progress | Doing | started 2026-10-06 | closed - | parent FEAT-2 | tags legal",
                "WI-5 | Add the audit trail | User Story | To Do | ToDo | started - | closed - | parent FEAT-2 | tags -");
            ThenTheWorkItemMovesStoredAre(teamId,
                "WI-1 To Do -> In Progress at 2026-10-19 09:30",
                "WI-2 In Progress -> Done at 2026-10-19 09:30",
                "WI-3 Waiting -> In Progress at 2026-10-19 09:30");
            ThenTheWorkItemBlockedPeriodsAre(teamId,
                "WI-3 blocked from 2026-10-18 09:30 until 2026-10-19 09:30");
            ThenTheAnnouncementsAre(
                "WI-1 moved from To Do to In Progress",
                "WI-2 moved from In Progress to Done",
                "WI-3 moved from Waiting to In Progress",
                "WI-3 unblocked",
                "Team Pinned Team refreshed");
        }

        [Test]
        public async Task A_Portfolios_first_refresh_stores_its_Features_under_their_parents_and_announces_nothing()
        {
            var portfolioId = GivenAPortfolioReading(PortfolioFirstUpload);

            var outcome = await WhenThePortfolioIsRefreshed(portfolioId);

            ThenTheRefreshReports(outcome, "Full sync: 3 scanned, 3 fetched");
            ThenThePortfolioHolds(portfolioId,
                "FEAT-1 | Self-service export | Feature | In Progress | Doing | started 2026-10-01 | closed - | parent EPIC-1 | size 5",
                "FEAT-2 | Legal sign-off | Feature | To Do | ToDo | started - | closed - | parent EPIC-1 | size 3",
                "FEAT-3 | Archive | Feature | Done | Done | started 2026-09-01 | closed 2026-09-30 | parent - | size 2");
            ThenTheFeatureMovesStoredAre(portfolioId);
            ThenTheFeatureBlockedPeriodsAre(portfolioId);
            ThenTheAnnouncementsAre();
        }

        [Test]
        public async Task A_Portfolios_next_refresh_stores_the_new_states_without_moves_and_opens_the_blocked_period_of_the_waiting_Feature()
        {
            var portfolioId = GivenAPortfolioReading(PortfolioFirstUpload);
            await GivenThePortfolioWasRefreshed(portfolioId);
            GivenADayLaterThePortfolioReads(portfolioId, PortfolioNextUpload);

            var outcome = await WhenThePortfolioIsRefreshed(portfolioId);

            ThenTheRefreshReports(outcome, "Full sync: 3 scanned, 3 fetched");
            ThenThePortfolioHolds(portfolioId,
                "FEAT-1 | Self-service export | Feature | Waiting | Doing | started 2026-10-01 | closed - | parent EPIC-1 | size 5",
                "FEAT-2 | Legal sign-off | Feature | In Progress | Doing | started 2026-10-19 | closed - | parent EPIC-1 | size 3",
                "FEAT-3 | Archive | Feature | Done | Done | started 2026-09-01 | closed 2026-09-30 | parent - | size 2");
            ThenTheFeatureMovesStoredAre(portfolioId);
            ThenTheFeatureBlockedPeriodsAre(portfolioId,
                "FEAT-1 blocked from 2026-10-19 09:30 until still blocked");
            ThenTheAnnouncementsAre(
                "FEAT-1 blocked in Pinned Portfolio: Waiting");
        }

        // --- Given ---

        private int GivenATeamReading(string upload)
        {
            using var scope = harness.NewScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var team = new Team
            {
                Name = TeamName,
                DataRetrievalValue = upload,
                WorkTrackingSystemConnection = ACsvConnection(),
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = ["To Do"],
                DoingStates = ["In Progress", BlockedState],
                DoneStates = ["Done"],
                BlockedRuleSetJson = BlockedWhenInState("workitem.state"),
            };
            dbContext.Teams.Add(team);
            dbContext.SaveChanges();

            return team.Id;
        }

        private int GivenAPortfolioReading(string upload)
        {
            using var scope = harness.NewScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var portfolio = new Portfolio
            {
                Name = PortfolioName,
                DataRetrievalValue = upload,
                WorkTrackingSystemConnection = ACsvConnection(),
                WorkItemTypes = ["Feature"],
                ToDoStates = ["To Do"],
                DoingStates = ["In Progress", BlockedState],
                DoneStates = ["Done"],
                BlockedRuleSetJson = BlockedWhenInState("feature.state"),
            };
            dbContext.Portfolios.Add(portfolio);
            dbContext.SaveChanges();

            return portfolio.Id;
        }

        private async Task GivenTheTeamWasRefreshed(int teamId) => await WhenTheTeamIsRefreshed(teamId);

        private async Task GivenThePortfolioWasRefreshed(int portfolioId) => await WhenThePortfolioIsRefreshed(portfolioId);

        private void GivenADayLaterTheTeamReads(int teamId, string upload)
        {
            using (var scope = harness.NewScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                dbContext.Teams.Single(team => team.Id == teamId).DataRetrievalValue = upload;
                dbContext.SaveChanges();
            }

            ADayPasses();
        }

        private void GivenADayLaterThePortfolioReads(int portfolioId, string upload)
        {
            using (var scope = harness.NewScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                dbContext.Portfolios.Single(portfolio => portfolio.Id == portfolioId).DataRetrievalValue = upload;
                dbContext.SaveChanges();
            }

            ADayPasses();
        }

        private void ADayPasses()
        {
            clock.SetInstant(NextRefreshAt);
            timeProvider.SetUtcNow(NextRefreshAt);
        }

        private static WorkTrackingSystemConnection ACsvConnection()
        {
            var connection = new WorkTrackingSystemFactory(NullLogger<WorkTrackingSystemFactory>.Instance)
                .CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);
            connection.Name = "Pinned upload";
            connection.Options.Single(option => option.Key == CsvWorkTrackingOptionNames.DateTimeFormat).Value = "yyyy-MM-dd";

            return connection;
        }

        private static string BlockedWhenInState(string stateField)
            => JsonSerializer.Serialize(new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions = [new WorkItemRuleCondition { FieldKey = stateField, Operator = RuleOperators.Equals, Value = BlockedState }],
            });

        // --- When ---

        private async Task<SyncOutcome> WhenTheTeamIsRefreshed(int teamId)
        {
            eventsPublished.StartAfresh();

            using var scope = harness.NewScope();
            var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId)!;

            return await scope.ServiceProvider.GetRequiredService<ITeamDataService>().UpdateTeamData(team);
        }

        private async Task<SyncOutcome> WhenThePortfolioIsRefreshed(int portfolioId)
        {
            eventsPublished.StartAfresh();

            using var scope = harness.NewScope();
            var portfolioRepository = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
            var portfolio = portfolioRepository.GetById(portfolioId)!;

            var outcome = await scope.ServiceProvider.GetRequiredService<IWorkItemService>().UpdateFeaturesForPortfolio(portfolio);
            await portfolioRepository.Save();

            return outcome;
        }

        // --- Then ---

        private static void ThenTheRefreshReports(SyncOutcome outcome, string expected)
        {
            var read = $"{outcome.Mode} sync: {outcome.RecordsScanned} scanned, {outcome.RecordsFetched} fetched";

            Assert.That(read, Is.EqualTo(expected),
                $"The refresh reported \"{read}\", not \"{expected}\". The upload is the same, so a refresh that reports a different sync changed how it reads the tracker; a behaviour-free move must leave the refresh result as it was. Find the change and undo it.");
        }

        private void ThenTheTeamHolds(int teamId, params string[] expected)
        {
            var read = Read(dbContext => dbContext.WorkItems.Where(item => item.TeamId == teamId).OrderBy(item => item.ReferenceId).AsEnumerable()
                .Select(item => $"{item.ReferenceId} | {item.Name} | {item.Type} | {item.State} | {item.StateCategory} | started {Day(item.StartedDate)} | closed {Day(item.ClosedDate)} | parent {OrDash(item.ParentReferenceId)} | tags {OrDash(string.Join(';', item.Tags))}")
                .ToList());

            AssertTheLines(read, expected, "Work Items the Team holds",
                "A Work Item stored differently, left out or kept after it left the upload means the refresh writes something else than before. Every field here comes from the upload, so find what changed in reading or storing it and undo it.");
        }

        private void ThenThePortfolioHolds(int portfolioId, params string[] expected)
        {
            var read = Read(dbContext => dbContext.Features.Where(feature => feature.Portfolios.Any(portfolio => portfolio.Id == portfolioId)).OrderBy(feature => feature.ReferenceId).AsEnumerable()
                .Select(feature => $"{feature.ReferenceId} | {feature.Name} | {feature.Type} | {feature.State} | {feature.StateCategory} | started {Day(feature.StartedDate)} | closed {Day(feature.ClosedDate)} | parent {OrDash(feature.ParentReferenceId)} | size {feature.EstimatedSize}")
                .ToList());

            AssertTheLines(read, expected, "Features the Portfolio holds",
                "A Feature stored differently, under another parent, or left out means the refresh writes something else than before. Every field here comes from the upload, so find what changed in reading or storing it and undo it.");
        }

        private void ThenTheWorkItemMovesStoredAre(int teamId, params string[] expected)
        {
            var read = Read(dbContext =>
                (from move in dbContext.WorkItemStateTransitions
                 join item in dbContext.WorkItems on move.WorkItemId equals item.Id
                 where item.TeamId == teamId
                 select new { item.ReferenceId, move.FromState, move.ToState, move.TransitionedAt })
                .AsEnumerable()
                .OrderBy(move => move.ReferenceId, StringComparer.Ordinal).ThenBy(move => move.TransitionedAt)
                .Select(move => $"{move.ReferenceId} {OrDash(move.FromState)} -> {move.ToState} at {Instant(move.TransitionedAt)}")
                .ToList());

            AssertTheLines(read, expected, "moves between states stored for the Team's Work Items",
                "A move added, dropped or dated differently changes cycle times and the flow charts built on them. With the same uploads and the same clock the same moves must be stored; find what changed in recording a move and undo it.");
        }

        private void ThenTheFeatureMovesStoredAre(int portfolioId, params string[] expected)
        {
            var read = Read(dbContext =>
                (from move in dbContext.FeatureStateTransitions
                 join feature in dbContext.Features on move.FeatureId equals feature.Id
                 where feature.Portfolios.Any(portfolio => portfolio.Id == portfolioId)
                 select new { feature.ReferenceId, move.FromState, move.ToState, move.TransitionedAt })
                .AsEnumerable()
                .OrderBy(move => move.ReferenceId, StringComparer.Ordinal).ThenBy(move => move.TransitionedAt)
                .Select(move => $"{move.ReferenceId} {OrDash(move.FromState)} -> {move.ToState} at {Instant(move.TransitionedAt)}")
                .ToList());

            AssertTheLines(read, expected, "moves between states stored for the Portfolio's Features",
                "A move added, dropped or dated differently changes the Feature's history. With the same uploads and the same clock the same moves must be stored; find what changed in recording a move and undo it.");
        }

        private void ThenTheWorkItemBlockedPeriodsAre(int teamId, params string[] expected)
        {
            var read = Read(dbContext =>
                (from period in dbContext.WorkItemBlockedTransitions
                 join item in dbContext.WorkItems on period.WorkItemId equals item.Id
                 where item.TeamId == teamId
                 select new { item.ReferenceId, period.EnteredAt, period.LeftAt })
                .AsEnumerable()
                .OrderBy(period => period.ReferenceId, StringComparer.Ordinal).ThenBy(period => period.EnteredAt)
                .Select(period => $"{period.ReferenceId} blocked from {Instant(period.EnteredAt)} until {(period.LeftAt is { } left ? Instant(left) : "still blocked")}")
                .ToList());

            AssertTheLines(read, expected, "blocked periods of the Team's Work Items",
                "A blocked period that is not opened, not closed or dated differently changes the blocked history users read. The blocked periods are written by the reactions to the blocked and unblocked announcements; find which reaction stopped running or changed and undo it.");
        }

        private void ThenTheFeatureBlockedPeriodsAre(int portfolioId, params string[] expected)
        {
            var read = Read(dbContext =>
                (from period in dbContext.FeatureBlockedTransitions
                 join feature in dbContext.Features on period.FeatureId equals feature.Id
                 where period.PortfolioId == portfolioId
                 select new { feature.ReferenceId, period.EnteredAt, period.LeftAt })
                .AsEnumerable()
                .OrderBy(period => period.ReferenceId, StringComparer.Ordinal).ThenBy(period => period.EnteredAt)
                .Select(period => $"{period.ReferenceId} blocked from {Instant(period.EnteredAt)} until {(period.LeftAt is { } left ? Instant(left) : "still blocked")}")
                .ToList());

            AssertTheLines(read, expected, "blocked periods of the Portfolio's Features",
                "A blocked period that is not opened, not closed or dated differently changes the blocked history users read. The blocked periods are written by the reactions to the Feature blocked and unblocked announcements; find which reaction stopped running or changed and undo it.");
        }

        private void ThenTheAnnouncementsAre(params string[] expected)
        {
            var read = Read(dbContext => eventsPublished.All.Select(published => Describe(published, dbContext)).ToList());

            AssertTheLines(read, expected, "announcements the refresh published, in order",
                "Other parts of Lighthouse react to these announcements: the blocked history, the flow metrics and the forecasts. One missing, added or out of order changes what they do after a refresh. Find what changed in collecting or publishing them and undo it.");
        }

        private static void AssertTheLines(IReadOnlyList<string> read, string[] expected, string what, string whyAndHow)
        {
            Assert.That(read, Is.EqualTo(expected),
                $"The {what} read{Environment.NewLine}  {Lines(read)}{Environment.NewLine}not{Environment.NewLine}  {Lines(expected)}{Environment.NewLine}{whyAndHow}");
        }

        private T Read<T>(Func<LighthouseAppContext, T> query)
        {
            using var scope = harness.NewScope();
            return query(scope.ServiceProvider.GetRequiredService<LighthouseAppContext>());
        }

        private static string Describe(IDomainEvent published, LighthouseAppContext dbContext)
        {
            string WorkItem(int id) => dbContext.WorkItems.Where(item => item.Id == id).Select(item => item.ReferenceId).SingleOrDefault() ?? $"Work Item #{id}";
            string Feature(int id) => dbContext.Features.Where(feature => feature.Id == id).Select(feature => feature.ReferenceId).SingleOrDefault() ?? $"Feature #{id}";
            string Portfolio(int id) => dbContext.Portfolios.Where(portfolio => portfolio.Id == id).Select(portfolio => portfolio.Name).SingleOrDefault() ?? $"Portfolio #{id}";
            string Team(int id) => dbContext.Teams.Where(team => team.Id == id).Select(team => team.Name).SingleOrDefault() ?? $"Team #{id}";

            return published switch
            {
                WorkItemTransitioned moved => $"{WorkItem(moved.WorkItemId)} moved from {moved.FromState} to {moved.ToState}",
                WorkItemBlocked blocked => $"{WorkItem(blocked.WorkItemId)} blocked: {blocked.Reason}",
                WorkItemUnblocked unblocked => $"{WorkItem(unblocked.WorkItemId)} unblocked",
                WorkItemBecameStale stale => $"{WorkItem(stale.WorkItemId)} became stale after {stale.ThresholdDays} days",
                FeatureBlocked blocked => $"{Feature(blocked.FeatureId)} blocked in {Portfolio(blocked.PortfolioId)}: {blocked.Reason}",
                FeatureUnblocked unblocked => $"{Feature(unblocked.FeatureId)} unblocked in {Portfolio(unblocked.PortfolioId)}",
                TeamDataRefreshed refreshed => $"Team {Team(refreshed.TeamId)} refreshed",
                PortfolioFeaturesRefreshed refreshed => $"Portfolio refreshed: {refreshed}",
                _ => $"{published.GetType().Name}: {published}",
            };
        }

        private static string Lines(IEnumerable<string> lines)
        {
            var all = lines.ToList();
            return all.Count == 0 ? "(nothing)" : string.Join($"{Environment.NewLine}  ", all);
        }

        private static string Day(DateTime? instant) => instant?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "-";

        private static string Instant(DateTime instant) => instant.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        private static string OrDash(string? text) => string.IsNullOrEmpty(text) ? "-" : text;

        // --- The refresh as the product builds it, reading a CSV upload, with every announcement recorded ---

        private void RegisterTheRefreshAsTheProductBuildsIt(IServiceCollection services)
        {
            services.AddSingleton<ILighthouseClock>(clock);
            services.AddSingleton<TimeProvider>(timeProvider);
            services.AddSingleton(eventsPublished);
            services.AddScoped(_ => PortDoubles.PremiumLicence());
            services.AddScoped(_ => RefreshSettingsAsSeeded());
            services.AddSingleton<Cache<string, object>>();
            services.AddSingleton<UpdateCancellationContext>();
            services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

            services.AddScoped<IRepository<Team>, TeamRepository>();
            services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            services.AddScoped<IFeatureRepository, FeatureRepository>();
            services.AddScoped<IWorkItemRepository, WorkItemRepository>();
            services.AddScoped<IWorkItemStateTransitionRepository, WorkItemStateTransitionRepository>();
            services.AddScoped<IFeatureStateTransitionRepository, FeatureStateTransitionRepository>();
            services.AddScoped<IWorkItemBlockedTransitionRepository, WorkItemBlockedTransitionRepository>();
            services.AddScoped<IFeatureBlockedTransitionRepository, FeatureBlockedTransitionRepository>();
            services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();
            services.AddScoped<IRepository<BlackoutPeriod>, BlackoutPeriodRepository>();
            services.AddScoped<IRepository<RecurringBlackoutRule>, RecurringBlackoutRuleRepository>();

            services.AddScoped<IWorkTrackingConnectorFactory, WorkTrackingConnectorFactory>();
            services.AddScoped<CsvWorkTrackingConnector>();

            services.AddScoped<IFeatureOrderingPolicyProvider, FeatureOrderingPolicyProvider>();
            services.AddScoped<IFeatureOrdering, FeatureOrdering>();
            services.AddScoped<IRuleEvaluator<WorkItem>, RuleEvaluator<WorkItem>>();
            services.AddScoped<IRuleFieldProvider<WorkItem>, WorkItemFieldProvider>();
            services.AddScoped<IBlockedItemService, BlockedItemService>();
            services.AddScoped<IDependencyReconciler, DependencyReconciler>();
            services.AddScoped<IDependencyHonourPolicy, DependencyHonourPolicy>();
            services.AddScoped<IDependencyDecision, DependencyDecision>();
            services.AddScoped<IDependencyRefreshReporter, DependencyRefreshReporter>();

            services.AddScoped<IBlackoutPeriodService, BlackoutPeriodService>();
            services.AddScoped<ForecastFilterRuleService>();
            services.AddScoped<IThroughputFilter>(provider => provider.GetRequiredService<ForecastFilterRuleService>());
            services.AddScoped<ForecastingAnswersToFlowMetrics>();
            services.AddScoped<IForecastDataSufficiency>(provider => provider.GetRequiredService<ForecastingAnswersToFlowMetrics>());
            services.AddScoped<ITeamMetricsService, TeamMetricsService>();
            services.AddScoped<IPortfolioMetricsService, PortfolioMetricsService>();

            services.AddScoped<IWorkItemService, WorkItemService>();
            services.AddScoped<ITeamDataService, TeamDataService>();

            services.AddScoped<IDomainEventHandler<WorkItemBlocked>, WorkItemBlockedTransitionCaptureHandler>();
            services.AddScoped<IDomainEventHandler<WorkItemUnblocked>, WorkItemBlockedTransitionCloseHandler>();
            services.AddScoped<IDomainEventHandler<FeatureBlocked>, FeatureBlockedTransitionCaptureHandler>();
            services.AddScoped<IDomainEventHandler<FeatureUnblocked>, FeatureBlockedTransitionCloseHandler>();

            RecordEveryAnnouncementOf<WorkItemTransitioned>(services);
            RecordEveryAnnouncementOf<WorkItemBlocked>(services);
            RecordEveryAnnouncementOf<WorkItemUnblocked>(services);
            RecordEveryAnnouncementOf<WorkItemBecameStale>(services);
            RecordEveryAnnouncementOf<FeatureBlocked>(services);
            RecordEveryAnnouncementOf<FeatureUnblocked>(services);
            RecordEveryAnnouncementOf<TeamDataRefreshed>(services);
            RecordEveryAnnouncementOf<PortfolioFeaturesRefreshed>(services);
        }

        /// <summary>
        /// The two refresh intervals a newly seeded database holds; the metrics a refresh renews read them. A setting
        /// nobody gave it fails the read, naming the setting, rather than answering a default no seeded database holds.
        /// </summary>
        private static IAppSettingService RefreshSettingsAsSeeded()
        {
            var settings = new Mock<IAppSettingService>(MockBehavior.Strict);
            settings.Setup(service => service.GetTeamDataRefreshSettings()).Returns(new RefreshSettings { Interval = 60, RefreshAfter = 180, StartDelay = 10 });
            settings.Setup(service => service.GetFeatureRefreshSettings()).Returns(new RefreshSettings { Interval = 60, RefreshAfter = 180, StartDelay = 15 });
            return settings.Object;
        }

        private static void RecordEveryAnnouncementOf<TEvent>(IServiceCollection services) where TEvent : IDomainEvent
            => services.AddScoped<IDomainEventHandler<TEvent>, RecordsWhatIsPublished<TEvent>>();

        /// <summary>Every announcement a refresh publishes, in the order it was published.</summary>
        private sealed class EventsPublished
        {
            private readonly List<IDomainEvent> published = [];

            public IReadOnlyList<IDomainEvent> All => [.. published];

            public void StartAfresh() => published.Clear();

            public void Add(IDomainEvent domainEvent) => published.Add(domainEvent);
        }

        private sealed class RecordsWhatIsPublished<TEvent>(EventsPublished log) : IDomainEventHandler<TEvent> where TEvent : IDomainEvent
        {
            public Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken)
            {
                log.Add(domainEvent);
                return Task.CompletedTask;
            }
        }
    }
}
