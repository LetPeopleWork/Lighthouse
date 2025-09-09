using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class DemoDataServiceTest
    {
        [SetUp]
        public void SetUp()
        {

        }

        [Test]
        [TestCase("When Will This Be Done?", 1, 1)]
        [TestCase("Crash Override", 1, 1)]
        [TestCase("Watermelon", 1, 1)]
        [TestCase("Product Launch", 2, 1)]
        public void LoadScenarios_ReturnsFreeScenarios(string scenarioName, int teams, int projects)
        {
            var subject = CreateSubject();

            var scenarios = subject.GetAllScenarios();

            using (Assert.EnterMultipleScope())
            {
                var scenario = scenarios.SingleOrDefault(x => x.Title == scenarioName);
                Assert.That(scenario, Is.Not.Null);
                Assert.That(scenario.IsPremium, Is.False);
                Assert.That(scenario.NumberOfTeams, Is.EqualTo(teams));
                Assert.That(scenario.NumberOfProjects, Is.EqualTo(projects));
            }
        }

        private DemoDataService CreateSubject()
        {
            return new DemoDataService();
        }
    }
}
