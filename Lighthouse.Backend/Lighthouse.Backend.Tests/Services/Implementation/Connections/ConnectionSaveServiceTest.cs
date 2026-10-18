using System.Globalization;
using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.Connections;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Connections
{
    public class ConnectionSaveServiceTest
    {
        private const int OwnedConnectionId = 12;

        private const int OtherConnectionId = 99;

        private const int CausedById = 40;

        private const int ParentLinkId = 41;

        private static readonly int[] TeamsOnTheOwnedConnection = [2];

        private static readonly int[] PortfoliosOnTheOwnedConnection = [7];

        private static readonly string[] TokenThenSave = ["token", "save"];

        private static readonly string[] TeamsOwningCausedByInCreationOrder = ["Yankee", "Zulu"];

        private static readonly string[] EachDefaultWithTheUpdatesQueuedForItsOwnGrain =
        [
            "Connection Connection changed the Teams' Parent Override Field default from none to Caused by; 2 Teams follow it: Alpha, Bravo; 2 updates queued.",
            "Connection Connection changed the Portfolios' Parent Override Field default from none to Caused by; 1 Portfolio follows it: Card Issuing; 1 update queued.",
        ];

        private static readonly string[] PortfoliosOwningCausedByInCreationOrder = ["Parent Override", "Dependency", "Size", "Feature Owner", "Estimation"];

        private Mock<IRepository<WorkTrackingSystemConnection>> connectionRepositoryMock;
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<IDomainEventDispatcher> domainEventDispatcherMock;
        private Mock<ILogger<ConnectionSaveService>> loggerMock;
        private Mock<ITerminologyService> terminologyServiceMock;

        [SetUp]
        public void Setup()
        {
            connectionRepositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
            loggerMock = new Mock<ILogger<ConnectionSaveService>>();
            terminologyServiceMock = new Mock<ITerminologyService>();
        }

        [Test]
        public async Task Open_AConnectionThatDoesNotExist_OpensNoSave()
        {
            var subject = CreateSubject();

            var handle = await subject.Open(OwnedConnectionId, CancellationToken.None);

            Assert.That(handle, Is.Null);
        }

        [Test]
        public async Task Commit_WithTheClientsConcurrencyToken_AppliesItBeforeSaving()
        {
            var token = Guid.NewGuid();
            var calls = new List<string>();
            connectionRepositoryMock.Setup(x => x.ApplyConcurrencyTokenForEdit(It.IsAny<IConcurrencyTokenEntity>(), token))
                .Callback(() => calls.Add("token"));
            connectionRepositoryMock.Setup(x => x.Save()).Callback(() => calls.Add("save")).Returns(Task.CompletedTask);
            var subject = CreateSubjectForAConnectionWithOwnersHereAndElsewhere();
            var handle = await OpenTheOwnedConnection(subject);

            var outcome = await subject.Commit(handle, token, startUpdates: true, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Refused, Is.False);
                Assert.That(calls, Is.EqualTo(TokenThenSave));
            }

            connectionRepositoryMock.Verify(x => x.Update(handle.Connection), Times.Once);
            connectionRepositoryMock.Verify(x => x.ApplyConcurrencyTokenForEdit(handle.Connection, token), Times.Once);
        }

        [Test]
        public async Task Commit_WithoutAConcurrencyToken_SavesWithoutApplyingOne()
        {
            var subject = CreateSubjectForAConnectionWithOwnersHereAndElsewhere();
            var handle = await OpenTheOwnedConnection(subject);

            await subject.Commit(handle, concurrencyToken: null, startUpdates: true, CancellationToken.None);

            connectionRepositoryMock.Verify(x => x.Save(), Times.Once);
            connectionRepositoryMock.Verify(x => x.ApplyConcurrencyTokenForEdit(It.IsAny<IConcurrencyTokenEntity>(), It.IsAny<Guid>()), Times.Never);
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_FieldAdded_PublishesTheTeamsAndPortfoliosOnThisConnectionOnly()
        {
            var subject = CreateSubjectForAConnectionWithOwnersHereAndElsewhere();
            ConnectionFetchShapeChanged? published = null;
            domainEventDispatcherMock.Setup(x => x.PublishAsync(It.IsAny<ConnectionFetchShapeChanged>(), It.IsAny<CancellationToken>()))
                .Callback<ConnectionFetchShapeChanged, CancellationToken>((change, _) => published = change)
                .Returns(Task.CompletedTask);
            var handle = await OpenTheOwnedConnection(subject);
            AddAField(handle);

            await subject.Commit(handle, concurrencyToken: null, startUpdates: true, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(published?.ConnectionId, Is.EqualTo(OwnedConnectionId));
                Assert.That(published?.TeamIds, Is.EqualTo(TeamsOnTheOwnedConnection));
                Assert.That(published?.PortfolioIds, Is.EqualTo(PortfoliosOnTheOwnedConnection));
            }
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_FieldAddedButUpdatesLeftForLater_PublishesNothing()
        {
            var subject = CreateSubjectForAConnectionWithOwnersHereAndElsewhere();
            var handle = await OpenTheOwnedConnection(subject);
            AddAField(handle);

            await subject.Commit(handle, concurrencyToken: null, startUpdates: false, CancellationToken.None);

            connectionRepositoryMock.Verify(x => x.Save(), Times.Once);
            VerifyNothingWasPublished();
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_OnlyTheNameChanged_PublishesNothing()
        {
            var subject = CreateSubjectForAConnectionWithOwnersHereAndElsewhere();
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.Name = "Renamed";

            await subject.Commit(handle, concurrencyToken: null, startUpdates: true, CancellationToken.None);

            connectionRepositoryMock.Verify(x => x.Save(), Times.Once);
            VerifyNothingWasPublished();
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_FieldAddedToAConnectionNoTeamOrPortfolioUses_PublishesNothing()
        {
            GivenTheOwnedConnection();
            teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>()))
                .Returns(new List<Team>().AsQueryable());
            portfolioRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Portfolio, bool>>>()))
                .Returns(new List<Portfolio>().AsQueryable());
            var subject = CreateSubject();
            var handle = await OpenTheOwnedConnection(subject);
            AddAField(handle);

            await subject.Commit(handle, concurrencyToken: null, startUpdates: true, CancellationToken.None);

            connectionRepositoryMock.Verify(x => x.Save(), Times.Once);
            VerifyNothingWasPublished();
        }

        [TestCase(FailingStep.LookingUpTheTeamsAndPortfolios)]
        [TestCase(FailingStep.PublishingTheChange)]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_StartingUpdatesFailsAfterAStoredSave_ReportsSavedAndLogsOneError(FailingStep failingStep)
        {
            var connection = GivenTheOwnedConnection();
            var failure = new InvalidOperationException("the database went away");
            if (failingStep == FailingStep.LookingUpTheTeamsAndPortfolios)
            {
                teamRepositoryMock.SetupSequence(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>()))
                    .Returns(new List<Team>().AsQueryable())
                    .Throws(failure);
            }
            else
            {
                teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>()))
                    .Returns(new List<Team> { new() { Id = 2, WorkTrackingSystemConnectionId = OwnedConnectionId, WorkTrackingSystemConnection = connection } }.AsQueryable());
                portfolioRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Portfolio, bool>>>()))
                    .Returns(new List<Portfolio>().AsQueryable());
                domainEventDispatcherMock.Setup(x => x.PublishAsync(It.IsAny<ConnectionFetchShapeChanged>(), It.IsAny<CancellationToken>()))
                    .ThrowsAsync(failure);
            }

            var subject = CreateSubject();
            var handle = await OpenTheOwnedConnection(subject);
            AddAField(handle);

            var outcome = await subject.Commit(handle, concurrencyToken: null, startUpdates: true, CancellationToken.None);

            Assert.That(outcome.Refused, Is.False);
            connectionRepositoryMock.Verify(x => x.Save(), Times.Once);
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains(OwnedConnectionId.ToString(CultureInfo.InvariantCulture))),
                    failure,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        public enum FailingStep
        {
            LookingUpTheTeamsAndPortfolios,
            PublishingTheChange,
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_TheTeamsDefaultChanged_LogsOneEntryNamingTheFollowersInTheOrderTheyWereCreated()
        {
            var log = new RecordingLogger<ConnectionSaveService>();
            var subject = CreateSubjectForAConnectionWithNamedTeams(log);
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.ParentOverrideForTeams = CausedById;

            await subject.Commit(handle, concurrencyToken: null, startUpdates: false, CancellationToken.None);

            var written = log.Written(LogLevel.Information);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(written, Has.Count.EqualTo(1));
                Assert.That(written, Has.Some.EqualTo("Connection Connection changed the Teams' Parent Override Field default from none to Caused by; 2 Teams follow it: Alpha, Bravo; no update queued."));
            }
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Commit_TheTeamsDefaultUnchanged_LogsNothing()
        {
            var log = new RecordingLogger<ConnectionSaveService>();
            var subject = CreateSubjectForAConnectionWithNamedTeams(log);
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.Name = "Renamed";

            await subject.Commit(handle, concurrencyToken: null, startUpdates: false, CancellationToken.None);

            Assert.That(log.Everything, Is.Empty);
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_TheTeamsDefaultSetButEveryTeamOwnsItsField_LogsOneEntrySayingNoTeamFollowsIt()
        {
            var log = new RecordingLogger<ConnectionSaveService>();
            var subject = CreateSubjectForAConnectionWithNamedTeams(log, [new Team { Id = 2, Name = "Alpha", WorkTrackingSystemConnectionId = OwnedConnectionId }.OwningParentOverride(CausedById)]);
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.ParentOverrideForTeams = CausedById;

            await subject.Commit(handle, concurrencyToken: null, startUpdates: false, CancellationToken.None);

            Assert.That(log.Written(LogLevel.Information), Has.Count.EqualTo(1).And.Contains("Connection Connection changed the Teams' Parent Override Field default from none to Caused by; no Team follows it; no update queued."));
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_TheTeamsDefaultSetAndOneTeamFollowsIt_LogsOneEntrySayingTheTeamFollowsIt()
        {
            var log = new RecordingLogger<ConnectionSaveService>();
            var subject = CreateSubjectForAConnectionWithNamedTeams(log, [new Team { Id = 2, Name = "Alpha", WorkTrackingSystemConnectionId = OwnedConnectionId }]);
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.ParentOverrideForTeams = CausedById;

            await subject.Commit(handle, concurrencyToken: null, startUpdates: false, CancellationToken.None);

            Assert.That(log.Written(LogLevel.Information), Has.Count.EqualTo(1).And.Contains("Connection Connection changed the Teams' Parent Override Field default from none to Caused by; 1 Team follows it: Alpha; no update queued."));
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_TheTeamsDefaultCleared_LogsOneEntryNamingTheFieldItWas()
        {
            var log = new RecordingLogger<ConnectionSaveService>();
            var subject = CreateSubjectForAConnectionWithNamedTeams(log, defaultBeforeSave: CausedById);
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.ParentOverrideForTeams = null;

            await subject.Commit(handle, concurrencyToken: null, startUpdates: false, CancellationToken.None);

            Assert.That(log.Written(LogLevel.Information), Has.Count.EqualTo(1).And.Contains("Connection Connection changed the Teams' Parent Override Field default from Caused by to none; 2 Teams follow it: Alpha, Bravo; no update queued."));
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Commit_TheCopyIsStale_LetsTheConflictThroughAndLogsAndPublishesNothing()
        {
            var log = new RecordingLogger<ConnectionSaveService>();
            var subject = CreateSubjectForAConnectionWithNamedTeams(log);
            connectionRepositoryMock.Setup(x => x.Save()).ThrowsAsync(new DbUpdateConcurrencyException("another admin saved first"));
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.ParentOverrideForTeams = CausedById;
            AddAField(handle);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => subject.Commit(handle, Guid.NewGuid(), startUpdates: true, CancellationToken.None), Throws.TypeOf<DbUpdateConcurrencyException>());
                Assert.That(log.Everything, Is.Empty);
            }

            VerifyNothingWasPublished();
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_TheDefaultNamesAFieldAddedInThisSave_PointsTheDefaultAtTheStoredField()
        {
            const int StoredEpicLinkId = 77;
            var log = new RecordingLogger<ConnectionSaveService>();
            var subject = CreateSubjectForAConnectionWithNamedTeams(log);
            var handle = await OpenTheOwnedConnection(subject);
            var epicLink = new AdditionalFieldDefinition { DisplayName = "Epic Link", Reference = "customfield_10008" };
            handle.Connection.AdditionalFieldDefinitions.Add(epicLink);
            handle.DefaultsAddedInThisSave[ConnectionDefaultSetting.ParentOverrideForTeams] = epicLink;
            connectionRepositoryMock.Setup(x => x.Save()).Callback(() => epicLink.Id = StoredEpicLinkId).Returns(Task.CompletedTask);

            await subject.Commit(handle, concurrencyToken: null, startUpdates: false, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handle.Connection.ParentOverrideForTeams, Is.EqualTo(StoredEpicLinkId));
                Assert.That(log.Written(LogLevel.Information), Has.Count.EqualTo(1).And.Contains("Connection Connection changed the Teams' Parent Override Field default from none to Epic Link; 2 Teams follow it: Alpha, Bravo; no update queued."));
            }

            connectionRepositoryMock.Verify(x => x.Save(), Times.Exactly(2));
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_PointingTheDefaultAtAFieldAddedInThisSaveFails_FailsTheSaveSayingWhichDefaultWasNotApplied()
        {
            const int StoredEpicLinkId = 77;
            var log = new RecordingLogger<ConnectionSaveService>();
            var subject = CreateSubjectForAConnectionWithNamedTeams(log);
            var handle = await OpenTheOwnedConnection(subject);
            var epicLink = new AdditionalFieldDefinition { DisplayName = "Epic Link", Reference = "customfield_10008" };
            handle.Connection.AdditionalFieldDefinitions.Add(epicLink);
            handle.DefaultsAddedInThisSave[ConnectionDefaultSetting.ParentOverrideForTeams] = epicLink;
            var failure = new InvalidOperationException("the database went away");
            connectionRepositoryMock.SetupSequence(x => x.Save())
                .Returns(() =>
                {
                    epicLink.Id = StoredEpicLinkId;
                    return Task.CompletedTask;
                })
                .ThrowsAsync(failure);

            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => subject.Commit(handle, concurrencyToken: null, startUpdates: true, CancellationToken.None));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(thrown?.Message, Is.EqualTo("Connection Connection was saved with its new field Epic Link, but the Teams' default Parent Override Field could not be set to it and is now none"));
                Assert.That(thrown?.InnerException, Is.SameAs(failure));
                Assert.That(log.Everything, Is.Empty);
            }

            VerifyNothingWasPublished();
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_PointingThePortfoliosDefaultAtAFieldAddedInThisSaveFails_SaysThePortfoliosDefaultWasNotApplied()
        {
            var subject = CreateSubjectForAConnectionWithNamedTeams(new RecordingLogger<ConnectionSaveService>());
            var handle = await OpenTheOwnedConnection(subject);
            handle.DefaultsAddedInThisSave[ConnectionDefaultSetting.ParentOverrideForPortfolios] = ANewField(handle, "Epic Link");
            var failure = GivenTheSecondSaveFails();

            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => subject.Commit(handle, concurrencyToken: null, startUpdates: true, CancellationToken.None));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(thrown?.Message, Is.EqualTo("Connection Connection was saved with its new field Epic Link, but the Portfolios' default Parent Override Field could not be set to it and is now none"));
                Assert.That(thrown?.InnerException, Is.SameAs(failure));
            }
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_PointingBothDefaultsAtFieldsAddedInThisSaveFails_SaysNeitherDefaultWasApplied()
        {
            var subject = CreateSubjectForAConnectionWithNamedTeams(new RecordingLogger<ConnectionSaveService>());
            var handle = await OpenTheOwnedConnection(subject);
            handle.DefaultsAddedInThisSave[ConnectionDefaultSetting.ParentOverrideForTeams] = ANewField(handle, "Epic Link");
            handle.DefaultsAddedInThisSave[ConnectionDefaultSetting.ParentOverrideForPortfolios] = ANewField(handle, "Initiative Link");
            GivenTheSecondSaveFails();

            var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => subject.Commit(handle, concurrencyToken: null, startUpdates: true, CancellationToken.None));

            Assert.That(thrown?.Message, Is.EqualTo("Connection Connection was saved with its new fields Epic Link and Initiative Link, but the Teams' default Parent Override Field could not be set to Epic Link and the Portfolios' default Parent Override Field could not be set to Initiative Link; each is now none"));
        }

        private static AdditionalFieldDefinition ANewField(ConnectionSaveHandle handle, string displayName)
        {
            var field = new AdditionalFieldDefinition { DisplayName = displayName, Reference = $"customfield_{displayName.Length}" };
            handle.Connection.AdditionalFieldDefinitions.Add(field);
            return field;
        }

        private InvalidOperationException GivenTheSecondSaveFails()
        {
            var failure = new InvalidOperationException("the database went away");
            connectionRepositoryMock.SetupSequence(x => x.Save())
                .Returns(Task.CompletedTask)
                .ThrowsAsync(failure);
            return failure;
        }

        public enum TheTeamsParentOverride
        {
            Own,
            FollowsTheDefault,
        }

        public enum TheEdit
        {
            ChangesTheTeamsDefault,
            AddsAField,
            RenamesTheConnection,
        }

        [TestCase(TheTeamsParentOverride.Own, TheEdit.ChangesTheTeamsDefault, true, false, false)]
        [TestCase(TheTeamsParentOverride.Own, TheEdit.AddsAField, true, true, true)]
        [TestCase(TheTeamsParentOverride.Own, TheEdit.RenamesTheConnection, true, false, false)]
        [TestCase(TheTeamsParentOverride.FollowsTheDefault, TheEdit.ChangesTheTeamsDefault, true, true, false)]
        [TestCase(TheTeamsParentOverride.FollowsTheDefault, TheEdit.AddsAField, true, true, true)]
        [TestCase(TheTeamsParentOverride.FollowsTheDefault, TheEdit.RenamesTheConnection, true, false, false)]
        [TestCase(TheTeamsParentOverride.Own, TheEdit.ChangesTheTeamsDefault, false, false, false)]
        [TestCase(TheTeamsParentOverride.Own, TheEdit.AddsAField, false, false, false)]
        [TestCase(TheTeamsParentOverride.Own, TheEdit.RenamesTheConnection, false, false, false)]
        [TestCase(TheTeamsParentOverride.FollowsTheDefault, TheEdit.ChangesTheTeamsDefault, false, false, false)]
        [TestCase(TheTeamsParentOverride.FollowsTheDefault, TheEdit.AddsAField, false, false, false)]
        [TestCase(TheTeamsParentOverride.FollowsTheDefault, TheEdit.RenamesTheConnection, false, false, false)]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_UpdatesExactlyTheOwnersWhoseFetchTheSaveMoved(TheTeamsParentOverride parentOverride, TheEdit edit, bool startUpdates, bool teamUpdates, bool portfolioUpdates)
        {
            var connection = GivenTheOwnedConnection();
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = CausedById, DisplayName = "Caused by", Reference = "customfield_10500" });
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = ParentLinkId, DisplayName = "Parent Link", Reference = "customfield_10014" });
            connection.ParentOverrideForTeams = CausedById;
            var team = new Team { Id = 2, Name = "Alpha", WorkTrackingSystemConnectionId = OwnedConnectionId, WorkTrackingSystemConnection = connection };
            if (parentOverride == TheTeamsParentOverride.Own)
            {
                team.OwningParentOverride(CausedById);
            }

            var portfolio = new Portfolio { Id = 7, Name = "Card Issuing", WorkTrackingSystemConnectionId = OwnedConnectionId, WorkTrackingSystemConnection = connection };
            teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>())).Returns(() => new List<Team> { team }.AsQueryable());
            portfolioRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Portfolio, bool>>>())).Returns(() => new List<Portfolio> { portfolio }.AsQueryable());
            var published = new List<ConnectionFetchShapeChanged>();
            domainEventDispatcherMock.Setup(x => x.PublishAsync(It.IsAny<ConnectionFetchShapeChanged>(), It.IsAny<CancellationToken>()))
                .Callback<ConnectionFetchShapeChanged, CancellationToken>((change, _) => published.Add(change))
                .Returns(Task.CompletedTask);
            var subject = CreateSubject();
            var handle = await OpenTheOwnedConnection(subject);
            Apply(edit, handle);

            await subject.Commit(handle, concurrencyToken: null, startUpdates, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(published, Has.Count.EqualTo(teamUpdates || portfolioUpdates ? 1 : 0), "One change carries every owner to update, and nothing is published when nobody is.");
                Assert.That(published.SelectMany(change => change.TeamIds), teamUpdates ? Is.EqualTo(TeamsOnTheOwnedConnection) : Is.Empty);
                Assert.That(published.SelectMany(change => change.PortfolioIds), portfolioUpdates ? Is.EqualTo(PortfoliosOnTheOwnedConnection) : Is.Empty);
            }
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_TheTeamsDefaultChangedWithUpdates_LogsHowManyWereQueued()
        {
            var log = new RecordingLogger<ConnectionSaveService>();
            var subject = CreateSubjectForAConnectionWithNamedTeams(log);
            portfolioRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Portfolio, bool>>>()))
                .Returns(new List<Portfolio>().AsQueryable());
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.ParentOverrideForTeams = CausedById;

            await subject.Commit(handle, concurrencyToken: null, startUpdates: true, CancellationToken.None);

            Assert.That(log.Written(LogLevel.Information), Has.Count.EqualTo(1).And.Contains("Connection Connection changed the Teams' Parent Override Field default from none to Caused by; 2 Teams follow it: Alpha, Bravo; 2 updates queued."));
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_TheTeamsDefaultChangedForOneFollowerWithUpdates_LogsOneUpdateQueued()
        {
            var log = new RecordingLogger<ConnectionSaveService>();
            var subject = CreateSubjectForAConnectionWithNamedTeams(log, [new Team { Id = 2, Name = "Alpha", WorkTrackingSystemConnectionId = OwnedConnectionId }]);
            portfolioRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Portfolio, bool>>>()))
                .Returns(new List<Portfolio>().AsQueryable());
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.ParentOverrideForTeams = CausedById;

            await subject.Commit(handle, concurrencyToken: null, startUpdates: true, CancellationToken.None);

            Assert.That(log.Written(LogLevel.Information), Has.Count.EqualTo(1).And.Contains("Connection Connection changed the Teams' Parent Override Field default from none to Caused by; 1 Team follows it: Alpha; 1 update queued."));
        }

        [Test]
        [Category("contract-shape:bounded-change")]
        public async Task Commit_BothDefaultsChangedWithUpdates_LogsForEachDefaultTheUpdatesQueuedForItsOwnGrain()
        {
            var log = new RecordingLogger<ConnectionSaveService>();
            var subject = CreateSubjectForAConnectionWithNamedTeams(log);
            var portfolio = new Portfolio { Id = 7, Name = "Card Issuing", WorkTrackingSystemConnectionId = OwnedConnectionId, WorkTrackingSystemConnection = connectionRepositoryMock.Object.GetById(OwnedConnectionId)! };
            portfolioRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Portfolio, bool>>>()))
                .Returns(() => new List<Portfolio> { portfolio }.AsQueryable());
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.ParentOverrideForTeams = CausedById;
            handle.Connection.ParentOverrideForPortfolios = CausedById;

            await subject.Commit(handle, concurrencyToken: null, startUpdates: true, CancellationToken.None);

            Assert.That(log.Written(LogLevel.Information), Is.EquivalentTo(EachDefaultWithTheUpdatesQueuedForItsOwnGrain));
        }

        [Test]
        public async Task Commit_RemovingAFieldTeamsOnThisConnectionChoseThemselves_IsRefusedNamingThoseTeamsInTheOrderTheyWereCreated()
        {
            var teams = new List<Team>
            {
                new Team { Id = 8, Name = "Zulu", WorkTrackingSystemConnectionId = OwnedConnectionId }.OwningParentOverride(CausedById),
                new() { Id = 4, Name = "Yankee", WorkTrackingSystemConnectionId = OwnedConnectionId, EstimationAdditionalFieldDefinitionId = CausedById },
                new() { Id = 6, Name = "Owns nothing", WorkTrackingSystemConnectionId = OwnedConnectionId },
                new Team { Id = 3, Name = "Elsewhere", WorkTrackingSystemConnectionId = OtherConnectionId }.OwningParentOverride(CausedById),
            };
            var subject = CreateSubjectWithOwnersOfTheCausedByField(teams, []);
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.AdditionalFieldDefinitions.RemoveAll(field => field.Id == CausedById);

            var outcome = await subject.Commit(handle, null, startUpdates: false, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Refused, Is.True);
                Assert.That(outcome.FieldsInUse.Single().Owners, Is.EqualTo(TeamsOwningCausedByInCreationOrder));
            }

            connectionRepositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task Commit_RemovingAFieldPortfoliosOnThisConnectionChoseForAnySetting_IsRefusedNamingThosePortfoliosInTheOrderTheyWereCreated()
        {
            var portfolios = new List<Portfolio>
            {
                new() { Id = 15, Name = "Estimation", WorkTrackingSystemConnectionId = OwnedConnectionId, EstimationAdditionalFieldDefinitionId = CausedById },
                new() { Id = 14, Name = "Feature Owner", WorkTrackingSystemConnectionId = OwnedConnectionId, FeatureOwnerAdditionalFieldDefinitionId = CausedById },
                new Portfolio { Id = 13, Name = "Size", WorkTrackingSystemConnectionId = OwnedConnectionId }.OwningSizeEstimateField(CausedById),
                new Portfolio { Id = 12, Name = "Dependency", WorkTrackingSystemConnectionId = OwnedConnectionId }.OwningDependencyField(CausedById),
                new Portfolio { Id = 11, Name = "Parent Override", WorkTrackingSystemConnectionId = OwnedConnectionId }.OwningParentOverride(CausedById),
                new() { Id = 10, Name = "Owns nothing", WorkTrackingSystemConnectionId = OwnedConnectionId },
                new Portfolio { Id = 9, Name = "Elsewhere", WorkTrackingSystemConnectionId = OtherConnectionId }.OwningParentOverride(CausedById),
            };
            var subject = CreateSubjectWithOwnersOfTheCausedByField([], portfolios);
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.AdditionalFieldDefinitions.RemoveAll(field => field.Id == CausedById);

            var outcome = await subject.Commit(handle, null, startUpdates: false, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Refused, Is.True);
                Assert.That(outcome.FieldsInUse.Single().Owners, Is.EqualTo(PortfoliosOwningCausedByInCreationOrder));
            }

            connectionRepositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task Commit_RemovingTheFieldThePortfoliosDefaultNames_IsRefusedNamingThatDefault()
        {
            var subject = CreateSubjectWithOwnersOfTheCausedByField([], []);
            connectionRepositoryMock.Object.GetById(OwnedConnectionId)!.ParentOverrideForPortfolios = CausedById;
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.AdditionalFieldDefinitions.RemoveAll(field => field.Id == CausedById);

            var outcome = await subject.Commit(handle, null, startUpdates: false, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outcome.Refused, Is.True);
                Assert.That(outcome.FieldsInUse.Single().Defaults.Single(), Is.EqualTo("Portfolios' Parent Override Field"));
                Assert.That(AdditionalFieldInUse.Refusal(outcome.FieldsInUse), Is.EqualTo(
                    "\"Caused by\" cannot be removed. It is the Portfolios' Parent Override Field default. Nothing was saved."));
            }
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Commit_RemovingAFieldBothDefaultsName_IsRefusedInTheWordsThisInstanceUses()
        {
            GivenThisInstanceCalls("Squad", "Squads", "Program", "Staff");
            var subject = CreateSubjectWithOwnersOfTheCausedByField([], []);
            connectionRepositoryMock.Object.GetById(OwnedConnectionId)!.ParentOverrideForTeams = CausedById;
            connectionRepositoryMock.Object.GetById(OwnedConnectionId)!.ParentOverrideForPortfolios = CausedById;
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.AdditionalFieldDefinitions.RemoveAll(field => field.Id == CausedById);

            var outcome = await subject.Commit(handle, null, startUpdates: false, CancellationToken.None);

            Assert.That(AdditionalFieldInUse.Refusal(outcome.FieldsInUse), Is.EqualTo(
                "\"Caused by\" cannot be removed. It is the Squads' Parent Override Field default and the Staff's Parent Override Field default. Nothing was saved."));
        }

        [Test]
        [Category("contract-shape:unbounded-preservation")]
        public async Task Commit_RemovingTheFieldThePortfoliosDependencyDefaultNames_IsRefusedInTheWordsThisInstanceUses()
        {
            GivenThisInstanceCalls("Squad", "Squads", "Program", "Programs");
            var subject = CreateSubjectWithOwnersOfTheCausedByField([], []);
            connectionRepositoryMock.Object.GetById(OwnedConnectionId)!.DependencyFieldForPortfolios = CausedById;
            var handle = await OpenTheOwnedConnection(subject);
            handle.Connection.AdditionalFieldDefinitions.RemoveAll(field => field.Id == CausedById);

            var outcome = await subject.Commit(handle, null, startUpdates: false, CancellationToken.None);

            Assert.That(AdditionalFieldInUse.Refusal(outcome.FieldsInUse), Is.EqualTo(
                "\"Caused by\" cannot be removed. It is the Programs' Dependency Field default. Nothing was saved."));
        }

        private void GivenThisInstanceCalls(string team, string teams, string portfolio, string portfolios)
            => terminologyServiceMock.Setup(x => x.GetAll()).Returns(
            [
                new TerminologyEntry { Id = 1, Key = "team", DefaultValue = "Team", Value = team },
                new TerminologyEntry { Id = 2, Key = "teams", DefaultValue = "Teams", Value = teams },
                new TerminologyEntry { Id = 3, Key = "portfolio", DefaultValue = "Portfolio", Value = portfolio },
                new TerminologyEntry { Id = 4, Key = "portfolios", DefaultValue = "Portfolios", Value = portfolios },
            ]);

        private ConnectionSaveService CreateSubjectWithOwnersOfTheCausedByField(List<Team> teams, List<Portfolio> portfolios)
        {
            var connection = GivenTheOwnedConnection();
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = CausedById, DisplayName = "Caused by", Reference = "customfield_10500" });
            var elsewhere = new WorkTrackingSystemConnection { Id = OtherConnectionId, Name = "Elsewhere" };

            foreach (var owner in teams.Cast<WorkTrackingSystemOptionsOwner>().Concat(portfolios))
            {
                owner.WorkTrackingSystemConnection = owner.WorkTrackingSystemConnectionId == OwnedConnectionId ? connection : elsewhere;
            }

            teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>()))
                .Returns((Expression<Func<Team, bool>> predicate) => teams.AsQueryable().Where(predicate));
            portfolioRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Portfolio, bool>>>()))
                .Returns((Expression<Func<Portfolio, bool>> predicate) => portfolios.AsQueryable().Where(predicate));

            return CreateSubject();
        }

        private static void Apply(TheEdit edit, ConnectionSaveHandle handle)
        {
            switch (edit)
            {
                case TheEdit.ChangesTheTeamsDefault:
                    handle.Connection.ParentOverrideForTeams = ParentLinkId;
                    break;
                case TheEdit.AddsAField:
                    AddAField(handle);
                    break;
                case TheEdit.RenamesTheConnection:
                    handle.Connection.Name = "Renamed";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(edit), edit, "An edit this table does not know");
            }
        }

        private ConnectionSaveService CreateSubjectForAConnectionWithNamedTeams(RecordingLogger<ConnectionSaveService> log, List<Team>? teamsOnTheConnection = null, int? defaultBeforeSave = null)
        {
            var connection = new WorkTrackingSystemConnection { Id = OwnedConnectionId, Name = "Connection", ParentOverrideForTeams = defaultBeforeSave };
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = CausedById, DisplayName = "Caused by", Reference = "customfield_10500" });
            connectionRepositoryMock.Setup(x => x.GetById(OwnedConnectionId)).Returns(connection);

            var teams = teamsOnTheConnection ??
            [
                new() { Id = 5, Name = "Bravo", WorkTrackingSystemConnectionId = OwnedConnectionId },
                new() { Id = 2, Name = "Alpha", WorkTrackingSystemConnectionId = OwnedConnectionId },
                new Team { Id = 9, Name = "Chose None", WorkTrackingSystemConnectionId = OwnedConnectionId }.OwningParentOverride(null),
                new() { Id = 3, Name = "Elsewhere", WorkTrackingSystemConnectionId = OtherConnectionId },
            ];
            foreach (var team in teams.Where(owner => owner.WorkTrackingSystemConnectionId == OwnedConnectionId))
            {
                team.WorkTrackingSystemConnection = connection;
            }

            teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>()))
                .Returns((Expression<Func<Team, bool>> predicate) => teams.AsQueryable().Where(predicate));

            return new(connectionRepositoryMock.Object, teamRepositoryMock.Object, portfolioRepositoryMock.Object, domainEventDispatcherMock.Object, terminologyServiceMock.Object, log);
        }

        private WorkTrackingSystemConnection GivenTheOwnedConnection()
        {
            var connection = new WorkTrackingSystemConnection { Id = OwnedConnectionId, Name = "Connection" };
            connectionRepositoryMock.Setup(x => x.GetById(OwnedConnectionId)).Returns(connection);
            return connection;
        }

        private ConnectionSaveService CreateSubjectForAConnectionWithOwnersHereAndElsewhere()
        {
            var connection = GivenTheOwnedConnection();
            var elsewhere = new WorkTrackingSystemConnection { Id = OtherConnectionId, Name = "Elsewhere" };

            var teams = new List<Team>
            {
                new() { Id = 2, WorkTrackingSystemConnectionId = OwnedConnectionId, WorkTrackingSystemConnection = connection },
                new() { Id = 3, WorkTrackingSystemConnectionId = OtherConnectionId, WorkTrackingSystemConnection = elsewhere },
            };
            var portfolios = new List<Portfolio>
            {
                new() { Id = 7, WorkTrackingSystemConnectionId = OwnedConnectionId, WorkTrackingSystemConnection = connection },
                new() { Id = 8, WorkTrackingSystemConnectionId = OtherConnectionId, WorkTrackingSystemConnection = elsewhere },
            };
            teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>()))
                .Returns((Expression<Func<Team, bool>> predicate) => teams.AsQueryable().Where(predicate));
            portfolioRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Portfolio, bool>>>()))
                .Returns((Expression<Func<Portfolio, bool>> predicate) => portfolios.AsQueryable().Where(predicate));

            return CreateSubject();
        }

        private static async Task<ConnectionSaveHandle> OpenTheOwnedConnection(ConnectionSaveService subject)
        {
            var handle = await subject.Open(OwnedConnectionId, CancellationToken.None);
            Assert.That(handle, Is.Not.Null);
            return handle!;
        }

        private static void AddAField(ConnectionSaveHandle handle)
            => handle.Connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { DisplayName = "Sprint", Reference = "System.Sprint" });

        private void VerifyNothingWasPublished()
            => domainEventDispatcherMock.Verify(
                x => x.PublishAsync(It.IsAny<ConnectionFetchShapeChanged>(), It.IsAny<CancellationToken>()),
                Times.Never);

        private ConnectionSaveService CreateSubject()
            => new(connectionRepositoryMock.Object, teamRepositoryMock.Object, portfolioRepositoryMock.Object, domainEventDispatcherMock.Object, terminologyServiceMock.Object, loggerMock.Object);
    }
}
