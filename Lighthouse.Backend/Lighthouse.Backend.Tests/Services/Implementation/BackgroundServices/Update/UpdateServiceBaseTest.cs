using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices.Update
{
    public class UpdateServiceBaseTest
    {
        private const string ConnectionName = "Company Jira";

        private const string SecretFieldKey = "Personal Access Token";

        private const string ReadableValue = "readable";

        private const string UnreadableValue = "unreadable";

        private Mock<ICryptoService> cryptoServiceMock;

        [SetUp]
        public void Setup()
        {
            cryptoServiceMock = new Mock<ICryptoService>();
            cryptoServiceMock
                .Setup(x => x.Read(It.IsAny<string>()))
                .Returns((string storedValue) => new SecretReadResult(SecretState.Envelope, storedValue, "current"));
            cryptoServiceMock
                .Setup(x => x.Read(UnreadableValue))
                .Returns(new SecretReadResult(SecretState.Unreadable, null, "retired"));
        }

        [Test]
        public void BuildUnreadableSecretReason_NamesTheConnectionAndTheFieldHoldingTheUnreadableCredential()
        {
            var connection = CreateConnection((SecretFieldKey, UnreadableValue));

            var reason = ReasonProbe.Build(CreateException(), connection, cryptoServiceMock.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reason, Does.Contain(ConnectionName),
                    "An operator with several connections cannot act on a reason that does not say which one broke.");
                Assert.That(reason, Does.Contain(SecretFieldKey),
                    "Naming the connection but not the field leaves the operator guessing which credential to re-enter.");
            }
        }

        [Test]
        public void BuildUnreadableSecretReason_SaysTheStoredCredentialCouldNotBeRead_NeverThatItWasRejected()
        {
            var connection = CreateConnection((SecretFieldKey, UnreadableValue));

            var reason = ReasonProbe.Build(CreateException(), connection, cryptoServiceMock.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reason, Does.Contain("cannot be read").IgnoreCase,
                    "The whole point of the reason is that this instance could not read its own stored credential.");
                Assert.That(reason, Does.Not.Contain("reject").IgnoreCase);
                Assert.That(reason, Does.Not.Contain("refus").IgnoreCase);
                Assert.That(reason, Does.Not.Contain("invalid").IgnoreCase);
                Assert.That(reason, Does.Not.Contain("expired").IgnoreCase,
                    "Rejection wording sends the operator to reissue a token the work tracking system never saw.");
            }
        }

        [Test]
        public void BuildUnreadableSecretReason_SeveralUnreadableFields_NamesEveryOneOfThem()
        {
            var connection = CreateConnection((SecretFieldKey, UnreadableValue), ("Client Secret", UnreadableValue));

            var reason = ReasonProbe.Build(CreateException(), connection, cryptoServiceMock.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reason, Does.Contain(SecretFieldKey));
                Assert.That(reason, Does.Contain("Client Secret"),
                    "Re-entering one of two broken credentials leaves the refresh failing for the same reason.");
            }
        }

        [Test]
        public void BuildUnreadableSecretReason_ClassifiesEverySecretByReadingIt_AndNamesOnlyTheUnreadableOnes()
        {
            var connection = CreateConnection((SecretFieldKey, UnreadableValue), ("Username", ReadableValue));
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Url", Value = "https://example.com", IsSecret = false });

            var reason = ReasonProbe.Build(CreateException(), connection, cryptoServiceMock.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reason, Does.Not.Contain("Username"),
                    "A field that reads fine is not the one to re-enter.");
                Assert.That(reason, Does.Not.Contain("Url"),
                    "Only secrets are classified; a plain option has nothing to be unreadable about.");
                cryptoServiceMock.Verify(x => x.Read(UnreadableValue), Times.Once);
                cryptoServiceMock.Verify(x => x.Read(ReadableValue), Times.Once);
                cryptoServiceMock.Verify(x => x.Decrypt(It.IsAny<string>()), Times.Never,
                    "The connection screen classifies by asking the total reader. Deciding the field any other way lets the two surfaces name different fields for the same connection.");
            }
        }

        [Test]
        public void BuildUnreadableSecretReason_SeveralUnreadableFields_SeparatesThemSoBothCanBeRead()
        {
            var connection = CreateConnection((SecretFieldKey, UnreadableValue), ("Client Secret", UnreadableValue));

            var reason = ReasonProbe.Build(CreateException(), connection, cryptoServiceMock.Object);

            Assert.That(reason, Does.Contain($"{SecretFieldKey}, Client Secret"),
                "Run together, two field names read as one field nobody has.");
        }

        // A refresh token lives on the OAuth credential rather than on a connection option, so the read that
        // failed leaves nothing on the connection to name. Saying "The stored " and then nothing at all is
        // worse than saying less: it reads as a truncated message and tells the operator to look for a field
        // that does not exist.
        [Test]
        public void BuildUnreadableSecretReason_NoConnectionFieldIsUnreadable_StillNamesACredentialRatherThanNothing()
        {
            var connection = CreateConnection((SecretFieldKey, ReadableValue));

            var reason = ReasonProbe.Build(CreateException(), connection, cryptoServiceMock.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reason, Does.StartWith("A stored credential"));
                Assert.That(reason, Does.Contain(ConnectionName));
            }
        }

        [Test]
        public void BuildUnreadableSecretReason_ExceptionNamingAKey_RepeatsThatKeyIdSoTheOperatorCanTellWhichOneIsMissing()
        {
            var connection = CreateConnection((SecretFieldKey, UnreadableValue));

            var reason = ReasonProbe.Build(new UnreadableSecretException(SecretState.Unreadable, "key-retired"), connection, cryptoServiceMock.Object);

            Assert.That(reason, Does.Contain("it names encryption key 'key-retired'"));
        }

        // A legacy blob carries no key id at all, and neither does an empty one. Either way there is no key to
        // name, and printing an empty pair of quotes claims the secret names a key called nothing.
        [TestCase(null, TestName = "BuildUnreadableSecretReason_ExceptionWithNoKeyId")]
        [TestCase("", TestName = "BuildUnreadableSecretReason_ExceptionWithABlankKeyId")]
        public void BuildUnreadableSecretReason_ExceptionNamingNoKey_MentionsNoKeyAtAll(string? claimedKeyId)
        {
            var connection = CreateConnection((SecretFieldKey, UnreadableValue));

            var reason = ReasonProbe.Build(new UnreadableSecretException(SecretState.LegacyCbc, claimedKeyId), connection, cryptoServiceMock.Object);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reason, Does.Not.Contain("it names encryption key"));
                Assert.That(reason, Does.Not.Contain("''"));
                Assert.That(reason, Does.EndWith(
                    "cannot be read with the current encryption key, so this refresh stopped before contacting the work tracking system. " +
                    "Enter the credential again to store it under the key this instance uses now."),
                    "The sentence has to survive whole: it is the only place the operator is told what happened and what to do about it.");
            }
        }

        /// <summary>
        /// Every connector reaches this one composer, so the cut has to hold here rather than in whichever of
        /// them happened to think of it.
        /// </summary>
        [Test]
        public void BuildRefusalReason_QueryLongerThanALogLine_CutsItAndSaysItWasCut()
        {
            var enormousQuery = new string('x', 4000);

            var reason = ReasonProbe.Build(
                new WorkTrackingRefusedException("Jira could not parse this query.", enormousQuery, HttpStatusCode.BadRequest));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reason, Has.Length.LessThan(1000),
                    "This is one line in a log an operator scrolls through; it has to end somewhere.");
                Assert.That(reason, Does.Contain("Jira could not parse this query."),
                    "Shortening the query must not cost the sentence that names what was wrong.");
                Assert.That(reason, Does.EndWith("…"),
                    "A query that simply stops mid-clause reads like the query Lighthouse sent was itself cut short.");
            }
        }

        /// <summary>
        /// A query split between the two halves of a surrogate pair leaves a character that cannot be written
        /// as UTF-8 at all, and this text reaches log sinks and the browser as JSON.
        /// </summary>
        [Test]
        public void BuildRefusalReason_CutLandsInsideACharacterBuiltFromTwoHalves_LeavesNoHalfBehind()
        {
            var queryEndingInAstralCharacters = new string('x', 499) + string.Concat(Enumerable.Repeat("🚀", 20));

            var reason = ReasonProbe.Build(
                new WorkTrackingRefusedException("Jira could not parse this query.", queryEndingInAstralCharacters, HttpStatusCode.BadRequest));

            Assert.That(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(reason)), Is.EqualTo(reason),
                "Written out and read back, the line has to say the same thing - a half character left at the "
                + "cut comes back as a replacement mark, and everything downstream stores that instead.");
        }

        private static WorkTrackingSystemConnection CreateConnection(params (string Key, string Value)[] secrets)
        {
            var connection = new WorkTrackingSystemConnection { Name = ConnectionName };

            foreach (var (key, value) in secrets)
            {
                connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = key, Value = value, IsSecret = true });
            }

            return connection;
        }

        private static UnreadableSecretException CreateException()
        {
            return new UnreadableSecretException(SecretState.Unreadable, "retired");
        }
    }

    /// <summary>
    /// Reaches the shared reason helper from a test. Every updater inherits it, so it is exercised here once
    /// rather than once per updater.
    /// </summary>
    public sealed class ReasonProbe : UpdateServiceBase<Team>
    {
        public ReasonProbe()
            : base(Mock.Of<ILogger<UpdateServiceBase<Team>>>(), Mock.Of<IServiceScopeFactory>(), Mock.Of<IUpdateQueueService>(), UpdateType.Team)
        {
        }

        public static string Build(UnreadableSecretException exception, WorkTrackingSystemConnection connection, ICryptoService cryptoService)
        {
            return BuildUnreadableSecretReason(exception, connection, cryptoService);
        }

        public static string Build(WorkTrackingRefusedException refusal)
        {
            return BuildRefusalReason(refusal);
        }

        protected override RefreshSettings GetRefreshSettings() => new();

        protected override Task Update(int id, IServiceProvider serviceProvider) => Task.CompletedTask;

        protected override bool ShouldUpdateEntity(Team entity, RefreshSettings refreshSettings) => false;
    }
}
