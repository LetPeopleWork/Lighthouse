using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// What the browser was told about an update, in the order it was told. The terminal status is the
    /// one thing about a refresh that is never readable afterwards: the status store drops the key the
    /// moment the run ends, so a test that looks in the store after the fact sees nothing at all. The
    /// push is the only place the answer exists.
    ///
    /// Each push is copied rather than kept. The queue pushes the same <see cref="UpdateStatus"/> object
    /// it goes on to advance, so holding the reference would show every earlier push wearing the status
    /// of the last one. A real client is sent the values as they were at the moment of the push, and this
    /// records the same thing.
    /// </summary>
    public sealed class CapturedUpdateNotifications
    {
        private readonly List<Announcement> announcements = [];
        private readonly Lock gate = new();

        private readonly record struct Announcement(UpdateType UpdateType, int Id, UpdateProgress Status);

        public void Record(UpdateStatus status)
        {
            ArgumentNullException.ThrowIfNull(status);

            lock (gate)
            {
                announcements.Add(new Announcement(status.UpdateType, status.Id, status.Status));
            }
        }

        public void Clear()
        {
            lock (gate)
            {
                announcements.Clear();
            }
        }

        public IReadOnlyList<UpdateProgress> For(UpdateType updateType, int id)
        {
            lock (gate)
            {
                return
                [
                    .. announcements
                        .Where(announcement => announcement.UpdateType == updateType && announcement.Id == id)
                        .Select(announcement => announcement.Status)
                ];
            }
        }

        public UpdateProgress? LastStatusOf(UpdateType updateType, int id)
        {
            var seen = For(updateType, id);
            return seen.Count == 0 ? null : seen[^1];
        }

        /// <summary>
        /// Everything the browser was told, rendered for an assertion message. A scenario that fails on
        /// the terminal status is almost always explained by the sequence that led to it.
        /// </summary>
        public string Describe()
        {
            lock (gate)
            {
                return announcements.Count == 0
                    ? "(the browser was told nothing at all)"
                    : string.Join(" -> ", announcements.Select(a => $"{a.UpdateType}#{a.Id}={a.Status}"));
            }
        }
    }
}
