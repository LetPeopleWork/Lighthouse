namespace Lighthouse.Backend.Services.Interfaces.UsageData
{
    /// <summary>
    /// The administrator's switch for the whole feature: may Lighthouse ask, and may it send.
    ///
    /// It is one question asked in two places that must never disagree - the emit path, so that a
    /// tab left open since before the switch was flipped cannot keep sending, and the state the
    /// dialog reads, so that nobody is asked on an instance whose administrator has said no. A
    /// second copy of "a missing row means on" is how those two drift apart.
    /// </summary>
    public interface IUsageDataMasterSwitch
    {
        /// <summary>
        /// True unless an administrator has switched it off. Absence is on: the row does not exist
        /// until the slice that introduces the switch, and an upgrade that silently stopped asking
        /// would produce an uptake figure of zero for a reason nothing reports.
        /// </summary>
        bool IsOn();
    }
}
