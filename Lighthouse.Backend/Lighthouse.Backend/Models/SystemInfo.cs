namespace Lighthouse.Backend.Models
{
    public record SystemInfo(
        string Os,
        string Runtime,
        string Architecture,
        int ProcessId,
        string DatabaseProvider,
        string? DatabaseConnection,
        string? LogPath);
}
