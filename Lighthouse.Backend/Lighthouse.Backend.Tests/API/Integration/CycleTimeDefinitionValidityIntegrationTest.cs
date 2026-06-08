using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// DISTILL RED scaffolds for Slice 03 / US-03 (D5 invalid-on-removal): the DISCUSS HIGH cross-surface
    /// consistency risk. Validity is ONE method on the settings aggregate (IsCycleTimeDefinitionValid),
    /// stamped as IsValid into every read DTO (ADR-063) — the config-list DTO, the scatter read, and the
    /// cumulative-scope read must all agree for the same removed boundary, and an invalid definition must
    /// never compute and never 500. This fixture is the by-construction proof of that single source of truth.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class CycleTimeDefinitionValidityIntegrationTest
    {
        private const string ScaffoldReason = "DISTILL RED scaffold — un-skip in DELIVER Slice 03 (US-03)";

        [Test]
        [Ignore(ScaffoldReason)]
        public void RemovingABoundaryState_StampsDefinitionDtoIsValidFalse_WithoutReSave()
        {
            Assert.Fail(
                "ADR-063 §2 / ADR-064 §2: save 'Concept to Cash' (Planned→Done), then remove 'Planned' from the team's " +
                "states via settings (no re-save of the definition). GET settings ⇒ the CycleTimeDefinitionDto reports " +
                "IsValid==false, recomputed at projection time from the current AllStates (IsValid is projected, not stored).");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void RemovingABoundaryState_ScatterReadReturnsEmptySeriesPlusInvalidSignal_Not500()
        {
            Assert.Fail(
                "ADR-062 §1 / D5: with a removed boundary, cycleTimeData?definitionId returns the structured 'definition " +
                "invalid' signal (empty series + IsValid:false), NOT a 500 and NOT a wrong series. The chart degrades to " +
                "disabled-with-warning rather than crashing.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void RemovingABoundaryState_CumulativeScopeRead_IsRefused_AndStaysUnscoped()
        {
            Assert.Fail(
                "ADR-063 §4 / US-04 example 3: cumulativeStateTime?definitionId for an invalid definition is refused and the " +
                "chart falls back to unscoped — never scoped against missing states.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void ConfigDtoScatterReadAndCumulativeRead_AllReportInvalid_ForTheSameRemovedBoundary()
        {
            Assert.Fail(
                "DISCUSS HIGH cross-surface risk (ADR-063 enforcement): for ONE definition whose boundary was removed, the " +
                "config-list DTO IsValid, the scatter read invalid-signal, and the cumulative-scope refusal must AGREE in a " +
                "single test. They all consume the SAME IsCycleTimeDefinitionValid verdict — none recomputes independently.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void EditingAnInvalidDefinitionToAPresentBoundary_MakesItValidAgain()
        {
            Assert.Fail(
                "US-03 recovery: editing the invalid definition's start to a still-present state and saving flips IsValid " +
                "back to true across all surfaces and the named read computes again. Ordering correctness stays the user's " +
                "responsibility (no auto-correct, D5).");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void DeletingAnInvalidDefinition_RemovesItCleanly()
        {
            Assert.Fail(
                "US-03: deleting an invalid definition removes it from the owner; it disappears from the config list and both " +
                "selectors with no orphaned read.");
        }
    }
}
