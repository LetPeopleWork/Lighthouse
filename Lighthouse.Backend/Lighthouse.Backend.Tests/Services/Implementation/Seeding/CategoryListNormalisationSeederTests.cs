using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Seeding
{
    public class CategoryListNormalisationSeederTests
    {
        private const string Pending = "pending: category lists hold raw states";

        private static Team ACollapsedTeam(int id)
        {
            var team = new Team { Id = id, Name = "Team", ToDoStates = ["Backlog"], DoingStates = ["Analysis", "Review"], DoneStates = ["Done"] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });
            return team;
        }

        [Test]
        [Ignore(Pending)]
        public async Task Seed_SaveFails_LogsAWarningAndLetsTheStartContinue()
        {
            var teams = new Mock<IRepository<Team>>();
            teams.Setup(r => r.GetAll()).Returns([ACollapsedTeam(1)]);
            teams.Setup(r => r.Save()).ThrowsAsync(new InvalidOperationException("the database is read-only"));
            var portfolios = new Mock<IRepository<Portfolio>>();
            portfolios.Setup(r => r.GetAll()).Returns([]);
            var log = new RecordingLogger<CategoryListNormalisationSeeder>();

            var start = async () => await new CategoryListNormalisationSeeder(teams.Object, portfolios.Object, log).Seed();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(start, Throws.Nothing);
                Assert.That(log.Warnings, Has.Count.EqualTo(1));
            }
        }

        [Test]
        [Ignore(Pending)]
        public async Task Seed_NothingToRewrite_SavesNothing()
        {
            var raw = new Team { Id = 1, Name = "Team", DoingStates = ["Analysis", "Testing"] };
            var teams = new Mock<IRepository<Team>>();
            teams.Setup(r => r.GetAll()).Returns([raw]);
            var portfolios = new Mock<IRepository<Portfolio>>();
            portfolios.Setup(r => r.GetAll()).Returns([]);

            await new CategoryListNormalisationSeeder(teams.Object, portfolios.Object, new RecordingLogger<CategoryListNormalisationSeeder>()).Seed();

            teams.Verify(r => r.Save(), Times.Never);
            portfolios.Verify(r => r.Save(), Times.Never);
        }
    }
}
