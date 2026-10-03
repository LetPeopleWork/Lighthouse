using System.Net;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Refinement
{
    /// <summary>
    /// The command line and an assistant read votes and readiness through the same read as the tab, and
    /// cast, comment and take back through the same writes, under the same rules: without sign-in the
    /// client must send the name its user configured and the key it keeps, and with sign-in the vote is
    /// the credential's owner's - or refused when the credential belongs to no person. Each entry says it
    /// came from a client, so a reader of the log can tell.
    ///
    /// This is the Lighthouse half of the client slices; the commands and assistant tools themselves are
    /// specified in the clients' own repository. Step definitions live in Slice17ClientVotesSpecifications.cs.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-17a")]
    [Category("slice-17b")]
    public partial class Slice17ClientVotesTest : SizingVotesAcceptanceTest
    {
        // --- Reading votes and readiness (17a) ---

        // @driving_port @real-io @us-17a @slice-17a @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice17a)]
        public async Task A_client_is_told_which_Work_Items_need_discussion_and_which_are_Ready()
        {
            var gravity = await GivenApiVersioningNeedsDiscussionAndConfigurationManagementIsReady();

            var tab = await WhenPriyasClientReadsTheRefinement(gravity);

            ThenTheClientReads(tab, (ApiVersioning, "NeedsDiscussion"), (ConfigurationManagement, "Ready"));
        }

        // @driving_port @real-io @us-14 @us-17a @slice-17a @error @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice17a)]
        public async Task A_client_whose_user_has_not_voted_is_not_told_the_split()
        {
            var gravity = await GivenApiVersioningNeedsDiscussionAndConfigurationManagementIsReady();

            var tab = await WhenPriyasClientReadsTheRefinement(gravity);

            ThenTheClientSeesCountsButNoSplitOn(tab, ApiVersioning, voteCount: 4);
        }

        // @driving_port @real-io @us-14 @us-17a @slice-17a @boundary @contract-shape:pure-function
        [Test]
        [Ignore(PendingSlice17a)]
        public async Task A_client_whose_user_voted_through_it_is_told_the_split()
        {
            var gravity = await GivenApiVersioningNeedsDiscussionAndConfigurationManagementIsReady();
            await HasVoted(PriyasClient, gravity, ApiVersioning, Answer.Yes, channel: Channel.Cli);

            var tab = await WhenPriyasClientReadsTheRefinement(gravity);

            Assert.That(RowOf(tab, ApiVersioning).Split, Is.EqualTo(new SplitReading(Yes: 4, YesBut: 0, No: 1)));
        }

        // --- Casting from a client without sign-in (17b) ---

        // @driving_port @real-io @us-17b @slice-17b @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice17b)]
        public async Task A_voter_casts_a_Yes_but_with_its_condition_from_the_command_line()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();

            await WhenAnasCommandLineSays(gravity, AdvancedReporting, Answer.YesBut, OnlyIfThePdfExportMovesOut);

            await ThenAnaReadsHerEntryMarkedAsFrom(gravity, AdvancedReporting, nameof(Channel.Cli), nameof(Answer.YesBut), OnlyIfThePdfExportMovesOut);
        }

        // @driving_port @real-io @us-17b @slice-17b @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice17b)]
        public async Task A_vote_cast_through_an_assistant_is_marked_as_cast_through_an_assistant()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();

            await HasVoted(AnasClient, gravity, AdvancedReporting, Answer.No, channel: Channel.Assistant);

            await ThenAnaReadsHerEntryMarkedAsFrom(gravity, AdvancedReporting, nameof(Channel.Assistant), nameof(Answer.No), comment: null);
        }

        // @driving_port @real-io @us-17b @slice-17b @error @contract-shape:unbounded-preservation
        // A client never makes a name up for its user; the server refuses a nameless vote anyway.
        [Test]
        [Ignore(PendingSlice17b)]
        public async Task A_client_vote_without_a_name_is_refused_and_nothing_is_recorded()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();

            using var refused = await Votes(new Voter(null, NewVoterKey()), gravity, AdvancedReporting, Answer.Yes, channel: Channel.Cli);

            await ThenTheClientIsRefusedAndNothingCounts(refused, gravity, HttpStatusCode.BadRequest, VoterNameRequired);
        }

        // @driving_port @real-io @us-12 @us-17b @slice-17b @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice17b)]
        public async Task A_question_asked_through_an_assistant_flags_the_Work_Item()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();

            await HasCommented(AnasClient, gravity, ApiVersioning, "Which API version?", Channel.Assistant);

            ThenTheRowHasAnOpenQuestion(await TheTabAsSeenBy(ABrowserOf(PriyaSharma), gravity), ApiVersioning);
        }

        // @driving_port @real-io @us-16 @us-17b @slice-17b @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice17b)]
        public async Task A_client_takes_back_the_vote_it_cast()
        {
            var gravity = await GivenAnasCommandLineVotedYesAndJonasVotedYesOnAdvancedReporting();

            using var takeBack = await TakesBackTheirVote(AnasClient, gravity, AdvancedReporting);

            await ThenJonasReadsTheLogKinds(takeBack, gravity, "Vote", "Vote", "Revocation");
        }

        // @driving_port @real-io @us-16 @us-17b @slice-17b @error @contract-shape:unbounded-preservation
        // The command line keeps a key of its own: it is not the browser, even under the same name.
        [Test]
        [Ignore(PendingSlice17b)]
        public async Task A_client_cannot_take_back_a_vote_its_user_cast_from_a_browser()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            await HasVoted(AnasBrowser, gravity, AdvancedReporting, Answer.Yes);
            await HasVoted(Jonas, gravity, AdvancedReporting, Answer.Yes);

            using var takeBack = await TakesBackTheirVote(AnasClient, gravity, AdvancedReporting);

            await ThenJonasReadsTheLogKinds(takeBack, gravity, "Vote", "Vote");
        }
    }

    /// <summary>
    /// With sign-in on and roles not enforced, an API key speaks for the person who owns it - or for
    /// nobody, in which case it cannot vote.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5510-5881-refinement")]
    [Category("slice-17b")]
    public class Slice17ClientVotesWithAnApiKeyTest : SizingVotesAcceptanceTest
    {
        protected override InstanceUnderTest Instance => InstanceUnderTest.WithSignInWithoutRoles;

        // @driving_port @real-io @us-15 @us-17b @slice-17b @contract-shape:bounded-change
        [Test]
        [Ignore(PendingSlice17b)]
        public async Task A_personal_API_key_votes_under_its_owners_name()
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            var anasKey = await APersonalApiKeyOf("ana", AnaLima);

            await HasVoted(anasKey, gravity, AdvancedReporting, Answer.Yes, channel: Channel.Cli);

            var entries = EntriesIn(await TheLogAsSeenBy(anasKey, gravity, AdvancedReporting));
            List<(string?, string?)> anaFromTheCommandLine = [(AnaLima, nameof(Channel.Cli))];

            Assert.That(entries.Select(entry => (entry.VoterName, entry.Channel)), Is.EqualTo(anaFromTheCommandLine));
        }

        // @driving_port @real-io @us-15 @us-17b @slice-17b @error @contract-shape:unbounded-preservation
        // A key nobody owns may read the Team, but a vote under it would have no voter.
        [TestCase("vote")]
        [TestCase("comment")]
        [TestCase("take back")]
        [Ignore(PendingSlice17b)]
        public async Task A_credential_that_belongs_to_no_person_cannot_add_to_the_log(string action)
        {
            var gravity = await GravityRefinesSixWorkItemsNobodyHasVotedOn();
            var bridge = await AnApiKeyThatBelongsToNoPerson();

            using var refused = await WhenTheKeyTriesTo(action, bridge, gravity);
            var code = await RefusalCodeOf(refused);
            var row = RowOf(await TheTabAsSeenBy(ASignedInPersonWithoutARoleOn("priya", PriyaSharma), gravity), AdvancedReporting);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(code, Is.EqualTo(VoteNeedsAPerson));
                Assert.That(row.VoteCount, Is.Zero);
                Assert.That(row.HasComments, Is.False);
            }
        }

        private async Task<HttpResponseMessage> WhenTheKeyTriesTo(string action, Voter key, TeamUnderTest team)
            => action switch
            {
                "vote" => await Votes(key, team, AdvancedReporting, Answer.Yes, channel: Channel.Assistant),
                "comment" => await Comments(key, team, AdvancedReporting, "Which API version?", Channel.Assistant),
                _ => await TakesBackTheirVote(key, team, AdvancedReporting),
            };
    }
}
