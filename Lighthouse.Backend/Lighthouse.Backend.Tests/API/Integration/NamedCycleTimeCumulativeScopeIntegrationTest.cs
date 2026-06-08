using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// DISTILL RED scaffolds for Slice 04 / US-04 (D6b/D10): scoping the cumulative-time-per-state chart
    /// to a named window. Driving port: the EXISTING GET cumulativeStateTime extended with an optional
    /// &amp;definitionId (ADR-063 §4). Absent ⇒ byte-identical; present+valid ⇒ the per-state aggregation is
    /// restricted to the half-open [enter start … enter end) span reusing the SAME boundary resolution as
    /// the scatter (so the scatter duration and the cumulative span are the identical window by construction).
    /// Mirror CumulativeStateTimeReadApiIntegrationTest for the seeding + JSON-parsing idiom.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class NamedCycleTimeCumulativeScopeIntegrationTest
    {
        private const string ScaffoldReason = "DISTILL RED scaffold — un-skip in DELIVER Slice 04 (US-04)";

        [Test]
        [Ignore(ScaffoldReason)]
        public void DefinitionIdAbsent_CumulativeStateTime_IsByteIdenticalToTodaysUnscopedResponse()
        {
            Assert.Fail(
                "ADR-063 §4 / US-04 'switch off': cumulativeStateTime with NO definitionId equals the pre-feature golden " +
                "unscoped response, byte-for-shape. The additive param must not perturb the default path.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void DefinitionIdValid_BarsRecomputeOverHalfOpenWindow_AndTheEndStateHasNoBar()
        {
            Assert.Fail(
                "D10 / ADR-063 §4: with definitionId for Planned→Done, the bars cover [enter Planned … enter Done) — " +
                "start-inclusive, the 'Done' dwell EXCLUDED so 'Done' contributes no bar; only states occupied before " +
                "entering Done get a bar (the upstream Planned/Validation bars dominate). Seed an item walking the window " +
                "and assert the Done bar is absent/zero while in-window state bars carry their durations.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void ScatterNamedDurationWindow_EqualsCumulativeScopedSpan_ForTheSameDefinition()
        {
            Assert.Fail(
                "ADR-063 §4 cross-surface (by construction): for the same item and definition, the scatter named duration " +
                "(cycleTimeData?definitionId) equals the sum of the in-window cumulative bars (cumulativeStateTime?" +
                "definitionId) within tolerance — they resolve through the SAME NamedCycleTimeDays boundary logic.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void DefinitionIdInvalid_CumulativeScope_IsRefused_ChartStaysUnscoped()
        {
            Assert.Fail(
                "D5 / US-04 example 3: a removed-boundary (invalid) definitionId on cumulativeStateTime is refused; the chart " +
                "stays unscoped, never scoped against missing states, never 500.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void CumulativeNamedBranch_IsPremiumGated_NonPremiumDefinitionIdRefused()
        {
            Assert.Fail(
                "D8 premium gate (defence-in-depth): a non-premium caller passing definitionId to cumulativeStateTime is " +
                "refused while the unscoped read is unaffected.");
        }
    }
}
