using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Models.Logging
{
    /// <summary>
    /// What a problem about a refresh was a refresh of, held as a reference rather than as a name. Whoever
    /// reads the problem looks the name up then, so a team renamed after its refresh broke is read under
    /// the name it has today rather than the one it had when it broke.
    /// </summary>
    /// <param name="Kind">Which sort of work was being refreshed, as the queue names it.</param>
    /// <param name="Id">Which one of them.</param>
    /// <param name="AsWritten">
    /// The words the log line used to point at it — "Team with ID 7" and the like. Carried so that a reader
    /// with a name in hand can put it in the sentence's place without having to know how the sentence was
    /// phrased.
    /// </param>
    public sealed record RefreshSubject(UpdateType Kind, int Id, string AsWritten);
}
