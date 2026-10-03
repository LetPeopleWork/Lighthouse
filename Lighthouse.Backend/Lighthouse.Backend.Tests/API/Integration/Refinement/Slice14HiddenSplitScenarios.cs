using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// Until you have voted on a Work Item you see how many have voted, not how they split and not what
    /// they wrote, so your view is your own. Once you vote, everything is shown. Readiness stays visible to
    /// everybody, because it does not reveal the split. The hiding is the server's: a reader who has not
    /// voted is never sent the split or the comments, so no client can show them.
    ///
    /// Driving ports: the Refinement tab's read and the Work Item's log. Step definitions live in
    /// Slice14HiddenSplitSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-14")]
    public partial class Slice14HiddenSplitTest : SizingVotesAcceptanceTest
    {
        // @driving_port @real-io @us-14 @slice-14 @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice14)]
        public async Task The_split_is_hidden_from_somebody_who_has_not_voted()
        {
            var gravity = await GivenAnaAndMoSaidYesAndPriyaSaidNoOnAdvancedReporting();

            var row = await WhenJonasLooksAt(gravity, AdvancedReporting);

            ThenHeSeesOnlyTheCountAndTheReadiness(row, voteCount: 3, readiness: "MoreYesNeeded");
        }

        // @driving_port @real-io @us-14 @slice-14 @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice14)]
        public async Task Voting_reveals_the_split()
        {
            var gravity = await GivenAnaAndMoSaidYesAndPriyaSaidNoOnAdvancedReporting();

            await WhenJonasVotes(gravity, AdvancedReporting, Answer.Yes);

            ThenHeSeesTheSplit(await WhenJonasLooksAt(gravity, AdvancedReporting), new SplitReading(Yes: 3, YesBut: 0, No: 1));
        }

        // @driving_port @real-io @us-14 @slice-14 @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice14)]
        public async Task The_log_stays_closed_to_somebody_who_has_not_voted()
        {
            var gravity = await GivenAnaAndMoSaidYesAndPriyaSaidNoOnAdvancedReporting();

            var log = await WhenJonasOpensTheLogOf(gravity, AdvancedReporting);

            ThenTheLogIsClosedSaying(log, voteCount: 3);
        }

        // @driving_port @real-io @us-14 @slice-14 @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice14)]
        public async Task Voting_opens_the_log()
        {
            var gravity = await GivenAnaAndMoSaidYesAndPriyaSaidNoOnAdvancedReporting();

            await WhenJonasVotes(gravity, AdvancedReporting, Answer.No);

            ThenTheLogIsOpenWithEntriesFrom(await WhenJonasOpensTheLogOf(gravity, AdvancedReporting), AnaLima, MoOkafor, PriyaSharma, JonasWeber);
        }

        // @driving_port @real-io @us-14 @slice-14 @boundary @contract-shape:pure-function
        // A question is not a view: it does not buy a look at everybody else's.
        [Test]
        [Ignore(PendingSlice14)]
        public async Task Asking_a_question_does_not_reveal_the_split()
        {
            var gravity = await GivenAnaAndMoSaidYesAndPriyaSaidNoOnAdvancedReporting();

            await WhenJonasAsks(gravity, AdvancedReporting, "Which export formats?");

            await ThenJonasStillSeesNeitherTheSplitNorTheLog(gravity, AdvancedReporting);
        }

        // @driving_port @real-io @us-14 @slice-14 @boundary @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice14)]
        public async Task Having_voted_on_one_Work_Item_reveals_nothing_about_another()
        {
            var gravity = await GivenAnaAndMoSaidYesAndPriyaSaidNoOnAdvancedReporting();

            await WhenJonasVotes(gravity, ConfigurationManagement, Answer.Yes);

            await ThenJonasStillSeesNeitherTheSplitNorTheLog(gravity, AdvancedReporting);
        }

        // @driving_port @real-io @us-14 @slice-14 @boundary @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice14)]
        public async Task Each_voter_sees_the_split_only_where_they_have_voted_themselves()
        {
            var gravity = await GivenAnaAndMoSaidYesAndPriyaSaidNoOnAdvancedReporting();

            var anaSees = await WhenAnaLooksAt(gravity, AdvancedReporting);
            var jonasSees = await WhenJonasLooksAt(gravity, AdvancedReporting);

            ThenOnlyAnaSeesTheSplit(anaSees, jonasSees);
        }

        // @driving_port @real-io @us-13 @us-14 @slice-14 @boundary @contract-shape:pure-function
        // Ready says enough people said Yes; it does not say who or how many said what.
        [Test]
        [Ignore(PendingSlice14)]
        public async Task A_Ready_Work_Item_shows_Ready_to_somebody_who_has_not_voted()
        {
            var gravity = await GivenAnaMoAndPriyaSaidYesOnConfigurationManagement();

            var row = await WhenJonasLooksAt(gravity, ConfigurationManagement);

            ThenHeSeesOnlyTheCountAndTheReadiness(row, voteCount: 3, readiness: "Ready");
        }

        // @driving_port @real-io @us-14 @us-17a @slice-14 @boundary @contract-shape:pure-function
        // A reader that brings no voter key - a script, an old client - has voted on nothing.
        [Test]
        [Ignore(PendingSlice14)]
        public async Task A_reader_who_brings_no_voter_key_sees_counts_and_readiness_only()
        {
            var gravity = await GivenAnaAndMoSaidYesAndPriyaSaidNoOnAdvancedReporting();

            var row = RowOf(await TheTabAsSeenBy(new Voter(null, null), gravity), AdvancedReporting);

            ThenHeSeesOnlyTheCountAndTheReadiness(row, voteCount: 3, readiness: "MoreYesNeeded");
        }
    }

    /// <summary>
    /// With sign-in on and roles enforced, a Team admin is a voter like any other: there is no way round
    /// the hiding by holding more rights.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-14")]
    public class Slice14HiddenFromTeamAdminsTest : SizingVotesAcceptanceTest
    {
        protected override InstanceUnderTest Instance => InstanceUnderTest.WithSignInAndRoles;

        // @driving_port @real-io @us-14 @slice-14 @error @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice14)]
        public async Task A_Team_admin_who_has_not_voted_sees_no_split_and_no_log_either()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            await HasVoted(ASignedInReaderOf(gravity, "ana", AnaLima), gravity, AdvancedReporting, Answer.Yes);
            await HasVoted(ASignedInReaderOf(gravity, "mo", MoOkafor), gravity, AdvancedReporting, Answer.No);
            var priya = ASignedInAdminOf(gravity, "priya", PriyaSharma);

            var row = RowOf(await TheTabAsSeenBy(priya, gravity), AdvancedReporting);
            var log = await TheLogAsSeenBy(priya, gravity, AdvancedReporting);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.VoteCount, Is.EqualTo(2));
                Assert.That(row.Split, Is.Null, "being a Team admin does not reveal the split");
                Assert.That(LogIsHidden(log), Is.True, "being a Team admin does not open the log");
                Assert.That(EntriesIn(log), Is.Empty);
            }
        }
    }
}
