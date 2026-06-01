using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models
{
    public record SystemInfo(
        string Os,
        string Runtime,
        string Architecture,
        int ProcessId,
        string DatabaseProvider,
        string? DatabaseConnection,
        string? LogPath,
        [property: JsonPropertyName("authenticationEnabled")] bool IsAuthenticationEnabled,
        [property: JsonPropertyName("authorizationEnabled")] bool IsAuthorizationEnabled,
        IReadOnlyList<string> EmergencyAdminSubjects,
        string BaseUrl,
        string? InstallTimestamp);
}
