using System.Text.Json;
using Lighthouse.Backend.API.DTO.Archived;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class ArchivedDeliveryProjectionTest
    {
        private static readonly DateTime ClosingInstant = new(2026, 8, 22, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string[] PinnedReferenceIds = ["FTR-1", "FTR-2"];

        private static readonly double[] PinnedProbabilities = [50.0, 95.0];

        private static readonly ArchivedDeliveryIdentity Identity = new(
            7, "Q3 Launch", new DateTime(2026, 9, 30, 0, 0, 0, DateTimeKind.Utc), 3, Guid.Empty, 11);

        [Test]
        public void ToDto_ClosureRecordHoldingFeatureRows_CarriesThemInline()
        {
            var record = ClosureRecordWith(FeatureBreakdownJson(
                new DeliveryFeatureMetric("FTR-1", "Checkout", 70.0, 82.5) { TotalItems = 10, IsUsingDefaultSize = false },
                new DeliveryFeatureMetric("FTR-2", "Search", 25.0, null) { TotalItems = 4, IsUsingDefaultSize = true }));

            var dto = ArchivedDeliveryProjection.ToDto(Identity, record);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.FeatureBreakdown.Select(row => row.ReferenceId), Is.EqualTo(PinnedReferenceIds));
                Assert.That(dto.FeatureBreakdown[0].Name, Is.EqualTo("Checkout"));
                Assert.That(dto.FeatureBreakdown[0].Completion, Is.EqualTo(70.0));
                Assert.That(dto.FeatureBreakdown[0].Likelihood, Is.EqualTo(82.5));
                Assert.That(dto.FeatureBreakdown[0].TotalItems, Is.EqualTo(10));
                Assert.That(dto.FeatureBreakdown[1].Likelihood, Is.Null);
                Assert.That(dto.FeatureBreakdown[1].IsUsingDefaultSize, Is.True);
            }
        }

        [Test]
        public void ToDto_ClosureRecordWithoutFeatureRows_CarriesAnEmptyGridRatherThanNull()
        {
            var dto = ArchivedDeliveryProjection.ToDto(Identity, ClosureRecordWith(null));

            Assert.That(dto.FeatureBreakdown, Is.Empty);
        }

        [Test]
        public void ArchivedDeliveryDto_CarriesNoLiveFeatureIdsForAnythingToReFetchBy()
        {
            var propertyNames = typeof(ArchivedDeliveryDto)
                .GetProperties()
                .Select(property => property.Name)
                .ToList();

            Assert.That(propertyNames, Does.Not.Contain("Features"), string.Join(", ", propertyNames));
        }

        [Test]
        public void ToDto_ClosureRecordHoldingPinnedDates_CarriesThemWithoutRecalculating()
        {
            var record = ClosureRecordWith(null);
            record.WhenDistributionJson = JsonSerializer.Serialize(new[]
            {
                new { Probability = 50.0, ExpectedDate = new DateTime(2026, 9, 10, 0, 0, 0, DateTimeKind.Utc) },
                new { Probability = 95.0, ExpectedDate = new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc) },
            });

            var dto = ArchivedDeliveryProjection.ToDto(Identity, record);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.WhenDistribution.Select(point => point.Probability), Is.EqualTo(PinnedProbabilities));
                Assert.That(dto.WhenDistribution[1].ExpectedDate, Is.EqualTo(new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc)));
            }
        }

        [Test]
        public void ToDto_ClosureRecordWithNoPinnedDates_CarriesNone()
        {
            var dto = ArchivedDeliveryProjection.ToDto(Identity, ClosureRecordWith(null));

            Assert.That(dto.WhenDistribution, Is.Empty);
        }

        [Test]
        public void ToDto_RuleBasedClosureRecord_StillShowsTheRuleItWasBuiltFrom()
        {
            var record = ClosureRecordWith(null);
            record.SelectionMode = DeliverySelectionMode.RuleBased;
            record.RuleSchemaVersion = WorkItemRuleSet.SchemaVersion;
            record.RuleDefinitionJson = WorkItemRuleSetJson.Serialize(new WorkItemRuleSet
            {
                Version = WorkItemRuleSet.SchemaVersion,
                Mode = WorkItemRuleSet.ModeOr,
                Conditions = [new WorkItemRuleCondition { FieldKey = "name", Operator = RuleOperators.Contains, Value = "Checkout" }],
            });

            var dto = ArchivedDeliveryProjection.ToDto(Identity, record);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.SelectionMode, Is.EqualTo(DeliverySelectionMode.RuleBased));
                Assert.That(dto.Mode, Is.EqualTo(WorkItemRuleSet.ModeOr));
                Assert.That(dto.Rules, Has.Count.EqualTo(1));
                Assert.That(dto.Rules[0].Value, Is.EqualTo("Checkout"));
            }
        }

        [Test]
        public void ToDto_ManualClosureRecord_ShowsNoRule()
        {
            var dto = ArchivedDeliveryProjection.ToDto(Identity, ClosureRecordWith(null));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
                Assert.That(dto.Rules, Is.Empty);
            }
        }

        [Test]
        public void ToDto_ArchivedDelivery_CarriesHowManyDaysOfHistoryStandBehindIt()
        {
            var dto = ArchivedDeliveryProjection.ToDto(Identity, ClosureRecordWith(null));

            Assert.That(dto.MetricSnapshotCount, Is.EqualTo(11));
        }

        [Test]
        public void ToDto_PartlyFinishedDelivery_ReportsTheShareThatWasDone()
        {
            var record = ClosureRecordWith(null);
            record.TotalWork = 14;
            record.DoneWork = 7;

            var dto = ArchivedDeliveryProjection.ToDto(Identity, record);

            // Half of fourteen, as a percentage. Reading it off either number alone, or dividing the
            // wrong way round, still lands on a plausible-looking figure.
            Assert.That(dto.Progress, Is.EqualTo(50.0));
        }

        [Test]
        public void ToDto_DeliveryThatFinishedNothing_ReportsNoProgressRatherThanAll()
        {
            var record = ClosureRecordWith(null);
            record.TotalWork = 9;
            record.DoneWork = 0;

            var dto = ArchivedDeliveryProjection.ToDto(Identity, record);

            Assert.That(dto.Progress, Is.Zero);
        }

        [Test]
        public void ToDto_DeliveryHoldingNoWork_ReportsNoProgressRatherThanDividingByZero()
        {
            var record = ClosureRecordWith(null);
            record.TotalWork = 0;
            record.DoneWork = 0;

            var dto = ArchivedDeliveryProjection.ToDto(Identity, record);

            Assert.That(dto.Progress, Is.Zero);
        }

        [Test]
        public void ToDto_TeamsWithoutForecastStoredAsNullText_ReadsAsNoTeamsRatherThanThrowing()
        {
            var record = ClosureRecordWith(null);
            record.TeamsWithoutForecastJson = "null";

            var dto = ArchivedDeliveryProjection.ToDto(Identity, record);

            Assert.That(dto.TeamsWithoutForecast, Is.Empty);
        }

        private static DeliveryClosureRecord ClosureRecordWith(string? featureBreakdownJson)
        {
            return new DeliveryClosureRecord
            {
                DeliveryId = Identity.Id,
                ArchivedOn = ClosingInstant,
                TotalWork = 14,
                DoneWork = 8,
                RemainingWork = 6,
                HasSufficientData = true,
                SelectionMode = DeliverySelectionMode.Manual,
                FeatureBreakdownJson = featureBreakdownJson,
            };
        }

        private static string FeatureBreakdownJson(params DeliveryFeatureMetric[] rows)
        {
            return JsonSerializer.Serialize(rows);
        }
    }
}
