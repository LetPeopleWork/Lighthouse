using Lighthouse.Backend.Configuration;

namespace Lighthouse.Backend.Tests.Configuration
{
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataConfigurationValidatorTests
    {
        [Test]
        public void Validate_TheShippedDefaults_Pass()
        {
            var result = new UsageDataConfigurationValidator().Validate(null, new UsageDataConfiguration());

            Assert.That(result.Succeeded, Is.True,
                "a validator that rejects what the product ships with would stop every instance starting");
        }

        // The one an operator most plausibly reaches for, reading it as "do not retain". It does the
        // opposite of nothing: every row, including one written a second ago, is older than "now",
        // so the next housekeeping pass deletes every consenting browser on the instance. People
        // find the footer indicator has flipped and they are asked again, with nobody having touched
        // anything and one information-level log line to explain it.
        [Test]
        public void Validate_RetentionOfZero_IsRefused()
        {
            var result = new UsageDataConfigurationValidator()
                .Validate(null, new UsageDataConfiguration { ConsentRetentionDays = 0 });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Failed, Is.True);
                Assert.That(result.FailureMessage, Does.Contain(nameof(UsageDataConfiguration.ConsentRetentionDays)),
                    "the message has to name the setting, or an operator cannot act on it");
            }
        }

        // Collapses the throttle that keeps a read-shaped request off the write path, so every page
        // load becomes a database write - and SQLite serialises writers across the whole process.
        [Test]
        public void Validate_ALivenessWindowOfZero_IsRefused()
        {
            var result = new UsageDataConfigurationValidator()
                .Validate(null, new UsageDataConfiguration { ConsentLivenessWindowDays = 0 });

            Assert.That(result.Failed, Is.True);
        }

        [Test]
        public void Validate_AReAskWindowOfZero_IsRefused()
        {
            var result = new UsageDataConfigurationValidator()
                .Validate(null, new UsageDataConfiguration { ReAskAfterDays = 0 });

            Assert.That(result.Failed, Is.True,
                "zero means a browser that closed the dialog is asked again on its very next visit");
        }

        // The exception, and deliberately so: the end-to-end runs start an instance with it at zero
        // precisely so a fresh instance is due to ask. It is the one of these durations where "no
        // wait at all" is a coherent thing to want.
        [Test]
        public void Validate_AskingImmediately_IsAllowed()
        {
            var result = new UsageDataConfigurationValidator()
                .Validate(null, new UsageDataConfiguration { AskAfterInstallDays = 0 });

            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public void Validate_ANegativeDuration_IsRefused()
        {
            var result = new UsageDataConfigurationValidator()
                .Validate(null, new UsageDataConfiguration { AskAfterInstallDays = -1 });

            Assert.That(result.Failed, Is.True);
        }

        [Test]
        public void Validate_ReportsEverySettingThatIsWrong_NotOnlyTheFirst()
        {
            var result = new UsageDataConfigurationValidator().Validate(null, new UsageDataConfiguration
            {
                ConsentRetentionDays = 0,
                ReAskAfterDays = 0,
            });

            Assert.That(result.Failures?.Count(), Is.EqualTo(2),
                "an operator who mis-set two numbers should not have to restart twice to find out");
        }
    }
}
