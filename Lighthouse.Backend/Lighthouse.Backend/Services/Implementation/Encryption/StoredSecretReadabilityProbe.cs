using Lighthouse.Backend.Models.Encryption;
using System.Data.Common;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // Four answers, for the same reason the presence probe has three: the question is asked while the
    // application is still being built, so "I could not ask" has to be tellable apart from "I asked and the
    // answer is none". Only one of these is worth stopping a start over.
    public enum StoredSecretReadability
    {
        CannotTell,
        NothingStored,
        SomethingReadable,
        NothingReadable,
    }

    // The answer, and the names the stored values put on themselves while it was being worked out. The
    // names are gathered from the same pass rather than from a second query: a start that reads every
    // secret column twice gets slow on exactly the instance that is already in trouble. They are only
    // read when nothing could be read at all, which is the one case where the pass has seen every value.
    public sealed record StoredSecretFinding(StoredSecretReadability Readability, IReadOnlyList<string> KeyIdsSeen);

    public interface IStoredSecretReadabilityProbe
    {
        StoredSecretFinding Look(EncryptionKeyRing ring);
    }

    // An instance that starts on the wrong key looks exactly like an instance that starts on the right one:
    // the banner is cheerful, every page loads, and the first anyone hears of it is a sync failing hours
    // later against a credential nobody touched. This is the one question that tells those two apart, and it
    // can only be asked once the key is known - so it is asked after resolution rather than during it.
    public static class KeyThatReadsNothing
    {
        // Both keys are named, and neither naming costs anything in secrecy: a key id is not a key, and the
        // encryption settings page already lists them. Without them an operator is told two keys disagree
        // and left to work out which two.
        //
        // What this must not do is promise. The old wording said "nothing is lost - the credentials are
        // still there, encrypted under the key they were written with", which is true of an operator who
        // pointed at the wrong key and false of one whose key store was destroyed. Lighthouse cannot tell
        // those apart, so it says the thing that is true either way: this start changed nothing.
        public static string RefusalFor(EncryptionKeyRing resolved, IReadOnlyList<string> keyIdsStoredValuesName)
        {
            ArgumentNullException.ThrowIfNull(resolved);
            ArgumentNullException.ThrowIfNull(keyIdsStoredValuesName);

            return string.Join(
                " ",
                $"This instance has stored credentials and not one of them can be read with the key it started on, '{resolved.ActiveKey.Id}'.",
                WhatTheCredentialsSay(keyIdsStoredValuesName),
                "Nothing has been changed by this start: every stored value is exactly as it was.",
                TheRemedyThatFits(resolved.Custody),
                "Otherwise set Encryption__Key to the key those credentials were written under, or set " +
                "Encryption__KeyStorePath to the key store that belongs to this database, and start Lighthouse again.",
                $"If that key is genuinely gone, set {StartAnywayEnvironmentVariable}=true to start without it. " +
                "Lighthouse will then run with credentials it cannot read and every one of them has to be " +
                "entered again - the encryption settings name the Connection and the field each one sits in. " +
                "Nothing is deleted, and while that setting is in force it is said on every start and on that page.");
        }

        // Spelled from the setting name rather than beside it, because a refusal quoting a variable that no
        // longer exists is worse than one that quotes none.
        private static string StartAnywayEnvironmentVariable =>
            EncryptionKeyRingBootstrapper.StartAnywaySettingKey.Replace(":", "__", StringComparison.Ordinal);

        private static string WhatTheCredentialsSay(IReadOnlyList<string> keyIds)
        {
            if (keyIds.Count == 0)
            {
                return "They carry no key name at all, so they were written before the release that started putting one on them.";
            }

            return $"They say they were written under '{string.Join("', '", keyIds)}'.";
        }

        // The likeliest cause is not the one the old wording assumed. An operator who has been minting for
        // months and sets an encryption key for the first time displaces the minted key out of the ring: the
        // key store is already correct, so pointing at it does nothing, and the key it asks them to supply
        // was generated and kept in a file they never read. Undoing what they just did is the only remedy
        // they can carry out unaided, so it goes first - but only where there is something to undo.
        private static string TheRemedyThatFits(KeyCustody custody)
        {
            return custody is KeyCustody.SuppliedByConfiguration or KeyCustody.SuppliedByExternalSecret
                ? "If you have just started supplying an encryption key to an instance that was managing its own, remove that setting and start Lighthouse again - the key it was using is still in its key store."
                : "This instance is using a key it made for itself, so the key store it is reading is not the one those credentials were written under.";
        }
    }

    // Reads the stored values through a raw connection for the same reason the presence probe does: there is
    // no database context yet. Every secret is asked about, because the answer that matters is whether even
    // one of them can still be read - one is enough to say the key is the right one.
    public sealed class DatabaseSecretReadabilityProbe : IStoredSecretReadabilityProbe
    {
        private static readonly string[] StoredSecretQueries =
        [
            """SELECT "Value" FROM "WorkTrackingSystemConnectionOption" WHERE "IsSecret" = true AND "Value" IS NOT NULL AND "Value" <> ''""",
            """SELECT "AccessToken" FROM "OAuthCredentials" WHERE "AccessToken" IS NOT NULL AND "AccessToken" <> ''""",
            """SELECT "RefreshToken" FROM "OAuthCredentials" WHERE "RefreshToken" IS NOT NULL AND "RefreshToken" <> ''""",
        ];

        private readonly Func<DbConnection> connectionFactory;

        public DatabaseSecretReadabilityProbe(Func<DbConnection> connectionFactory)
        {
            ArgumentNullException.ThrowIfNull(connectionFactory);

            this.connectionFactory = connectionFactory;
        }

        public StoredSecretFinding Look(EncryptionKeyRing ring)
        {
            ArgumentNullException.ThrowIfNull(ring);

            var classifier = new SecretStateClassifier(new EncryptionKeyRingHolder(ring));

            try
            {
                using var connection = connectionFactory();
                connection.Open();

                var stored = 0;
                var keyIdsSeen = new List<string>();

                foreach (var value in StoredSecretQueries.SelectMany(query => ValuesFrom(connection, query)))
                {
                    stored++;

                    var read = classifier.Classify(value);

                    if (read.KeyId is not null && !keyIdsSeen.Contains(read.KeyId, StringComparer.Ordinal))
                    {
                        keyIdsSeen.Add(read.KeyId);
                    }

                    if (read.State != SecretState.Unreadable)
                    {
                        return new StoredSecretFinding(StoredSecretReadability.SomethingReadable, keyIdsSeen);
                    }
                }

                return new StoredSecretFinding(
                    stored == 0 ? StoredSecretReadability.NothingStored : StoredSecretReadability.NothingReadable,
                    keyIdsSeen);
            }
            // A database that will not answer is not an answer. It refuses in two different ways - the
            // server saying no, and there being no server configured to ask at all - and this question is
            // asked on every start, so treating either as "nothing here can be read" would stop instances
            // that are perfectly fine.
            catch (Exception failedToAsk) when (failedToAsk is DbException or InvalidOperationException)
            {
                return new StoredSecretFinding(StoredSecretReadability.CannotTell, []);
            }
        }

        private static IEnumerable<string> ValuesFrom(DbConnection connection, string query)
        {
            using var command = connection.CreateCommand();
            command.CommandText = query;

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                if (!reader.IsDBNull(0))
                {
                    yield return reader.GetString(0);
                }
            }
        }
    }
}
