using System.Globalization;
using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Implementation.Connections;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Connections
{
    public class ConnectionSaveServiceTest
    {
        private const int OwnedConnectionId = 12;

        private const int OtherConnectionId = 99;

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
