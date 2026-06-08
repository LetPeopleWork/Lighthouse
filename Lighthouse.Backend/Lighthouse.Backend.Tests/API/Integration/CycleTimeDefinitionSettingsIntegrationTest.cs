using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// DISTILL RED scaffolds for Slice 02 / US-02: defining a named cycle time in Team settings. Driving
    /// port: the EXISTING team settings write (PUT) carrying an additive CycleTimeDefinitions field
    /// (ADR-064), plus the settings read that projects CycleTimeDefinitionDto { id,name,startState,
    /// endState,isValid }. Mirror ForecastFilterTeamSettingsIntegrationTest for the round-trip/RBAC shape.
    /// A real-provider read-your-writes test is REQUIRED (InMemory misses the migration — ADR-064 §1).
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class CycleTimeDefinitionSettingsIntegrationTest
    {
        private const string ScaffoldReason = "DISTILL RED scaffold — un-skip in DELIVER Slice 02 (US-02)";

        [Test]
        [Ignore(ScaffoldReason)]
        public void SaveNamedDefinition_SurvivesReload_ReadYourWrites()
        {
            Assert.Fail(
                "US-02 / ADR-064: PUT team settings with CycleTimeDefinitions=[{name:'Concept to Cash',start:'Planned'," +
                "end:'Done'}] persists on WorkTrackingSystemOptionsOwner; GET settings after reload returns it with a " +
                "stable id. Run against a REAL provider (not only InMemory) — the new owned-collection needs its migration.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void SaveNamedDefinition_AppearsInTheScatterplotSelectorSource()
        {
            Assert.Fail(
                "US-02 shared-artifact: a saved definition is the source for the scatterplot cycle-time selector. Assert the " +
                "settings DTO CycleTimeDefinitions feeds the same definition id the cycleTimeData?definitionId read consumes " +
                "(one SSOT, journey cycleTimeDefinitions registry).");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void SaveEndStateBeforeStartState_RejectedInline_NothingPersisted()
        {
            Assert.Fail(
                "D4 / ADR-064 §3: saving start='Done', end='Planned' (end not strictly after start in AllStates order) is " +
                "rejected (400) with 'End state must come after the start state in the workflow'; a follow-up GET shows " +
                "NOTHING persisted. End-after-start is the save-time ordering check, distinct from D5 read-time presence.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void SaveEmptyOrDuplicateName_RejectedInline_NothingPersisted()
        {
            Assert.Fail(
                "D4: an empty name, or a name duplicating an existing definition on the same owner, is rejected at save; " +
                "nothing persisted. Name uniqueness is per-owner (case-insensitive, matching AllStates comparer).");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void SaveMappingNameBoundary_ResolvesViaGetRawStatesForCategory()
        {
            Assert.Fail(
                "D3 / ADR-061 §2: a boundary saved as a State-Mapping name (e.g. 'Validation') is accepted and resolves to " +
                "its raw states via the EXISTING owner.GetRawStatesForCategory — no second resolver. Seed a mapping, save a " +
                "definition using the mapping name, assert it persists valid and the named read resolves the raw states.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void SaveDefinition_RequiresTeamAdmin_ViewerIsForbidden()
        {
            Assert.Fail(
                "RBAC (cross-cutting): CycleTimeDefinitions rides the existing tokened settings write governed by " +
                "IRbacAdministrationService — a team Viewer PUT is 403; a team Admin PUT is accepted. No new authz surface.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void CycleTimesConfig_IsPremiumGated_NonPremiumWriteRefused()
        {
            Assert.Fail(
                "D8 premium gate: with CanUsePremiumFeatures()==false the cycle-times write is refused while the rest of the " +
                "settings round-trip is unaffected (non-destructive — existing definitions are not wiped on a downgrade).");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void ConcurrentSettingsEdit_IsRejectedByTheInheritedConcurrencyToken()
        {
            Assert.Fail(
                "ADR-064 §3 (epic-5121 inherited): two PUTs with a stale concurrency token ⇒ the second is rejected as a " +
                "lost-update, with CycleTimeDefinitions participating in the tokened aggregate. No new concurrency surface.");
        }
    }
}
