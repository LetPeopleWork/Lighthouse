using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Refinement
{
    [TestFixture]
    public class RefinementViewQueryTest
    {
        private const int TeamId = 12;

        private static readonly string[] BothWorkItems = ["GR-1", "GR-2"];

        private static readonly string[] OnlyGr1 = ["GR-1"];

        private static readonly bool[] OnlyMosLatestCommentIsOpen = [false, true, false, false];

        private static readonly string[] Nobody = [];

        private static readonly string[] OnlyAna = ["Ana Lima"];

        private static readonly string[] OnlyPriya = ["Priya Sharma"];

        private static readonly string[] JonasWeberTwice = ["Jonas Weber", "Jonas Weber"];

        private static readonly DateTime VotedAt = new(2026, 10, 3, 9, 0, 0, DateTimeKind.Utc);

        private static readonly RowStanding[] StandingsWithOneYesOfOne =
        [
            RowStanding.Ready,
            new(RowReadiness.MoreYesNeeded, 1),
            new(RowReadiness.MoreYesNeeded, 1),
        ];

        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;
        private List<WorkItem> workItems;
        private Mock<ISizingLogRepository> sizingLogMock;
        private RefinementViewQuery subject;

        [SetUp]
        public void SetUp()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
            teamMetricsServiceMock
                .Setup(service => service.GetCycleTimePercentilesForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([new PercentileValue(85, 12)]);

            workItems = [];
            sizingLogMock = new Mock<ISizingLogRepository>();
            sizingLogMock
                .Setup(log => log.ReadForTeam(It.IsAny<int>(), It.IsAny<IReadOnlyCollection<string>>()))
                .Returns(Enumerable.Empty<SizingLogEntry>());

            var workItemRepositoryMock = new Mock<IWorkItemRepository>();
            workItemRepositoryMock
                .Setup(repository => repository.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns(() => workItems.AsQueryable());

            var clockMock = new Mock<ILighthouseClock>();
            clockMock.Setup(clock => clock.Today).Returns(new DateOnly(2026, 10, 3));
            var blackoutPeriodService = Mock.Of<IBlackoutPeriodService>(service =>
                service.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()) == new List<BlackoutPeriod>());

            subject = new RefinementViewQuery(
                teamRepositoryMock.Object,
                new RefinementList(workItemRepositoryMock.Object),
                new SleYardstickResolver(teamMetricsServiceMock.Object, clockMock.Object),
                sizingLogMock.Object,
                new VoterIdentityResolver(Mock.Of<IAuthModeResolver>(resolver => resolver.Resolve() == new RuntimeAuthStatus { Mode = AuthMode.Disabled }), Mock.Of<IHttpContextAccessor>()),
                new StageRuleMatcher(
                    new RuleEvaluator<WorkItem>(),
                    new WorkItemFieldProvider(),
                    new ForecastFilterRuleService(new RuleEvaluator<WorkItem>(), new WorkItemFieldProvider(), Mock.Of<ILicenseService>())),
                new RefinementNeedCalculator(
                    new RefinementCalendar(clockMock.Object, blackoutPeriodService),
                    clockMock.Object,
                    blackoutPeriodService,
                    teamMetricsServiceMock.Object,
                    Mock.Of<IForecastService>()));
        }

        [Test]
        public void ATeamWithoutRefinementStatesHasNoYardstickAndItsCycleTimeIsNeverWorkedOut()
        {
            GivenTheTeam(new Team { Id = TeamId });

            var view = subject.ForTeam(TeamId, null);

            Assert.That(view?.Yardstick, Is.EqualTo(Yardstick.None));
            teamMetricsServiceMock.Verify(
                service => service.GetCycleTimePercentilesForTeam(It.IsAny<Team>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        [Test]
        public void ATeamThatRefinesWithoutAnSleGetsItsCycleTimeFallback()
        {
            GivenTheTeam(new Team
            {
                Id = TeamId,
                RefinementSettings = new RefinementSettings { States = [new RefinementStateSetting { State = "Backlog" }] },
            });

            var view = subject.ForTeam(TeamId, null);

            Assert.That(view?.Yardstick, Is.EqualTo(new Yardstick(YardstickSource.CycleTimeFallback, 12, 85)));
        }

        [Test]
        public void AnUnknownTeamHasNoRefinementView()
        {
            var view = subject.ForTeam(TeamId, null);

            Assert.That(view, Is.Null);
        }

        [Test]
        public void ATeamWithoutRefinementStatesIsNotConfiguredAndListsNoWorkItems()
        {
            GivenTheTeam(new Team { Id = TeamId });

            var view = subject.ForTeam(TeamId, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(view?.RefinementConfigured, Is.False);
                Assert.That(view?.WorkItems, Is.Empty);
            }
        }

        [Test]
        public void ATeamWithRefinementStatesIsConfigured()
        {
            GivenTheTeam(new Team
            {
                Id = TeamId,
                RefinementSettings = new RefinementSettings { States = [new RefinementStateSetting { State = "Backlog" }] },
            });

            var view = subject.ForTeam(TeamId, null);

            Assert.That(view?.RefinementConfigured, Is.True);
        }

        [Test]
        public void EachRowCarriesItsOwnVotesAndARowNobodyVotedOnCarriesNone()
        {
            const string jonasBrowserKey = "jonas-browser-keeps-this-voter-key";
            GivenTheTeam(new Team
            {
                Id = TeamId,
                RefinementSettings = new RefinementSettings { States = [new RefinementStateSetting { State = "Backlog" }] },
            });
            workItems.AddRange(
            [
                new WorkItem { TeamId = TeamId, ReferenceId = "GR-1", State = "Backlog", Order = "1" },
                new WorkItem { TeamId = TeamId, ReferenceId = "GR-2", State = "Backlog", Order = "2" },
            ]);
            sizingLogMock
                .Setup(log => log.ReadForTeam(TeamId, It.Is<IReadOnlyCollection<string>>(references => references.SequenceEqual(BothWorkItems))))
                .Returns(
                [
                    VoteOn("GR-1", 1, SizingLogEntry.SelfDeclaredVoterKeyOf(jonasBrowserKey), SizingAnswer.Yes),
                    VoteOn("GR-1", 2, "self:ana", SizingAnswer.No),
                ]);

            var view = subject.ForTeam(TeamId, jonasBrowserKey);

            Assert.That(
                view?.WorkItems.Select(row => (row.WorkItem.ReferenceId, row.Votes)),
                Is.EqualTo(new[]
                {
                    ("GR-1", new RowVotes(2, SizingAnswer.Yes, new VoteSplit(1, 0, 1))),
                    ("GR-2", RowVotes.None),
                }));
        }

        [Test]
        public void EachRowStandsUnderTheTeamsOwnReadinessAndOnlyReadyRowsAreCounted()
        {
            GivenTheTeam(new Team
            {
                Id = TeamId,
                RefinementSettings = new RefinementSettings
                {
                    States = [new RefinementStateSetting { State = "Backlog" }],
                    Readiness = new ReadinessSetting { MinYes = 1, MinVoters = 1 },
                },
            });
            workItems.AddRange(
            [
                new WorkItem { TeamId = TeamId, ReferenceId = "GR-1", State = "Backlog", Order = "1" },
                new WorkItem { TeamId = TeamId, ReferenceId = "GR-2", State = "Backlog", Order = "2" },
                new WorkItem { TeamId = TeamId, ReferenceId = "GR-3", State = "Backlog", Order = "3" },
            ]);
            sizingLogMock
                .Setup(log => log.ReadForTeam(TeamId, It.IsAny<IReadOnlyCollection<string>>()))
                .Returns([VoteOn("GR-1", 1, "self:jonas", SizingAnswer.Yes)]);

            var view = subject.ForTeam(TeamId, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(view?.WorkItems.Select(row => row.Standing), Is.EqualTo(StandingsWithOneYesOfOne));
                Assert.That(view?.ReadyByVotesCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void TheLogOfAWorkItemReadsOldestFirstAndMarksOnlyTheReadersOwnEntries()
        {
            const string anaBrowserKey = "ana-browser-keeps-this-voter-key-x";
            var anaKey = SizingLogEntry.SelfDeclaredVoterKeyOf(anaBrowserKey);
            GivenTheTeamRefinesGr1();
            var recordedWithoutAKind = new DateTime(2026, 10, 3, 9, 15, 0, DateTimeKind.Unspecified);
            sizingLogMock
                .Setup(log => log.ReadForTeam(TeamId, It.Is<IReadOnlyCollection<string>>(references => references.SequenceEqual(OnlyGr1))))
                .Returns(
                [
                    VoteOn("GR-1", 3, anaKey, SizingAnswer.YesBut, "only if the export moves out"),
                    VoteOn("GR-1", 1, "self:jonas", SizingAnswer.Yes),
                    CommentOn("GR-1", 2, "self:mo", "Which API version?", recordedWithoutAKind),
                ]);

            var log = subject.LogOf(TeamId, "GR-1", anaBrowserKey);

            Assert.That(log?.Entries, Is.EqualTo(new[]
            {
                new SizingLogLine(SizingEntryKind.Vote, SizingAnswer.Yes, null, "self:jonas", SizingChannel.Web, VotedAt, false, false),
                new SizingLogLine(SizingEntryKind.Comment, null, "Which API version?", "self:mo", SizingChannel.Cli, DateTime.SpecifyKind(recordedWithoutAKind, DateTimeKind.Utc), false, true),
                new SizingLogLine(SizingEntryKind.Vote, SizingAnswer.YesBut, "only if the export moves out", anaKey, SizingChannel.Web, VotedAt, true, false),
            }));
        }

        [Test]
        public void TheLogSaysWhenEachEntryWasRecordedAsAUtcInstant()
        {
            GivenTheTeamRefinesGr1();
            sizingLogMock
                .Setup(log => log.ReadForTeam(TeamId, It.IsAny<IReadOnlyCollection<string>>()))
                .Returns([CommentOn("GR-1", 1, "self:mo", "Why?", new DateTime(2026, 10, 3, 9, 15, 0, DateTimeKind.Unspecified))]);

            var log = subject.LogOf(TeamId, "GR-1", null);

            Assert.That(log?.Entries.Select(entry => entry.RecordedAt.Kind), Has.All.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void WithoutAReaderKeyNoEntryIsTheReaders()
        {
            GivenTheTeamRefinesGr1();
            sizingLogMock
                .Setup(log => log.ReadForTeam(TeamId, It.IsAny<IReadOnlyCollection<string>>()))
                .Returns([VoteOn("GR-1", 1, "self:jonas", SizingAnswer.Yes)]);

            var log = subject.LogOf(TeamId, "GR-1", null);

            Assert.That(log?.Entries.Select(entry => entry.IsMine), Has.All.False);
        }

        [Test]
        public void OnlyTheAskersLatestCommentIsMarkedAsTheOpenQuestionUntilTheyVote()
        {
            GivenTheTeamRefinesGr1();
            var askedAt = new DateTime(2026, 10, 3, 9, 15, 0, DateTimeKind.Utc);
            sizingLogMock
                .Setup(log => log.ReadForTeam(TeamId, It.IsAny<IReadOnlyCollection<string>>()))
                .Returns(
                [
                    CommentOn("GR-1", 1, "self:mo", "Which API version?", askedAt),
                    CommentOn("GR-1", 2, "self:mo", "And which client?", askedAt),
                    CommentOn("GR-1", 3, "self:jonas", "Is the export in scope?", askedAt),
                    VoteOn("GR-1", 4, "self:jonas", SizingAnswer.Yes),
                ]);

            var log = subject.LogOf(TeamId, "GR-1", null);

            Assert.That(log?.Entries.Select(entry => entry.IsOpenQuestion), Is.EqualTo(OnlyMosLatestCommentIsOpen));
        }

        [TestCaseSource(nameof(LogsWithVoters))]
        public void TheLogNamesWhoCurrentlyVotesEachAnswerAndAgreesWithTheRowsSplit(SizingLogEntry[] entries, string[] yes, string[] yesBut, string[] no)
        {
            GivenTheTeamRefinesGr1();
            sizingLogMock
                .Setup(log => log.ReadForTeam(TeamId, It.IsAny<IReadOnlyCollection<string>>()))
                .Returns(entries);

            var voters = subject.LogOf(TeamId, "GR-1", null)!.Voters;
            var split = subject.ForTeam(TeamId, null)!.WorkItems.Single().Votes.Split;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(voters.Yes, Is.EqualTo(yes), "Yes");
                Assert.That(voters.YesBut, Is.EqualTo(yesBut), "Yes, if");
                Assert.That(voters.No, Is.EqualTo(no), "No");
                Assert.That(new VoteSplit(voters.Yes.Count, voters.YesBut.Count, voters.No.Count), Is.EqualTo(split),
                    "the log names exactly as many people under each answer as the row counts");
            }
        }

        private static IEnumerable<TestCaseData> LogsWithVoters()
        {
            yield return new TestCaseData(Array.Empty<SizingLogEntry>(), Nobody, Nobody, Nobody)
                .SetName("A Work Item nobody voted on names nobody");

            yield return new TestCaseData(
                    new[]
                    {
                        NamedEntry(1, "self:jonas", "Jonas Weber", SizingEntryKind.Vote, SizingAnswer.Yes),
                        NamedEntry(2, "self:ana", "Ana Lima", SizingEntryKind.Vote, SizingAnswer.No),
                        NamedEntry(3, "self:mo", "Mo Okafor", SizingEntryKind.Comment, null),
                        NamedEntry(4, "self:priya", "Priya Sharma", SizingEntryKind.Vote, SizingAnswer.YesBut),
                        NamedEntry(5, "self:other-jonas", "Jonas Weber", SizingEntryKind.Vote, SizingAnswer.Yes),
                    },
                    JonasWeberTwice, OnlyPriya, OnlyAna)
                .SetName("Each answer names its voters, a shared name once per voter, and a question names nobody");

            yield return new TestCaseData(
                    new[]
                    {
                        NamedEntry(1, "self:ana", "Ana", SizingEntryKind.Vote, SizingAnswer.Yes),
                        NamedEntry(2, "self:jonas", "Jonas Weber", SizingEntryKind.Vote, SizingAnswer.No),
                        NamedEntry(3, "self:ana", "Ana Lima", SizingEntryKind.Vote, SizingAnswer.No),
                        NamedEntry(4, "self:jonas", "Jonas Weber", SizingEntryKind.Revocation, null),
                    },
                    Nobody, Nobody, OnlyAna)
                .SetName("A renamed voter who changed their mind is named once, and a vote taken back names nobody");
        }

        private static SizingLogEntry NamedEntry(int id, string voterKey, string displayName, SizingEntryKind kind, SizingAnswer? answer) => new()
        {
            Id = id,
            TeamId = TeamId,
            WorkItemReferenceId = "GR-1",
            Kind = kind,
            Answer = answer,
            Comment = kind == SizingEntryKind.Comment ? "Which API version?" : null,
            VoterKey = voterKey,
            VoterDisplayName = displayName,
            RecordedAt = VotedAt,
            Channel = SizingChannel.Web,
            YardstickSource = YardstickSource.CycleTimeFallback,
        };

        [TestCase("GR-2")]
        [TestCase("GR-999")]
        public void AWorkItemOutsideTheTeamsRefinementHasNoLog(string workItem)
        {
            GivenTheTeamRefinesGr1();
            workItems.Add(new WorkItem { TeamId = TeamId, ReferenceId = "GR-2", State = "Implementation", Order = "2" });

            var log = subject.LogOf(TeamId, workItem, null);

            Assert.That(log, Is.Null);
        }

        [Test]
        public void AnUnknownTeamOrOneThatDoesNotRefineHasNoLog()
        {
            var unknown = subject.LogOf(TeamId, "GR-1", null);
            GivenTheTeam(new Team { Id = TeamId });
            var notRefining = subject.LogOf(TeamId, "GR-1", null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(unknown, Is.Null);
                Assert.That(notRefining, Is.Null);
            }
        }

        private static SizingLogEntry CommentOn(string workItemReference, int id, string voterKey, string comment, DateTime recordedAt) => new()
        {
            Id = id,
            TeamId = TeamId,
            WorkItemReferenceId = workItemReference,
            Kind = SizingEntryKind.Comment,
            Comment = comment,
            VoterKey = voterKey,
            VoterDisplayName = voterKey,
            RecordedAt = recordedAt,
            Channel = SizingChannel.Cli,
            YardstickSource = YardstickSource.CycleTimeFallback,
        };

        private void GivenTheTeamRefinesGr1()
        {
            GivenTheTeam(new Team
            {
                Id = TeamId,
                RefinementSettings = new RefinementSettings { States = [new RefinementStateSetting { State = "Backlog" }] },
            });
            workItems.Add(new WorkItem { TeamId = TeamId, ReferenceId = "GR-1", State = "Backlog", Order = "1" });
        }

        private static SizingLogEntry VoteOn(string workItemReference, int id, string voterKey, SizingAnswer answer, string? comment = null) => new()
        {
            Id = id,
            TeamId = TeamId,
            WorkItemReferenceId = workItemReference,
            Kind = SizingEntryKind.Vote,
            Answer = answer,
            Comment = comment,
            VoterKey = voterKey,
            VoterDisplayName = voterKey,
            RecordedAt = VotedAt,
            Channel = SizingChannel.Web,
            YardstickSource = YardstickSource.CycleTimeFallback,
        };

        private void GivenTheTeam(Team team)
            => teamRepositoryMock.Setup(repository => repository.GetById(TeamId)).Returns(team);
    }
}
