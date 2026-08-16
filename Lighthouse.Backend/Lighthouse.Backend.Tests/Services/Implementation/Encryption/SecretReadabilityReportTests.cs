using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models.Encryption;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    /// <summary>
    /// The one thing a rotation hands back. An operator reads the counts to decide whether the exposure
    /// is contained and reads the list to find what is still broken, so the two cannot be allowed to
    /// disagree - which is why every count here is derived from the list rather than carried beside it.
    /// </summary>
    [TestFixture]
    [Category("epic-5775-secret-encryption")]
    public class SecretReadabilityReportTests
    {
        private const string ActiveKeyId = "k-2026-08-16-01";

        private const int ContosoId = 7;

        private const int FabrikamId = 9;

        private const string Contoso = "Contoso Board";

        private const string Fabrikam = "Fabrikam Tracker";

        [Test]
        public void AReport_ExposesTheSecretsItWasBuiltFrom_InOrder()
        {
            var secrets = new[]
            {
                Secret(ContosoId, Contoso, "PersonalAccessToken", SecretMoveOutcome.Moved),
                Secret(ContosoId, Contoso, "ClientSecret", SecretMoveOutcome.CouldNotBeRead),
                Secret(FabrikamId, Fabrikam, "ApiToken", SecretMoveOutcome.Moved),
            };

            var report = new SecretReadabilityReport(ActiveKeyId, secrets);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.ActiveKeyId, Is.EqualTo(ActiveKeyId));
                Assert.That(report.Secrets, Is.EqualTo(secrets).AsCollection);
            }
        }

        [Test]
        public void TheCounts_AreExactlyWhatTheListSays()
        {
            var report = new SecretReadabilityReport(ActiveKeyId,
            [
                Secret(ContosoId, Contoso, "PersonalAccessToken", SecretMoveOutcome.Moved),
                Secret(ContosoId, Contoso, "ClientSecret", SecretMoveOutcome.CouldNotBeRead),
                Secret(FabrikamId, Fabrikam, "ApiToken", SecretMoveOutcome.Moved),
                Secret(FabrikamId, Fabrikam, "RefreshToken", SecretMoveOutcome.MovedByAnotherWriter),
            ]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.MovedCount, Is.EqualTo(2),
                    "a secret somebody else rewrote arrived at the destination by another route and is not this pass's work");
                Assert.That(report.UnreadableCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void AValueThatWasNeverEncrypted_IsNeitherMovedNorUnreadable()
        {
            var report = new SecretReadabilityReport(ActiveKeyId,
            [
                Secret(ContosoId, Contoso, "PersonalAccessToken", SecretMoveOutcome.NotEncrypted, SecretState.LegacyPlaintext),
            ]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.MovedCount, Is.Zero);
                Assert.That(report.UnreadableCount, Is.Zero,
                    "a value stored as plain text can be read perfectly well; what is wrong with it is different and is said differently");
                Assert.That(report.Secrets, Has.One.Matches<StoredSecretRecord>(
                    secret => secret.Outcome == SecretMoveOutcome.NotEncrypted));
            }
        }

        [Test]
        public void TheRollup_GroupsByConnection_AndAgreesWithTheList()
        {
            var report = new SecretReadabilityReport(ActiveKeyId,
            [
                Secret(ContosoId, Contoso, "PersonalAccessToken", SecretMoveOutcome.Moved),
                Secret(ContosoId, Contoso, "ClientSecret", SecretMoveOutcome.CouldNotBeRead),
                Secret(ContosoId, Contoso, "AccessToken", SecretMoveOutcome.Moved),
                Secret(FabrikamId, Fabrikam, "ApiToken", SecretMoveOutcome.CouldNotBeRead),
            ]);

            var contoso = report.ByConnection.Single(summary => summary.ConnectionId == ContosoId);
            var fabrikam = report.ByConnection.Single(summary => summary.ConnectionId == FabrikamId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.ByConnection, Has.Count.EqualTo(2));
                Assert.That(contoso.ConnectionName, Is.EqualTo(Contoso));
                Assert.That(contoso.MovedCount, Is.EqualTo(2));
                Assert.That(contoso.UnreadableCount, Is.EqualTo(1));
                Assert.That(fabrikam.MovedCount, Is.Zero);
                Assert.That(fabrikam.UnreadableCount, Is.EqualTo(1));
                Assert.That(report.ByConnection.Sum(summary => summary.MovedCount), Is.EqualTo(report.MovedCount));
                Assert.That(report.ByConnection.Sum(summary => summary.UnreadableCount), Is.EqualTo(report.UnreadableCount));
            }
        }

        [Test]
        public void AReportOverNothing_IsZeroEverywhere()
        {
            var report = new SecretReadabilityReport(ActiveKeyId, []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(report.Secrets, Is.Empty);
                Assert.That(report.MovedCount, Is.Zero);
                Assert.That(report.UnreadableCount, Is.Zero);
                Assert.That(report.ByConnection, Is.Empty);
            }
        }

        /// <summary>
        /// The report travels to a browser and into a log line. A record shaped to carry the value it is
        /// describing would put every stored credential on that journey, so the type is asserted to have
        /// no way of holding one.
        /// </summary>
        [Test]
        public void NoPartOfARecord_CanHoldAStoredValueOrACredential()
        {
            var readable = typeof(StoredSecretRecord).GetProperties()
                .Select(property => property.Name)
                .ToList();

            Assert.That(readable, Is.EquivalentTo(new[]
            {
                nameof(StoredSecretRecord.ConnectionId),
                nameof(StoredSecretRecord.ConnectionName),
                nameof(StoredSecretRecord.Field),
                nameof(StoredSecretRecord.KeyId),
                nameof(StoredSecretRecord.State),
                nameof(StoredSecretRecord.Outcome),
            }), "a property was added to the record that describes a secret; if it can hold the secret itself, " +
                "every stored credential now travels to a browser and into a log. Found: " + string.Join(", ", readable));
        }

        [Test]
        public void EveryPartOfAReport_RefusesWhatItCannotDescribe()
        {
            var secret = Secret(ContosoId, Contoso, "PersonalAccessToken", SecretMoveOutcome.Moved);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => new SecretReadabilityReport(" ", []), Throws.ArgumentException,
                    "a report that cannot name the key everything was moved onto describes nothing");
                Assert.That(() => new SecretReadabilityReport(ActiveKeyId, null!), Throws.ArgumentNullException);
                Assert.That(() => new SecretReadabilityReportDto(null!), Throws.ArgumentNullException);
                Assert.That(() => new StoredSecretDto(null!), Throws.ArgumentNullException);
                Assert.That(() => new ConnectionSecretSummaryDto(null!), Throws.ArgumentNullException);
                Assert.That(() => new SecretReadabilityReportDto(new SecretReadabilityReport(ActiveKeyId, [secret])), Throws.Nothing);
            }
        }

        private static StoredSecretRecord Secret(
            int connectionId,
            string connectionName,
            string field,
            SecretMoveOutcome outcome,
            SecretState state = SecretState.Envelope)
        {
            return new StoredSecretRecord(connectionId, connectionName, field, "k-2025-11-02-01", state, outcome);
        }
    }
}
