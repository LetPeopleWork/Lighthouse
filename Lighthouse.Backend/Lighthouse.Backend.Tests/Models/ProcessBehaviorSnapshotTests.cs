using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models
{
    [TestFixture]
    [Category("epic-5427-percentiles-over-time")]
    public class ProcessBehaviorSnapshotTests
    {
        [Test]
        public void Constructor_DefaultValues_AreSetCorrectly()
        {
            var snapshot = new ProcessBehaviorSnapshot();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.Id, Is.Zero);
                Assert.That(snapshot.OwnerId, Is.Zero);
                Assert.That(snapshot.OwnerType, Is.Default);
                Assert.That(snapshot.RecordedAt, Is.Default);
                Assert.That(snapshot.MetricType, Is.Default);
                Assert.That(snapshot.Unpl, Is.Zero);
                Assert.That(snapshot.Average, Is.Zero);
                Assert.That(snapshot.Lnpl, Is.Zero);
            }
        }

        [Test]
        public void Properties_CanBeSetAndRead()
        {
            var snapshot = new ProcessBehaviorSnapshot
            {
                Id = 42,
                OwnerId = 7,
                OwnerType = OwnerType.Team,
                RecordedAt = new DateOnly(2026, 7, 1),
                MetricType = ProcessBehaviorMetricType.Throughput,
                Unpl = 13,
                Average = 8,
                Lnpl = 3,
            };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.Id, Is.EqualTo(42));
                Assert.That(snapshot.OwnerId, Is.EqualTo(7));
                Assert.That(snapshot.OwnerType, Is.EqualTo(OwnerType.Team));
                Assert.That(snapshot.RecordedAt, Is.EqualTo(new DateOnly(2026, 7, 1)));
                Assert.That(snapshot.MetricType, Is.EqualTo(ProcessBehaviorMetricType.Throughput));
                Assert.That(snapshot.Unpl, Is.EqualTo(13));
                Assert.That(snapshot.Average, Is.EqualTo(8));
                Assert.That(snapshot.Lnpl, Is.EqualTo(3));
            }
        }

        [Test]
        public void Limits_AreIntegers_MatchingTheProcessBehaviourChartComputePath()
        {
            var snapshot = new ProcessBehaviorSnapshot();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot.Unpl, Is.TypeOf<int>(),
                    "ProcessBehaviourChart.UpperNaturalProcessLimit is int — persisting a wider type invents precision the compute path does not have");
                Assert.That(snapshot.Average, Is.TypeOf<int>(),
                    "ProcessBehaviourChart.Average is int");
                Assert.That(snapshot.Lnpl, Is.TypeOf<int>(),
                    "ProcessBehaviourChart.LowerNaturalProcessLimit is int");
            }
        }

        [Test]
        public void ProcessBehaviorMetricType_ThroughputIsOrdinalZero_AndIsTheOnlyMemberThisSlice()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)ProcessBehaviorMetricType.Throughput, Is.Zero,
                    "ProcessBehaviorMetricType persists as its ordinal — Throughput must stay 0 or every shipped row re-maps");
                Assert.That(Enum.GetValues<ProcessBehaviorMetricType>(), Has.Length.EqualTo(1),
                    "slice 03 ships Throughput only; slice 04 APPENDS the remaining families at the end");
            }
        }

        [Test]
        public void MetricType_IsDeclaredWithTheProcessBehaviorFamilyEnum_NotThePercentileOne()
        {
            var snapshot = new ProcessBehaviorSnapshot();

            Assert.That(snapshot.MetricType, Is.InstanceOf<ProcessBehaviorMetricType>(),
                "percentile families and process-behaviour families grow independently — sharing one ordinal space couples them");
        }

        [TestCase(OwnerType.Team)]
        [TestCase(OwnerType.Portfolio)]
        public void OwnerType_DiscriminatesBetweenTeamAndPortfolio(OwnerType ownerType)
        {
            var snapshot = new ProcessBehaviorSnapshot
            {
                OwnerId = 1,
                OwnerType = ownerType,
            };

            Assert.That(snapshot.OwnerType, Is.EqualTo(ownerType));
        }

        [Test]
        public void Snapshot_ImplementsIEntity()
        {
            var snapshot = new ProcessBehaviorSnapshot();
            Assert.That(snapshot, Is.InstanceOf<IEntity>());
        }
    }
}
