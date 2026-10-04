using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    /// <summary>
    /// The sizing log is appended to and read, never edited: there is deliberately no member that updates
    /// or removes an entry, so a vote cannot be rewritten or lost through this port.
    /// </summary>
    public interface ISizingLogRepository
    {
        void Append(SizingLogEntry entry);

        /// <returns>The Team's entries on these Work Items, oldest first.</returns>
        IEnumerable<SizingLogEntry> ReadForTeam(int teamId, IReadOnlyCollection<string> workItemReferences);
    }
}
