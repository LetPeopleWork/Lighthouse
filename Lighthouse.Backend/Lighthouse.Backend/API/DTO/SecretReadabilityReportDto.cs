using Lighthouse.Backend.Models.Encryption;

namespace Lighthouse.Backend.API.DTO
{
    public sealed class SecretReadabilityReportDto
    {
        public SecretReadabilityReportDto(SecretReadabilityReport report)
        {
            ArgumentNullException.ThrowIfNull(report);

            ActiveKeyId = report.ActiveKeyId;
            MovedCount = report.MovedCount;
            UnreadableCount = report.UnreadableCount;
            Secrets = [.. report.Secrets.Select(secret => new StoredSecretDto(secret))];
            ByConnection = [.. report.ByConnection.Select(connection => new ConnectionSecretSummaryDto(connection))];
        }

        public string ActiveKeyId { get; }

        public int MovedCount { get; }

        public int UnreadableCount { get; }

        public IReadOnlyList<StoredSecretDto> Secrets { get; }

        public IReadOnlyList<ConnectionSecretSummaryDto> ByConnection { get; }
    }

    public sealed class StoredSecretDto
    {
        public StoredSecretDto(StoredSecretRecord secret)
        {
            ArgumentNullException.ThrowIfNull(secret);

            ConnectionId = secret.ConnectionId;
            ConnectionName = secret.ConnectionName;
            Field = secret.Field;
            KeyId = secret.KeyId;
            State = secret.State;
            Outcome = secret.Outcome;
        }

        public int ConnectionId { get; }

        public string ConnectionName { get; }

        // Which field of which Connection, so an operator told a credential cannot be read is told where to
        // go and does not have to reissue every token to find out which one it was.
        public string Field { get; }

        public string? KeyId { get; }

        public SecretState State { get; }

        public SecretMoveOutcome Outcome { get; }
    }

    public sealed class ConnectionSecretSummaryDto
    {
        public ConnectionSecretSummaryDto(ConnectionSecretSummary connection)
        {
            ArgumentNullException.ThrowIfNull(connection);

            ConnectionId = connection.ConnectionId;
            ConnectionName = connection.ConnectionName;
            MovedCount = connection.MovedCount;
            UnreadableCount = connection.UnreadableCount;
        }

        public int ConnectionId { get; }

        public string ConnectionName { get; }

        public int MovedCount { get; }

        public int UnreadableCount { get; }
    }
}
