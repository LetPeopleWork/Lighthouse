using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class BaselineSettingsTest
    {
        [Test]
        public void Team_ProcessBehaviourChartBaselineStartDate_DefaultsToNull()
        {
            var team = new Team();

            Assert.That(team.ProcessBehaviourChartBaselineStartDate, Is.Null);
        }

        [Test]
        public void Team_ProcessBehaviourChartBaselineEndDate_DefaultsToNull()
        {
            var team = new Team();

            Assert.That(team.ProcessBehaviourChartBaselineEndDate, Is.Null);
        }

        [Test]
        public void Team_BaselineDatesCanBeSet()
        {
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

            var team = new Team
            {
                ProcessBehaviourChartBaselineStartDate = start,
                ProcessBehaviourChartBaselineEndDate = end
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.ProcessBehaviourChartBaselineStartDate, Is.EqualTo(start));
                Assert.That(team.ProcessBehaviourChartBaselineEndDate, Is.EqualTo(end));
            }
        }

        [Test]
        public void Portfolio_ProcessBehaviourChartBaselineStartDate_DefaultsToNull()
        {
            var portfolio = new Portfolio();

            Assert.That(portfolio.ProcessBehaviourChartBaselineStartDate, Is.Null);
        }

        [Test]
        public void Portfolio_ProcessBehaviourChartBaselineEndDate_DefaultsToNull()
        {
            var portfolio = new Portfolio();

            Assert.That(portfolio.ProcessBehaviourChartBaselineEndDate, Is.Null);
        }

        [Test]
        public void Portfolio_BaselineDatesCanBeSet()
        {
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var end = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);

            var portfolio = new Portfolio
            {
                ProcessBehaviourChartBaselineStartDate = start,
                ProcessBehaviourChartBaselineEndDate = end
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portfolio.ProcessBehaviourChartBaselineStartDate, Is.EqualTo(start));
                Assert.That(portfolio.ProcessBehaviourChartBaselineEndDate, Is.EqualTo(end));
            }
        }
    }
}
