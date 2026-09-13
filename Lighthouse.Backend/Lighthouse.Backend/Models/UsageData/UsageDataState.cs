namespace Lighthouse.Backend.Models.UsageData
{
    /// <summary>
    /// What the footer indicator and the dialog need, and deliberately nothing more. It lives here
    /// rather than beside the controller because the application core returns it, and the core must
    /// not depend on the API adapter.
    ///
    /// It is served without authentication, so every field is something an anonymous caller may
    /// know: whether this instance is currently sending, what this browser previously answered, and
    /// whether it will be asked again.
    ///
    /// The licence tier is NOT here and must not be added. <c>MayAsk</c> is derived from it on the
    /// server precisely so that the question can be answered without telling an unauthenticated
    /// caller which tier the instance runs.
    ///
    /// <c>MayAsk</c> is derived for the same reason and from more besides: how long this instance
    /// has been installed, whether an administrator has stopped usage data, and how long ago
    /// this browser was last asked. None of those are the browser's to read - the install timestamp
    /// sits behind authentication and this endpoint has none - and none of them would be safe to
    /// leave to a browser anyway, since a privacy gate decided against a clock the caller controls
    /// is not a gate.
    ///
    /// <c>AdministratorDisabled</c> is the one fact here that is about the instance rather than
    /// about this browser, and it is disclosed on purpose. Without it, somebody who agreed and was
    /// then overruled sees exactly what somebody who refused sees, and reads it as their own doing.
    /// It is a boolean rather than a sentence because two surfaces render it - the footer's
    /// accessible name and the dialog's hint - and a sentence travelling over the wire would be one
    /// copy of the wording for both of them to drift from. It names no tier and no person.
    /// </summary>
    /// <remarks>
    /// <c>ReAskAfterDays</c> is the one value here that is not about this browser at all - it is the
    /// same number for every caller, straight from configuration. It has to travel because the
    /// browser holds the half of the cadence the server cannot see: a browser with no consent row
    /// that was shown the dialog and closed it leaves nothing behind on the server, so only the
    /// browser can tell when that was, and only the server knows how long it should count for.
    /// </remarks>
    public sealed record UsageDataState(
        bool Sending, string? Decision, bool MayAsk, int ReAskAfterDays, bool AdministratorDisabled);
}
