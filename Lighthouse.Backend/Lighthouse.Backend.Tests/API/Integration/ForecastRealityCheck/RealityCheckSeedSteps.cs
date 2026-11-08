using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// The Teams and finished Work Items a reality check scenario starts from, written straight into the
    /// database the scenario's services read, and the read-back that shows the check changed none of them.
    /// </summary>
    public sealed class RealityCheckSeedSteps(IServiceProvider services, RealityCheckForecastScript forecasts, DateOnly today)
    {
        private readonly List<(int OwnerId, OwnerType OwnerType)> ownersSeeded = [];

        /// <summary>
        /// Every Team the current scenario seeded. A cache that keeps notes per owner id outlives the
        /// scenario, and every fresh database numbers its Teams from one again, so these are the ids it has
        /// to be told to forget when the scenario is over.
        /// </summary>
        public IReadOnlyList<(int OwnerId, OwnerType OwnerType)> OwnersSeeded => ownersSeeded;

        public void StartAfresh() => ownersSeeded.Clear();

        public int GivenATeam(string name, int samplingWindowDays)
        {
            var teamId = SeedTeam(name, team => team.ThroughputHistory = samplingWindowDays);
            forecasts.AlsoSweeps(samplingWindowDays);
            return teamId;
        }

        /// <summary>
        /// A Team that forecasts from a fixed pair of dates. Its sampling window length is still stored,
        /// and deliberately off the standard ladder, so a check that wrongly added it would show a fifth
        /// window rather than hiding the mistake behind a standard one.
        /// </summary>
        public int GivenATeamForecastingFromFixedDates(string name, int inertSamplingWindowDays)
        {
            return SeedTeam(name, team =>
            {
                team.ThroughputHistory = inertSamplingWindowDays;
                team.UseFixedDatesForThroughput = true;
                team.ThroughputHistoryStartDate = today.AddDays(-200).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                team.ThroughputHistoryEndDate = today.AddDays(-100).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            });
        }

        public int GivenAnotherTeam(string name) => SeedTeam(name, _ => { });

        /// <summary>One finished Work Item on every day, today included, reaching back far enough for every check.</summary>
        public void GivenTheTeamFinishedAWorkItemEveryDay(int teamId) => GivenTheTeamFinishedAWorkItemEvery(teamId, 1);

        /// <summary>
        /// Work finished in bursts: one Work Item every <paramref name="days"/> days. Every fifth day leaves
        /// a two-week window holding two to four days with finished work - below the bar of five - while a
        /// thirty-day window holds six or more.
        /// </summary>
        public void GivenTheTeamFinishedAWorkItemEvery(int teamId, int days)
        {
            const int FurtherBackThanTheLongestCheckReaches = 160;

            var closedOn = new List<DateOnly>();
            for (var daysAgo = 0; daysAgo < FurtherBackThanTheLongestCheckReaches; daysAgo += days)
            {
                closedOn.Add(today.AddDays(-daysAgo));
            }

            GivenTheTeamFinishedWorkItemsOn(teamId, closedOn);
        }

        public void GivenTheTeamFinishedWorkItemsOn(int teamId, List<DateOnly> days)
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var existing = dbContext.WorkItems.Count(item => item.TeamId == teamId);
            var items = days.Select((day, index) => new WorkItem
            {
                ReferenceId = $"RC-{teamId}-{existing + index + 1}",
                Name = $"Finished Work Item {existing + index + 1}",
                Type = "User Story",
                State = "Done",
                StateCategory = StateCategories.Done,
                TeamId = teamId,
                Url = string.Empty,
                ParentReferenceId = string.Empty,
                Order = $"{existing + index + 1}",
                StartedDate = day.AddDays(-3).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc),
                ClosedDate = day.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc),
            });

            dbContext.WorkItems.AddRange(items);
            dbContext.SaveChanges();
        }

        public TeamAsStored WhatIsStoredFor(int teamId)
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var team = dbContext.Teams.AsNoTracking().Single(candidate => candidate.Id == teamId);
            var workItems = dbContext.WorkItems.AsNoTracking()
                .Where(item => item.TeamId == teamId)
                .Select(item => $"{item.ReferenceId}|{item.State}|{item.ClosedDate:O}")
                .ToList();

            return new TeamAsStored(
                team.Name,
                team.ThroughputHistory,
                team.UseFixedDatesForThroughput,
                team.ThroughputHistoryStartDate,
                team.ThroughputHistoryEndDate,
                team.FeatureWIP,
                string.Join(";", workItems.Order(StringComparer.Ordinal)),
                dbContext.Teams.Count());
        }

        private int SeedTeam(string name, Action<Team> configure)
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"{name} connection",
                WorkTrackingSystem = WorkTrackingSystems.Csv,
            };
            dbContext.WorkTrackingSystemConnections.Add(connection);
            dbContext.SaveChanges();

            var team = new Team { Name = name, WorkTrackingSystemConnectionId = connection.Id };
            configure(team);
            dbContext.Teams.Add(team);
            dbContext.SaveChanges();

            ownersSeeded.Add((team.Id, OwnerType.Team));

            return team.Id;
        }

        public sealed record TeamAsStored(
            string Name,
            int SamplingWindowDays,
            bool UsesFixedDates,
            DateTime? FixedStart,
            DateTime? FixedEnd,
            int FeatureWip,
            string WorkItems,
            int TeamsInTheInstance);
    }
}
