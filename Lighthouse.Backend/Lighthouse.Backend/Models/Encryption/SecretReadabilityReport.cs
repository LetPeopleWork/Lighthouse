namespace Lighthouse.Backend.Models.Encryption
{
    // What happened to one stored secret when a pass walked past it. A pass that only looks reports
    // Unmoved for everything it could read; a pass that writes reports one of the others.
    // "Could not be read" and "could not be written" are kept apart because they send an operator to
    // different places: the first is a credential to re-enter, the second is a database that was busy and
    // a pass to run again.
    public enum SecretMoveOutcome
    {
        Unmoved,

        Moved,

        MovedByAnotherWriter,

        CouldNotBeRead,

        CouldNotBeWritten,

        NotEncrypted,
    }

    // An operator who is told a credential is unreadable and not told which one has been given a search
    // rather than an answer, so the Connection and the field that holds it travel with every record.
    public sealed record StoredSecretRecord(
        int ConnectionId,
        string ConnectionName,
        string Field,
        string? KeyId,
        SecretState State,
        SecretMoveOutcome Outcome);

    public sealed record ConnectionSecretSummary(
        int ConnectionId,
        string ConnectionName,
        int MovedCount,
        int UnreadableCount);

    // Every number here is counted off the list beside it rather than carried separately, because an
    // operator decides whether an exposure is contained from the counts and then goes looking for what is
    // still wrong in the list. Two totals that could drift apart would send them looking for a secret that
    // is fine, or leave them believing a rotation finished when it did not.
    public sealed class SecretReadabilityReport
    {
        public SecretReadabilityReport(string activeKeyId, IEnumerable<StoredSecretRecord> secrets)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(activeKeyId);
            ArgumentNullException.ThrowIfNull(secrets);

            ActiveKeyId = activeKeyId;
            Secrets = [.. secrets];

            MovedCount = Secrets.Count(secret => secret.Outcome == SecretMoveOutcome.Moved);
            UnreadableCount = Secrets.Count(secret => secret.Outcome == SecretMoveOutcome.CouldNotBeRead);

            ByConnection = [.. Secrets
                .GroupBy(secret => (secret.ConnectionId, secret.ConnectionName))
                .Select(connection => new ConnectionSecretSummary(
                    connection.Key.ConnectionId,
                    connection.Key.ConnectionName,
                    connection.Count(secret => secret.Outcome == SecretMoveOutcome.Moved),
                    connection.Count(secret => secret.Outcome == SecretMoveOutcome.CouldNotBeRead)))];
        }

        public string ActiveKeyId { get; }

        public IReadOnlyList<StoredSecretRecord> Secrets { get; }

        public int MovedCount { get; }

        public int UnreadableCount { get; }

        public IReadOnlyList<ConnectionSecretSummary> ByConnection { get; }
    }
}
