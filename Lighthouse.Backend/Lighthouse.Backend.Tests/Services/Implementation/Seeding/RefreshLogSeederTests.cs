using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Seeding
{
    public class RefreshLogSeederTests
    {
        private Mock<IRefreshLogService> refreshLogServiceMock;

        [SetUp]
        public void Setup()
        {
            refreshLogServiceMock = new Mock<IRefreshLogService>();
        }

        [Test]
        public async Task Seed_CallsRemoveOrphanedRefreshLogs()
        {
            var subject = CreateSubject();

            await subject.Seed();

            refreshLogServiceMock.Verify(s => s.RemoveOrphanedRefreshLogs(), Times.Once);
        }

        [Test]
        public async Task Seed_CanBeCalledMultipleTimes_WithoutErrors()
        {
            var subject = CreateSubject();

            await subject.Seed();
            await subject.Seed();
            await subject.Seed();

            refreshLogServiceMock.Verify(s => s.RemoveOrphanedRefreshLogs(), Times.Exactly(3));
        }

        private RefreshLogSeeder CreateSubject()
        {
            return new RefreshLogSeeder(refreshLogServiceMock.Object, Mock.Of<ILogger<RefreshLogSeeder>>());
        }
    }
}
