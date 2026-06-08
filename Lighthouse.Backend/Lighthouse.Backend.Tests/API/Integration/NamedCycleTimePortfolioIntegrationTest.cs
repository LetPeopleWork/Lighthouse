using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    /// <summary>
    /// DISTILL RED scaffolds for Slice 05 / US-05 (D7): full named-cycle-time parity at Portfolio scope.
    /// The Team build generalises with no new concepts — same CycleTimeDefinitions on the shared
    /// WorkTrackingSystemOptionsOwner, the Portfolio twins of cycleTimeData / cycleTimePercentiles /
    /// cumulativeStateTime extended with definitionId, RbacGuard(PortfolioRead/PortfolioWrite). Mirror
    /// CumulativeStateTimePortfolioReadApiIntegrationTest for the portfolio seeding idiom.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class NamedCycleTimePortfolioIntegrationTest
    {
        private const string ScaffoldReason = "DISTILL RED scaffold — un-skip in DELIVER Slice 05 (US-05)";

        [Test]
        [Ignore(ScaffoldReason)]
        public void Portfolio_SaveNamedDefinition_SurvivesReload_ReadYourWrites()
        {
            Assert.Fail(
                "US-05: a portfolio-admin saves 'Idea to Live' (Backlog→Released) via the existing Portfolio settings write; " +
                "it survives reload on the Portfolio owner (real-provider read-your-writes, same as Team).");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void Portfolio_NamedDefinition_ScatterReplotsOverTheWindow_WithRecomputedPercentiles()
        {
            Assert.Fail(
                "US-05: GET portfolio cycleTimeData?definitionId re-plots dots over Backlog→Released and " +
                "cycleTimePercentiles?definitionId recomputes 50/70/85/95 — Portfolio twin of US-01.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void Portfolio_CumulativeScope_RecomputesBarsOverTheWindow()
        {
            Assert.Fail(
                "US-05: GET portfolio cumulativeStateTime?definitionId scopes bars to the named half-open window (D10) — " +
                "Portfolio twin of US-04.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void Portfolio_InvalidOnRemoval_ReadsDisabledAcrossConfigAndBothSelectors()
        {
            Assert.Fail(
                "US-05 / D5 parity: removing a Portfolio boundary state stamps IsValid==false across the config DTO, the " +
                "scatter read invalid-signal, and the cumulative refusal — Portfolio twin of US-03.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void Portfolio_FewQualifyingItems_StillPlotsThemWithCountAndPercentiles_NoSpecialLowSampleState()
        {
            Assert.Fail(
                "US-05 / D9 parity (locked): few items still plot with count + percentiles, no special low-sample state — " +
                "identical to the Team behaviour and the default scatterplot.");
        }

        [Test]
        [Ignore(ScaffoldReason)]
        public void Portfolio_NonPremiumViewer_SelectorAndScopeSwitchGatedOff_DefaultUnaffected()
        {
            Assert.Fail(
                "US-05: a non-premium viewer at Portfolio scope is refused the named branch (definitionId) on both reads " +
                "while the Default scatterplot and unscoped cumulative are unaffected.");
        }
    }
}
