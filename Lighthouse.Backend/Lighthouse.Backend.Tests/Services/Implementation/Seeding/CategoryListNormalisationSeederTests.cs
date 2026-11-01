using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Seeding
{
    public class CategoryListNormalisationSeederTests
    {
        private static readonly string[] NamesTheLogMayCarry = ["OwnerType", "OwnerId", "Scanned", "Rewritten", "LegacyExpanded", "Skipped", "Rule"];

        private static readonly string[] WordsTheLogMayCarry = ["Team", "Portfolio", "Shadowing", "Chained", "EmptyGroup", "SaveFailed"];

        private static Team ACollapsedTeam(int id)
        {
            var team = new Team { Id = id, Name = "Payments Squad", ToDoStates = ["Backlog"], DoingStates = ["Analysis", "Review"], DoneStates = ["Done"] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });
            return team;
        }

        private static Mock<IRepository<Portfolio>> NoPortfolios()
        {
            var portfolios = new Mock<IRepository<Portfolio>>();
            portfolios.Setup(r => r.GetAll()).Returns([]);
            return portfolios;
        }

        [Test]
        public async Task Seed_SaveFails_LogsAWarningAndLetsTheStartContinue()
        {
            var teams = new Mock<IRepository<Team>>();
            teams.Setup(r => r.GetAll()).Returns([ACollapsedTeam(1)]);
            teams.Setup(r => r.Save()).ThrowsAsync(new InvalidOperationException("the database is read-only"));
            var log = new RecordingLogger<CategoryListNormalisationSeeder>();

            await Assert.ThatAsync(async () => await new CategoryListNormalisationSeeder(teams.Object, NoPortfolios().Object, log).Seed(), Throws.Nothing);

            Assert.That(log.Warnings, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task Seed_SaveFailsForTheFirstOwner_StillRewritesTheSecond()
        {
            var first = ACollapsedTeam(1);
            var second = ACollapsedTeam(2);
            var teams = new Mock<IRepository<Team>>();
            teams.Setup(r => r.GetAll()).Returns([first, second]);
            teams.SetupSequence(r => r.Save())
                .ThrowsAsync(new InvalidOperationException("the database is read-only"))
                .Returns(Task.CompletedTask);

            await new CategoryListNormalisationSeeder(teams.Object, NoPortfolios().Object, new RecordingLogger<CategoryListNormalisationSeeder>()).Seed();

            using (Assert.EnterMultipleScope())
            {
                teams.Verify(r => r.Save(), Times.Exactly(2));
                Assert.That(second.DoingStates, Is.EqualTo(new List<string> { "Analysis", "Code Review", "Testing" }));
            }
        }

        // A mapping with no states is not a group: the owner is left as it is, one Warning names the rule, nothing throws.
        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task Seed_MappingWithNoStates_LogsOneWarningWithAFixedRuleAndLeavesTheOwnerAlone(bool statesAreNull)
        {
            var team = new Team { Id = 3, Name = "Broken Squad", ToDoStates = ["Backlog"], DoingStates = ["Analysis", "Review"], DoneStates = ["Done"] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = statesAreNull ? null! : [] });
            var teams = new Mock<IRepository<Team>>();
            teams.Setup(r => r.GetAll()).Returns([team]);
            var log = new RecordingLogger<CategoryListNormalisationSeeder>();

            await Assert.ThatAsync(async () => await new CategoryListNormalisationSeeder(teams.Object, NoPortfolios().Object, log).Seed(), Throws.Nothing);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(team.DoingStates, Is.EqualTo(new List<string> { "Analysis", "Review" }));
                Assert.That(log.Warnings, Has.Count.EqualTo(1));
                Assert.That(log.Everything.Single(entry => entry.Level == LogLevel.Warning).Properties["Rule"], Is.EqualTo("EmptyGroup"));
                teams.Verify(r => r.Save(), Times.Never);
            }
        }

        [Test]
        public async Task Seed_NothingToRewrite_SavesNothing()
        {
            var raw = new Team { Id = 1, Name = "Team", DoingStates = ["Analysis", "Testing"] };
            var teams = new Mock<IRepository<Team>>();
            teams.Setup(r => r.GetAll()).Returns([raw]);
            var portfolios = NoPortfolios();

            await new CategoryListNormalisationSeeder(teams.Object, portfolios.Object, new RecordingLogger<CategoryListNormalisationSeeder>()).Seed();

            using (Assert.EnterMultipleScope())
            {
                teams.Verify(r => r.Save(), Times.Never);
                portfolios.Verify(r => r.Save(), Times.Never);
            }
        }

        [Test]
        public async Task Seed_EveryLineItWrites_CarriesOnlyTheOwnerTypeIdCountsAndAFixedRule()
        {
            var shadowing = new Team { Id = 2, Name = "Shadow Squad", ToDoStates = ["Backlog"], DoingStates = ["In Arbeit", "In Progress"], DoneStates = ["Done"] };
            shadowing.StateMappings.Add(new StateMapping { Name = "In Arbeit", States = ["In Progress"] });
            var unreadable = new Team { Id = 3, Name = "Broken Squad", DoingStates = ["Review"] };
            unreadable.StateMappings.Add(new StateMapping { Name = "Review", States = null! });
            var teams = new Mock<IRepository<Team>>();
            teams.Setup(r => r.GetAll()).Returns([ACollapsedTeam(1), shadowing, unreadable]);
            teams.Setup(r => r.Save()).Returns(Task.CompletedTask);
            var log = new RecordingLogger<CategoryListNormalisationSeeder>();

            await new CategoryListNormalisationSeeder(teams.Object, NoPortfolios().Object, log).Seed();

            var keys = log.Everything.SelectMany(entry => entry.Properties.Keys).Distinct().ToList();
            var words = log.Everything.SelectMany(entry => entry.Properties.Values).OfType<string>().Distinct().ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(log.Everything, Is.Not.Empty);
                Assert.That(keys, Is.SubsetOf(NamesTheLogMayCarry));
                Assert.That(words, Is.SubsetOf(WordsTheLogMayCarry));
                Assert.That(log.Everything.Select(entry => entry.Failure), Has.All.Null, "A failure's own text could carry a name.");
                Assert.That(string.Join('\n', log.Everything.Select(entry => entry.Message)), Does.Not.Contain("Squad").And.Not.Contain("Review").And.Not.Contain("Arbeit"));
            }
        }
    }
}
