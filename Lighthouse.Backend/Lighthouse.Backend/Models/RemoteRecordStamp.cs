namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// One record as the identity sweep saw it: which record it is, and when the tracker says it last
    /// changed (Epic #5687, D1/D12). The comparison is per item against the stored stamp - there is no
    /// global watermark, so clock skew and server-time drift are not part of the decision.
    /// </summary>
    public sealed record RemoteRecordStamp(string ReferenceId, DateTime ChangedAt);
}
