using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class FeatureHasNoChildWorkItemsYetTest
    {
        private static readonly JsonSerializerOptions AsTheWireWritesIt = new(JsonSerializerDefaults.Web);

        private static readonly Team Atlas = new() { Id = 1, Name = "Atlas" };

        [Test]
        public void FeatureWithoutChildWorkItemsAndNoDefaultSize_SaysItHasNoChildWorkItemsYet_OnBothPayloads()
        {
            var feature = new Feature { StateCategory = StateCategories.ToDo };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(OnTheFeaturePayload(feature), Is.True, "The Feature payload must carry hasNoChildWorkItemsYet = true for a childless Feature that is not sized by default, so the detail views can say why it shows 0.");
                Assert.That(OnTheListRow(feature), Is.True, "The Feature list row must carry hasNoChildWorkItemsYet = true for the same Feature, so the Portfolio list can say why it shows 0.");
            }
        }

        [Test]
        public void FeatureSizedByTheDefault_DoesNotClaimToHaveNoChildWorkItemsYet()
        {
            var feature = new Feature { StateCategory = StateCategories.Doing, IsUsingDefaultFeatureSize = true };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(OnTheFeaturePayload(feature), Is.False, "A Feature counted by the default size has its own warning; the note would claim a cause that is false. The payload must carry hasNoChildWorkItemsYet = false.");
                Assert.That(OnTheListRow(feature), Is.False, "A Feature counted by the default size has its own warning on the list row too. The row must carry hasNoChildWorkItemsYet = false.");
            }
        }

        [Test]
        public void FeatureWithChildWorkItems_DoesNotClaimToHaveNoChildWorkItemsYet()
        {
            var feature = new Feature([(Atlas, 3, 5)]) { StateCategory = StateCategories.Doing };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(OnTheFeaturePayload(feature), Is.False, "A Feature with real child Work Items must not say it has none.");
                Assert.That(OnTheListRow(feature), Is.False, "A list row with real child Work Items must not say it has none.");
            }
        }

        [Test]
        public void DoneFeatureWithoutChildWorkItems_DoesNotClaimToHaveNoChildWorkItemsYet()
        {
            var feature = new Feature { StateCategory = StateCategories.Done };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(OnTheFeaturePayload(feature), Is.False, "A finished Feature is not waiting for child Work Items.");
                Assert.That(OnTheListRow(feature), Is.False, "A finished Feature's list row is not waiting for child Work Items.");
            }
        }

        [Test]
        [TestCase(false, true)]
        [TestCase(true, false)]
        public async Task A_refreshed_Feature_without_child_Work_Items_says_so_only_while_default_sizing_is_off(bool defaultSizingIsOn, bool saysNoChildWorkItemsYet)
        {
            var portfolio = new Portfolio { Id = 100, Name = "Ocean", DefaultAmountOfWorkItemsPerFeature = 4, InfersDefaultFeatureSize = defaultSizingIsOn };
            portfolio.WorkItemTypes.Add("Feature");
            portfolio.WorkTrackingSystemConnection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Jira };
            portfolio.UpdateTime = TestToday.FrozenNow.UtcDateTime;
            var feature = new Feature { ReferenceId = "F-1" };
            var teams = new List<Team> { Atlas, new() { Id = 2, Name = "Pacific" } };

            var connector = new Mock<IWorkTrackingConnector>();
            connector.As<IFeatureSource>()
                .Setup(source => source.GetFeaturesForPortfolio(portfolio, It.IsAny<CancellationToken>()))
                .ReturnsAsync([feature]);
            var workItemRepository = new Mock<IWorkItemRepository>();
            workItemRepository
                .Setup(repository => repository.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns(new List<WorkItem>().AsQueryable());
            var teamRepository = new Mock<IRepository<Team>>();
            teamRepository.Setup(repository => repository.GetAll()).Returns(teams);
            var featureStateTransitions = new Mock<IFeatureStateTransitionRepository>();
            featureStateTransitions
                .Setup(repository => repository.GetAllByPredicate(It.IsAny<Expression<Func<FeatureStateTransition, bool>>>()))
                .Returns(new List<FeatureStateTransition>().AsQueryable());

            var service = new WorkItemServiceTestBuilder()
                .WithConnector(connector.Object)
                .WithWorkItemRepository(workItemRepository.Object)
                .WithTeamRepository(teamRepository.Object)
                .WithFeatureStateTransitionRepository(featureStateTransitions.Object)
                .Build();

            await service.UpdateFeaturesForPortfolio(portfolio);

            var refreshed = portfolio.Features.Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(OnTheFeaturePayload(refreshed), Is.EqualTo(saysNoChildWorkItemsYet), $"After a refresh with default sizing {(defaultSizingIsOn ? "on" : "off")}, the Feature payload must carry hasNoChildWorkItemsYet = {saysNoChildWorkItemsYet}.");
                Assert.That(OnTheListRow(refreshed), Is.EqualTo(saysNoChildWorkItemsYet), $"After a refresh with default sizing {(defaultSizingIsOn ? "on" : "off")}, the list row must carry hasNoChildWorkItemsYet = {saysNoChildWorkItemsYet}.");
            }
        }

        private static bool? OnTheFeaturePayload(Feature feature)
        {
            var dto = new FeatureDto(feature, TestToday.Clock, [], false, null);
            return ReadTheFlag(dto);
        }

        private static bool? OnTheListRow(Feature feature)
        {
            var dto = new FeatureListRowDto(feature, TestToday.Clock, [], false, new HashSet<int>());
            return ReadTheFlag(dto);
        }

        private static bool? ReadTheFlag(object dto)
        {
            var wire = JsonNode.Parse(JsonSerializer.Serialize(dto, dto.GetType(), AsTheWireWritesIt))!.AsObject();
            return wire["hasNoChildWorkItemsYet"]?.GetValue<bool>();
        }
    }
}
