using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// DISTILL RED scaffolds enforcing the ADR-061 / ADR-063 seams: the named ordered-boundary duration is
    /// computed in BaseMetricsService by reusing the existing transition-ordering primitive (no second
    /// engine), WorkItemBase.CycleTime is untouched, and validity has a single backend source of truth.
    /// Flesh these as ArchUnitNET/reflection rules in DELIVER (mirror CumulativeStateTimeSeamArchUnitTest).
    /// </summary>
    [TestFixture]
    public class NamedCycleTimeSeamArchUnitTest
    {
        private const string ScaffoldReason = "DISTILL RED scaffold — un-skip in DELIVER (ADR-061/063 seam)";

        [Test]
        [Ignore(ScaffoldReason)]
        public void NamedCycleTimeDuration_IsAProtectedHelperOnBaseMetricsService_NotOnAnyInterface()
        {
            Assert.Fail(
                "ADR-061 §1: the named-duration helper (NamedCycleTimeDays) lives as a protected member on BaseMetricsService " +
                "(intra-inheritance only), never on any interface — mirror CumulativeStateTimeSeamArchUnitTest's protected/" +
                "no-interface rule.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void NamedCycleTimeDuration_ReusesTheTransitionOrderingPrimitive_NoSecondTransitionWalkOutsideBaseMetricsService()
        {
            Assert.Fail(
                "ADR-061 §1/§3 (no parallel engine): no new OrderBy(TransitionedAt) transition walk exists outside " +
                "BaseMetricsService; the named duration reuses the CompletedVisits/GroupTransitionsByItem ordering shared " +
                "with the cumulative computation, so scatter and cumulative-scope spans cannot diverge.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void WorkItemBaseCycleTime_IsNotModifiedByThisFeature()
        {
            Assert.Fail(
                "ADR-061 §3 blast-radius: WorkItemBase.CycleTime stays the summary-date StartedDate→ClosedDate property with " +
                "no dependency on the owner's ordered states or the mapping resolver. The named computation must not be " +
                "routed through the model. (Git-diff review gate; assert the property's existing shape/signature is intact.)");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void IsCycleTimeDefinitionValid_IsTheOnlyBoundaryPresenceCheckAgainstAllStates()
        {
            Assert.Fail(
                "ADR-063 §1/§2: only WorkTrackingSystemOptionsOwner.IsCycleTimeDefinitionValid resolves a definition's " +
                "boundaries against AllStates; no DTO projection or service recomputes presence-validity independently — " +
                "every surface consumes the stamped IsValid.");
        }
    }
}
