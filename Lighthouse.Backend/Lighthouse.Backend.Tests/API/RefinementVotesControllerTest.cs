using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    /// <summary>
    /// Casting a vote over HTTP: which answer each refusal gets, which of them name a code the browser can act
    /// on, and that a recorded vote is answered with the row as it now stands.
    /// </summary>
    [TestFixture]
    [Category("epic-5510-5881-refinement")]
    public class RefinementVotesControllerTest
    {
        private const int TeamId = 12;

        private const string InRefinement = "GR-1";

        private const string Jonas = "Jonas";

        private const string BrowserKey = "jonas-browser-keeps-this-voter-key";

        private Mock<ISizingLogCommands> sizingLogCommandsMock;
        private Mock<IRefinementViewQuery> refinementViewQueryMock;
        private Mock<ILogger<RefinementVotesController>> loggerMock;
        private AuthMode authMode;

        [SetUp]
        public void SetUp()
        {
            sizingLogCommandsMock = new Mock<ISizingLogCommands>();
            refinementViewQueryMock = new Mock<IRefinementViewQuery>();
            loggerMock = new Mock<ILogger<RefinementVotesController>>();
            authMode = AuthMode.Disabled;
        }

        [TestCase(null, SizingChannel.Web)]
        [TestCase(SizingAnswer.Yes, null)]
        public void AVoteWithoutAnAnswerOrAChannelIsABadRequest(SizingAnswer? answer, SizingChannel? channel)
        {
            var result = CastVote(InRefinement, new SizingVoteDto { Answer = answer, Channel = channel, VoterName = Jonas });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(StatusOf(result), Is.EqualTo(StatusCodes.Status400BadRequest));
                Assert.That(ProblemOf(result).Title, Is.EqualTo("A vote needs an answer and the channel it was cast from."));
                sizingLogCommandsMock.VerifyNoOtherCalls();
            }
        }

        [TestCase(null, BrowserKey, AuthMode.Disabled, "voter-name-required", "A vote or comment needs the name of whoever sends it.", LogLevel.Information, true, TestName = "No name")]
        [TestCase(Jonas, null, AuthMode.Disabled, "voter-key-required", "A vote or comment needs the key the sender's browser keeps.", LogLevel.Information, true, TestName = "No key")]
        [TestCase(NameOf101Characters, BrowserKey, AuthMode.Disabled, "voter-name-too-long", "A name is at most 100 characters.", LogLevel.Information, false, TestName = "A name too long")]
        [TestCase(Jonas, BrowserKey, AuthMode.Enabled, "vote-needs-a-person", "A vote or comment needs a person to send it.", LogLevel.Warning, false, TestName = "Nobody behind the credential")]
        public void AVoteWithoutAVoterIsRefusedSayingWhyAndLogsTheReason(
            string? name, string? key, AuthMode mode, string reason, string title, LogLevel level, bool namesTheReason)
        {
            authMode = mode;

            var result = CastVote(InRefinement, new SizingVoteDto { Answer = SizingAnswer.Yes, Channel = SizingChannel.Cli, VoterName = name }, key);

            var problem = ProblemOf(result);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(StatusOf(result), Is.EqualTo(StatusCodes.Status400BadRequest));
                Assert.That(problem.Title, Is.EqualTo(title));
                Assert.That(problem.Extensions.TryGetValue("code", out var code) ? code : null, Is.EqualTo(namesTheReason ? reason : null),
                    "only a refusal the voter can put right names its reason to the browser");
                Assert.That(LoggedLines(), Is.EqualTo(new[] { (level, reason, TeamId.ToString(System.Globalization.CultureInfo.InvariantCulture), "Cli") }));
                sizingLogCommandsMock.VerifyNoOtherCalls();
            }
        }

        [TestCase(InRefinement, InRefinement)]
        [TestCase("ABC%2F7", "ABC/7")]
        [TestCase("abc%2f7", "abc/7")]
        [TestCase("A%25B", "A%25B")]
        public void ARecordedVoteIsAnsweredWithItsRowAsItNowStands(string routeValue, string reference)
        {
            sizingLogCommandsMock
                .Setup(commands => commands.Vote(TeamId, reference, new SizingVote(SizingAnswer.YesBut, SizingChannel.Web, "if it stays small"), It.IsAny<Voter>()))
                .Returns(VoteOutcome.Recorded);
            GivenTheRows(Row("GR-0", 3), Row(reference, 2, SizingAnswer.YesBut));

            var result = CastVote(routeValue, new SizingVoteDto { Answer = SizingAnswer.YesBut, Channel = SizingChannel.Web, Comment = "if it stays small", VoterName = Jonas });

            var row = (result.Result as OkObjectResult)?.Value as RefinementRowDto;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(row?.ReferenceId, Is.EqualTo(reference));
                Assert.That(row?.VoteCount, Is.EqualTo(2));
                Assert.That(row?.MyVote, Is.EqualTo(SizingAnswer.YesBut));
                refinementViewQueryMock.Verify(query => query.ForTeam(TeamId, BrowserKey));
            }
        }

        [TestCase(VoteOutcome.Recorded, false)]
        [TestCase(VoteOutcome.RecordedAndMadeReady, true)]
        public void ARecordedVoteSaysWhetherItIsTheVoteThatMadeItsRowReady(VoteOutcome outcome, bool madeReady)
        {
            sizingLogCommandsMock
                .Setup(commands => commands.Vote(TeamId, InRefinement, It.IsAny<SizingVote>(), It.IsAny<Voter>()))
                .Returns(outcome);
            GivenTheRows(Row(InRefinement, 3));

            var result = CastVote(InRefinement, new SizingVoteDto { Answer = SizingAnswer.Yes, Channel = SizingChannel.Web, VoterName = Jonas });

            var row = (result.Result as OkObjectResult)?.Value as VotedRowDto;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(row?.ReferenceId, Is.EqualTo(InRefinement));
                Assert.That(row?.Readiness, Is.EqualTo(RowReadiness.Ready));
                Assert.That(row?.MadeReady, Is.EqualTo(madeReady));
            }
        }

        [Test]
        public void ARecordedVoteIsCastAsTheVoterTheBrowserNamed()
        {
            sizingLogCommandsMock
                .Setup(commands => commands.Vote(TeamId, InRefinement, It.IsAny<SizingVote>(), It.IsAny<Voter>()))
                .Returns(VoteOutcome.Recorded);
            GivenTheRows(Row(InRefinement, 1));

            CastVote(InRefinement, new SizingVoteDto { Answer = SizingAnswer.No, Channel = SizingChannel.Web, VoterName = "  Jonas  " });

            sizingLogCommandsMock.Verify(commands => commands.Vote(
                TeamId,
                InRefinement,
                new SizingVote(SizingAnswer.No, SizingChannel.Web, null),
                new Voter(SizingLogEntry.SelfDeclaredVoterKeyOf(BrowserKey), Jonas, null)));
        }

        [Test]
        public void ARecordedVoteOnARowThatIsGoneByTheTimeItIsReadIsNotFound()
        {
            sizingLogCommandsMock
                .Setup(commands => commands.Vote(TeamId, InRefinement, It.IsAny<SizingVote>(), It.IsAny<Voter>()))
                .Returns(VoteOutcome.Recorded);
            GivenTheRows(Row("GR-0", 1));

            var result = CastVote(InRefinement, new SizingVoteDto { Answer = SizingAnswer.Yes, Channel = SizingChannel.Web, VoterName = Jonas });

            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void AVoteForATeamThatDoesNotExistIsNotFound()
        {
            sizingLogCommandsMock
                .Setup(commands => commands.Vote(TeamId, InRefinement, It.IsAny<SizingVote>(), It.IsAny<Voter>()))
                .Returns(VoteOutcome.TeamNotFound);

            var result = CastVote(InRefinement, new SizingVoteDto { Answer = SizingAnswer.Yes, Channel = SizingChannel.Web, VoterName = Jonas });

            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void AVoteOnAWorkItemOutOfRefinementIsAConflictThatNamesWhy()
        {
            sizingLogCommandsMock
                .Setup(commands => commands.Vote(TeamId, InRefinement, It.IsAny<SizingVote>(), It.IsAny<Voter>()))
                .Returns(VoteOutcome.WorkItemNotInRefinement);

            var result = CastVote(InRefinement, new SizingVoteDto { Answer = SizingAnswer.Yes, Channel = SizingChannel.Web, VoterName = Jonas });

            var problem = ProblemOf(result);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(StatusOf(result), Is.EqualTo(StatusCodes.Status409Conflict));
                Assert.That(problem.Title, Is.EqualTo("That Work Item is not in refinement."));
                Assert.That(problem.Extensions["code"], Is.EqualTo("work-item-not-in-refinement"));
            }
        }

        private const string NameOf101Characters =
            "Jonas Weber Jonas Weber Jonas Weber Jonas Weber Jonas Weber Jonas Weber Jonas Weber Jonas Weber Jonas";

        private ActionResult<RefinementRowDto> CastVote(string routeValue, SizingVoteDto vote, string? voterKey = BrowserKey)
        {
            var authModeResolver = Mock.Of<IAuthModeResolver>(resolver => resolver.Resolve() == new RuntimeAuthStatus { Mode = authMode });
            var controller = new RefinementVotesController(
                sizingLogCommandsMock.Object,
                refinementViewQueryMock.Object,
                new VoterIdentityResolver(authModeResolver),
                loggerMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
                ProblemDetailsFactory = new PlainProblemDetailsFactory(),
            };

            return controller.CastVote(TeamId, routeValue, vote, voterKey);
        }

        private static readonly RefinementNeed NoNeed = RefinementNeed.Unavailable(NeedUnavailableReason.NoCadence);

        private void GivenTheRows(params RefinementRow[] rows)
            => refinementViewQueryMock
                .Setup(query => query.ForTeam(TeamId, It.IsAny<string?>()))
                .Returns(new RefinementView(true, [.. rows], Yardstick.None, VoterIdentityKind.SelfDeclared, NoNeed));

        private static RefinementRow Row(string reference, int voteCount, SizingAnswer? myVote = null)
            => new(
                new WorkItem { TeamId = TeamId, ReferenceId = reference, Name = reference, State = "Backlog", Order = "1" },
                new RowVotes(voteCount, myVote, new VoteSplit(voteCount, 0, 0)),
                RowStanding.Ready);

        private static int? StatusOf(ActionResult<RefinementRowDto> result) => (result.Result as ObjectResult)?.StatusCode;

        private static ProblemDetails ProblemOf(ActionResult<RefinementRowDto> result)
            => (result.Result as ObjectResult)?.Value as ProblemDetails ?? throw new AssertionException($"expected problem details, got {result.Result}");

        private List<(LogLevel Level, string? Reason, string? TeamId, string? Channel)> LoggedLines()
            => [.. loggerMock.Invocations
                .Where(invocation => invocation.Method.Name == nameof(ILogger.Log))
                .Select(invocation => invocation.Arguments is [LogLevel level, _, IEnumerable<KeyValuePair<string, object?>> state, ..]
                    ? (level, FieldOf(state, "Reason"), FieldOf(state, "TeamId"), FieldOf(state, "Channel"))
                    : default)];

        private static string? FieldOf(IEnumerable<KeyValuePair<string, object?>> state, string name)
            => Convert.ToString(state.FirstOrDefault(field => field.Key == name).Value, System.Globalization.CultureInfo.InvariantCulture);

        private sealed class PlainProblemDetailsFactory : ProblemDetailsFactory
        {
            public override ProblemDetails CreateProblemDetails(
                HttpContext httpContext, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
                => new() { Status = statusCode, Title = title, Type = type, Detail = detail, Instance = instance };

            public override ValidationProblemDetails CreateValidationProblemDetails(
                HttpContext httpContext, ModelStateDictionary modelStateDictionary, int? statusCode = null, string? title = null, string? type = null, string? detail = null, string? instance = null)
                => new(modelStateDictionary) { Status = statusCode, Title = title };
        }
    }
}
