using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Configuration
{
    /// <summary>
    /// Refuses to start on a usage-data setting that would quietly do the opposite of what it reads
    /// like.
    ///
    /// Every number here is a duration, and zero is the value an operator most plausibly reaches
    /// for meaning "none" or "off" - but none of them mean that. A retention of zero does not stop
    /// Lighthouse remembering browsers, it deletes every consenting browser on the next housekeeping
    /// pass, so people find their footer indicator has flipped and they are being asked again with
    /// nobody having touched anything. A liveness window of zero collapses the throttle that keeps a
    /// read-shaped request off the write path, turning every page load into a database write.
    ///
    /// Neither reports anything. Failing the boot is the only version of this an operator finds out
    /// about while they still remember making the change.
    /// </summary>
    public sealed class UsageDataConfigurationValidator : IValidateOptions<UsageDataConfiguration>
    {
        public ValidateOptionsResult Validate(string? name, UsageDataConfiguration options)
        {
            var complaints = new List<string>();

            Require(options.ConsentLivenessWindowDays, nameof(options.ConsentLivenessWindowDays));
            Require(options.AskAfterInstallDays, nameof(options.AskAfterInstallDays), zeroIsAllowed: true);
            Require(options.ReAskAfterDays, nameof(options.ReAskAfterDays));
            Require(options.ConsentRetentionDays, nameof(options.ConsentRetentionDays));

            if (options.DailyEventBudget < 0)
            {
                complaints.Add(
                    $"{nameof(options.DailyEventBudget)} must not be negative. Zero sends nothing, "
                    + "which is a legitimate way to switch forwarding off.");
            }

            return complaints.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(complaints);

            void Require(int value, string setting, bool zeroIsAllowed = false)
            {
                var floor = zeroIsAllowed ? 0 : 1;

                if (value < floor)
                {
                    complaints.Add(
                        $"UsageData:{setting} is {value}; it must be at least {floor} days. "
                        + "These are durations, and a zero or negative one does not switch the "
                        + "behaviour off - it makes it happen constantly.");
                }
            }
        }
    }
}
