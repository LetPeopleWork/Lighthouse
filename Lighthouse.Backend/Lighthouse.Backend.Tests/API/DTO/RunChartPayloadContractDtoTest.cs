using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class RunChartPayloadContractDtoTest
    {
        [Test]
        public void CreatePortfolioRunChartWorkItemDto_ItemIsNotAFeature_ReportsNoSizeAndNoOwningTeam()
        {
            var workItem = new WorkItem
            {
                Id = 42,
                Name = "An item that is not a Feature",
                State = "Doing",
                StateCategory = StateCategories.Doing,
            };

            var subject = new PortfolioRunChartWorkItemDto(workItem, new FakeLighthouseClock(DateTimeOffset.UtcNow), false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.OwningTeam, Is.Empty);
                Assert.That(subject.Size, Is.Zero);
            }
        }
    }
}
