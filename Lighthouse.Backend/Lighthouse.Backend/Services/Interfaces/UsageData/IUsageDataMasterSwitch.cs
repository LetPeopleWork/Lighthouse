namespace Lighthouse.Backend.Services.Interfaces.UsageData
{
    /// <summary>
    /// The administrator's veto over the whole feature: may Lighthouse ask, and may it send.
    ///
    /// It is one question asked in three places that must never disagree - the emit path, so that a
    /// tab left open since before the veto was engaged cannot keep sending; the state the dialog
    /// reads, so that nobody is asked on an instance whose administrator has said no; and the
    /// sentence the footer shows, so that nothing claims to be sending while every batch is
    /// dropped. A second copy of "a missing row means nothing is vetoed" is how those drift apart.
    /// </summary>
    public interface IUsageDataMasterSwitch
    {
        /// <summary>
        /// True unless an administrator has stopped usage data for this instance. The stored value
        /// is a veto, so it is true that reads as "stopped" - and an absent row means nobody has
        /// stopped anything. An upgrade that silently went quiet would produce an uptake figure of
        /// zero for a reason nothing reports.
        /// </summary>
        bool IsAllowed();
    }
}
