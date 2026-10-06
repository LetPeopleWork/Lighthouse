using System.Text.Json;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Step definitions for the client half of the need. A client is a reader with a key of its own, exactly
    /// as a browser is; it is shown the need facts the tab is shown.
    /// </summary>
    public partial class Slice09ClientNeedTest
    {
        private static readonly Voter PriyasClient = AClientOf(PriyaSharma);

        private static readonly Voter Jonas = ABrowserOf(JonasWeber);

        private static readonly Voter Ana = ABrowserOf(AnaLima);

        private static readonly Voter Mo = ABrowserOf(MoOkafor);

        // --- Given ---

        private async Task<TeamUnderTest> GivenGravityHasTwoReadyAndIsLikelyToPullFiveToEightOverItsCycle()
            => await GravityWithTwoReadyLikelyToPullFiveToEightOverItsCycle();

        private async Task<TeamUnderTest> GivenGravityHasTwoReadyWithoutACadence()
        {
            var gravity = await GravityWithItsReadyRule((UserActivityTracking, ReadyTag), (AdvancedSearch, ReadyTag));
            TheTeamFinishedWorkEveryDay(gravity, 1);
            return gravity;
        }

        private async Task GivenThreeVotersSaidYesOn(TeamUnderTest team, string workItem)
        {
            await HasVoted(Jonas, team, workItem, Answer.Yes);
            await HasVoted(Mo, team, workItem, Answer.Yes);
            await HasVoted(Ana, team, workItem, Answer.Yes);
        }

        // --- When ---

        private async Task<JsonElement> WhenPriyasClientReadsTheRefinement(TeamUnderTest team)
            => await TheTabAsSeenBy(PriyasClient, team);

        // --- Then ---

        private static void ThenTheClientReadsBelowFiveToEightWithTwoReadyByStageForThursday(JsonElement tab)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(NeedIn(tab), Is.EqualTo(new NeedReading(Below, null, 5, 8, 50, 85, GravitysCycleWorkingDays)));
                Assert.That(CycleIn(tab), Is.EqualTo(new CycleReading("2026-10-08", "2026-10-15")));
                Assert.That(CadenceFactsIn(tab), Is.EqualTo(new CadenceFactsReading("2026-10-08", false)));
                Assert.That((ReadyCountIn(tab), ReadySourceIn(tab)), Is.EqualTo(((int?)2, (string?)FromStages)));
            }
        }

        private static void ThenTheClientIsToldThereIsNoNumberBecause(JsonElement tab, string reason)
        {
            var need = NeedIn(tab);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(need.UnavailableReason, Is.EqualTo(reason));
                Assert.That((need.Verdict, need.Low, need.High), Is.EqualTo(((string?)null, (int?)null, (int?)null)));
                Assert.That(CadenceFactsIn(tab).NextRefinementDate, Is.Null);
            }
        }

        /// <summary>
        /// GR-073 has three Yes votes and is Ready by its votes, though the stage rules leave it Waiting; an
        /// older client still finds its readiness, its vote count and the votes' ready count.
        /// </summary>
        private static void ThenTheClientStillReadsTheVotesAndReadyByVotesNextToTheNeed(JsonElement tab, string workItem)
        {
            var row = RowOf(tab, workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((row.Readiness, row.VoteCount), Is.EqualTo(("Ready", 3)));
                Assert.That(ReadyByVotesIn(tab), Is.EqualTo(1));
                Assert.That(NeedIn(tab).Verdict, Is.EqualTo(Below), "the need sits next to the vote facts, not in place of them");
            }
        }
    }

    public partial class Slice09ClientNeedWithAnApiKeyTest
    {
        private async Task<TeamUnderTest> GivenGravityHasTwoReadyAndIsLikelyToPullFiveToEightOverItsCycle()
            => await GravityWithTwoReadyLikelyToPullFiveToEightOverItsCycle();

        private static void ThenTheKeyIsToldBelowFiveToEightForThursday(JsonElement tab)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(NeedIn(tab), Is.EqualTo(new NeedReading(Below, null, 5, 8, 50, 85, GravitysCycleWorkingDays)));
                Assert.That(CadenceFactsIn(tab), Is.EqualTo(new CadenceFactsReading("2026-10-08", false)));
            }
        }
    }
}
