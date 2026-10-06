using System.Net;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// With sign-in on, a vote carries the account that cast it: no name is asked for, and a name or key
    /// sent anyway is ignored. Reading a Team is enough to vote on it - the vote changes nothing but the
    /// Team's own sizing log - and somebody who cannot read the Team is told it does not exist, whether they
    /// try to read or to vote.
    ///
    /// Driving ports: the vote and comment writes, the Work Item's log and the Refinement tab's read. Step
    /// definitions live in Slice15VotesWithAnAccountSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-15")]
    public partial class Slice15VotesWithAnAccountTest : SizingVotesAcceptanceTest
    {
        protected override InstanceUnderTest Instance => InstanceUnderTest.WithSignInAndRoles;

        // @driving_port @real-io @us-15 @slice-15 @contract-shape:bounded-change
        [Test]
        public async Task A_signed_in_reader_votes_under_their_account_without_giving_a_name()
        {
            var (gravity, jonas) = await GivenJonasReadsTeamGravity();

            await WhenTheyVote(jonas, gravity, ConfigurationManagement, Answer.Yes);

            await ThenTheLogShowsTheVoteUnder(jonas, gravity, ConfigurationManagement, JonasWeber);
        }

        // @driving_port @real-io @us-15 @slice-15 @error @contract-shape:bounded-change
        // With sign-in, who you are comes from the session; a name in the request cannot change it.
        [Test]
        public async Task A_name_sent_with_a_signed_in_vote_is_ignored()
        {
            var (gravity, jonas) = await GivenJonasReadsTeamGravity();

            await WhenTheyVoteClaimingToBe(jonas, gravity, ConfigurationManagement, AnaLima);

            await ThenTheLogShowsTheVoteUnder(jonas, gravity, ConfigurationManagement, JonasWeber);
        }

        // @driving_port @real-io @us-15 @slice-15 @boundary @contract-shape:bounded-change
        [Test]
        public async Task One_account_is_one_voter_whichever_browser_it_votes_from()
        {
            var (gravity, jonas) = await GivenJonasReadsTeamGravity();
            var jonasOnHisLaptop = jonas with { Key = NewVoterKey() };
            var jonasOnHisPhone = jonas with { Key = NewVoterKey() };

            await WhenTheyVote(jonasOnHisLaptop, gravity, ConfigurationManagement, Answer.Yes);
            await WhenTheyVote(jonasOnHisPhone, gravity, ConfigurationManagement, Answer.No);

            await ThenBothBrowsersSeeOneVoteAndItIsHis(gravity, jonasOnHisLaptop, jonasOnHisPhone, Answer.No);
        }

        // @driving_port @real-io @us-11 @us-15 @slice-15 @contract-shape:bounded-change
        [Test]
        public async Task A_Team_admin_votes_like_any_reader()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            var priya = ASignedInAdminOf(gravity, "priya", PriyaSharma);

            await WhenTheyVote(priya, gravity, ConfigurationManagement, Answer.YesBut);

            await ThenTheLogShowsTheVoteUnder(priya, gravity, ConfigurationManagement, PriyaSharma);
        }

        // @driving_port @real-io @us-15 @slice-15 @boundary @contract-shape:bounded-change
        [Test]
        public async Task Two_accounts_with_the_same_name_are_two_voters()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();

            await WhenTheyVote(ASignedInReaderOf(gravity, "ana-lima-zurich", AnaLima), gravity, ConfigurationManagement, Answer.Yes);
            await WhenTheyVote(ASignedInReaderOf(gravity, "ana-lima-lisbon", AnaLima), gravity, ConfigurationManagement, Answer.Yes);

            await ThenTheRowCounts(gravity, ConfigurationManagement, voteCount: 2);
        }

        // @driving_port @real-io @us-15 @slice-15 @error @contract-shape:unbounded-preservation
        // The tab opens and a vote is taken for a reader first, so the refusal can only be about the role.
        [Test]
        [Ignore(PendingSlice15)]
        public async Task Somebody_without_a_role_on_the_Team_can_neither_open_the_tab_nor_vote()
        {
            var (gravity, jonas) = await GivenJonasReadsTeamGravityAndHasVotedYes();
            var lena = ASignedInPersonWithoutARoleOn("lena", "Lena Brandt");

            using var tab = await OpensTheRefinementTab(lena, gravity.TeamId);
            using var vote = await Votes(lena, gravity, ConfigurationManagement, Answer.No);

            await ThenBothAreNotFoundAndOnlyJonassVoteCounts(tab, vote, gravity, jonas);
        }

        // @driving_port @real-io @us-12 @us-15 @slice-15 @error @contract-shape:unbounded-preservation
        [Test]
        [Ignore(PendingSlice15)]
        public async Task Somebody_without_a_role_on_the_Team_can_neither_read_the_log_nor_comment()
        {
            var (gravity, jonas) = await GivenJonasReadsTeamGravityAndHasVotedYes();
            var lena = ASignedInPersonWithoutARoleOn("lena", "Lena Brandt");

            using var log = await OpensTheLog(lena, gravity, ConfigurationManagement);
            using var comment = await Comments(lena, gravity, ConfigurationManagement, "Is this still needed?");

            await ThenBothAreNotFoundAndTheLogHoldsOnlyJonassVote(log, comment, gravity, jonas);
        }

        // @driving_port @real-io @us-15 @slice-15 @contract-shape:pure-function
        [Test]
        public async Task With_sign_in_the_tab_says_a_voter_is_known_by_their_account()
        {
            var (gravity, jonas) = await GivenJonasReadsTeamGravity();

            var tab = await TheTabAsSeenBy(jonas, gravity);

            Assert.That(VoterIdentityIn(tab), Is.EqualTo("Account"));
        }
    }

    /// <summary>
    /// With sign-in on and roles not enforced, everybody signed in may read every Team, and so may vote.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-15")]
    public class Slice15VotesWithoutRolesTest : SizingVotesAcceptanceTest
    {
        protected override InstanceUnderTest Instance => InstanceUnderTest.WithSignInWithoutRoles;

        // @driving_port @real-io @us-15 @slice-15 @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice15)]
        public async Task Every_signed_in_person_votes_when_roles_are_not_enforced()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            var jonas = ASignedInPersonWithoutARoleOn("jonas", JonasWeber);

            using var vote = await Votes(jonas, gravity, ConfigurationManagement, Answer.Yes);
            var row = RowOf(await TheTabAsSeenBy(jonas, gravity), ConfigurationManagement);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(vote.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(row.VoteCount, Is.EqualTo(1));
                Assert.That(row.MyVote, Is.EqualTo(nameof(Answer.Yes)));
            }
        }
    }
}
