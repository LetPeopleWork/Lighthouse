using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// The command line and an assistant answer "how much should we refine?" from the same read as the tab:
    /// the same need facts, the same next Refinement, the same ready count and where it comes from - facts
    /// only, which the clients put into words of their own. Reading needs no voter key, and on an instance
    /// with sign-in a personal API key is shown exactly what a browser is. The facts join the answer; nothing
    /// an older client reads is taken away.
    ///
    /// This is the Lighthouse half of the slice; the commands and assistant tools themselves are specified in
    /// the clients' own repository. Step definitions live in Slice09ClientNeedSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-09")]
    public partial class Slice09ClientNeedTest : RefinementNeedAcceptanceTest
    {
        // @driving_port @real-io @us-09 @slice-09 @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice09)]
        public async Task A_client_is_told_whether_to_refine_more_or_stop()
        {
            var gravity = await GivenGravityHasTwoReadyAndIsLikelyToPullFiveToEightOverItsCycle();

            var tab = await WhenPriyasClientReadsTheRefinement(gravity);

            ThenTheClientReadsBelowFiveToEightWithTwoReadyByStageForThursday(tab);
        }

        // @driving_port @real-io @us-09 @slice-09 @error @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice09)]
        public async Task A_client_is_told_why_there_is_no_number()
        {
            var gravity = await GivenGravityHasTwoReadyWithoutACadence();

            var tab = await WhenPriyasClientReadsTheRefinement(gravity);

            ThenTheClientIsToldThereIsNoNumberBecause(tab, NoCadence);
        }

        // @driving_port @real-io @us-09 @us-17a @slice-09 @boundary @contract-shape:unbounded-preservation
        // A client released before the need existed still finds every vote fact it reads today.
        [Test]
        [Ignore(PendingSlice09)]
        public async Task The_need_joins_the_answer_without_taking_away_what_older_clients_read()
        {
            var gravity = await GivenGravityHasTwoReadyAndIsLikelyToPullFiveToEightOverItsCycle();
            await GivenThreeVotersSaidYesOn(gravity, ConfigurationManagement);

            var tab = await WhenPriyasClientReadsTheRefinement(gravity);

            ThenTheClientStillReadsTheVotesAndReadyByVotesNextToTheNeed(tab, ConfigurationManagement);
        }
    }

    /// <summary>
    /// With sign-in on and roles not enforced, a personal API key reads the need exactly as a browser does.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-09")]
    public partial class Slice09ClientNeedWithAnApiKeyTest : RefinementNeedAcceptanceTest
    {
        protected override InstanceUnderTest Instance => InstanceUnderTest.WithSignInWithoutRoles;

        // @driving_port @real-io @us-09 @us-15 @slice-09 @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice09)]
        public async Task A_personal_API_key_is_told_the_same_need_as_a_browser()
        {
            var gravity = await GivenGravityHasTwoReadyAndIsLikelyToPullFiveToEightOverItsCycle();
            var anasKey = await APersonalApiKeyOf("ana", AnaLima);

            var tab = await TheTabAsSeenBy(anasKey, gravity);

            ThenTheKeyIsToldBelowFiveToEightForThursday(tab);
        }
    }
}
