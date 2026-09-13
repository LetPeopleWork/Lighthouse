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
    /// The licence tier is NOT here and must not be added. <c>WillAskAgain</c> is derived from it on
    /// the server precisely so that the question can be answered without telling an unauthenticated
    /// caller which tier the instance runs.
    ///
    /// <c>MayAsk</c> is derived for the same reason and from more besides: how long this instance
    /// has been installed, whether an administrator has switched the asking off, and how long ago
    /// this browser was last asked. None of those are the browser's to read - the install timestamp
    /// sits behind authentication and this endpoint has none - and none of them would be safe to
    /// leave to a browser anyway, since a privacy gate decided against a clock the caller controls
    /// is not a gate.
    /// </summary>
    public sealed record UsageDataState(bool Sending, string? Decision, bool WillAskAgain, bool MayAsk);
}
