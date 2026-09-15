using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Serilog;
using Serilog.Extensions.Logging;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.TaskManager
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic #5511 slice 06 — the warnings, without reading
    /// the log.
    ///
    /// The contract these steps fix, which is what DELIVER has to build:
    ///
    /// <c>GET /api/latest/logs/problems</c> — on the existing <c>LogsController</c>, inheriting the
    /// System-Administrator guard it already carries — answering a JSON array, most recent first, with one
    /// object per retained event:
    ///
    /// <list type="bullet">
    /// <item><c>recordedAt</c> — when it happened</item>
    /// <item><c>level</c> — <c>Warning</c>, <c>Error</c> or <c>Fatal</c>, rendered as the name, because the
    /// browser reads it as a word and a renumbering would silently relabel what an operator sees</item>
    /// <item><c>source</c> — the part of Lighthouse the event came from</item>
    /// <item><c>message</c> — the sentence as it was rendered, and where the row is about a refresh, naming
    /// whatever the refresh was of rather than pointing at it by id. A team or portfolio that has gone by
    /// the time the section is read has only a type and an id left to be called, and the row says those
    /// instead of naming nobody. Anything that was never about a refresh reads exactly as it was
    /// written</item>
    /// <item><c>exceptionType</c> — the type of what broke, absent when nothing threw</item>
    /// </list>
    ///
    /// <para>Names are resolved as the section is read, so a team renamed after its refresh broke is read
    /// under the name it has now — which is why nothing here asserts on what was captured at the time.</para>
    ///
    /// The buffer holds <c>RecentProblems:Capacity</c> entries (200 by default) and evicts oldest-first.
    ///
    /// <para><b>The wiring these specifications exist to prove.</b> The harness this fixture inherits
    /// replaces <see cref="ILoggerFactory"/> with one writing only to <c>CapturedLogs</c>, so an
    /// application warning would never reach a sink attached by <c>Program.ConfigureLogging</c> — and every
    /// scenario below would pass against a buffer that is not wired into the product at all. So the factory
    /// is rebuilt here around the running host's own production Serilog logger, taken from the container
    /// <c>UseSerilog</c> registered it in. Nothing is reconstructed and nothing is re-configured: if
    /// <c>Program</c> stops handing the sink to the logger, or hands it one instance and registers another,
    /// every scenario below goes red. <c>CapturedLogs</c> rides alongside so the harness's own observations
    /// keep working.</para>
    ///
    /// <para>The one deliberate difference from production: this wrapper admits everything
    /// (<c>MinimumLevel.Verbose</c>) and applies none of the harness's framework overrides, because
    /// anything it filtered would be filtered before the production logger ever saw it — and what the
    /// production logger sees is the subject. All the filtering that matters is the production logger's
    /// own, which is what AC-06.1 and AC-06.3 are about. The cost is that <c>CapturedLogs</c> is noisier
    /// here than in the other slices; nothing in this fixture asserts on it.</para>
    /// </summary>
    public partial class Slice06TheWarningsWithoutTheLogTest : TaskManagerAcceptanceTest
    {
        private const string ProblemsRoute = "/api/latest/logs/problems";
        private const string LogLevelRoute = "/api/latest/logs/level";
        private const string LogFileRoute = "/api/latest/logs";

        /// <summary>
        /// Where an identity provider comes back to when somebody signs in, including to say they were
        /// refused. Used here because it is the shortest way to a real complaint that has nothing to do
        /// with any refresh and so carries nothing a name could be looked up by.
        /// </summary>
        private const string TheSignInCallback = "/api/oauth/callback";

        /// <summary>
        /// What the tracker says when it turns a refresh away. One sentence, so a scenario can tell the
        /// failure it caused from anything else the instance happened to be complaining about.
        /// </summary>
        private const string TheTrackerComplaint = "Jira did not answer";

        /// <summary>
        /// What the instance says went wrong, with nothing about which team or portfolio it was. Written
        /// out here because a row has two halves and the name is only one of them: putting a name into the
        /// sentence means replacing the words that stood for the thing being refreshed, and a replacement
        /// that reaches too far takes the half that says anything broke at all with it. A row that has been
        /// reduced to a bare team name reads like a heading, not like trouble.
        /// </summary>
        private const string WhatTheInstanceSaysWhenARefreshBreaks = "Error processing update task";

        /// <summary>
        /// The level at which nothing this fixture can produce is reported. Used to observe that the
        /// instance's reporting level governs what is retained.
        /// </summary>
        private const string OnlyTheVeryWorst = "Fatal";

        private const string TheOrdinaryReportingLevel = "Information";

        /// <summary>
        /// How many problems this instance is given room for. Small on purpose: eviction is a promise about
        /// a full buffer, and filling the shipped default would mean provoking two hundred real failures.
        /// Large enough that the two or three events one refresh produces cannot push the one under
        /// assertion out on their own.
        /// </summary>
        private const int RoomForProblems = 5;

        private string? theReportingLevelThisTestChanged;

        /// <summary>
        /// Something an operator recognises and the queue's own name for the work of refreshing it. A
        /// portfolio's refresh is queued under the kind of work it does rather than under the word
        /// "portfolio", so the two cannot be told apart by id alone.
        /// </summary>
        private readonly record struct SeededWork(UpdateType Kind, int Id, string Name);

        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            services.RemoveAll<ILoggerFactory>();
            services.AddSingleton<ILoggerFactory>(serviceProvider => new SerilogLoggerFactory(
                new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Logger(serviceProvider.GetRequiredService<Serilog.ILogger>())
                    .WriteTo.Sink(CapturedLogs)
                    .CreateLogger(),
                dispose: true));
        }

        /// <summary>
        /// Runs after the harness has built its host, because NUnit works from the base class down.
        ///
        /// It has to be <c>UseSetting</c> rather than <c>ConfigureAppConfiguration</c>, and that is not a
        /// style choice: the buffer's size is read while the logger is built, which happens on the way in to
        /// <c>builder.Build()</c>, and configuration sources added the other way are not applied until the
        /// build itself — so they arrive after the sink already exists and the override silently does
        /// nothing. Measured, not assumed: with <c>ConfigureAppConfiguration</c> the sink reported room for
        /// 200.
        /// </summary>
        [SetUp]
        public void GiveTheInstanceRoomForOnlyAFewProblems()
        {
            Factory = Factory.WithWebHostBuilder(builder =>
                builder.UseSetting("RecentProblems:Capacity", RoomForProblems.ToString(CultureInfo.InvariantCulture)));
        }

        /// <summary>
        /// Runs before the harness tears the host down, because NUnit unwinds from the derived class
        /// outwards. Setting the level also rewrites the settings file beside the binaries, so a test that
        /// raised it puts it back rather than leaving the next run to inherit it.
        /// </summary>
        [TearDown]
        public async Task PutTheReportingLevelBackAsItWasFound()
        {
            if (theReportingLevelThisTestChanged is null)
            {
                return;
            }

            await SetTheReportingLevelTo(TheOrdinaryReportingLevel);
            theReportingLevelThisTestChanged = null;
        }

        // --- Given ---

        private SeededWork GivenATeamCalled(string name)
            => new(UpdateType.Team, SeedTeam(SeedConnection(), name), name);

        private SeededWork GivenAPortfolioCalled(string name)
            => new(UpdateType.Features, SeedPortfolio(SeedConnection(), name), name);

        /// <summary>
        /// One more team than the instance has room for problems, so that running all of them is guaranteed
        /// to have pushed the first one out — however many lines each failure happens to produce.
        /// </summary>
        private List<SeededWork> GivenMoreTeamsThanTheInstanceHasRoomToRememberProblemsFor()
            => [.. Enumerable
                .Range(1, RoomForProblems + 1)
                .Select(number => GivenATeamCalled($"Brewery {number.ToString(CultureInfo.InvariantCulture)}"))];

        /// <summary>
        /// A tracker that refuses every refresh. Validation is answered too, so that recording connection
        /// health has nothing to complain about on its own — this fixture counts what a failing refresh
        /// leaves behind, and a second warning about the first one would muddy every count in it.
        /// </summary>
        private void GivenTheTrackerTurnsEveryRefreshAway()
        {
            TheTrackerIsUnreachable(new InvalidOperationException(TheTrackerComplaint));

            ConnectorMock
                .Setup(c => c.ValidateConnection(It.IsAny<WorkTrackingSystemConnection>()))
                .ReturnsAsync(ConnectionValidationResult.Failure(
                    "connection_failed",
                    "Could not reach Jira.",
                    TheTrackerComplaint));
        }

        // --- When ---

        private Task WhenTheScheduledRefreshOfThatTeamRuns(SeededWork team)
            => TheTeamRefreshRuns(team.Id);

        private Task WhenTheScheduledRefreshOfThatPortfolioRuns(SeededWork portfolio)
            => ThePortfolioRefreshRuns(portfolio.Id);

        private async Task WhenTheScheduledRefreshOfEachOfThoseTeamsRuns(IEnumerable<SeededWork> teams)
        {
            foreach (var team in teams)
            {
                await WhenTheScheduledRefreshOfThatTeamRuns(team);
            }
        }

        /// <summary>
        /// Answers with what the team is called now, because that is what the rest of the scenario is
        /// about: a section read after a rename has to say the name somebody would recognise today.
        /// </summary>
        private SeededWork WhenThatTeamIsRenamedTo(SeededWork team, string newName)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

            var renamed = repository.GetById(team.Id)!;
            renamed.Name = newName;
            repository.Update(renamed);
            repository.Save().GetAwaiter().GetResult();

            return team with { Name = newName };
        }

        private void WhenThatTeamIsDeleted(SeededWork team)
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();

            repository.Remove(team.Id);
            repository.Save().GetAwaiter().GetResult();
        }

        /// <summary>
        /// A complaint with nothing to do with any refresh, raised the way the instance really raises one:
        /// the identity provider comes back saying the person was refused. Nothing in it carries a team or
        /// a portfolio, which is what makes it the control on rows that do.
        /// </summary>
        private async Task WhenAnIdentityProviderTurnsSomebodyAway(string refusal)
        {
            using var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            using var response = await client.GetAsync(
                new Uri($"{TheSignInCallback}?error={refusal}", UriKind.Relative));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect),
                $"Positive control: the instance was meant to take the refusal and send the browser on, and "
                + $"it answered {(int)response.StatusCode} — so nothing was complained about and the rest of "
                + $"this scenario would be reading an empty list.");
        }

        private async Task WhenTheOperatorTellsTheInstanceToReportOnlyTheVeryWorst()
        {
            theReportingLevelThisTestChanged = OnlyTheVeryWorst;
            await SetTheReportingLevelTo(OnlyTheVeryWorst);
        }

        private async Task WhenTheOperatorTellsTheInstanceToReportProblemsAgain()
        {
            await SetTheReportingLevelTo(TheOrdinaryReportingLevel);
            theReportingLevelThisTestChanged = null;
        }

        private async Task SetTheReportingLevelTo(string level)
        {
            using var client = Factory.CreateClient();
            using var content = JsonContent.Create(new { level });
            using var response = await client.PostAsync(new Uri(LogLevelRoute, UriKind.Relative), content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"Changing what the instance reports is how an operator already controls this, and it answered {(int)response.StatusCode}.");
        }

        // --- Then ---

        private async Task ThenRecentProblemsSaysWhatBrokeFor(SeededWork team)
        {
            var problems = await TheRecentProblems();

            Assert.That(problems.Exists(problem => IsAbout(problem, team)), Is.True,
                $"Nothing in the popover would tell an operator that '{team.Name}' stopped refreshing, so the "
                + $"only way to find out is still to go and read the log. Recent problems held: {Describe(problems)}");
        }

        private async Task ThenRecentProblemsSaysNothingAbout(SeededWork team)
        {
            var problems = await TheRecentProblems();

            Assert.That(problems.Exists(problem => IsAbout(problem, team)), Is.False,
                $"'{team.Name}' is in the list, so an operator is being shown something to worry about that is "
                + $"not a problem. Recent problems held: {Describe(problems)}");
        }

        /// <summary>
        /// The whole record, asserted on a failure a connector actually produced. An operator reading one
        /// row has to be able to tell when it happened, how seriously to take it, which part of Lighthouse
        /// is complaining and what actually broke — anything less and the section is a list of sentences
        /// with no way to act on them.
        /// </summary>
        private async Task ThenThatProblemSaysWhenHowSeriousWhereFromAndWhatBroke(SeededWork team)
        {
            var problem = await TheProblemAbout(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    DateTimeOffset.TryParse(Text(problem, "recordedAt"), CultureInfo.InvariantCulture, out _),
                    Is.True,
                    $"A problem with no moment on it cannot be told from one that happened last week. The row said: {problem}");
                Assert.That(Text(problem, "level"), Is.EqualTo("Error"),
                    $"A refusal from the tracker is an error, and the word is how the row is coloured. The row said: {problem}");
                Assert.That(Text(problem, "source"), Does.Contain("Update"),
                    $"The row has to say which part of Lighthouse is complaining, or an operator cannot tell a "
                    + $"refresh that broke from a setting that would not save. The row said: {problem}");
                Assert.That(Text(problem, "exceptionType"), Does.Contain(nameof(InvalidOperationException)),
                    $"What broke is the half of the row that says whether this is worth chasing. The row said: {problem}");
            }
        }

        /// <summary>
        /// The whole row, not only the name in it. An id identifies the refresh to whoever is reading the
        /// source, and identifies nothing at all to the person the section was built for: they cannot look
        /// it up, cannot tell whether it matters, and are back in the log file working out which team it
        /// was — which is the trip the section exists to save them.
        ///
        /// So the row has to say three things at once, and the two that are easy to lose are the ones that
        /// get a sentence each here. It still says what went wrong, because a row reduced to a bare team
        /// name reads as a heading and is worse than the id it replaced. And it no longer says the id,
        /// because a name printed beside the number it was meant to replace leaves the operator doing the
        /// looking-up anyway.
        /// </summary>
        private async Task ThenThatProblemSaysWhoseRefreshBroke(SeededWork work)
        {
            var problem = await TheProblemAbout(work);
            var message = TheSentence(problem);
            var number = work.Id.ToString(CultureInfo.InvariantCulture);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(message, Does.Contain(work.Name),
                    $"The row does not say that this is '{work.Name}', so the only way to find out which one "
                    + $"broke is still to go and read the log. The row said: {problem}");
                Assert.That(message, Does.Contain(WhatTheInstanceSaysWhenARefreshBreaks),
                    $"The row names '{work.Name}' and no longer says anything went wrong, which is worse than "
                    + $"the id it replaced: an operator scanning the section reads it as a heading and moves "
                    + $"on. The row said: {problem}");
                Assert.That(message, Does.Not.Contain(number),
                    $"The row still carries the id {number} somewhere. The name was meant to take its place, "
                    + $"not to be added beside it — nothing else in this section makes an operator do the "
                    + $"looking-up themselves. The row said: {problem}");
            }
        }

        /// <summary>
        /// Something whose refresh broke can be gone by the time anybody reads about it, and then there is
        /// no name left to say. What is left is the kind of work and the id it ran under, and a row saying
        /// those is worth more than a row naming nobody.
        /// </summary>
        private async Task ThenThatProblemStillSaysWhichRefreshBroke(SeededWork work)
        {
            var problem = await TheProblemAbout(work);
            var message = TheSentence(problem);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(message, Does.Contain(work.Kind.ToString()),
                    $"With nothing left to name, the row has to say what kind of work it was. It said: {problem}");
                Assert.That(message, Does.Contain(work.Id.ToString(CultureInfo.InvariantCulture)),
                    $"With nothing left to name, the row has to say which one it was. It said: {problem}");
            }
        }

        /// <summary>
        /// Most of what lands in this section was never about a refresh and carries nothing a name could be
        /// looked up by. Those rows say what they always said — or making one kind of row readable has
        /// quietly emptied every other kind.
        /// </summary>
        private async Task ThenThatProblemStillReadsAsTheInstanceWroteIt(string whatTheInstanceSaid, string whatItWasTold)
        {
            var problems = await TheRecentProblems();
            var message = problems
                .Select(TheSentence)
                .FirstOrDefault(text => text.Contains(whatTheInstanceSaid, StringComparison.OrdinalIgnoreCase));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(message, Is.Not.Null,
                    $"Nothing in the section says '{whatTheInstanceSaid}', so a complaint that was never about "
                    + $"a refresh has stopped being reported at all. It held: {Describe(problems)}");
                Assert.That(message ?? string.Empty, Does.Contain(whatItWasTold),
                    $"The row is there but no longer carries what the instance was told, so it tells an "
                    + $"operator less than it did. It held: {Describe(problems)}");
            }
        }

        /// <summary>
        /// Position, not contents. A short list read from the top has to open on what just happened; two
        /// failures a minute apart are otherwise indistinguishable in it.
        /// </summary>
        private async Task ThenTheProblemAboutIsReadBeforeTheProblemAbout(SeededWork newer, SeededWork older)
        {
            var problems = await TheRecentProblems();

            var newerAt = problems.FindIndex(problem => IsAbout(problem, newer));
            var olderAt = problems.FindIndex(problem => IsAbout(problem, older));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(newerAt, Is.GreaterThanOrEqualTo(0),
                    $"'{newer.Name}' broke and is not listed at all. Recent problems held: {Describe(problems)}");
                Assert.That(olderAt, Is.GreaterThan(newerAt),
                    $"'{older.Name}' broke first, so it is the older of the two and belongs below '{newer.Name}'. "
                    + $"Recent problems held: {Describe(problems)}");
            }
        }

        private async Task ThenTheRefreshOfThatTeamReallyRan(SeededWork team)
        {
            var problems = await TheRecentProblems();

            Assert.That(TheRecordedRefreshFor(RefreshType.Team, team.Id), Is.Not.Null,
                "Positive control: no refresh of this team was recorded at all, so a list that says nothing "
                + $"about it says nothing about anything. Recent problems held: {Describe(problems)}");
        }

        private async Task ThenRecentProblemsIsRefusedToWhoeverTheLogFileIsRefusedTo()
        {
            using var factory = RootFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var refuses = new Mock<IRbacAdministrationService>();
                    refuses
                        .Setup(s => s.CanSatisfyRequirementAsync(
                            It.IsAny<System.Security.Claims.ClaimsPrincipal>(),
                            It.IsAny<Lighthouse.Backend.Models.Authorization.RbacGuardRequirement>(),
                            It.IsAny<int?>(),
                            It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

                    services.RemoveAll<IRbacAdministrationService>();
                    services.AddScoped(_ => refuses.Object);
                });
            });

            using var client = factory.CreateClient();
            using var problems = await client.GetAsync(new Uri(ProblemsRoute, UriKind.Relative));
            using var logFile = await client.GetAsync(new Uri(LogFileRoute, UriKind.Relative));

            Assert.That(problems.StatusCode, Is.EqualTo(logFile.StatusCode),
                "Recent problems is the log file with the boring parts taken out — the same team names, the "
                + "same work-tracking URLs, the same connector errors. Handing it to somebody who may not "
                + "download the log would undo the guard rather than inherit it.");
        }

        // --- Reading recent problems ---

        private async Task<List<JsonElement>> TheRecentProblems()
        {
            using var client = Factory.CreateClient();
            using var response = await client.GetAsync(new Uri(ProblemsRoute, UriKind.Relative));

            var body = await response.Content.ReadAsStringAsync();

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"Recent problems is the driving port for this whole slice; {ProblemsRoute} answered "
                + $"{(int)response.StatusCode}: {body}");

            using var document = JsonDocument.Parse(body);

            Assert.That(document.RootElement.ValueKind, Is.EqualTo(JsonValueKind.Array),
                $"The section renders a list of rows, so the endpoint answers an array. It said: {body}");

            return [.. document.RootElement.EnumerateArray().Select(element => element.Clone())];
        }

        private async Task<JsonElement> TheProblemAbout(SeededWork work)
        {
            var problems = await TheRecentProblems();
            var matches = problems.FindAll(problem => IsAbout(problem, work));

            Assert.That(matches, Is.Not.Empty,
                $"'{work.Name}' broke and nothing in recent problems says so. It held: {Describe(problems)}");

            return matches[0];
        }

        /// <summary>
        /// The row as an operator reads it. A row with nothing to say is not a state the section has, so a
        /// row that arrives without its sentence is matched and asserted on as the empty one it is, rather
        /// than throwing somewhere further down and hiding which promise broke.
        /// </summary>
        private static string TheSentence(JsonElement problem) => Text(problem, "message") ?? string.Empty;

        /// <summary>
        /// Which refresh a row is about — picking the row out, not judging what it says, which is what the
        /// Then steps are for. A row about a refresh names what the refresh was of; one whose subject has
        /// gone has only the kind of work and the id left, so both forms find the same refresh.
        /// </summary>
        private static bool IsAbout(JsonElement problem, SeededWork work)
        {
            var message = TheSentence(problem);

            return message.Contains(work.Name, StringComparison.OrdinalIgnoreCase)
                || PointsAtTheRefreshWithoutNamingIt(message, work);
        }

        /// <summary>
        /// A row that says which refresh broke without saying what it was of: the wording the queue falls
        /// back to when the thing it was refreshing has gone and there is nothing left to name.
        /// </summary>
        private static bool PointsAtTheRefreshWithoutNamingIt(string message, SeededWork work)
        {
            var number = work.Id.ToString(CultureInfo.InvariantCulture);

            return message.Contains($"{work.Kind} {number}", StringComparison.OrdinalIgnoreCase);
        }
    }
}
