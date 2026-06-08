using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// DISTILL RED scaffolds for Slice 01 / US-01 (walking skeleton): a named cycle time read on the
    /// Team Cycle Time Scatterplot. Driving port: GET /api/latest/teams/{id}/metrics/cycleTimeData and
    /// /cycleTimePercentiles, extended with an optional &amp;definitionId (ADR-062). Each test maps to one
    /// DELIVER TDD cycle: un-[Ignore] it, seed via the cumulative-test seeding idiom (real EF +
    /// WorkItemStateTransition log), drive over real HTTP, parse the WorkItemDto[] / PercentileValue[]
    /// JSON, then make it green. RED-not-BROKEN: bodies fail by assertion, reference only existing symbols.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class NamedCycleTimeReadApiIntegrationTest
    {
        private const string ScaffoldReason = "DISTILL RED scaffold — un-skip in DELIVER Slice 01 (US-01)";

        [Test]
        [Ignore(ScaffoldReason)]
        public void DefinitionIdAbsent_ReturnsByteIdenticalDefaultCycleTimeSeries()
        {
            Assert.Fail(
                "ADR-062 §1: GET cycleTimeData with NO definitionId must return today's default series byte-for-byte. " +
                "Seed a team + closed items, snapshot the no-param response, then assert it is unchanged when the " +
                "feature ships (the named branch must not perturb the default path).");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void NamedDefinition_PlotsEachClosedItemAtItsOrderedBoundaryDuration_Phx204Is47Days()
        {
            Assert.Fail(
                "US-01 / D1 / D10: with definitionId for 'Concept to Cash' (Planned→Done), each WorkItemDto.cycleTime " +
                "carries the half-open [enter Planned-or-later … enter Done-or-later) duration. Seed PHX-204 first " +
                "reaching Planned on day 0 and Done on day 47 ⇒ cycleTime == 47 (same inclusive day convention as the " +
                "default, ADR-061 §1).");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void NamedDefinition_RecomputesPercentileLinesOverTheNamedSeries()
        {
            Assert.Fail(
                "US-01: GET cycleTimePercentiles with definitionId returns the same PercentileValue[] shape (50/70/85/95) " +
                "computed over the NAMED durations via the existing PercentileCalculator (ADR-061 §4), not the default " +
                "series. Assert P85 lands on the named-series value, not the default-window value.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void NamedDefinition_ReEntryUsesFirstCrossingNotTheReopen_Phx211()
        {
            Assert.Fail(
                "D2 first-crossing: seed PHX-211 Planned→Doing→(reopened)→Planned→Done. The named duration measures the " +
                "FIRST entry into Planned-or-later up to the FIRST subsequent entry into Done-or-later, ignoring the " +
                "re-entry (mirrors the default cycle time / CompletedVisits ordering).");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void NamedDefinition_ItemsCrossingNeitherOrOnlyOneBoundary_AreExcludedFromTheSeries()
        {
            Assert.Fail(
                "D9 exclusion / ADR-061 §1: NamedCycleTimeDays returns null for a closed item that never crossed BOTH " +
                "boundaries; such items must NOT appear as dots. Seed one item that reached Planned but never Done and " +
                "assert it is absent from the returned WorkItemDto[].");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void NamedDefinition_FewQualifyingItems_StillPlotsThemWithCountAndPercentiles_NoSpecialLowSampleState()
        {
            Assert.Fail(
                "D9 (locked refinement 2026-06-08): with only 2 items crossing both boundaries the series STILL returns " +
                "those 2 WorkItemDto rows and the 50/70/85/95 percentiles — exactly like the default scatterplot with 2 " +
                "items. There is NO threshold and NO special low-sample signal in the payload. (Slice-01/05 docs say " +
                "'explicit low-sample state' — STALE; the locked D9 forbids it. See distill/upstream-issues.md.)");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void NamedBranch_NonPremiumCaller_PassingDefinitionId_IsRefused()
        {
            Assert.Fail(
                "ADR-062 §1 premium gate (defence-in-depth): with ILicenseService.CanUsePremiumFeatures()==false, a request " +
                "carrying definitionId is refused (403 / feature-disabled), while the no-definitionId default request still " +
                "succeeds. Wire the license mock via RemoveAll<ILicenseService>()+AddScoped, as ForecastFilter*Test does.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void NamedBranch_TeamViewer_CanReadTheNamedSeries()
        {
            Assert.Fail(
                "RBAC TeamRead: the named read rides the existing RbacGuard(TeamRead) class guard — a team Viewer (premium) " +
                "can read the named series. client.AsTeamViewer(teamId) ⇒ 200. Anonymous ⇒ 401/403 (no new authz surface).");
        }
    }
}
