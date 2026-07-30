namespace Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow
{
    /// <summary>
    /// One `metric_instance` row: a record sat in a state, and this is when it arrived there.
    /// </summary>
    /// <remarks>
    /// The type deliberately carries no <c>end</c> and no <c>duration</c> (ADR-118 decisions 1 and 6).
    /// Transitions are derived by pairing consecutive spans at their <see cref="Start"/>, so the 68 %
    /// of rows that are still open need no special case and the Glide duration — an epoch offset where
    /// <c>1970-01-01 21:09:13</c> means 21 h 9 min — is never parsed. Leaving the fields off the type
    /// makes that structural rather than a rule someone has to remember.
    /// </remarks>
    /// <param name="RecordId">The <c>sys_id</c> of the work item the span belongs to.</param>
    /// <param name="Label">The state label, read from <c>value</c>. Never <c>field_value</c>, which
    /// carries the instance-specific choice number.</param>
    /// <param name="Start">When the record entered the state, in universal time.</param>
    public sealed record ServiceNowStateSpan(string RecordId, string Label, DateTime Start);
}
