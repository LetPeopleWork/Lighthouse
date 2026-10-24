using System.Text.RegularExpressions;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// The line an operator reads to tell Jira's time from Lighthouse's. Judged by its sentence and by the
    /// named values a structured log keeps beside it, since either could carry something of the customer's.
    /// </summary>
    [TestFixture]
    [Category("story-6398-jira-release-loads-once")]
    [Category("slice-03")]
    [Category("us-03")]
    [Category("kpi-OUT-6398-sweep-cost-visible")]
    public partial class JiraReleaseSweepCostTest
    {
        private const string Pending = "The sweep does not write its cost yet";

        private static readonly JiraReleaseSweepCost TheReportersMorning = new(
            DeliverySourceSweepPurpose.Picker, ElapsedMs: 3912, JiraDeployment.DataCenter,
            ReleasesKept: 37, VersionsRead: 1480, VersionPages: 260, ProjectCount: 212, ProjectsSkipped: 0, Complete: true);

        [Test]
        [Ignore(Pending)]
        public void A_picker_sweep_reads_as_one_Information_line_of_counts_and_time()
        {
            var entry = TheLineWrittenFor(TheReportersMorning);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(entry.Level, Is.EqualTo(LogLevel.Information));
                Assert.That(entry.Message, Is.EqualTo(
                    "Jira Release sweep for the Picker took 3912 ms on this DataCenter connection: " +
                    "kept 37 of 1480 versions read over 260 pages from 212 projects; complete: True, projects skipped: 0"));
                Assert.That(entry.Failure, Is.Null, "the line is about cost, and an exception's text may name the host.");
            }
        }

        [Test]
        [Ignore(Pending)]
        public void The_line_carries_exactly_the_allowed_names()
        {
            var entry = TheLineWrittenFor(TheReportersMorning);

            Assert.That(entry.Properties.Keys, Is.EquivalentTo(JiraReleaseSweepCost.PropertyNames));
        }

        [Test]
        [Ignore(Pending)]
        public void A_refresh_sweep_that_lost_a_project_says_it_is_incomplete_and_how_many_were_skipped()
        {
            var entry = TheLineWrittenFor(TheReportersMorning with
            {
                Purpose = DeliverySourceSweepPurpose.Refresh, ProjectsSkipped = 1, Complete = false,
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(entry.Message, Does.StartWith("Jira Release sweep for the Refresh took"));
                Assert.That(entry.Message, Does.EndWith("complete: False, projects skipped: 1"));
                Assert.That(entry.Properties["Complete"], Is.False);
                Assert.That(entry.Properties["ProjectsSkipped"], Is.EqualTo(1));
            }
        }

        [TestCase(DeliverySourceSweepPurpose.Picker)]
        [TestCase(DeliverySourceSweepPurpose.Preview)]
        [TestCase(DeliverySourceSweepPurpose.Save)]
        [TestCase(DeliverySourceSweepPurpose.Refresh)]
        [Ignore(Pending)]
        public void The_line_opens_with_the_caller_it_was_swept_for(DeliverySourceSweepPurpose purpose)
        {
            var entry = TheLineWrittenFor(TheReportersMorning with { Purpose = purpose });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(entry.Message, Does.StartWith($"Jira Release sweep for the {purpose} took"));
                Assert.That(entry.Properties["SweepPurpose"]?.ToString(), Is.EqualTo(purpose.ToString()));
            }
        }

        [Test]
        [Ignore(Pending)]
        public void A_Cloud_connection_is_named_as_Cloud()
        {
            var entry = TheLineWrittenFor(TheReportersMorning with { Deployment = JiraDeployment.Cloud });

            Assert.That(entry.Message, Does.Contain(" on this Cloud connection: "));
        }

        [Test]
        public void The_wording_asks_for_exactly_the_allowed_names()
        {
            var placeholders = Placeholder().Matches(JiraReleaseSweepCost.Template).Select(match => match.Groups[1].Value);

            Assert.That(placeholders, Is.EquivalentTo(JiraReleaseSweepCost.PropertyNames));
        }

        private static RecordingLogger<JiraWorkTrackingConnector>.Entry TheLineWrittenFor(JiraReleaseSweepCost cost)
        {
            var logger = new RecordingLogger<JiraWorkTrackingConnector>();

            cost.WriteTo(logger);

            Assert.That(logger.Everything, Has.Count.EqualTo(1), "one sweep, one line.");
            return logger.Everything[0];
        }

        [GeneratedRegex(@"\{(\w+)\}")]
        private static partial Regex Placeholder();
    }
}
