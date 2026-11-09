using System.Text.Json.Serialization;

namespace Lighthouse.Backend.Models.Authorization
{
    /// <summary>
    /// Whether everyone signed in can view every Team and Portfolio. The instance keeps one row of it, and a missing row
    /// means off. It can only say view, so it never makes anyone an admin. A save names the token it was read with, so
    /// two System Admins cannot overwrite each other unseen.
    /// </summary>
    public class EveryoneSignedInSetting : IConcurrencyTokenEntity
    {
        [JsonIgnore]
        public int Id { get; private set; }

        public bool EveryoneCanView { get; private set; }

        public Guid ConcurrencyToken { get; set; }

        public static EveryoneSignedInSetting Off() => new();

        public static EveryoneSignedInSetting StoredAs(bool everyoneCanView) => new() { EveryoneCanView = everyoneCanView };

        public void ChangeTo(bool everyoneCanView)
        {
            EveryoneCanView = everyoneCanView;
        }
    }
}
