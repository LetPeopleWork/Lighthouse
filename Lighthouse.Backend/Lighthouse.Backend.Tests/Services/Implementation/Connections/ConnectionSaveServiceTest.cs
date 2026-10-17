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

        private static readonly int[] TeamsOnTheOwnedConnection = [2];

        private static readonly int[] PortfoliosOnTheOwnedConnection = [7];

        private static readonly string[] TokenThenSave = ["token", "save"];

        private Mock<IRepository<WorkTrackingSystemConnection>> connectionRepositoryMock;
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<IDomainEventDispatcher> domainEventDispatcherMock;
        private Mock<ILogger<ConnectionSaveService>> loggerMock;

        [SetUp]
        public void Setup()
        {
            connectionRepositoryMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
            teamRepositoryMock = new Mock<IRepository<Team>>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
            loggerMock = new Mock<ILogger<ConnectionSaveService>>();
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
            GivenTheOwnedConnection();
            var failure = new InvalidOperationException("the database went away");
            if (failingStep == FailingStep.LookingUpTheTeamsAndPortfolios)
            {
                teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>())).Throws(failure);
            }
            else
            {
                teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>()))
                    .Returns(new List<Team> { new() { Id = 2 } }.AsQueryable());
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
                Assert.That(written, Has.Some.EqualTo("Connection Connection: the Teams' default Parent Override Field changed from none to Caused by; 2 Teams follow it: Alpha, Bravo"));
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

            Assert.That(log.Written(LogLevel.Information), Has.Count.EqualTo(1).And.Contains("Connection Connection: the Teams' default Parent Override Field changed from none to Caused by; no Team follows it"));
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

            Assert.That(log.Written(LogLevel.Information), Has.Count.EqualTo(1).And.Contains("Connection Connection: the Teams' default Parent Override Field changed from none to Caused by; 1 Team follows it: Alpha"));
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

            Assert.That(log.Written(LogLevel.Information), Has.Count.EqualTo(1).And.Contains("Connection Connection: the Teams' default Parent Override Field changed from Caused by to none; 2 Teams follow it: Alpha, Bravo"));
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
            handle.TeamsParentOverrideDefaultAddedInThisSave = epicLink;
            connectionRepositoryMock.Setup(x => x.Save()).Callback(() => epicLink.Id = StoredEpicLinkId).Returns(Task.CompletedTask);

            await subject.Commit(handle, concurrencyToken: null, startUpdates: false, CancellationToken.None);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(handle.Connection.ParentOverrideForTeams, Is.EqualTo(StoredEpicLinkId));
                Assert.That(log.Written(LogLevel.Information), Has.Count.EqualTo(1).And.Contains("Connection Connection: the Teams' default Parent Override Field changed from none to Epic Link; 2 Teams follow it: Alpha, Bravo"));
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
            handle.TeamsParentOverrideDefaultAddedInThisSave = epicLink;
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
            teamRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<Team, bool>>>()))
                .Returns((Expression<Func<Team, bool>> predicate) => teams.AsQueryable().Where(predicate));

            return new(connectionRepositoryMock.Object, teamRepositoryMock.Object, portfolioRepositoryMock.Object, domainEventDispatcherMock.Object, log);
        }

        private void GivenTheOwnedConnection()
        {
            connectionRepositoryMock.Setup(x => x.GetById(OwnedConnectionId))
                .Returns(new WorkTrackingSystemConnection { Id = OwnedConnectionId, Name = "Connection" });
        }

        private ConnectionSaveService CreateSubjectForAConnectionWithOwnersHereAndElsewhere()
        {
            GivenTheOwnedConnection();

            var teams = new List<Team>
            {
                new() { Id = 2, WorkTrackingSystemConnectionId = OwnedConnectionId },
                new() { Id = 3, WorkTrackingSystemConnectionId = OtherConnectionId },
            };
            var portfolios = new List<Portfolio>
            {
                new() { Id = 7, WorkTrackingSystemConnectionId = OwnedConnectionId },
                new() { Id = 8, WorkTrackingSystemConnectionId = OtherConnectionId },
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
            => new(connectionRepositoryMock.Object, teamRepositoryMock.Object, portfolioRepositoryMock.Object, domainEventDispatcherMock.Object, loggerMock.Object);
    }
}
