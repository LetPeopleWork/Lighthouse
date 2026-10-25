using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    /// <summary>
    /// The Features list reads through a narrow reader, and that reader has to be the very repository the rest of
    /// the request uses: a second instance would be a second unit of work, reading Features the first one does
    /// not know it has changed.
    /// </summary>
    [TestFixture]
    [Category("epic-6400-fast-with-many-features")]
    [Category("slice-04")]
    public class FeatureListReaderRegistrationTest
    {
        private TestWebApplicationFactory<Program> factory = null!;

        [SetUp]
        public void SetUp()
        {
            factory = new TestWebApplicationFactory<Program>();
        }

        [TearDown]
        public void TearDown()
        {
            factory.Dispose();
        }

        [Test]
        public void Within_one_scope_the_reader_and_the_repository_are_one_instance()
        {
            using var scope = factory.Services.CreateScope();

            var reader = scope.ServiceProvider.GetRequiredService<IFeatureListReader>();
            var repository = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();

            Assert.That(reader, Is.SameAs(repository));
        }

        [Test]
        public void Across_scopes_the_reader_is_not_shared()
        {
            using var first = factory.Services.CreateScope();
            using var second = factory.Services.CreateScope();

            var inFirst = first.ServiceProvider.GetRequiredService<IFeatureListReader>();
            var inSecond = second.ServiceProvider.GetRequiredService<IFeatureListReader>();

            Assert.That(inFirst, Is.Not.SameAs(inSecond));
        }
    }
}
