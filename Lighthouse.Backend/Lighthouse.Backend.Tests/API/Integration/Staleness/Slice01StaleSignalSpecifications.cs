using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Staleness
{
    public partial class Slice01StaleSignalTest
    {
        private const string InProgress = "In Progress";
        private const string BlockedState = "Blocked";

        private readonly List<RemoteRecord> trackerRecords = [];

        [SetUp]
        public void ForgetTheTrackersRecords() => trackerRecords.Clear();

        // The Given counts days back from today and the refresh judges against today, so both have to
        // see the same today even when a scenario runs across midnight.
        protected override void ReplaceServicesForTheScenario(IServiceCollection services)
        {
            services.RemoveAll<ILighthouseClock>();
            services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(DateTimeOffset.UtcNow, LighthouseClock.ResolveInstanceTimeZone(null)));
        }

        // --- Given ---

        private int GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked()
            => GivenATeamWithThresholds(timeInStateDays: 5, blockedDays: 3);

        private int GivenATeamWithThresholds(int timeInStateDays, int blockedDays)
        {
            var teamId = SeedTeam(SeedConnection(), $"Team {Guid.NewGuid():N}", timeInStateDays);

            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            var team = repository.GetById(teamId)!;

            team.DoingStates = [InProgress, BlockedState];
            team.BlockedStalenessThresholdDays = blockedDays;
            team.BlockedRuleSetJson = JsonSerializer.Serialize(new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions = [new WorkItemRuleCondition { FieldKey = "workitem.state", Operator = RuleOperators.Equals, Value = BlockedState }],
            });

            repository.Update(team);
            repository.Save().GetAwaiter().GetResult();

            return teamId;
        }

        /// <summary>
        /// A Work Item the team already holds, recorded as having entered its state the given number of
        /// days ago on the instance calendar, and still the same on the tracker today.
        /// </summary>
        private int GivenTheTeamHasAWorkItemThatHasBeenInItsStateFor(int teamId, string referenceId, string state, int days)
        {
            var enteredAt = EnteredDaysBeforeToday(days);
            var record = new RemoteRecord(referenceId, enteredAt)
            {
                State = state,
                StateCategory = StateCategories.Doing,
                StartedDate = enteredAt.AddDays(-1),
                StateEnteredAt = enteredAt,
            };

            SeedStoredWorkItems(teamId, record);
            var workItemId = TheStoredWorkItemsFor(teamId).Single(stored => stored.ReferenceId == referenceId).Id;
            SeedStoredTransition(workItemId, "New", state, enteredAt);

            trackerRecords.Add(record);
            TheTrackerHolds([.. trackerRecords]);

            return workItemId;
        }

        private DateTime EnteredDaysBeforeToday(int days)
        {
            var clock = Factory.Services.GetRequiredService<ILighthouseClock>();
            var enteredDay = clock.Today.AddDays(-(days - 1));

            return TimeZoneInfo.ConvertTimeToUtc(enteredDay.ToDateTime(new TimeOnly(12, 0)), clock.Zone);
        }

        // --- When ---

        private Task WhenTheScheduledRefreshRuns(int teamId) => TheTeamRefreshRuns(teamId);

        // --- Then ---

        private void ThenTheWorkItemWasSignalledAsStale(int workItemId, int thresholdDays)
        {
            var expected = new List<WorkItemBecameStale> { new(workItemId, thresholdDays) };

            Assert.That(CapturedEvents.Of<WorkItemBecameStale>(), Is.EqualTo(expected),
                "One signal, for the Work Item that went stale, naming the threshold of the reason that made it stale.");
        }

        private void ThenNoWorkItemWasSignalledAsStale()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(trackerRecords.ConvertAll(record => record.ReferenceId),
                    Is.SubsetOf(TheStoredWorkItemsForEveryTeam()),
                    "positive control: the refresh has to still hold the Work Items, or there was nothing to judge.");
                Assert.That(CapturedEvents.Of<WorkItemBecameStale>(), Is.Empty);
            }
        }

        private List<string> TheStoredWorkItemsForEveryTeam()
        {
            using var scope = Factory.Services.CreateScope();
            return [.. scope.ServiceProvider.GetRequiredService<IWorkItemRepository>().GetAll().Select(workItem => workItem.ReferenceId)];
        }
    }
}
