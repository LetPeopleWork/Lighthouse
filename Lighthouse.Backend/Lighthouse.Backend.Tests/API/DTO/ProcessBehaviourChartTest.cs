using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class ProcessBehaviourChartTest
    {
        [Test]
        public void NotReady_BaselineMissing_ReturnsCorrectStatus()
        {
            var dto = ProcessBehaviourChart.NotReady(BaselineStatus.BaselineMissing, "No baseline configured.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.Status, Is.EqualTo(BaselineStatus.BaselineMissing));
                Assert.That(dto.StatusReason, Is.EqualTo("No baseline configured."));
                Assert.That(dto.DataPoints, Is.Empty);
                Assert.That(dto.Average, Is.Zero);
                Assert.That(dto.UpperNaturalProcessLimit, Is.Zero);
                Assert.That(dto.LowerNaturalProcessLimit, Is.Zero);
            }
        }

        [Test]
        public void NotReady_InsufficientData_ReturnsCorrectStatus()
        {
            var dto = ProcessBehaviourChart.NotReady(BaselineStatus.InsufficientData, "Not enough data points in baseline range.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.Status, Is.EqualTo(BaselineStatus.InsufficientData));
                Assert.That(dto.StatusReason, Does.Contain("Not enough"));
                Assert.That(dto.DataPoints, Is.Empty);
            }
        }

        [Test]
        public void NotReady_BaselineInvalid_ReturnsCorrectStatus()
        {
            var dto = ProcessBehaviourChart.NotReady(BaselineStatus.BaselineInvalid, "Baseline end date is in the future.");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.Status, Is.EqualTo(BaselineStatus.BaselineInvalid));
                Assert.That(dto.StatusReason, Does.Contain("future"));
            }
        }

        [Test]
        public void ReadyDto_ContainsAllExpectedFields()
        {
            var dataPoints = new[]
            {
                new ProcessBehaviourChartDataPoint("2026-01-15T00:00:00Z", 5.0, [], [101, 102]),
                new ProcessBehaviourChartDataPoint("2026-01-16T00:00:00Z", 8.0, [SpecialCauseType.LargeChange], [103]),
            };

            var dto = new ProcessBehaviourChart
            {
                Status = BaselineStatus.Ready,
                StatusReason = string.Empty,
                XAxisKind = XAxisKind.Date,
                Average = 6.5,
                UpperNaturalProcessLimit = 14.0,
                LowerNaturalProcessLimit = 0.0,
                DataPoints = dataPoints,
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.Status, Is.EqualTo(BaselineStatus.Ready));
                Assert.That(dto.XAxisKind, Is.EqualTo(XAxisKind.Date));
                Assert.That(dto.Average, Is.EqualTo(6.5));
                Assert.That(dto.UpperNaturalProcessLimit, Is.EqualTo(14.0));
                Assert.That(dto.LowerNaturalProcessLimit, Is.Zero);
                Assert.That(dto.DataPoints, Has.Length.EqualTo(2));
            }
        }

        [Test]
        public void ReadyDto_DefaultBaselineConfigured_IsTrue()
        {
            var dto = new ProcessBehaviourChart
            {
                Status = BaselineStatus.Ready,
                DataPoints = [],
            };

            Assert.That(dto.BaselineConfigured, Is.True);
        }

        [Test]
        public void ReadyDto_BaselineConfigured_CanBeSetToFalse()
        {
            var dto = new ProcessBehaviourChart
            {
                Status = BaselineStatus.Ready,
                BaselineConfigured = false,
                DataPoints = [],
            };

            Assert.That(dto.BaselineConfigured, Is.False);
        }

        [Test]
        public void NotReady_BaselineConfigured_DefaultsToTrue()
        {
            var dto = ProcessBehaviourChart.NotReady(BaselineStatus.BaselineInvalid, "reason");

            Assert.That(dto.BaselineConfigured, Is.True);
        }

        [Test]
        public void DataPoint_ContainsWorkItemIdsForDrillIn()
        {
            int[] workItemIds = [201, 202, 203];

            var point = new ProcessBehaviourChartDataPoint("2026-01-15T00:00:00Z", 3.0, [], workItemIds);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(point.XValue, Is.EqualTo("2026-01-15T00:00:00Z"));
                Assert.That(point.YValue, Is.EqualTo(3.0));
                Assert.That(point.SpecialCauses, Is.Empty);
                Assert.That(point.WorkItemIds, Is.EqualTo(workItemIds));
            }
        }

        [Test]
        public void DataPoint_SupportsDateTimeXValue()
        {
            var point = new ProcessBehaviourChartDataPoint("2026-01-15T14:30:00Z", 7.5, [SpecialCauseType.SmallShift], [301]);

            var dto = new ProcessBehaviourChart
            {
                Status = BaselineStatus.Ready,
                XAxisKind = XAxisKind.DateTime,
                Average = 5.0,
                UpperNaturalProcessLimit = 10.0,
                LowerNaturalProcessLimit = 0.0,
                DataPoints = [point],
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.XAxisKind, Is.EqualTo(XAxisKind.DateTime));
                Assert.That(dto.DataPoints[0].XValue, Does.Contain("T14:30:00Z"));
            }
        }
    }
}
