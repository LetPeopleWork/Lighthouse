using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Auth;
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

            subject = new RefinementViewQuery(
                teamRepositoryMock.Object,
                new RefinementList(workItemRepositoryMock.Object),
                new SleYardstickResolver(teamMetricsServiceMock.Object, clockMock.Object),
                sizingLogMock.Object,
                new VoterIdentityResolver(Mock.Of<IAuthModeResolver>(resolver => resolver.Resolve() == new RuntimeAuthStatus { Mode = AuthMode.Disabled })),
                new StageRuleMatcher(new RuleEvaluator<WorkItem>(), new WorkItemFieldProvider()));
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

        private static SizingLogEntry VoteOn(string workItemReference, int id, string voterKey, SizingAnswer answer) => new()
        {
            Id = id,
            TeamId = TeamId,
            WorkItemReferenceId = workItemReference,
            Kind = SizingEntryKind.Vote,
            Answer = answer,
            VoterKey = voterKey,
            VoterDisplayName = voterKey,
            RecordedAt = new DateTime(2026, 10, 3, 9, 0, 0, DateTimeKind.Utc),
            Channel = SizingChannel.Web,
            YardstickSource = YardstickSource.CycleTimeFallback,
        };

        private void GivenTheTeam(Team team)
            => teamRepositoryMock.Setup(repository => repository.GetById(TeamId)).Returns(team);
    }
}
