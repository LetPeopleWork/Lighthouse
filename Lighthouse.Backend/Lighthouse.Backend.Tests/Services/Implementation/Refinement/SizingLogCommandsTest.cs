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

        private static readonly Voter Ana = new("account:ana", "Ana Lima", 7);

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
                Assert.That(outcome, Is.EqualTo(SizingOutcome.Recorded));
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
                Assert.That(outcome, Is.EqualTo(SizingOutcome.Recorded));
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
                Assert.That(outcome, Is.EqualTo(SizingOutcome.WorkItemNotInRefinement));
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
                Assert.That(outcome, Is.EqualTo(SizingOutcome.TeamNotFound));
                Assert.That(appended, Is.Empty);
            }
        }

        [TestCase(SizingAnswer.Yes, SizingOutcome.RecordedAndMadeReady)]
        [TestCase(SizingAnswer.YesBut, SizingOutcome.RecordedAndMadeReady)]
        [TestCase(SizingAnswer.No, SizingOutcome.Recorded)]
        public void AVoteSaysWhetherItMadeTheWorkItemReadyUnderTheTeamsOwnReadiness(SizingAnswer answer, SizingOutcome expected)
        {
            var team = ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7);
            team.RefinementSettings!.Readiness = new ReadinessSetting { MinYes = 1, MinVoters = 1 };
            GivenTheTeam(team);
            GivenTheLogKeepsWhatIsAppended();

            var outcome = subject.Vote(TeamId, InRefinement, new SizingVote(answer, SizingChannel.Web, null), Jonas);

            Assert.That(outcome, Is.EqualTo(expected));
        }

        [TestCase(1)]
        [TestCase(SizingLogEntry.LongestComment)]
        public void ACommentOfOneToTwoThousandCharactersIsRecordedWhole(int length)
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7));
            var text = new string('x', length);

            var outcome = subject.Comment(TeamId, InRefinement, new SizingComment(text, SizingChannel.Web), Jonas);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome, Is.EqualTo(SizingOutcome.Recorded));
                Assert.That(appended.Select(entry => (entry.Kind, entry.Answer, entry.Comment)), Is.EqualTo(new[] { (SizingEntryKind.Comment, (SizingAnswer?)null, (string?)text) }));
            }
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("   ")]
        [TestCase("\t\n")]
        public void ACommentThatSaysNothingAppendsNothing(string text)
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7));

            var outcome = subject.Comment(TeamId, InRefinement, new SizingComment(text, SizingChannel.Web), Jonas);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome, Is.EqualTo(SizingOutcome.CommentMissing));
                Assert.That(appended, Is.Empty);
            }
        }

        [Test]
        public void ACommentLongerThanTwoThousandCharactersAppendsNothing()
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7));

            var outcome = subject.Comment(TeamId, InRefinement, new SizingComment(new string('x', SizingLogEntry.LongestComment + 1), SizingChannel.Web), Jonas);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome, Is.EqualTo(SizingOutcome.CommentTooLong));
                Assert.That(appended, Is.Empty);
            }
        }

        [TestCase(SizingAnswer.Yes)]
        [TestCase(SizingAnswer.YesBut)]
        [TestCase(SizingAnswer.No)]
        public void AVoteWhoseCommentIsLongerThanTwoThousandCharactersAppendsNothing(SizingAnswer answer)
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7));

            var outcome = subject.Vote(TeamId, InRefinement, new SizingVote(answer, SizingChannel.Web, new string('x', SizingLogEntry.LongestComment + 1)), Jonas);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome, Is.EqualTo(SizingOutcome.CommentTooLong));
                Assert.That(appended, Is.Empty);
            }
        }

        [Test]
        public void AVoteWhoseCommentIsTwoThousandCharactersIsRecordedWhole()
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7));
            var text = new string('x', SizingLogEntry.LongestComment);

            var outcome = subject.Vote(TeamId, InRefinement, new SizingVote(SizingAnswer.YesBut, SizingChannel.Web, text), Jonas);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome, Is.EqualTo(SizingOutcome.Recorded));
                Assert.That(appended.Select(entry => entry.Comment), Is.EqualTo(new[] { text }));
            }
        }

        [TestCase("   ", SizingChannel.Cli, "comment-required")]
        [TestCase(null, SizingChannel.Assistant, "comment-too-long")]
        public void ARefusedCommentLogsWhyForWhichTeamAndChannelButNeverWhoOrWhat(string? text, SizingChannel channel, string reason)
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7));
            var said = text ?? "too big " + new string('x', SizingLogEntry.LongestComment);

            subject.Comment(TeamId, InRefinement, new SizingComment(said, channel), Jonas);

            var lines = LoggedLines();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(lines.Select(line => line.Level), Is.EqualTo(new List<LogLevel> { LogLevel.Information }));
                Assert.That(lines.Single().Fields, Is.EqualTo(new Dictionary<string, string?>
                {
                    ["Reason"] = reason,
                    ["TeamId"] = TeamId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["Channel"] = channel.ToString(),
                }));
                Assert.That(lines.Single().Text, Does.Not.Contain(InRefinement).And.Not.Contain(Jonas.DisplayName).And.Not.Contain(Jonas.Key).And.Not.Contain("too big"));
            }
        }

        [TestCase(SizingChannel.Web)]
        [TestCase(SizingChannel.Cli)]
        public void TakingBackACurrentVoteAppendsExactlyOneRevocationUnderTheVotersName(SizingChannel channel)
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7));
            GivenTheLogHolds(LoggedBy(Jonas, 1, SizingEntryKind.Vote), LoggedBy(Ana, 2, SizingEntryKind.Vote));

            var outcome = subject.TakeBack(TeamId, InRefinement, channel, Jonas.Key);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome, Is.EqualTo(SizingOutcome.Recorded));
                Assert.That(appended.Select(Summary), Is.EqualTo(new[]
                {
                    new EntrySummary(TeamId, InRefinement, SizingEntryKind.Revocation, null, null, Jonas.Key, Jonas.ProfileId, Jonas.DisplayName, CastAt.UtcDateTime, channel),
                }));
            }
        }

        [TestCaseSource(nameof(LogsWithoutJonassVote))]
        public void TakingBackWithoutACurrentVoteAppendsNothing(SizingLogEntry[] log)
        {
            GivenTheTeam(ATeamThatRefinesInBacklog(sleProbability: 85, sleDays: 7));
            GivenTheLogHolds(log);

            var outcome = subject.TakeBack(TeamId, InRefinement, SizingChannel.Web, Jonas.Key);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome, Is.EqualTo(SizingOutcome.NothingTakenBack));
                Assert.That(appended, Is.Empty);
            }
        }

        private static IEnumerable<TestCaseData> LogsWithoutJonassVote()
        {
            yield return new TestCaseData(arg: Array.Empty<SizingLogEntry>()).SetName("An empty log");
            yield return new TestCaseData(arg: new[] { LoggedBy(Ana, 1, SizingEntryKind.Vote) }).SetName("Only somebody else voted");
            yield return new TestCaseData(arg: new[] { LoggedBy(Jonas, 1, SizingEntryKind.Comment) }).SetName("He only commented");
            yield return new TestCaseData(arg: new[] { LoggedBy(Jonas, 1, SizingEntryKind.Vote), LoggedBy(Jonas, 2, SizingEntryKind.Revocation) })
                .SetName("He already took it back");
        }

        private static SizingLogEntry LoggedBy(Voter voter, int id, SizingEntryKind kind) => new()
        {
            Id = id,
            TeamId = TeamId,
            WorkItemReferenceId = InRefinement,
            Kind = kind,
            Answer = kind == SizingEntryKind.Vote ? SizingAnswer.Yes : null,
            Comment = kind == SizingEntryKind.Comment ? "why?" : null,
            VoterKey = voter.Key,
            VoterProfileId = voter.ProfileId,
            VoterDisplayName = voter.DisplayName,
            RecordedAt = CastAt.UtcDateTime,
            Channel = SizingChannel.Web,
            YardstickSource = YardstickSource.Sle,
        };

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

        private void GivenTheLogHolds(params SizingLogEntry[] log)
            => sizingLogMock
                .Setup(sizingLog => sizingLog.ReadForTeam(TeamId, It.Is<IReadOnlyCollection<string>>(references => references.Contains(InRefinement))))
                .Returns(log);

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
