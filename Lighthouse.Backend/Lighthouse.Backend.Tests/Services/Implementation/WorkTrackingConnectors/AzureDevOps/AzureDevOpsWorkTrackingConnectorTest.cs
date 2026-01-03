using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    public class AzureDevOpsWorkTrackingConnectorTest
    {
        [Test]
        public async Task GetWorkItemsForTeam_GetsAllItemsThatMatchQuery()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [System.Title] CONTAINS 'Unparented' AND [System.State] <> 'Closed'");

            team.ResetUpdateTime();

            var matchingItems = await subject.GetWorkItemsForTeam(team);

            Assert.That(matchingItems.Count(), Is.EqualTo(2));
        }

        [Test]
        [TestCase(0, 2)]
        [TestCase(10, 0)]
        [TestCase(5500, 2)]
        public async Task GetWorkItemsForTeam_CutOffDateSet_SkipsItemsIfBeyondCutOff(int cutOffDays, int expectedItems)
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\PreviousReleaseAreaPath'");

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("User Story");
            
            team.DoneItemsCutoffDays =  cutOffDays;

            var actualItems = await subject.GetWorkItemsForTeam(team);

            Assert.That(actualItems.ToList(), Has.Count.EqualTo(expectedItems));
        }

        [Test]
        [TestCase("377", "", null)]
        [TestCase("365", "371", null)]
        [TestCase("375", "279", "Custom.RemoteFeatureID")]
        public async Task GetWorkItemsForTeam_SetsParentRelationCorrect(string workItemId, string expectedParentReference, string? parentFieldReference)
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'");
            if (parentFieldReference != null)
            {
                team.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
                {
                    Id = 1,
                    DisplayName = "Parent Field",
                    Reference = parentFieldReference
                });

                team.ParentOverrideAdditionalFieldDefinitionId = 1;
            }

            var workItems = await subject.GetWorkItemsForTeam(team);
            var workItem = workItems.Single(wi => wi.ReferenceId == workItemId);

            Assert.That(workItem.ParentReferenceId, Is.EqualTo(expectedParentReference));
        }

        [Test]
        [TestCase("377", "", null)]
        [TestCase("365", "371", null)]
        [TestCase("375", "279", "Custom.RemoteFeatureID")]
        public async Task GetFeaturesForProject_SetsParentRelationCorrect(string workItemId, string expectedParentReference, string? parentFieldReference)
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'");
            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("User Story");

            if (parentFieldReference != null)
            {
                portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
                {
                    Id = 1,
                    DisplayName = "Parent Field",
                    Reference = parentFieldReference
                });

                portfolio.ParentOverrideAdditionalFieldDefinitionId = 1;
            }

            var workItems = await subject.GetFeaturesForProject(portfolio);
            var workItem = workItems.Single(wi => wi.ReferenceId == workItemId);

            Assert.That(workItem.ParentReferenceId, Is.EqualTo(expectedParentReference));
        }

        [Test]
        public async Task GetWorkItemsForTeam_OrCaseInWorkItemQuery_HandlesCorrectly()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' OR [{AzureDevOpsFieldNames.TeamProject}] = 'DummyProject'");
            team.DoneStates.Clear();
            team.DoneStates.Add("Closed");
            team.UseFixedDatesForThroughput = true;
            team.ThroughputHistoryStartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            team.ThroughputHistoryEndDate = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            
            var startDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            team.DoneItemsCutoffDays =  (DateTime.UtcNow - startDate).Days;
            
            var result = await subject.GetWorkItemsForTeam(team);

            Assert.That(result.Count(), Is.EqualTo(22));
        }

        [Test]
        public async Task SetStartedAndClosedDate_RegularStateTransition_SetsStartedAndClosedDateCorrect()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '395'");

            var startDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            team.DoneItemsCutoffDays =  (DateTime.UtcNow - startDate).Days;
            
            var result = await subject.GetWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count(), Is.EqualTo(1));
                var workItem = result.Single();

                Assert.That(workItem.StartedDate.HasValue, Is.True);
                Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 5, 4, 707, DateTimeKind.Utc)));

                Assert.That(workItem.ClosedDate.HasValue, Is.True);
                Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 6, 34, 677, DateTimeKind.Utc)));
            }
        }

        [Test]
        public async Task SetStartedDate_IgnoresStateTransitionWithinStateCategory()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '396'");

            var result = await subject.GetWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count(), Is.EqualTo(1));
                var workItem = result.Single();

                Assert.That(workItem.StartedDate.HasValue, Is.True);
                Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 8, 10, 647, DateTimeKind.Utc)));

                Assert.That(workItem.ClosedDate.HasValue, Is.False);
            }
        }

        [Test]
        public async Task SetStartedAndClosedDate_IgnoresStateCasing()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '395'");
            team.DoneStates.Clear();
            team.DoneStates.Add("CLOSED");
            
            var startDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            team.DoneItemsCutoffDays =  (DateTime.UtcNow - startDate).Days;
            
            var result = await subject.GetWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count(), Is.EqualTo(1));
                var workItem = result.Single();

                Assert.That(workItem.StartedDate.HasValue, Is.True);
                Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 5, 4, 707, DateTimeKind.Utc)));

                Assert.That(workItem.ClosedDate.HasValue, Is.True);
                Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 6, 34, 677, DateTimeKind.Utc)));
            }
        }

        [Test]
        public async Task SetStartedDate_IgnoresStateCasing()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '396'");
            team.DoingStates.Clear();
            team.DoingStates.Add("ACTIVE");
            team.DoingStates.Add("ReSoLvED");

            var result = await subject.GetWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count(), Is.EqualTo(1));
                var workItem = result.Single();

                Assert.That(workItem.StartedDate.HasValue, Is.True);
                Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 8, 10, 647, DateTimeKind.Utc)));

                Assert.That(workItem.ClosedDate.HasValue, Is.False);
            }
        }

        [Test]
        public async Task SetClosedDate_IgnoresTransitionWithinStateCategory()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '395'");
            team.DoingStates.Remove("Resolved");
            team.DoneStates.Add("Resolved");
            
            var startDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            team.DoneItemsCutoffDays =  (DateTime.UtcNow - startDate).Days;

            var result = await subject.GetWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count(), Is.EqualTo(1));
                var workItem = result.Single();

                Assert.That(workItem.StartedDate.HasValue, Is.True);
                Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 5, 4, 707, DateTimeKind.Utc)));

                Assert.That(workItem.ClosedDate.HasValue, Is.True);
                Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 5, 57, 453, DateTimeKind.Utc)));
            }
        }

        [Test]
        public async Task SetStartedDate_ItemMovedFromDoingToToDoBackToDoing_UsesSecondTransitionToDoing()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '396'");

            var result = await subject.GetWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count(), Is.EqualTo(1));
                var workItem = result.Single();

                Assert.That(workItem.StartedDate.HasValue, Is.True);
                Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 8, 10, 647, DateTimeKind.Utc)));

                Assert.That(workItem.ClosedDate.HasValue, Is.False);
            }
        }

        [Test]
        public async Task SetStartedAndClosedDate_ItemMovedFromDoneToDoingBackToDone_UsesSecondTransitionToSetDates()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '397'");
            
            var startDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            team.DoneItemsCutoffDays =  (DateTime.UtcNow - startDate).Days;

            var result = await subject.GetWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count(), Is.EqualTo(1));
                var workItem = result.Single();

                Assert.That(workItem.StartedDate.HasValue, Is.True);
                Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 5, 54, 907, DateTimeKind.Utc)));

                Assert.That(workItem.ClosedDate.HasValue, Is.True);
                Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 22, 12, 623, DateTimeKind.Utc)));
            }
        }

        [Test]
        public async Task SetStartedAndClosedDate_ItemMovedFromUnknownStateToDoingToDone_SetsDatesCorrectly()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '398'");

            var startDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            team.DoneItemsCutoffDays =  (DateTime.UtcNow - startDate).Days;

            var result = await subject.GetWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count(), Is.EqualTo(1));
                var workItem = result.Single();

                Assert.That(workItem.StartedDate.HasValue, Is.True);
                Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 7, 54, 460, DateTimeKind.Utc)));

                Assert.That(workItem.ClosedDate.HasValue, Is.True);
                Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 8, 58, 753, DateTimeKind.Utc)));
            }
        }

        [Test]
        public async Task SetStartedAndClosedDate_ItemMovedFromUnknownStateToDone_SetsDatesCorrectly()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '399'");

            var startDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            team.DoneItemsCutoffDays =  (DateTime.UtcNow - startDate).Days;

            var result = await subject.GetWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Count(), Is.EqualTo(1));
                var workItem = result.Single();

                Assert.That(workItem.StartedDate.HasValue, Is.True);
                Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 25, 25, 217, DateTimeKind.Utc)));

                Assert.That(workItem.ClosedDate.HasValue, Is.True);
                Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2025, 4, 24, 8, 25, 25, 217, DateTimeKind.Utc)));
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_GetsCorrectTags()
        {
            var workItemId = "373";

            var subject = CreateSubject();
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'");

            var workItems = await subject.GetWorkItemsForTeam(team);
            var workItem = workItems.Single(wi => wi.ReferenceId == workItemId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.Tags, Has.Count.EqualTo(2));
                Assert.That(workItem.Tags, Contains.Item("Release1"));
                Assert.That(workItem.Tags, Contains.Item("TagTest"));
            }
        }

        [Test]
        public async Task GetFeaturesForProject_TagDoesNotExist_ReturnsNoItems()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'NotExistingTag'");

            var itemsByTag = await subject.GetFeaturesForProject(portfolio);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_TagExists_ReturnsCorrectNumberOfItems()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'Release1'");

            var itemsByTag = await subject.GetFeaturesForProject(portfolio);

            Assert.That(itemsByTag, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetFeaturesForProject_TagExists_WorkItemTypeDoesNotMatch_ReturnsNoItems()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'Release1'");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Bug");

            var itemsByTag = await subject.GetFeaturesForProject(portfolio);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_AreaPathDoesNotExist_ReturnsNoItems()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\NotExistingAreaPath'");

            var itemsByTag = await subject.GetFeaturesForProject(portfolio);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_AreaPathExists_ReturnsCorrectNumberOfItems()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths'");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("User Story");

            var itemsByTag = await subject.GetFeaturesForProject(portfolio);

            Assert.That(itemsByTag, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetFeaturesForProject_AreaPathExists_WorkItemTypeDoesNotMatch_ReturnsNoItems()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths'");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Bug");

            var itemsByTag = await subject.GetFeaturesForProject(portfolio);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProjectByTag_ItemIsOpen_ReturnsItem()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'Release1'");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("User Story");

            var actualItems = await subject.GetFeaturesForProject(portfolio);

            Assert.That(actualItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetFeaturesForProjectByTag_ItemIsClosed_ReturnsItem()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'PreviousRelease'");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("User Story");
            
            var startDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            portfolio.DoneItemsCutoffDays =  (DateTime.UtcNow - startDate).Days;

            var actualItems = await subject.GetFeaturesForProject(portfolio);

            Assert.That(actualItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetFeaturesForProjectByAreaPath_ItemIsOpen_ReturnsItem()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\SomeReleeaseThatIsUsingAreaPaths'");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("User Story");

            var actualItems = await subject.GetFeaturesForProject(portfolio);

            Assert.That(actualItems, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetFeaturesForProjectByAreaPath_ItemIsClosed_ReturnsItem()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\PreviousReleaseAreaPath'");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("User Story");
            
            var startDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            portfolio.DoneItemsCutoffDays =  (DateTime.UtcNow - startDate).Days;

            var actualItems = await subject.GetFeaturesForProject(portfolio);

            Assert.That(actualItems, Has.Count.EqualTo(2));
        }

        [Test]
        [TestCase(0, 2)]
        [TestCase(10, 0)]
        [TestCase(5500, 2)]
        public async Task GetFeaturesForProject_CutOffDateSet_SkipsItemsIfBeyondCutOff(int cutOffDays, int expectedItems)
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.AreaPath}] UNDER 'CMFTTestTeamProject\\PreviousReleaseAreaPath'");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("User Story");
            
            portfolio.DoneItemsCutoffDays =  cutOffDays;

            var actualItems = await subject.GetFeaturesForProject(portfolio);

            Assert.That(actualItems, Has.Count.EqualTo(expectedItems));
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsCorrectFeatureProperties()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '366'");
            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("User Story");

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.Single(f => f.ReferenceId == "366");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.Name, Is.EqualTo("Test Test Test"));
                Assert.That(feature.Order, Is.EqualTo("1999821120"));
                Assert.That(feature.State, Is.EqualTo("Resolved"));
                Assert.That(feature.StateCategory, Is.EqualTo(StateCategories.Doing));
                Assert.That(feature.Url, Is.EqualTo("https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/366"));
            }
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsCorrectStartedDateBasedOnStateMapping()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '370'");

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.Single(f => f.ReferenceId == "370");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.StartedDate.HasValue, Is.True);
                Assert.That(feature.StartedDate?.Date, Is.EqualTo(new DateTime(2024, 2, 16, 0, 0, 0, DateTimeKind.Utc)));

                Assert.That(feature.ClosedDate.HasValue, Is.False);
            }
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsCorrectClosedDateBasedOnStateMapping()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '370'");
            portfolio.DoingStates.Remove("Resolved");
            portfolio.DoneStates.Add("Resolved");

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.Single(f => f.ReferenceId == "370");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.ClosedDate.HasValue, Is.True);
                Assert.That(feature.ClosedDate?.Date, Is.EqualTo(new DateTime(2024, 2, 16, 0, 0, 0, DateTimeKind.Utc)));
            }
        }

        [Test]
        public async Task GetFeaturesForProject_ClosedDateButNoStartedDate_SetsStartedDateToClosedDate()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '370'");
            portfolio.DoingStates.Remove("Resolved");
            portfolio.DoneStates.Add("Resolved");

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.Single(f => f.ReferenceId == "370");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.StartedDate, Is.EqualTo(feature.ClosedDate));
            }
        }

        [Test]
        [TestCase("", "370", 0)]
        [TestCase("Microsoft.VSTS.Scheduling.Size", "370", 12)]
        [TestCase("Microsoft.VSTS.Scheduling.Size", "380", 0)]
        [TestCase("Microsoft.VSTS.Scheduling.Size", "371", 2)]
        public async Task GetFeaturesForProject_ReadsEstimatedSizeCorrect(string fieldName, string workItemId, int expectedEstimatedSize)
        {
            var subject = CreateSubject();

            var additionalFieldDefs = new List<AdditionalFieldDefinition>();
            int? sizeFieldId = null;
            if (!string.IsNullOrEmpty(fieldName))
            {
                var sizeField = new AdditionalFieldDefinition { Id = 1, DisplayName = "Size", Reference = fieldName };
                additionalFieldDefs.Add(sizeField);
                sizeFieldId = sizeField.Id;
            }

            var portfolio = CreatePortfolioWithAdditionalFields($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'", additionalFieldDefs);
            portfolio.SizeEstimateAdditionalFieldDefinitionId = sizeFieldId;

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.Single(f => f.ReferenceId == workItemId);

            Assert.That(feature.EstimatedSize, Is.EqualTo(expectedEstimatedSize));
        }

        [Test]
        [TestCase("", "370", "")]
        [TestCase("Microsoft.VSTS.Scheduling.Size", "370", "12")]
        [TestCase("System.AreaPath", "370", "CMFTTestTeamProject")]
        [TestCase("System.Tags", "370", "Release1; TagTest")]
        public async Task GetFeaturesForProject_ReadsFeatureOwnerFieldCorrect(string fieldName, string workItemId, string expectedFeatureOwnerFieldValue)
        {
            var subject = CreateSubject();

            var additionalFieldDefs = new List<AdditionalFieldDefinition>();
            int? ownerFieldId = null;
            if (!string.IsNullOrEmpty(fieldName))
            {
                var ownerField = new AdditionalFieldDefinition { Id = 2, DisplayName = "Owner", Reference = fieldName };
                additionalFieldDefs.Add(ownerField);
                ownerFieldId = ownerField.Id;
            }

            var portfolio = CreatePortfolioWithAdditionalFields($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '{workItemId}'", additionalFieldDefs);
            portfolio.FeatureOwnerAdditionalFieldDefinitionId = ownerFieldId;

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.Single(f => f.ReferenceId == workItemId);

            Assert.That(feature.OwningTeam, Is.EqualTo(expectedFeatureOwnerFieldValue));
        }

        [Test]
        public async Task GetFeaturesForProject_GetsCorrectTags()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Id}] = '370'");

            var features = await subject.GetFeaturesForProject(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features, Has.Count.EqualTo(1));
                Assert.That(features.Single().Tags, Has.Count.EqualTo(2));
                Assert.That(features.Single().Tags, Contains.Item("Release1"));
                Assert.That(features.Single().Tags, Contains.Item("TagTest"));
            }
        }

        [Test]
        public async Task GetParentFeaturesDetails_ReturnsCorrectDetails()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject' AND [{AzureDevOpsFieldNames.Tags}] CONTAINS 'Release1'");

            var parentItems = await subject.GetParentFeaturesDetails(portfolio, ["400"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parentItems, Has.Count.EqualTo(1));
                var parentItem = parentItems.Single();

                Assert.That(parentItem.ReferenceId, Is.EqualTo("400"));
                Assert.That(parentItem.Name, Is.EqualTo("Delivery for Agnieszka"));
                Assert.That(parentItem.Url, Is.EqualTo("https://dev.azure.com/huserben/e7b3c1df-8d70-4943-98a7-ef00c7a0c523/_workitems/edit/400"));
            }
        }

        [Test]
        public async Task ValidateConnection_GivenValidSettings_ReturnsTrue()
        {
            var subject = CreateSubject();

            var organizationUrl = "https://dev.azure.com/huserben";
            var personalAccessToken = Environment.GetEnvironmentVariable("AzureDevOpsLighthouseIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'AzureDevOpsLighthouseIntegrationTestToken' is set!");

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = personalAccessToken, IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "30", IsSecret = false },
                ]);

            var isValid = await subject.ValidateConnection(connectionSetting);

            Assert.That(isValid, Is.True);
        }

        [Test]
        [TestCase("https://dev.azure.com/huserben", "Yah-yah-yah, Coco Jamboo, yah-yah-yeh")]
        [TestCase("https://dev.azure.com/huserben", "")]
        [TestCase("", "PATPATPAT")]
        [TestCase("https://not.valid", "PATPATPAT")]
        [TestCase("asdfasdfasdfasdf", "PATPATPAT")]
        public async Task ValidateConnection_GivenInvalidSettings_ReturnsFalse(string organizationUrl, string personalAccessToken)
        {
            var subject = CreateSubject();

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = personalAccessToken, IsSecret = true },
                ]);

            var isValid = await subject.ValidateConnection(connectionSetting);

            Assert.That(isValid, Is.False);
        }

        [Test]
        [TestCase(new string[] { "System.AreaPath" }, true)]
        [TestCase(new string[] { "System.AreaPath", "System.IterationPath" }, true)]
        [TestCase(new string[] { "Custom.RemoteFeatureID" }, true)]
        [TestCase(new string[] { "MamboJambo" }, false)]
        [TestCase(new string[] { "System.AreaPath", "Custom.NOTEXISTING" }, false)]
        public async Task ValidateConnection_GivenAdditionalFields_ReturnsTrueOnlyIfFieldsExist(string[] additionalFields, bool expectedValidationResult)
        {
            var subject = CreateSubject();

            var organizationUrl = "https://dev.azure.com/huserben";
            var personalAccessToken = Environment.GetEnvironmentVariable("AzureDevOpsLighthouseIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'AzureDevOpsLighthouseIntegrationTestToken' is set!");

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = personalAccessToken, IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "30", IsSecret = false },
            ]);

            foreach (var additionalField in additionalFields) 
            {
                connectionSetting.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
                {
                    DisplayName = additionalField,
                    Reference =  additionalField,
                });
            }

            var isValid = await subject.ValidateConnection(connectionSetting);

            Assert.That(isValid, Is.EqualTo(expectedValidationResult));
        }

        [Test]
        [TestCase("[System.TeamProject] = 'CMFTTestTeamProject'", true)]
        [TestCase("[System.TeamProject] = 'CMFTTestTeamProject' AND [System.Tags] CONTAINS 'NotExistingTag'", false)]
        [TestCase("[System.TeamProject] = 'SomethingThatDoesNotExist'", false)]
        public async Task ValidateTeamSettings_ValidConnectionSettings_ReturnsTrueIfTeamHasThroughput(string query, bool expectedValue)
        {
            var history = (DateTime.Now - new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Days;

            var team = CreateTeam(query);
            team.ThroughputHistory = history;

            var subject = CreateSubject();

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task ValidateTeamSettings_InvalidConnectionSettings_ReturnsFalse()
        {
            var team = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            var subject = CreateSubject();

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = "https://dev.azure.com/huserben", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = "dsakjflasdkjflasdkfjlaskdjflskdjfa", IsSecret = true },
                ]);

            team.WorkTrackingSystemConnection = connectionSetting;

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.False);
        }

        [Test]
        [TestCase("[System.TeamProject] = 'CMFTTestTeamProject'", true)]
        [TestCase("[System.TeamProject] = 'CMFTTestTeamProject' AND [System.Tags] CONTAINS 'NotExistingTag'", false)]
        [TestCase("[System.TeamProject] = 'SomethingThatDoesNotExist'", false)]
        public async Task ValidateProjectSettings_ValidConnectionSettings_ReturnsTrueIfFeaturesAreFound(string query, bool expectedValue)
        {
            var team = CreateTeam("[System.TeamProject] = 'CMFTTestTeamProject'");
            var portfolio = CreatePortfolio(query, team);

            var subject = CreateSubject();

            var isValid = await subject.ValidatePortfolioSettings(portfolio);

            Assert.That(isValid, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task ValidateProjectSettings_InvalidConnectionSettings_ReturnsFalse()
        {
            _ = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");
            var portfolio = CreatePortfolio("[System.TeamProject] = 'CMFTTestTeamProject'");
            var subject = CreateSubject();

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = "https://dev.azure.com/huserben", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = "dsakjflasdkjflasdkfjlaskdjflskdjfa", IsSecret = true },
                ]);

            portfolio.WorkTrackingSystemConnection = connectionSetting;

            var isValid = await subject.ValidatePortfolioSettings(portfolio);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public async Task ValidateProjectSettings_NotExistingAdditionalField_ReturnsFalse()
        {
            _ = CreateTeam($"[{AzureDevOpsFieldNames.TeamProject}] = 'CMFTTestTeamProject'");

            // Add an additional field definition with an invalid reference
            var invalidField = new AdditionalFieldDefinition { Id = 1, DisplayName = "Invalid", Reference = "MamboJambo" };
            var portfolio = CreatePortfolioWithAdditionalFields("[System.TeamProject] = 'CMFTTestTeamProject'", [invalidField]);

            var subject = CreateSubject();

            var isValid = await subject.ValidatePortfolioSettings(portfolio);

            Assert.That(isValid, Is.False);
        }

        private Team CreateTeam(string query)
        {
            var team = new Team
            {
                Name = "TestTeam",
                DataRetrievalValue = query
            };

            var workTrackingSystemConnection = CreateWorkTrackingSystemConnection();
            team.WorkTrackingSystemConnection = workTrackingSystemConnection;

            return team;
        }

        private Portfolio CreatePortfolio(string query, params Team[] teams)
        {
            return CreatePortfolioWithAdditionalFields(query, [], teams);
        }

        private Portfolio CreatePortfolioWithAdditionalFields(string query, List<AdditionalFieldDefinition> additionalFieldDefs, params Team[] teams)
        {
            var portfolio = new Portfolio
            {
                Name = "TestProject",
                DataRetrievalValue = query,
            };

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Feature");

            var workTrackingSystemConnection = CreateWorkTrackingSystemConnection();
            workTrackingSystemConnection.AdditionalFieldDefinitions.AddRange(additionalFieldDefs);
            portfolio.WorkTrackingSystemConnection = workTrackingSystemConnection;

            portfolio.UpdateTeams(teams);

            return portfolio;
        }

        private WorkTrackingSystemConnection CreateWorkTrackingSystemConnection()
        {
            var organizationUrl = "https://dev.azure.com/huserben";
            var personalAccessToken = Environment.GetEnvironmentVariable("AzureDevOpsLighthouseIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'AzureDevOpsLighthouseIntegrationTestToken' is set!");

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = personalAccessToken, IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "100", IsSecret = false },
                ]);

            return connectionSetting;
        }

        private static AzureDevOpsWorkTrackingConnector CreateSubject()
        {
            return new AzureDevOpsWorkTrackingConnector(Mock.Of<ILogger<AzureDevOpsWorkTrackingConnector>>(), new FakeCryptoService());
        }
    }
}