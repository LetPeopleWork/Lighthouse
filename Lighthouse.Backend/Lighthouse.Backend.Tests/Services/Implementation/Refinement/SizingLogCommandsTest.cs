using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Refinement
{
    public class SizingLogCommandsTest
    {
        private const int TeamId = 12;

        private const string InRefinement = "GR-073";

        private const string BeingBuilt = "GR-040";

        private const string AlsoInRefinement = "GR-074";

        private static readonly DateTimeOffset CastAt = new(2026, 10, 4, 7, 30, 0, TimeSpan.Zero);

        private static readonly Voter Jonas = new("self:jonas", "Jonas Weber", null);

        private static readonly string?[] NoComment = [null];

        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<ISizingLogRepository> sizingLogMock;
        private List<SizingLogEntry> appended;
        private Mock<ILogger<SizingLogCommands>> loggerMock;
        private SizingLogCommands subject;

        [SetUp]
        public void SetUp()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();

            var workItemRepositoryMock = new Mock<IWorkItemRepository>();
            workItemRepositoryMock
                .Setup(repository => repository.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns(new List<WorkItem>
                {
                    new() { TeamId = TeamId, ReferenceId = InRefinement, State = "Backlog", Order = "1" },
                    new() { TeamId = TeamId, ReferenceId = BeingBuilt, State = "Implementation", Order = "2" },
                    new() { TeamId = TeamId, ReferenceId = AlsoInRefinement, State = "Backlog", Order = "3" },
                }.AsQueryable());

            var teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            teamMetricsServiceMock
                .Setup(service => service.GetCycleTimePercentilesForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([new PercentileValue(85, 12)]);

            var clockMock = new Mock<ILighthouseClock>();
            clockMock.Setup(clock => clock.Now).Returns(CastAt);
            clockMock.Setup(clock => clock.Today).Returns(new DateOnly(2026, 10, 4));

            appended = [];
            sizingLogMock = new Mock<ISizingLogRepository>();
            sizingLogMock.Setup(log => log.Append(It.IsAny<SizingLogEntry>())).Callback<SizingLogEntry>(appended.Add);

            loggerMock = new Mock<ILogger<SizingLogCommands>>();

            subject = new SizingLogCommands(
                teamRepositoryMock.Object,
                new RefinementList(workItemRepositoryMock.Object),
                new SleYardstickResolver(teamMetricsServiceMock.Object, clockMock.Object),
                sizingLogMock.Object,
                clockMock.Object,
                loggerMock.Object);
        }

        [TestCase(SizingAnswer.Yes, SizingChannel.Web, null)]
        [TestCase(SizingAnswer.YesBut, SizingChannel.LiveSession, "if the API stays as it is")]
        [TestCase(SizingAnswer.No, SizingChannel.Cli, null)]
        [TestCase(SizingAnswer.No, SizingChannel.Assistant, "too big")]
        public void AVoteAppendsExactlyOneEntryAsCast(SizingAnswer answer, SizingChannel channel, string? comment)
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7));

            var outcome = subject.Vote(TeamId, InRefinement, new SizingVote(answer, channel, comment), Jonas);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome, Is.EqualTo(VoteOutcome.Recorded));
                Assert.That(appended, Has.Count.EqualTo(1));
                Assert.That(appended.Select(Summary), Has.All.EqualTo(new EntrySummary(
                    TeamId, InRefinement, SizingEntryKind.Vote, answer, comment, Jonas.Key, null, Jonas.DisplayName, CastAt.UtcDateTime, channel)));
            }
        }

        [TestCase("")]
        [TestCase("   ")]
        public void AVoteWhoseCommentIsOnlyBlankIsRecordedWithoutOne(string comment)
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7));

            subject.Vote(TeamId, InRefinement, new SizingVote(SizingAnswer.Yes, SizingChannel.Web, comment), Jonas);

            Assert.That(appended.Select(entry => entry.Comment), Is.EqualTo(NoComment));
        }

        [TestCase(InRefinement)]
        [TestCase(AlsoInRefinement)]
        public void AVoteOnAnyOfSeveralWorkItemsInRefinementIsRecordedAgainstThatWorkItem(string workItem)
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7));

            var outcome = subject.Vote(TeamId, workItem, new SizingVote(SizingAnswer.Yes, SizingChannel.Web, null), Jonas);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome, Is.EqualTo(VoteOutcome.Recorded));
                Assert.That(appended.Select(entry => entry.WorkItemReferenceId), Is.EqualTo(new[] { workItem }));
            }
        }

        [TestCase(85, 7, YardstickSource.Sle, 7, 85)]
        [TestCase(70, 14, YardstickSource.Sle, 14, 70)]
        [TestCase(0, 0, YardstickSource.CycleTimeFallback, 12, 85)]
        public void AVoteKeepsTheYardstickItWasCastAgainst(int sleProbability, int sleDays, YardstickSource source, int days, int probability)
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability, sleDays));

            subject.Vote(TeamId, InRefinement, new SizingVote(SizingAnswer.Yes, SizingChannel.Web, null), Jonas);

            Assert.That(
                appended.Select(entry => (entry.YardstickSource, entry.YardstickDays, entry.YardstickProbability)).Single(),
                Is.EqualTo((source, (int?)days, (int?)probability)));
        }

        [TestCase(BeingBuilt)]
        [TestCase("GR-999")]
        public void AVoteOnAWorkItemOutsideRefinementAppendsNothing(string workItem)
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7));

            var outcome = subject.Vote(TeamId, workItem, new SizingVote(SizingAnswer.Yes, SizingChannel.Web, null), Jonas);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome, Is.EqualTo(VoteOutcome.WorkItemNotInRefinement));
                sizingLogMock.Verify(log => log.Append(It.IsAny<SizingLogEntry>()), Times.Never);
            }
        }

        [TestCase(BeingBuilt, SizingChannel.Web)]
        [TestCase("GR-999", SizingChannel.Cli)]
        public void ARefusedVoteLogsWhyForWhichTeamAndChannelButNeverWhoOrWhat(string workItem, SizingChannel channel)
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7));

            subject.Vote(TeamId, workItem, new SizingVote(SizingAnswer.Yes, channel, "too big"), Jonas);

            var lines = LoggedLines();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(lines.Select(line => line.Level), Is.EqualTo(new List<LogLevel> { LogLevel.Information }));
                Assert.That(lines.Single().Fields, Is.EqualTo(new Dictionary<string, string?>
                {
                    ["Reason"] = "work-item-not-in-refinement",
                    ["TeamId"] = TeamId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["Channel"] = channel.ToString(),
                }));
                Assert.That(lines.Single().Text, Does.Not.Contain(workItem).And.Not.Contain(Jonas.DisplayName).And.Not.Contain(Jonas.Key).And.Not.Contain("too big"));
            }
        }

        [Test]
        public void AVoteThatIsTakenLogsNothingAtInformation()
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7));

            subject.Vote(TeamId, InRefinement, new SizingVote(SizingAnswer.Yes, SizingChannel.Web, null), Jonas);

            Assert.That(LoggedLines().Where(line => line.Level >= LogLevel.Information), Is.Empty);
        }

        [Test]
        public void AVoteForATeamThatDoesNotExistAppendsNothing()
        {
            var outcome = subject.Vote(TeamId, InRefinement, new SizingVote(SizingAnswer.Yes, SizingChannel.Web, null), Jonas);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome, Is.EqualTo(VoteOutcome.TeamNotFound));
                Assert.That(appended, Is.Empty);
            }
        }

        [TestCase(SizingAnswer.Yes, VoteOutcome.RecordedAndMadeReady)]
        [TestCase(SizingAnswer.YesBut, VoteOutcome.RecordedAndMadeReady)]
        [TestCase(SizingAnswer.No, VoteOutcome.Recorded)]
        public void AVoteSaysWhetherItMadeTheWorkItemReadyUnderTheTeamsOwnReadiness(SizingAnswer answer, VoteOutcome expected)
        {
            var team = ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7);
            team.RefinementSettings!.Readiness = new ReadinessSetting { MinYes = 1, MinVoters = 1 };
            GivenTheTeam(team);
            GivenTheLogKeepsWhatIsAppended();

            var outcome = subject.Vote(TeamId, InRefinement, new SizingVote(answer, SizingChannel.Web, null), Jonas);

            Assert.That(outcome, Is.EqualTo(expected));
        }

        private static Team ATeamThatRefinesInBacklog(int sleProbability, int sleDays) => new()
        {
            Id = TeamId,
            ServiceLevelExpectationProbability = sleProbability,
            ServiceLevelExpectationRange = sleDays,
            RefinementSettings = new RefinementSettings { States = [new RefinementStateSetting { State = "Backlog" }] },
        };

        private static EntrySummary Summary(SizingLogEntry entry) => new(
            entry.TeamId, entry.WorkItemReferenceId, entry.Kind, entry.Answer, entry.Comment,
            entry.VoterKey, entry.VoterProfileId, entry.VoterDisplayName, entry.RecordedAt, entry.Channel);

        private List<LoggedLine> LoggedLines()
            => loggerMock.Invocations
                .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
                .Select(invocation => AsLoggedLine(invocation.Arguments))
                .OfType<LoggedLine>()
                .ToList();

        private static LoggedLine? AsLoggedLine(IReadOnlyList<object?> arguments)
            => arguments is [LogLevel level, _, IEnumerable<KeyValuePair<string, object?>> state, ..]
                ? new LoggedLine(
                    level,
                    state
                        .Where(field => field.Key != "{OriginalFormat}")
                        .ToDictionary(field => field.Key, field => Convert.ToString(field.Value, System.Globalization.CultureInfo.InvariantCulture)),
                    state.ToString() ?? string.Empty)
                : null;

        private void GivenTheTeam(Team team)
            => teamRepositoryMock.Setup(repository => repository.GetById(TeamId)).Returns(team);

        private void GivenTheLogKeepsWhatIsAppended()
            => sizingLogMock
                .Setup(log => log.ReadForTeam(TeamId, It.IsAny<IReadOnlyCollection<string>>()))
                .Returns(() => appended);

        private sealed record LoggedLine(LogLevel Level, Dictionary<string, string?> Fields, string Text);

        private sealed record EntrySummary(
            int TeamId,
            string WorkItemReferenceId,
            SizingEntryKind Kind,
            SizingAnswer? Answer,
            string? Comment,
            string VoterKey,
            int? VoterProfileId,
            string VoterDisplayName,
            DateTime RecordedAt,
            SizingChannel Channel);
    }
}
