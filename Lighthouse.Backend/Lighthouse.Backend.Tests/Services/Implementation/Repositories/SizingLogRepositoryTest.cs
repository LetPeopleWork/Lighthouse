using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    [Category("epic-5510-5881-refinement")]
    public class SizingLogRepositoryTest : IntegrationTestBase
    {
        private const string AskedFor = "GR-1";

        private const string AlsoAskedFor = "GR-2";

        private static readonly string[] OldestFirst = ["self:first", "self:second", "self:third"];

        private int teamId;
        private int otherTeamId;

        // An entry carries a foreign key to its Team, so the Teams have to exist first.
        [SetUp]
        public void SeedTeams()
        {
            var connection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira, Name = "Connection" };
            var team = new Team { Name = "Gravity", WorkTrackingSystemConnection = connection };
            var otherTeam = new Team { Name = "Ocean", WorkTrackingSystemConnection = connection };
            DatabaseContext.Teams.AddRange(team, otherTeam);
            DatabaseContext.SaveChanges();

            teamId = team.Id;
            otherTeamId = otherTeam.Id;
        }

        [Test]
        public void AnAppendedEntryIsStoredAtOnce()
        {
            CreateSubject().Append(AnEntry(teamId, AskedFor, "self:jonas"));

            var stored = DatabaseContext.SizingLogEntries.AsNoTracking().ToList();

            Assert.That(stored.Select(entry => (entry.TeamId, entry.WorkItemReferenceId, entry.VoterKey)), Is.EqualTo(new[] { (teamId, AskedFor, "self:jonas") }));
        }

        [Test]
        public void ReadingATeamsLogReturnsOnlyThatTeamsEntriesOnTheAskedWorkItemsOldestFirst()
        {
            var subject = CreateSubject();
            subject.Append(AnEntry(teamId, AskedFor, "self:first"));
            subject.Append(AnEntry(otherTeamId, AskedFor, "self:other-team"));
            subject.Append(AnEntry(teamId, "GR-9", "self:not-asked-for"));
            subject.Append(AnEntry(teamId, AlsoAskedFor, "self:second"));
            subject.Append(AnEntry(teamId, AskedFor, "self:third"));

            var read = subject.ReadForTeam(teamId, [AskedFor, AlsoAskedFor]);

            Assert.That(read.Select(entry => entry.VoterKey), Is.EqualTo(OldestFirst));
        }

        private SizingLogRepository CreateSubject() => new(DatabaseContext);

        private static SizingLogEntry AnEntry(int team, string workItemReference, string voterKey) => new()
        {
            TeamId = team,
            WorkItemReferenceId = workItemReference,
            Kind = SizingEntryKind.Vote,
            Answer = SizingAnswer.Yes,
            VoterKey = voterKey,
            VoterDisplayName = voterKey,
            RecordedAt = new DateTime(2026, 10, 4, 7, 30, 0, DateTimeKind.Utc),
            Channel = SizingChannel.Web,
            YardstickSource = YardstickSource.CycleTimeFallback,
        };
    }
}
