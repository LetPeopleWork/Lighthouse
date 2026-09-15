using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Authorization;
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
    /// <item><c>message</c> — the sentence as it was rendered</item>
    /// <item><c>exceptionType</c> — the type of what broke, absent when nothing threw</item>
    /// </list>
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
        /// What the tracker says when it turns a refresh away. One sentence, so a scenario can tell the
        /// failure it caused from anything else the instance happened to be complaining about.
        /// </summary>
        private const string TheTrackerComplaint = "Jira did not answer";

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

        private readonly record struct SeededTeam(int Id, string Name);

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

        private SeededTeam GivenATeamCalled(string name)
            => new(SeedTeam(SeedConnection(), name), name);

        /// <summary>
        /// One more team than the instance has room for problems, so that running all of them is guaranteed
        /// to have pushed the first one out — however many lines each failure happens to produce.
        /// </summary>
        private List<SeededTeam> GivenMoreTeamsThanTheInstanceHasRoomToRememberProblemsFor()
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

        private Task WhenTheScheduledRefreshOfThatTeamRuns(SeededTeam team)
            => TheTeamRefreshRuns(team.Id);

        private async Task WhenTheScheduledRefreshOfEachOfThoseTeamsRuns(IEnumerable<SeededTeam> teams)
        {
            foreach (var team in teams)
            {
                await WhenTheScheduledRefreshOfThatTeamRuns(team);
            }
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

        private async Task ThenRecentProblemsSaysWhatBrokeFor(SeededTeam team)
        {
            var problems = await TheRecentProblems();

            Assert.That(problems.Exists(problem => IsAbout(problem, team)), Is.True,
                $"Nothing in the popover would tell an operator that '{team.Name}' stopped refreshing, so the "
                + $"only way to find out is still to go and read the log. Recent problems held: {Describe(problems)}");
        }

        private async Task ThenRecentProblemsSaysNothingAbout(SeededTeam team)
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
        private async Task ThenThatProblemSaysWhenHowSeriousWhereFromAndWhatBroke(SeededTeam team)
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
        /// Position, not contents. A short list read from the top has to open on what just happened; two
        /// failures a minute apart are otherwise indistinguishable in it.
        /// </summary>
        private async Task ThenTheProblemAboutIsReadBeforeTheProblemAbout(SeededTeam newer, SeededTeam older)
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

        private async Task ThenTheRefreshOfThatTeamReallyRan(SeededTeam team)
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

        private async Task<JsonElement> TheProblemAbout(SeededTeam team)
        {
            var problems = await TheRecentProblems();
            var matches = problems.FindAll(problem => IsAbout(problem, team));

            Assert.That(matches, Is.Not.Empty,
                $"'{team.Name}' broke and nothing in recent problems says so. It held: {Describe(problems)}");

            return matches[0];
        }

        /// <summary>
        /// Which refresh a row is about. The only line at warning-or-above that identifies a refresh that
        /// failed is the queue's own, and it identifies it by update type and id — the line that carries the
        /// team's <em>name</em> is the refresh summary, which is written at Information and is therefore
        /// below this section's threshold by design. That asymmetry is exactly what this slice's hypothesis
        /// is about, so it is matched here rather than smoothed over.
        /// </summary>
        private static bool IsAbout(JsonElement problem, SeededTeam team)
        {
            var message = Text(problem, "message") ?? string.Empty;

            return message.Contains("Team", StringComparison.OrdinalIgnoreCase)
                && message.Contains(
                    $"ID {team.Id.ToString(CultureInfo.InvariantCulture)}",
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
