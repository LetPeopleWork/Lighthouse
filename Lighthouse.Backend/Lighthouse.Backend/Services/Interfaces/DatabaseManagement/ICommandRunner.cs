using System.Diagnostics;

namespace Lighthouse.Backend.Services.Interfaces.DatabaseManagement
{
    public record CommandResult(int ExitCode, string StandardOutput, string StandardError);

    public interface ICommandRunner
    {
        Task<CommandResult> RunAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken = default);

        bool IsToolAvailable(string toolName);
    }
}
