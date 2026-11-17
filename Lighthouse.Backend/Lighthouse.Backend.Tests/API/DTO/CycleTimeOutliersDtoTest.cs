using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Modules.FlowMetrics.Application;

namespace Lighthouse.Backend.Tests.API.DTO
{
    /// <summary>The answer the scatterplot reads is made from the verdict: its status, its limit and the outlier ids.</summary>
    public class CycleTimeOutliersDtoTest
    {
        private static readonly int[] TheItemAboveTheLimit = [7];

        [Test]
        public void A_verdict_with_limits_becomes_Ready_with_the_unrounded_limit_and_the_outlier_ids()
        {
            var verdict = new UpperNaturalProcessLimitOutlierDefinition().Judge([10, 12], [new CycleTimeObservation(7, 17), new CycleTimeObservation(8, 16)]);

            var dto = CycleTimeOutliersDto.From(verdict);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.Status, Is.EqualTo(CycleTimeOutlierStatus.Ready),
                    "WHAT: a verdict with limits is not Ready. WHY: the scatterplot sets items apart only when Ready. HOW: map Limited to Ready.");
                Assert.That(dto.UpperNaturalProcessLimit, Is.EqualTo(16.32).Within(1e-9),
                    "WHAT: the limit is not the unrounded 11 + 2.66 x 2. WHY: the list title states it. HOW: pass the verdict's limit through unrounded.");
                Assert.That(dto.OutlierWorkItemIds, Is.EqualTo(TheItemAboveTheLimit),
                    "WHAT: the outlier ids are not the verdict's. WHY: the scatterplot sets apart the items it is given. HOW: pass the verdict's ids through.");
            }
        }

        [Test]
        public void A_verdict_without_limits_becomes_NoLimits_with_no_limit_and_no_outliers()
        {
            var dto = CycleTimeOutliersDto.From(new OutlierVerdict.NoLimits());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.Status, Is.EqualTo(CycleTimeOutlierStatus.NoLimits),
                    "WHAT: a verdict without limits is not NoLimits. WHY: the reader must be told the limits are missing. HOW: map NoLimits to NoLimits.");
                Assert.That(dto.UpperNaturalProcessLimit, Is.Null,
                    "WHAT: a limit is given although there is none. WHY: a made-up limit would read as real. HOW: answer null.");
                Assert.That(dto.OutlierWorkItemIds, Is.Empty,
                    "WHAT: outliers are listed without limits. WHY: without limits no item may be set apart. HOW: answer an empty list.");
            }
        }
    }
}
