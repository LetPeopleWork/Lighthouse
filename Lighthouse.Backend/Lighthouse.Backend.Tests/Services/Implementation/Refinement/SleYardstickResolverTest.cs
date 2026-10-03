using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.Tests.Services.Implementation.Refinement
{
    [TestFixture]
    public class SleYardstickResolverTest
    {
        [TestCase(85, 7)]
        [TestCase(1, 1)]
        [TestCase(50, 30)]
        [TestCase(99, 365)]
        public void ATeamWithAnSleAnswersItsOwnNumbers(int probability, int days)
        {
            var team = new Team { ServiceLevelExpectationProbability = probability, ServiceLevelExpectationRange = days };

            var yardstick = SleYardstickResolver.For(team);

            Assert.That(yardstick, Is.EqualTo(new Yardstick(YardstickSource.Sle, days, probability)));
        }

        [TestCase(0, 0)]
        [TestCase(85, 0)]
        [TestCase(0, 7)]
        public void ATeamWithoutAWholeSleIsNotAnsweredWithAnSle(int probability, int days)
        {
            var team = new Team { ServiceLevelExpectationProbability = probability, ServiceLevelExpectationRange = days };

            var yardstick = SleYardstickResolver.For(team);

            Assert.That(yardstick.Source, Is.Not.EqualTo(YardstickSource.Sle));
        }
    }
}
