using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // The connection screen, the validate button and a refresh that stopped all have to name the same field
    // for the same connection. Told to re-enter the API token on one screen and the client secret on
    // another, an operator has no way to tell which is true, and re-issuing the wrong credential is the
    // wasted afternoon this work exists to prevent. They agree because they ask here, rather than because
    // three copies of one query happen to still match.
    public static class ConnectionSecrets
    {
        // Left deferred so a caller that only wants the first unreadable field stops reading there. Every
        // field this walks past costs an attempt to decrypt whatever is stored in it.
        public static IEnumerable<string> FieldsThatCannotBeRead(WorkTrackingSystemConnection connection, ICryptoService cryptoService)
        {
            ArgumentNullException.ThrowIfNull(connection);
            ArgumentNullException.ThrowIfNull(cryptoService);

            return connection.Options
                .Where(option => option.IsSecret && !string.IsNullOrEmpty(option.Value))
                .Where(option => cryptoService.Read(option.Value) is { State: SecretState.Unreadable })
                .Select(option => option.Key);
        }
    }
}
