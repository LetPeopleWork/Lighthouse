using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Update;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.UpdateQueueLanes
{
    /// <summary>
    /// A team refresh and a portfolio refresh in flight at the same moment against a real Azure DevOps
    /// project, read back from what the instance recorded about them.
    ///
    /// Doubles can show that the queues exist. They cannot show that two refreshes survive each other
    /// through a real connector, a real database and a real write-back round, which is what was broken:
    /// one slow portfolio refresh held every team refresh behind it for hours. So this runs against a
    /// real connection, and it throws rather than skips when the credential is missing - a test that
    /// passes by never running is worse than no test, because it reads as covered.
    ///
    /// Azure DevOps rather than Linear: the Linear key is shared with CI, and repeated local runs
    /// rate-limit the next CI build.
    ///
    /// Its categories keep it out of the ordinary suite, which excludes both. Run it deliberately:
    ///
    /// <code>dotnet test --filter "TestCategory=UpdateQueueLanesLive"</code>
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("UpdateQueueLanesLive")]
    public class TwoLanesAgainstARealTrackerTest : UpdateQueueLanesAcceptanceTest
    {
        private const string TokenVariable = "AzureDevOpsLighthouseIntegrationTestToken";

        private const string OrganisationUrl = "https://dev.azure.com/huserben";

        private const string TheTestProject = "[System.TeamProject] = 'CMFTTestTeamProject'";

        /// <summary>
        /// Real work takes as long as it takes, and what is being observed is that neither refresh had to
        /// wait for the other to give its turn back.
        /// </summary>
        private static readonly TimeSpan PatienceForRealWork = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The harness fakes the connector for every other fixture in this folder. This one exists
        /// because a fake cannot answer the question it asks, so the production factory goes back in.
        /// </summary>
        protected override void ConfigureAdditionalServices(IServiceCollection services)
        {
            services.RemoveAll<IWorkTrackingConnectorFactory>();
            services.AddScoped<IWorkTrackingConnectorFactory, WorkTrackingConnectorFactory>();
        }

        [Test]
        public async Task A_team_refresh_and_a_portfolio_refresh_against_a_real_tracker_run_at_the_same_time()
        {
            var connectionId = GivenAConnectionToTheRealProject();
            var teamId = GivenATeamInThatProject(connectionId);
            var portfolioId = GivenAPortfolioInThatProject(connectionId);

            await WhenBothAreRefreshedAtOnce(teamId, portfolioId);

            var teamRun = TheRecordedRefreshFor(RefreshType.Team, teamId);
            var portfolioRun = TheRecordedRefreshFor(RefreshType.Portfolio, portfolioId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(teamRun, Is.Not.Null, "The team refresh recorded nothing, so there is nothing to compare.");
                Assert.That(portfolioRun, Is.Not.Null, "The portfolio refresh recorded nothing, so there is nothing to compare.");
            }

            var team = WhenItRan(teamRun!);
            var portfolio = WhenItRan(portfolioRun!);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.Started, Is.LessThan(portfolio.Ended), Overlap(team, portfolio));
                Assert.That(portfolio.Started, Is.LessThan(team.Ended), Overlap(team, portfolio));
            }
        }

        // --- Given ---

        private int GivenAConnectionToTheRealProject()
        {
            var token = Environment.GetEnvironmentVariable(TokenVariable)
                ?? throw new NotSupportedException($"Can run test only if Environment Variable '{TokenVariable}' is set!");

            using var scope = Factory.Services.CreateScope();
            var crypto = scope.ServiceProvider.GetRequiredService<ICryptoService>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Azure DevOps {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                AuthenticationMethodKey = AuthenticationMethodKeys.AzureDevOpsPat,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = OrganisationUrl, IsSecret = false },

                // Stored the way the instance stores it. A plaintext token here would come back through the
                // production secret reader as an unreadable secret rather than as a working one.
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = crypto.Encrypt(token), IsSecret = true },

                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "100", IsSecret = false },
            ]);

            var repository = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>();
            repository.Add(connection);
            repository.Save().GetAwaiter().GetResult();

            return connection.Id;
        }

        private int GivenATeamInThatProject(int connectionId)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = sp.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionId)!,
                DataRetrievalValue = TheTestProject,
                DoneItemsCutoffDays = 365,
                WorkItemTypes = ["User Story", "Bug"],
                ToDoStates = ["New"],
                DoingStates = ["Active", "Resolved"],
                DoneStates = ["Closed"],
                ThroughputHistory = 30,
                UpdateTime = DateTime.UtcNow,
            };

            var repository = sp.GetRequiredService<IRepository<Team>>();
            repository.Add(team);
            repository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int GivenAPortfolioInThatProject(int connectionId)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = sp.GetRequiredService<IRepository<WorkTrackingSystemConnection>>().GetById(connectionId)!,
                DataRetrievalValue = TheTestProject,
                DoneItemsCutoffDays = 365,
                WorkItemTypes = ["Feature"],
                ToDoStates = ["New"],
                DoingStates = ["Active", "Resolved"],
                DoneStates = ["Closed"],
                UpdateTime = DateTime.UtcNow,
            };

            var repository = sp.GetRequiredService<IPortfolioRepository>();
            repository.Add(portfolio);
            repository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }

        // --- When ---

        /// <summary>
        /// Both asked for before either is waited on. Triggering one and waiting for it would show only
        /// that two refreshes can happen, which was never in doubt.
        /// </summary>
        private async Task WhenBothAreRefreshedAtOnce(int teamId, int portfolioId)
        {
            Factory.Services.GetRequiredService<IPortfolioUpdater>().TriggerUpdate(portfolioId);
            Factory.Services.GetRequiredService<ITeamUpdater>().TriggerUpdate(teamId);

            var settled = await TheUpdateQueueSettling.SettlesWithin(Factory.Services, PatienceForRealWork);

            Assert.That(settled, Is.True,
                "Neither refresh finished within five minutes against the real project, so there is nothing "
                + "to read. Check the connection before reading this as a queueing problem.");
        }

        // --- Then ---

        private readonly record struct RunInterval(DateTime Started, DateTime Ended);

        /// <summary>
        /// The refresh log records when a run ended and how long it took, which is when it started said
        /// the other way round.
        /// </summary>
        private static RunInterval WhenItRan(RefreshLog run)
        {
            return new RunInterval(run.ExecutedAt.AddMilliseconds(-run.DurationMs), run.ExecutedAt);
        }

        private static string Overlap(RunInterval team, RunInterval portfolio)
        {
            return "The two refreshes have to have been in flight at the same moment. Intervals that only "
                + "meet end to end are one refresh waiting for the other, which is the behaviour being "
                + $"replaced. Team ran {team.Started:O} to {team.Ended:O}; portfolio ran "
                + $"{portfolio.Started:O} to {portfolio.Ended:O}.";
        }
    }
}
