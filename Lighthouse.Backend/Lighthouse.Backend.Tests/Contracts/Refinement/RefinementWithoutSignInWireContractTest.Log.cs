using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Lighthouse.Backend.Tests.API.Integration.Refinement;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Contracts.Refinement
{
    /// <summary>
    /// Comments, questions and the log of a Work Item on the wire: the routes a browser writes to and reads from,
    /// the status and refusal code each refusal is answered with, and the names, shapes and texts the browser
    /// reads in the log. What a log says and which comments are accepted is asserted at the port.
    /// </summary>
    public partial class RefinementWithoutSignInWireContractTest
    {
        private const string AWorkItemInRefinement = RefinementPortGivens.AdvancedReporting;

        private const string AnotherWorkItemInRefinement = RefinementPortGivens.ApiVersioning;

        private const string AWorkItemBeingImplemented = RefinementPortGivens.BillingExport;

        private const string TheQuestionAsked = "Which API version?";

        private const string JonasWeberTheAsker = "Jonas Weber";

        private const string AnaLimaTheVoter = "Ana Lima";

        private const string MoOkaforTheAsker = "Mo Okafor";

        private static readonly string[] EverythingALogEntryCarries =
            ["kind", "answer", "comment", "voterName", "channel", "recordedAt", "isMine", "isOpenQuestion"];

        private static readonly string[] EverythingALogCarries = ["entries", "voters"];

        private static readonly string[] EveryAnswerAVoterCanGive = ["yes", "yesBut", "no"];

        private static readonly bool[] OnlyTheThirdEntryIsAnOpenQuestion = [false, false, true];

        // The log opens for a listed Work Item first, so the refusal can only be about the Work Item.
        [Test]
        public async Task The_log_of_a_Work_Item_that_is_not_in_refinement_is_not_found()
        {
            var gravity = GivenGravityRefinesFiveWorkItems();
            var ana = AVoterNamed(AnaLimaTheVoter);
            await GivenTheVoterHasVotedYes(ana, gravity, AWorkItemInRefinement);
            using var opened = await WhenTheLogIsOpened(ana, gravity, AWorkItemInRefinement);
            Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                "the log of a listed Work Item does not open, so a refusal for another one proves nothing");

            using var refused = await WhenTheLogIsOpened(ana, gravity, AWorkItemBeingImplemented);

            Assert.That(refused.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "the log of a Work Item the tab does not list is not found");
        }

        // A reference is whatever text the tracker uses; a slash in it travels escaped, as a browser sends it.
        [TestCase("REQ/12")]
        [TestCase("REQ/12#3")]
        public async Task The_log_of_a_Work_Item_whose_reference_holds_a_slash_opens(string reference)
        {
            var gravity = new RefinementPortGivens(Services).GravityRefinesFiveWorkItemsAndOneReferencedAs(reference);
            var jonas = AVoterNamed(JonasWeberTheAsker);
            var beforeTheQuestion = TheInstancesInstantInUtc();
            await GivenTheVoterHasAsked(jonas, gravity, reference, TheQuestionAsked);

            using var opened = await WhenTheLogIsOpened(AVoterNamed(AnaLimaTheVoter), gravity, reference);
            var afterTheRead = TheInstancesInstantInUtc();

            Assert.That(opened.StatusCode, Is.EqualTo(HttpStatusCode.OK), "the log of a Work Item whose reference holds a slash must open");
            var log = await BodyOf(opened);
            ThenTheLogReadsOnlyTheQuestion(log, TheQuestionAsked, JonasWeberTheAsker);
            Assert.That(log.GetProperty("entries").EnumerateArray().Select(entry => InstantInUtc(TextIn(entry, "recordedAt"))),
                Has.All.InRange(beforeTheQuestion, afterTheRead), "every entry says when it was recorded");
        }

        [Test]
        public async Task A_question_without_a_name_is_refused()
        {
            var gravity = GivenGravityRefinesFiveWorkItems();

            using var refused = await WhenTheVoterAsks(AVoterNamed(null), gravity, AnotherWorkItemInRefinement, TheQuestionAsked);

            await ThenTheQuestionIsRefusedWith(refused, HttpStatusCode.BadRequest, "voter-name-required");
            await ThenTheRowIsUntouched(gravity, AnotherWorkItemInRefinement);
        }

        // The browser reads when an entry was recorded as this text and nothing else: a date, a time with
        // milliseconds, and the UTC mark.
        [Test]
        public async Task A_log_entry_names_when_it_was_recorded_as_a_UTC_time_with_milliseconds()
        {
            var gravity = GivenGravityRefinesFiveWorkItems();
            var ana = AVoterNamed(AnaLimaTheVoter);
            await GivenTheVoterHasVotedYes(ana, gravity, AWorkItemInRefinement);

            var log = await WhenTheLogIsRead(ana, gravity, AWorkItemInRefinement);

            Assert.That(TextIn(log.GetProperty("entries")[0], "recordedAt"), Does.Match(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$"),
                "the browser parses the time an entry was recorded from this text only");
        }

        // The browser reads exactly these members and marks the question from the flag; a voter's key must never
        // travel with an entry, and the voters are names, never keys.
        [Test]
        public async Task A_log_carries_only_what_the_browser_reads_and_marks_the_open_question()
        {
            var gravity = GivenGravityRefinesFiveWorkItems();
            await GivenTheVoterHasVotedYes(AVoterNamed(JonasWeberTheAsker), gravity, AWorkItemInRefinement);
            await GivenTheVoterHasVotedNo(AVoterNamed(AnaLimaTheVoter), gravity, AWorkItemInRefinement);
            await GivenTheVoterHasAsked(AVoterNamed(MoOkaforTheAsker), gravity, AWorkItemInRefinement, TheQuestionAsked);

            var log = await WhenTheLogIsRead(AVoterNamed(AnaLimaTheVoter), gravity, AWorkItemInRefinement);

            var entries = log.GetProperty("entries").EnumerateArray().ToList();
            var voters = log.GetProperty("voters");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(entries.Select(entry => FlagIn(entry, "isOpenQuestion")), Is.EqualTo(OnlyTheThirdEntryIsAnOpenQuestion), $"Log: {log}");
                Assert.That(entries.Select(entry => entry.EnumerateObject().Select(property => property.Name)),
                    Has.All.EquivalentTo(EverythingALogEntryCarries), "a log entry carries nothing else, and never the voter's key");
                Assert.That(log.EnumerateObject().Select(property => property.Name), Is.EquivalentTo(EverythingALogCarries),
                    "the log carries its entries and who votes what, nothing else");
                Assert.That(voters.EnumerateObject().Select(property => property.Name), Is.EquivalentTo(EveryAnswerAVoterCanGive),
                    "every answer is there, and nothing else is");
                Assert.That(voters.EnumerateObject().SelectMany(answer => answer.Value.EnumerateArray()).Select(name => name.ValueKind),
                    Has.All.EqualTo(JsonValueKind.String), "a voter is named, never handed out as a key or an object");
            }
        }

        [Test]
        public async Task A_question_on_a_Work_Item_that_is_not_in_refinement_answers_409_and_names_why()
        {
            var gravity = GivenGravityRefinesFiveWorkItems();

            using var refused = await WhenTheVoterAsks(AVoterNamed(JonasWeberTheAsker), gravity, AWorkItemBeingImplemented, TheQuestionAsked);

            await ThenTheQuestionIsRefusedWith(refused, HttpStatusCode.Conflict, "work-item-not-in-refinement");
        }

        [Test]
        public async Task An_empty_question_answers_400_and_names_that_a_comment_is_required()
        {
            var gravity = GivenGravityRefinesFiveWorkItems();

            using var refused = await WhenTheVoterAsks(AVoterNamed(JonasWeberTheAsker), gravity, AnotherWorkItemInRefinement, string.Empty);

            await ThenTheQuestionIsRefusedWith(refused, HttpStatusCode.BadRequest, "comment-required");
        }

        // The limit is exactly two thousand: that many is accepted and kept whole, one more is refused below.
        [Test]
        public async Task A_question_of_two_thousand_characters_is_accepted_and_kept_whole()
        {
            var gravity = GivenGravityRefinesFiveWorkItems();
            var twoThousand = new string('x', 2000);
            var jonas = AVoterNamed(JonasWeberTheAsker);

            using var accepted = await WhenTheVoterAsks(jonas, gravity, AnotherWorkItemInRefinement, twoThousand);

            Assert.That(accepted.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"a question of exactly the limit must be accepted: {await accepted.Content.ReadAsStringAsync()}");
            var log = await WhenTheLogIsRead(jonas, gravity, AnotherWorkItemInRefinement);
            Assert.That(log.GetProperty("entries").EnumerateArray().Select(entry => TextIn(entry, "comment")), Is.EqualTo(new[] { twoThousand }),
                "the question must be kept whole");
        }

        [Test]
        public async Task A_question_longer_than_two_thousand_characters_answers_400_and_names_it_too_long()
        {
            var gravity = GivenGravityRefinesFiveWorkItems();

            using var refused = await WhenTheVoterAsks(AVoterNamed(JonasWeberTheAsker), gravity, AnotherWorkItemInRefinement, new string('x', 2001));

            await ThenTheQuestionIsRefusedWith(refused, HttpStatusCode.BadRequest, "comment-too-long");
        }

        [Test]
        public async Task A_vote_whose_comment_is_longer_than_two_thousand_characters_answers_400_and_names_it_too_long()
        {
            var gravity = GivenGravityRefinesFiveWorkItems();

            using var refused = await WhenTheVoterVotesYes(AVoterNamed(JonasWeberTheAsker), gravity, AnotherWorkItemInRefinement, new string('x', 2001));

            await ThenTheQuestionIsRefusedWith(refused, HttpStatusCode.BadRequest, "comment-too-long");
        }

        // The browser replaces the row it shows with the row the answer carries, so the answer must flag the open
        // question itself.
        [Test]
        public async Task A_question_is_answered_with_the_row_of_its_Work_Item_flagged_as_an_open_question()
        {
            var gravity = GivenGravityRefinesFiveWorkItems();

            using var answered = await WhenTheVoterAsks(AVoterNamed(JonasWeberTheAsker), gravity, AnotherWorkItemInRefinement, TheQuestionAsked);

            await ThenTheAnswerIsTheRowFlaggedAsAnOpenQuestion(answered, AnotherWorkItemInRefinement);
        }

        // --- Given ---

        private int GivenGravityRefinesFiveWorkItems() => new RefinementPortGivens(Services).GravityRefinesFiveWorkItems();

        private async Task GivenTheVoterHasVotedYes(WireVoter voter, int teamId, string workItem)
        {
            using var response = await WhenTheVoterVotesYes(voter, teamId, workItem, comment: null);
            Assert.That(response.IsSuccessStatusCode, Is.True, $"{voter.Name}'s vote was not accepted, so nothing that follows can build on it: {await response.Content.ReadAsStringAsync()}");
        }

        private async Task GivenTheVoterHasVotedNo(WireVoter voter, int teamId, string workItem)
        {
            using var response = await SendsFrom(voter, HttpMethod.Post, $"{WorkItemRoute(teamId, workItem)}/votes", VoteBody(voter, "No", comment: null));
            Assert.That(response.IsSuccessStatusCode, Is.True, $"{voter.Name}'s vote was not accepted, so nothing that follows can build on it: {await response.Content.ReadAsStringAsync()}");
        }

        private async Task GivenTheVoterHasAsked(WireVoter voter, int teamId, string workItem, string question)
        {
            using var response = await WhenTheVoterAsks(voter, teamId, workItem, question);
            Assert.That(response.IsSuccessStatusCode, Is.True, $"{voter.Name}'s question was not accepted, so nothing that follows can build on it: {await response.Content.ReadAsStringAsync()}");
        }

        // --- When ---

        private async Task<HttpResponseMessage> WhenTheVoterAsks(WireVoter voter, int teamId, string workItem, string question)
            => await SendsFrom(voter, HttpMethod.Post, $"{WorkItemRoute(teamId, workItem)}/comments", CommentBody(voter, question));

        private async Task<HttpResponseMessage> WhenTheVoterVotesYes(WireVoter voter, int teamId, string workItem, string? comment)
            => await SendsFrom(voter, HttpMethod.Post, $"{WorkItemRoute(teamId, workItem)}/votes", VoteBody(voter, "Yes", comment));

        private async Task<HttpResponseMessage> WhenTheLogIsOpened(WireVoter voter, int teamId, string workItem)
            => await SendsFrom(voter, HttpMethod.Get, $"{WorkItemRoute(teamId, workItem)}/log", body: null);

        private async Task<JsonElement> WhenTheLogIsRead(WireVoter voter, int teamId, string workItem)
        {
            using var response = await WhenTheLogIsOpened(voter, teamId, workItem);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"the log could not be read: {await response.Content.ReadAsStringAsync()}");

            return await BodyOf(response);
        }

        // --- Then ---

        private static void ThenTheLogReadsOnlyTheQuestion(JsonElement log, string question, string askedBy)
        {
            var entries = log.GetProperty("entries").EnumerateArray()
                .Select(entry => (TextIn(entry, "kind"), TextIn(entry, "answer"), TextIn(entry, "comment"), TextIn(entry, "voterName"), TextIn(entry, "channel"), FlagIn(entry, "isMine")));

            Assert.That(entries, Is.EqualTo(new[] { ((string?)"Comment", (string?)null, (string?)question, (string?)askedBy, (string?)"Web", (bool?)false) }),
                $"the log of a Work Item whose reference holds a slash must say the question that was asked on it. Log: {log}");
        }

        private static async Task ThenTheQuestionIsRefusedWith(HttpResponseMessage refused, HttpStatusCode status, string code)
        {
            var body = await BodyOf(refused);

            Assert.That((refused.StatusCode, TextIn(body, "code")), Is.EqualTo((status, (string?)code)),
                $"the refusal must carry its status and the code the browser reads to say why. Body: {body}");
        }

        private async Task ThenTheRowIsUntouched(int teamId, string workItem)
        {
            using var response = await Client.GetAsync($"/api/latest/teams/{teamId}/refinement");
            var tab = await BodyOf(response);
            var row = tab.GetProperty("workItems").EnumerateArray().Single(candidate => TextIn(candidate, "referenceId") == workItem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(NumberIn(row, "voteCount"), Is.Zero, "a refused vote must not be counted");
                Assert.That(FlagIn(row, "hasComments"), Is.False, "a refused comment must leave no trace on the row");
                Assert.That(FlagIn(row, "hasOpenQuestion"), Is.False);
            }
        }

        private static async Task ThenTheAnswerIsTheRowFlaggedAsAnOpenQuestion(HttpResponseMessage answered, string workItem)
        {
            Assert.That(answered.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"the question was not answered with a row: {await answered.Content.ReadAsStringAsync()}");

            var row = await BodyOf(answered);
            Assert.That((TextIn(row, "referenceId"), NumberIn(row, "voteCount"), FlagIn(row, "hasComments"), FlagIn(row, "hasOpenQuestion")),
                Is.EqualTo(((string?)workItem, (int?)0, (bool?)true, (bool?)true)),
                $"the answer must carry the row of the Work Item the question was asked on, flagged as having an open question. Row: {row}");
        }

        // --- Sending and reading ---

        /// <summary>A reader on an instance without sign-in: the name they typed, if any, and the key their browser made up and sends with every request.</summary>
        private sealed record WireVoter(string? Name, string Key);

        private static WireVoter AVoterNamed(string? name) => new(name, $"{Guid.NewGuid():N}{Guid.NewGuid():N}");

        private static string WorkItemRoute(int teamId, string workItem)
            => $"/api/latest/teams/{teamId}/refinement/work-items/{Uri.EscapeDataString(workItem)}";

        private static JsonObject CommentBody(WireVoter voter, string comment)
            => WithTheVotersName(voter, new JsonObject { ["comment"] = comment, ["channel"] = "Web" });

        private static JsonObject VoteBody(WireVoter voter, string answer, string? comment)
        {
            var body = WithTheVotersName(voter, new JsonObject { ["answer"] = answer, ["channel"] = "Web" });
            if (comment is not null)
            {
                body["comment"] = comment;
            }

            return body;
        }

        private static JsonObject WithTheVotersName(WireVoter voter, JsonObject body)
        {
            if (voter.Name is not null)
            {
                body["voterName"] = voter.Name;
            }

            return body;
        }

        private async Task<HttpResponseMessage> SendsFrom(WireVoter voter, HttpMethod method, string address, JsonObject? body)
        {
            using var request = new HttpRequestMessage(method, address);
            request.Headers.Add(RefinementController.VoterKeyHeader, voter.Key);
            if (body is not null)
            {
                request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
            }

            return await Client.SendAsync(request);
        }

        /// <summary>The instance's clock now, in UTC and cut to the millisecond a log entry is written with.</summary>
        private DateTime TheInstancesInstantInUtc()
        {
            using var scope = NewScope();
            var now = scope.ServiceProvider.GetRequiredService<ILighthouseClock>().Now.UtcDateTime;
            return new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerMillisecond, DateTimeKind.Utc);
        }

        private static DateTime InstantInUtc(string? text)
            => DateTime.Parse(text ?? throw new InvalidOperationException("an entry carries no time"), System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal);

        private static async Task<JsonElement> BodyOf(HttpResponseMessage response)
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return document.RootElement.Clone();
        }

        private static bool? FlagIn(JsonElement owner, string member)
            => owner.TryGetProperty(member, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;
    }
}
