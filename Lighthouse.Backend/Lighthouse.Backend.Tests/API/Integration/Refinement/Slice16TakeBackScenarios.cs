using System.Net;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// A voter can take back their own vote. It stops counting at once - readiness and the split
    /// follow - and the log records that it was taken back; nothing is deleted. Only the browser that cast
    /// a vote (or, with sign-in, the same account) can take it back, and taking back a vote you do not have
    /// changes nothing.
    ///
    /// Driving ports: the take-back write, the vote write, the Work Item's log and the Refinement tab's
    /// read. Step definitions live in Slice16TakeBackSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-16")]
    public partial class Slice16TakeBackTest : SizingVotesAcceptanceTest
    {
        // @driving_port @real-io @us-16 @slice-16 @contract-shape:bounded-change
        [Test]
        public async Task A_taken_back_vote_stops_counting_and_the_log_keeps_both()
        {
            var gravity = await GivenJonasAndAnaSaidYesOnConfigurationManagement();

            await WhenJonasTakesBackHisVote(gravity);

            await ThenJonasNoLongerCountsAndAnaReadsInTheLog(gravity, "Vote", "Vote", "Revocation");
        }

        // @driving_port @real-io @us-16 @slice-16 @boundary @contract-shape:unbounded-preservation
        [Test]
        public async Task Taking_back_a_vote_you_do_not_have_records_nothing()
        {
            var gravity = await GivenOnlyAnaSaidYesOnConfigurationManagement();

            using var takeBack = await TakesBackTheirVote(Jonas, gravity, ConfigurationManagement);

            await ThenItIsAnsweredWithoutErrorAndAnaReadsInTheLog(takeBack, gravity, "Vote");
        }

        // @driving_port @real-io @us-16 @slice-16 @boundary @contract-shape:bounded-change
        [Test]
        public async Task Taking_back_twice_records_one_take_back()
        {
            var gravity = await GivenJonasAndAnaSaidYesOnConfigurationManagement();
            await WhenJonasTakesBackHisVote(gravity);

            using var again = await TakesBackTheirVote(Jonas, gravity, ConfigurationManagement);

            await ThenItIsAnsweredWithoutErrorAndAnaReadsInTheLog(again, gravity, "Vote", "Vote", "Revocation");
        }

        // @driving_port @real-io @us-16 @slice-16 @error @contract-shape:unbounded-preservation
        // Declaring somebody else's name in another browser does not make their vote yours.
        [Test]
        public async Task Nobody_can_take_back_somebody_elses_vote_even_under_their_name()
        {
            var gravity = await GivenOnlyAnaSaidYesOnConfigurationManagement();

            using var takeBack = await TakesBackTheirVote(ABrowserOf(AnaLima), gravity, ConfigurationManagement);

            await ThenItIsAnsweredWithoutErrorAndAnaReadsInTheLog(takeBack, gravity, "Vote");
        }

        // @driving_port @real-io @us-16 @slice-16 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task Taking_back_without_a_voter_key_is_refused()
        {
            var gravity = await GivenOnlyAnaSaidYesOnConfigurationManagement();

            using var refused = await TakesBackTheirVote(new Voter(AnaLima, null), gravity, ConfigurationManagement);

            await ThenItIsRefusedAndAnasVoteStillCounts(refused, gravity, HttpStatusCode.BadRequest, VoterKeyRequired);
        }

        // @driving_port @real-io @us-16 @slice-16 @error @contract-shape:unbounded-preservation
        [Test]
        public async Task Taking_back_on_a_Work_Item_that_is_not_in_refinement_is_refused()
        {
            var gravity = await GivenOnlyAnaSaidYesOnConfigurationManagement();

            using var refused = await TakesBackTheirVote(Ana, gravity, BillingExport);

            await ThenItIsRefusedAndAnasVoteStillCounts(refused, gravity, HttpStatusCode.Conflict, WorkItemNotInRefinement);
        }

        // @driving_port @real-io @us-13 @us-16 @slice-16 @boundary @contract-shape:bounded-change
        [Test]
        public async Task Taking_back_a_Yes_can_cost_a_Work_Item_its_Ready()
        {
            var gravity = await GivenJonasMoAndAnaSaidYesSoConfigurationManagementIsReady();

            await WhenMoTakesBackHisVote(gravity);

            await ThenConfigurationManagementNeeds(gravity, "MoreYesNeeded", missingVotes: 1);
        }

        // @driving_port @real-io @us-16 @slice-16 @contract-shape:bounded-change
        [Test]
        public async Task Voting_again_after_taking_back_counts_again()
        {
            var gravity = await GivenJonasMoAndAnaSaidYesSoConfigurationManagementIsReady();
            await WhenMoTakesBackHisVote(gravity);

            await HasVoted(Mo, gravity, ConfigurationManagement, Answer.Yes);

            await ThenConfigurationManagementNeeds(gravity, "Ready", missingVotes: null);
        }

        // @driving_port @real-io @us-16 @slice-16 @boundary @contract-shape:bounded-change
        [Test]
        public async Task Taking_back_takes_the_vote_out_of_the_split()
        {
            var gravity = await GivenJonasAndAnaSaidYesOnConfigurationManagement();

            await WhenJonasTakesBackHisVote(gravity);

            await ThenJonasSeesTheSplitWithoutHisVote(gravity, new SplitReading(Yes: 1, YesBut: 0, No: 0));
        }
    }

    /// <summary>
    /// With sign-in, a vote is the account's, so any of its sessions can take it back - and nobody without
    /// a role on the Team can take anything back.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-16")]
    public class Slice16TakeBackWithAnAccountTest : SizingVotesAcceptanceTest
    {
        protected override InstanceUnderTest Instance => InstanceUnderTest.WithSignInAndRoles;

        // @driving_port @real-io @us-15 @us-16 @slice-16 @contract-shape:bounded-change
        [Test]
        public async Task Any_session_of_the_account_that_voted_can_take_the_vote_back()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            var jonas = ASignedInReaderOf(gravity, "jonas", JonasWeber);
            var jonasOnHisPhone = jonas with { Key = NewVoterKey() };
            await HasVoted(jonas, gravity, ConfigurationManagement, Answer.Yes);

            using var takeBack = await TakesBackTheirVote(jonasOnHisPhone, gravity, ConfigurationManagement);
            var row = RowOf(await TheTabAsSeenBy(jonas, gravity), ConfigurationManagement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(takeBack.IsSuccessStatusCode, Is.True);
                Assert.That(row.VoteCount, Is.Zero);
                Assert.That(row.MyVote, Is.Null);
            }
        }

        // @driving_port @real-io @us-15 @us-16 @slice-16 @error @contract-shape:unbounded-preservation
        // A reader's own take-back is answered first, so the refusal can only be about the role.
        [Test]
        public async Task Somebody_without_a_role_on_the_Team_cannot_take_anything_back()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            var jonas = ASignedInReaderOf(gravity, "jonas", JonasWeber);
            using var readersTakeBack = await TakesBackTheirVote(jonas, gravity, ConfigurationManagement);
            await HasVoted(jonas, gravity, ConfigurationManagement, Answer.Yes);

            using var refused = await TakesBackTheirVote(ASignedInPersonWithoutARoleOn("lena", "Lena Brandt"), gravity, ConfigurationManagement);
            var row = RowOf(await TheTabAsSeenBy(jonas, gravity), ConfigurationManagement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readersTakeBack.IsSuccessStatusCode, Is.True, "a reader may take back, so the refusal below is about Lena's role");
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(row.VoteCount, Is.EqualTo(1));
            }
        }
    }
}
