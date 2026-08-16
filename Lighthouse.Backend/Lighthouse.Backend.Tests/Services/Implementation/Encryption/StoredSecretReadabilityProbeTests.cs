using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    // Asked of a real database rather than a stub, because the question this answers is entirely about what
    // is in one: which columns hold secrets, whether a row with nothing in it counts, and what happens when
    // the schema the query names is not there yet.
    public class StoredSecretReadabilityProbeTests
    {
        private const string TheKeyInForce = "k-in-force-01";

        private const string AKeyThisInstanceNeverHad = "k-somewhere-else-01";

        private const string AnotherKeyThisInstanceNeverHad = "k-elsewhere-02";

        private SqliteConnection keptOpenSoTheInMemoryDatabaseSurvives = null!;

        private string connectionString = string.Empty;

        [SetUp]
        public void SetUp()
        {
            connectionString = $"Data Source=readability-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";

            keptOpenSoTheInMemoryDatabaseSurvives = new SqliteConnection(connectionString);
            keptOpenSoTheInMemoryDatabaseSurvives.Open();
        }

        [TearDown]
        public void TearDown()
        {
            keptOpenSoTheInMemoryDatabaseSurvives.Dispose();
            SqliteConnection.ClearAllPools();
        }

        [Test]
        public void Look_NoSchemaToAsk_CannotTellRatherThanClaimingNothingIsThere()
        {
            Assert.That(Probe().Look(RingInForce()).Readability, Is.EqualTo(StoredSecretReadability.CannotTell));
        }

        [Test]
        public void Look_AConnectionThatCannotBeMadeAtAll_CannotTell()
        {
            var probe = new DatabaseSecretReadabilityProbe(
                () => throw new InvalidOperationException("Database:Provider is not set."));

            Assert.That(probe.Look(RingInForce()).Readability, Is.EqualTo(StoredSecretReadability.CannotTell));
        }

        [Test]
        public void Look_TheTablesAreThereAndEmpty_NothingStored()
        {
            CreateSchema();

            Assert.That(Probe().Look(RingInForce()).Readability, Is.EqualTo(StoredSecretReadability.NothingStored));
        }

        [Test]
        public void Look_EverySecretWrittenUnderAKeyThisInstanceDoesNotHave_NothingReadable()
        {
            CreateSchema();
            StoreConnectionSecret(WrittenUnder(AKeyThisInstanceNeverHad));
            StoreOAuthCredential(WrittenUnder(AKeyThisInstanceNeverHad), WrittenUnder(AKeyThisInstanceNeverHad));

            Assert.That(Probe().Look(RingInForce()).Readability, Is.EqualTo(StoredSecretReadability.NothingReadable));
        }

        [Test]
        public void Look_OneSecretAmongManyStillReadable_SomethingReadable()
        {
            CreateSchema();
            StoreConnectionSecret(WrittenUnder(AKeyThisInstanceNeverHad));
            StoreConnectionSecret(WrittenUnder(TheKeyInForce));

            Assert.That(Probe().Look(RingInForce()).Readability, Is.EqualTo(StoredSecretReadability.SomethingReadable));
        }

        [Test]
        public void Look_AnOAuthRefreshTokenIsTheOnlyThingStored_IsStillAsked()
        {
            CreateSchema();
            StoreOAuthCredential(accessToken: null, refreshToken: WrittenUnder(AKeyThisInstanceNeverHad));

            Assert.That(Probe().Look(RingInForce()).Readability, Is.EqualTo(StoredSecretReadability.NothingReadable));
        }

        // A row that holds no secret is not a secret that cannot be read. Counting one would refuse to start
        // an instance whose only fault is an option someone left blank.
        [Test]
        public void Look_RowsHoldingNothing_AreNotCountedAsSecrets()
        {
            CreateSchema();
            StoreConnectionSecret(null);
            StoreConnectionSecret(string.Empty);
            StoreOAuthCredential(null, null);

            Assert.That(Probe().Look(RingInForce()).Readability, Is.EqualTo(StoredSecretReadability.NothingStored));
        }

        // Values on connection options that were never marked secret are not encrypted, so reading them as
        // though they were would make every instance carrying one look like an instance on the wrong key.
        [Test]
        public void Look_AValueThatWasNeverASecret_IsNotAsked()
        {
            CreateSchema();
            StoreConnectionSecret(WrittenUnder(AKeyThisInstanceNeverHad), isSecret: false);

            Assert.That(Probe().Look(RingInForce()).Readability, Is.EqualTo(StoredSecretReadability.NothingStored));
        }

        // The names the stored values put on themselves. An operator staring at a start that stopped is
        // told which key their credentials were written under and which one the instance came up on, and
        // neither of those is a secret - a key id is already on the encryption panel.
        [Test]
        public void Look_EverySecretUnderOneKeyThisInstanceDoesNotHave_NamesThatKey()
        {
            CreateSchema();
            StoreConnectionSecret(WrittenUnder(AKeyThisInstanceNeverHad));
            StoreOAuthCredential(WrittenUnder(AKeyThisInstanceNeverHad), null);

            Assert.That(Probe().Look(RingInForce()).KeyIdsSeen, Is.EqualTo(new[] { AKeyThisInstanceNeverHad }).AsCollection);
        }

        [Test]
        public void Look_SecretsUnderTwoKeysNeitherOfWhichIsHeld_NamesBothOnceEach()
        {
            CreateSchema();
            StoreConnectionSecret(WrittenUnder(AKeyThisInstanceNeverHad));
            StoreConnectionSecret(WrittenUnder(AKeyThisInstanceNeverHad));
            StoreConnectionSecret(WrittenUnder(AnotherKeyThisInstanceNeverHad));

            Assert.That(
                Probe().Look(RingInForce()).KeyIdsSeen,
                Is.EqualTo(new[] { AKeyThisInstanceNeverHad, AnotherKeyThisInstanceNeverHad }).AsCollection,
                "an operator reading the same key id three times learns nothing they did not learn the first time");
        }

        [Test]
        public void Look_AValueInTheFormatThisVersionReplaced_NamesNoKey()
        {
            CreateSchema();
            StoreConnectionSecret("this was never an envelope");

            Assert.That(
                Probe().Look(RingInForce()).KeyIdsSeen,
                Is.Empty,
                "a value written before this version carries no key id at all, and inventing one would name a key that never existed");
        }

        [Test]
        public void Look_NothingStored_NamesNoKey()
        {
            CreateSchema();

            Assert.That(Probe().Look(RingInForce()).KeyIdsSeen, Is.Empty);
        }

        [Test]
        public void Look_ADatabaseThatCannotBeAsked_NamesNoKey()
        {
            Assert.That(
                Probe().Look(RingInForce()).KeyIdsSeen,
                Is.Empty,
                "no answer is not an answer, and half of one would be quoted at an operator as though it were");
        }

        [Test]
        public void Look_AnOAuthAccessTokenIsTheOnlyThingStored_IsStillAsked()
        {
            CreateSchema();
            StoreOAuthCredential(accessToken: WrittenUnder(AKeyThisInstanceNeverHad), refreshToken: null);

            Assert.That(Probe().Look(RingInForce()).Readability, Is.EqualTo(StoredSecretReadability.NothingReadable),
                "a connection whose access token is the only secret it holds is still a connection about to stop working");
        }

        [Test]
        public void TheProbe_RefusesToBeBuiltWithoutSomewhereToLook()
        {
            Assert.That(() => new DatabaseSecretReadabilityProbe(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void Look_NoRingToTryThingsAgainst_Refuses()
        {
            Assert.That(() => Probe().Look(null!), Throws.ArgumentNullException);
        }

        private static string WrittenUnder(string keyId)
        {
            return SecretEnvelope.Protect("a token", keyId, MaterialFor(keyId)).Format();
        }

        private static byte[] MaterialFor(string keyId)
        {
            return Enumerable.Repeat((byte)keyId.Length, EncryptionKey.MaterialLength).ToArray();
        }

        private static EncryptionKeyRing RingInForce()
        {
            return new EncryptionKeyRing(
                KeyCustody.GeneratedForThisInstance,
                new EncryptionKey(TheKeyInForce, MaterialFor(TheKeyInForce)));
        }

        private DatabaseSecretReadabilityProbe Probe()
        {
            return new DatabaseSecretReadabilityProbe(() => new SqliteConnection(connectionString));
        }

        private void CreateSchema()
        {
            Execute(
                """CREATE TABLE "WorkTrackingSystemConnectionOption" ("Id" INTEGER PRIMARY KEY, "Value" TEXT, "IsSecret" INTEGER)""");
            Execute(
                """CREATE TABLE "OAuthCredentials" ("Id" INTEGER PRIMARY KEY, "AccessToken" TEXT, "RefreshToken" TEXT)""");
        }

        private void StoreConnectionSecret(string? value, bool isSecret = true)
        {
            Execute(
                """INSERT INTO "WorkTrackingSystemConnectionOption" ("Value", "IsSecret") VALUES ($value, $isSecret)""",
                ("$value", value),
                ("$isSecret", isSecret ? 1 : 0));
        }

        private void StoreOAuthCredential(string? accessToken, string? refreshToken)
        {
            Execute(
                """INSERT INTO "OAuthCredentials" ("AccessToken", "RefreshToken") VALUES ($access, $refresh)""",
                ("$access", accessToken),
                ("$refresh", refreshToken));
        }

        private void Execute(string sql, params (string Name, object? Value)[] parameters)
        {
            using var command = keptOpenSoTheInMemoryDatabaseSurvives.CreateCommand();
            command.CommandText = sql;

            foreach (var (name, value) in parameters)
            {
                command.Parameters.AddWithValue(name, value ?? (object)DBNull.Value);
            }

            command.ExecuteNonQuery();
        }
    }
}
