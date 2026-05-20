using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Tests.Models.OAuth
{
    public class OAuthCredentialTest() : IntegrationTestBase
    {
        [TestCase(OAuthCredentialStatus.Valid, 0)]
        [TestCase(OAuthCredentialStatus.RefreshFailed, 1)]
        [TestCase(OAuthCredentialStatus.Disconnected, 2)]
        public void OAuthCredentialStatus_HasExpectedUnderlyingValues(OAuthCredentialStatus status, int expected)
        {
            Assert.That((int)status, Is.EqualTo(expected));
        }

        [Test]
        public async Task RoundTrip_PersistsAllPropertiesAndLinksToConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Jira OAuth Connection",
                AuthenticationMethodKey = "jira.oauth",
            };
            DatabaseContext.WorkTrackingSystemConnections.Add(connection);
            await DatabaseContext.SaveChangesAsync();

            var expiresAt = new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);
            var updatedAt = new DateTimeOffset(2026, 5, 14, 13, 0, 0, TimeSpan.Zero);
            var credential = new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connection.Id,
                AccessToken = "access-token-cleartext-12345",
                RefreshToken = "refresh-token-cleartext-67890",
                ExpiresAt = expiresAt,
                Status = OAuthCredentialStatus.Valid,
                UpdatedAt = updatedAt,
            };
            DatabaseContext.OAuthCredentials.Add(credential);
            await DatabaseContext.SaveChangesAsync();

            DatabaseContext.ChangeTracker.Clear();
            var reloaded = await DatabaseContext.OAuthCredentials.SingleAsync(c => c.Id == credential.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reloaded.WorkTrackingSystemConnectionId, Is.EqualTo(connection.Id));
                Assert.That(reloaded.ExpiresAt, Is.EqualTo(expiresAt));
                Assert.That(reloaded.Status, Is.EqualTo(OAuthCredentialStatus.Valid));
                Assert.That(reloaded.UpdatedAt, Is.EqualTo(updatedAt));
                Assert.That(reloaded.AccessToken, Is.Not.Empty);
                Assert.That(reloaded.RefreshToken, Is.Not.Empty);
            }
        }

        [Test]
        public async Task AccessTokenAndRefreshToken_AreEncryptedAtRest()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Jira OAuth Connection",
                AuthenticationMethodKey = "jira.oauth",
            };
            DatabaseContext.WorkTrackingSystemConnections.Add(connection);
            await DatabaseContext.SaveChangesAsync();

            const string accessTokenCleartext = "access-token-cleartext-XYZ-very-secret";
            const string refreshTokenCleartext = "refresh-token-cleartext-ABC-also-secret";
            var credential = new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connection.Id,
                AccessToken = accessTokenCleartext,
                RefreshToken = refreshTokenCleartext,
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                Status = OAuthCredentialStatus.Valid,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            DatabaseContext.OAuthCredentials.Add(credential);
            await DatabaseContext.SaveChangesAsync();

            var connectionStored = DatabaseContext.Database.GetDbConnection();
            await connectionStored.OpenAsync();
            using var command = connectionStored.CreateCommand();
            command.CommandText = "SELECT AccessToken, RefreshToken FROM OAuthCredentials WHERE Id = $id";
            var idParameter = command.CreateParameter();
            idParameter.ParameterName = "$id";
            idParameter.Value = credential.Id;
            command.Parameters.Add(idParameter);

            using var reader = await command.ExecuteReaderAsync();
            Assert.That(await reader.ReadAsync(), Is.True);
            var storedAccessToken = reader.GetString(0);
            var storedRefreshToken = reader.GetString(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(storedAccessToken, Is.Not.EqualTo(accessTokenCleartext), "AccessToken must be encrypted at rest");
                Assert.That(storedRefreshToken, Is.Not.EqualTo(refreshTokenCleartext), "RefreshToken must be encrypted at rest");
                Assert.That(storedAccessToken, Is.Not.Empty);
                Assert.That(storedRefreshToken, Is.Not.Empty);
            }
        }

        [Test]
        public async Task DeletingParentConnection_CascadeDeletesCredential()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = "Jira OAuth Connection",
                AuthenticationMethodKey = "jira.oauth",
            };
            DatabaseContext.WorkTrackingSystemConnections.Add(connection);
            await DatabaseContext.SaveChangesAsync();

            var credential = new OAuthCredential
            {
                WorkTrackingSystemConnectionId = connection.Id,
                AccessToken = "access",
                RefreshToken = "refresh",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
                Status = OAuthCredentialStatus.Valid,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
            DatabaseContext.OAuthCredentials.Add(credential);
            await DatabaseContext.SaveChangesAsync();
            var credentialId = credential.Id;

            DatabaseContext.WorkTrackingSystemConnections.Remove(connection);
            await DatabaseContext.SaveChangesAsync();

            DatabaseContext.ChangeTracker.Clear();
            var stillExists = await DatabaseContext.OAuthCredentials.AnyAsync(c => c.Id == credentialId);
            Assert.That(stillExists, Is.False);
        }

    }
}
