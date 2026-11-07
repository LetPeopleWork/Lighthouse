using System.Reflection;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Test selection leaves a class out by the category it inherits from <see cref="IntegrationFixture"/>, so an NUnit
    /// upgrade that stopped the attribute reaching a derived type would quietly move every such class between tiers.
    /// </summary>
    [TestFixture]
    public class IntegrationFixtureCategoryTest
    {
        [Test]
        public void ATypeOnTheBaseCarriesTheIntegrationCategory()
        {
            Assert.That(CategoriesOf(typeof(PlantedOnTheBase)), Has.Exactly(1).EqualTo("Integration"));
        }

        [Test]
        public void ATypeOffTheBaseCarriesNoCategory()
        {
            Assert.That(CategoriesOf(typeof(PlantedOffTheBase)), Is.Empty);
        }

        private static IEnumerable<string> CategoriesOf(Type type)
        {
            return type.GetCustomAttributes<CategoryAttribute>(inherit: true).Select(category => category.Name);
        }

        private abstract class PlantedOnTheBase : IntegrationFixture;

        private abstract class PlantedOffTheBase
        {
            public override string ToString() => nameof(PlantedOffTheBase);
        }
    }
}
