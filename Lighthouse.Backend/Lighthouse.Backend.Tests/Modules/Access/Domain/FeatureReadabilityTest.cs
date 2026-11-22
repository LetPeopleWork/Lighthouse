using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.Access.Domain;

namespace Lighthouse.Backend.Tests.Modules.Access.Domain
{
    [TestFixture]
    public class FeatureReadabilityTest
    {
        [Test]
        public void A_Feature_in_no_Portfolio_is_readable_even_when_the_reader_may_read_no_Portfolio()
        {
            var feature = new Feature { Id = 1 };

            Assert.That(FeatureReadability.IsReadableBy(feature, []), Is.True);
        }

        [Test]
        public void A_Feature_in_Portfolios_is_readable_when_one_of_them_is_readable()
        {
            var feature = new Feature { Id = 1 };
            feature.Portfolios.Add(new Portfolio { Id = 10, Name = "Hidden" });
            feature.Portfolios.Add(new Portfolio { Id = 11, Name = "Visible" });

            Assert.That(FeatureReadability.IsReadableBy(feature, [11]), Is.True);
        }

        [Test]
        public void A_Feature_in_Portfolios_is_not_readable_when_none_of_them_is_readable()
        {
            var feature = new Feature { Id = 1 };
            feature.Portfolios.Add(new Portfolio { Id = 10, Name = "Hidden" });
            feature.Portfolios.Add(new Portfolio { Id = 11, Name = "Hidden too" });

            Assert.That(FeatureReadability.IsReadableBy(feature, [12]), Is.False);
        }
    }
}
