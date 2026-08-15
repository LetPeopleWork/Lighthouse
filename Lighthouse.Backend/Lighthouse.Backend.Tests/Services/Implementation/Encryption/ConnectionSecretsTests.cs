using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Services.Interfaces;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    public class ConnectionSecretsTests
    {
        private const string SecretFieldKey = "Personal Access Token";

        private const string UnreadableValue = "unreadable";

        private Mock<ICryptoService> cryptoServiceMock;

        [SetUp]
        public void Setup()
        {
            cryptoServiceMock = new Mock<ICryptoService>();
            cryptoServiceMock
                .Setup(x => x.Read(It.IsAny<string>()))
                .Returns((string storedValue) => new SecretReadResult(SecretState.LegacyPlaintext, storedValue, null));
            cryptoServiceMock
                .Setup(x => x.Read(UnreadableValue))
                .Returns(new SecretReadResult(SecretState.Unreadable, null, "retired"));
        }

        // A field is offered for re-entry, so naming one the operator cannot retype is worse than naming
        // nothing: a base URL is not a credential, and telling somebody to enter it again sends them looking
        // for a password box that was never there. Only options marked secret are classified at all.
        [Test]
        public void FieldsThatCannotBeRead_OptionThatIsNotASecret_IsNeverNamedHoweverItsValueReads()
        {
            var connection = new WorkTrackingSystemConnection { Name = "Company Jira" };
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = SecretFieldKey, Value = UnreadableValue, IsSecret = true });
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "Url", Value = UnreadableValue, IsSecret = false });

            var fields = ConnectionSecrets.FieldsThatCannotBeRead(connection, cryptoServiceMock.Object).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fields, Has.Count.EqualTo(1));
                Assert.That(fields[0], Is.EqualTo(SecretFieldKey));
            }
        }

        [Test]
        public void FieldsThatCannotBeRead_SecretFieldNobodyHasFilledIn_IsNotNamed()
        {
            var connection = new WorkTrackingSystemConnection { Name = "Company Jira" };
            connection.Options.Add(new WorkTrackingSystemConnectionOption { Key = SecretFieldKey, Value = string.Empty, IsSecret = true });

            var fields = ConnectionSecrets.FieldsThatCannotBeRead(connection, cryptoServiceMock.Object);

            Assert.That(fields, Is.Empty);
        }

        [Test]
        public void FieldsThatCannotBeRead_WithoutAConnection_IsRefused()
        {
            Assert.That(() => ConnectionSecrets.FieldsThatCannotBeRead(null!, cryptoServiceMock.Object), Throws.ArgumentNullException);
        }

        [Test]
        public void FieldsThatCannotBeRead_WithoutAReader_IsRefused()
        {
            var connection = new WorkTrackingSystemConnection { Name = "Company Jira" };

            Assert.That(() => ConnectionSecrets.FieldsThatCannotBeRead(connection, null!), Throws.ArgumentNullException);
        }
    }
}
