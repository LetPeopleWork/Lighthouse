using Lighthouse.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    public class TestWebApplicationFactoryTest
    {
        [Test]
        public void AFreshDatabaseNeverReusesThePathOfAnEarlierOne()
        {
            using var factory = new TestWebApplicationFactory<Program>();

            var first = PathOfCurrentDatabase(factory);
            factory.UseFreshDatabase();
            var second = PathOfCurrentDatabase(factory);

            Assert.That(second, Is.Not.EqualTo(first));
        }

        private static string PathOfCurrentDatabase(TestWebApplicationFactory<Program> factory)
        {
            using var scope = factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.GetDbConnection().DataSource;
        }
    }
}
