using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Moq;
using AdoWorkItem = Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.WorkItem;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.AzureDevOps
{
    /// <summary>
    /// Reading, out of a work item's relations, the other items it is waiting on.
    ///
    /// Azure DevOps records a dependency twice - once on each end. The item that has to wait carries a
    /// Predecessor link, and the item being waited on carries the mirror Successor link. Only one of those
    /// two ends is read: the other is the same edge seen from the far side, so taking both would count
    /// every dependency in the instance twice.
    ///
    /// Which end is which is decided by the link type, not by the word Azure DevOps prints beside it. That
    /// word is localised and can be renamed by a project administrator; the link type cannot.
    /// </summary>
    [TestFixture]
    public class AzureDevOpsDependencyRelationTest
    {
        private const string Predecessor = "System.LinkTypes.Dependency-Reverse";

        private const string Successor = "System.LinkTypes.Dependency-Forward";

        private const string Parent = "System.LinkTypes.Hierarchy-Reverse";

        private static readonly string[] TheOneItemItWaitsOn = ["1801"];

        private static readonly string[] TheTwoItemsItWaitsOn = ["1801", "1799"];

        private const int TheFeature = 42;

        private const int TheItemTheFeatureWaitsOn = 1801;

        private const string TheParentFieldTheProjectNames = "Custom.RemoteFeatureID";

        private const string TheParentThatFieldPointsAt = "4711";

        [Test]
        public void ExtractDependencyReferences_YieldsTheItemAPredecessorPointsAt()
        {
            var workItem = AWorkItemRelatedTo(APredecessorPointingAt(1801));

            var references = workItem.ExtractDependencyReferences();

            Assert.That(references, Is.EqualTo(TheOneItemItWaitsOn));
        }

        [Test]
        public void ExtractDependencyReferences_YieldsNothingForAnItemThatOnlyHasSuccessors()
        {
            var workItem = AWorkItemRelatedTo(ASuccessorPointingAt(3540));

            var references = workItem.ExtractDependencyReferences();

            Assert.That(references, Is.Empty,
                "A Successor is the far end of somebody else's wait. Reading it here as if it were this item's "
                + "own would record every dependency a second time, pointing the wrong way.");
        }

        [Test]
        public void ExtractDependencyReferences_YieldsOnlyThePredecessorsWhenBothDirectionsAreLinked()
        {
            var workItem = AWorkItemRelatedTo(
                APredecessorPointingAt(1801),
                ASuccessorPointingAt(3540),
                APredecessorPointingAt(1799),
                ASuccessorPointingAt(3533),
                ASuccessorPointingAt(3512),
                AParentPointingAt(1700));

            var references = workItem.ExtractDependencyReferences();

            Assert.That(references, Is.EqualTo(TheTwoItemsItWaitsOn));
        }

        [Test]
        public void ExtractDependencyReferences_ReadsTheLinkTypeRatherThanTheDisplayName()
        {
            var workItem = AWorkItemRelatedTo(ARelation(Predecessor, TheUrlOf(1801), "Vorgaenger"));

            var references = workItem.ExtractDependencyReferences();

            Assert.That(references, Is.EqualTo(TheOneItemItWaitsOn),
                "The name beside a link is localised and renameable by a project administrator, so a German "
                + "project would lose every dependency it has if the name were what decided this.");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("nowhere")]
        [TestCase("https://dev.azure.com/letpeoplework/Lighthouse/_apis/wit/workItems/")]
        [TestCase("https://dev.azure.com/letpeoplework/Lighthouse/_apis/wit/workItems/not-an-id")]
        public void ExtractDependencyReferences_SkipsARelationWhoseUrlNamesNoWorkItem(string? url)
        {
            var workItem = AWorkItemRelatedTo(ARelation(Predecessor, url, "Predecessor"));

            var references = workItem.ExtractDependencyReferences();

            Assert.That(references, Is.Empty,
                "A link nobody can read has to leave no trace at all. Recording the readable half of it would "
                + "claim a wait on an item that does not exist, and throwing would abandon the rest of the "
                + "refresh over one bad link.");
        }

        [Test]
        public void ExtractDependencyReferences_YieldsNothingWhenTheItemCarriesNoRelationsAtAll()
        {
            var workItem = new AdoWorkItem();

            var references = workItem.ExtractDependencyReferences();

            Assert.That(references, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_APortfolioThatAlreadyNamesItsOwnParentFieldStillGetsItsDependencies()
        {
            var (subject, portfolio) = APortfolioWhoseParentComesFromACustomField();

            var feature = (await subject.GetFeaturesForProject(portfolio)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.DependsOnReferences.Select(reference => reference.ReferenceId), Is.EqualTo(TheOneItemItWaitsOn),
                    "Naming a field to read the parent from says nothing about dependencies, and both arrive in the "
                    + "same relations. A portfolio that names one would otherwise report no dependencies at all, for "
                    + "good - and an empty column is a believable answer, so nobody would go looking.");
                Assert.That(feature.ParentReferenceId, Is.EqualTo(TheParentThatFieldPointsAt),
                    "The named field stays the parent's only source. Reading the relations for the dependencies must "
                    + "not start overriding a parent the project deliberately keeps somewhere else.");
            }
        }

        private static (AzureDevOpsWorkTrackingConnector Subject, Portfolio Portfolio) APortfolioWhoseParentComesFromACustomField()
        {
            var clientMock = new Mock<WorkItemTrackingHttpClient>(new Uri("https://dev.azure.com/lighthouse-test"), new VssCredentials());

            clientMock
                .Setup(client => client.QueryByWiqlAsync(It.IsAny<Wiql>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WorkItemQueryResult { WorkItems = [new WorkItemReference { Id = TheFeature }] });

            clientMock
                .Setup(client => client.GetWorkItemFieldsAsync(It.IsAny<GetFieldsExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([new WorkItemField2 { Name = TheParentFieldTheProjectNames, ReferenceName = TheParentFieldTheProjectNames }]);

            clientMock
                .Setup(client => client.GetWorkItemsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<DateTime?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<WorkItemErrorPolicy?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns((IEnumerable<int> ids, IEnumerable<string> _, DateTime? _, WorkItemExpand? expand, WorkItemErrorPolicy? _, object _, CancellationToken _) =>
                    Task.FromResult<List<AdoWorkItem>>(
                        [.. ids.Select(id => expand == WorkItemExpand.Relations ? TheRelationsOf(id) : ThePayloadOf(id))]));

            clientMock
                .Setup(client => client.GetRevisionsAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            return (new RecordedAzureDevOpsConnector(clientMock.Object), APortfolioReadingItsParentFromAField());
        }

        private static AdoWorkItem ThePayloadOf(int id)
        {
            var item = new AdoWorkItem
            {
                Id = id,
                Links = new ReferenceLinks(),
                Fields = new Dictionary<string, object>
                {
                    [AzureDevOpsFieldNames.Id] = id,
                    [AzureDevOpsFieldNames.State] = "Active",
                    [AzureDevOpsFieldNames.Title] = $"Item {id}",
                    [AzureDevOpsFieldNames.WorkItemType] = "Feature",
                    [AzureDevOpsFieldNames.CreatedDate] = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc),
                    [AzureDevOpsFieldNames.StackRank] = $"{id}",
                    [TheParentFieldTheProjectNames] = TheParentThatFieldPointsAt,
                },
            };

            item.Links.AddLink(AzureDevOpsFieldNames.UrlPropertyName, $"https://dev.azure.com/lighthouse-test/_workitems/edit/{id}");

            return item;
        }

        private static AdoWorkItem TheRelationsOf(int id)
        {
            return new AdoWorkItem
            {
                Id = id,
                Relations = [APredecessorPointingAt(TheItemTheFeatureWaitsOn)],
            };
        }

        private static Portfolio APortfolioReadingItsParentFromAField()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = 1,
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                Name = "Test Setting",
                AuthenticationMethodKey = AuthenticationMethodKeys.AzureDevOpsPat,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.Url, Value = "https://dev.azure.com/lighthouse-test", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, Value = "encrypted-token", IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = AzureDevOpsWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "1", IsSecret = false },
            ]);

            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = 1,
                DisplayName = "Parent Field",
                Reference = TheParentFieldTheProjectNames,
            });

            var portfolio = new Portfolio
            {
                Id = 1,
                Name = "TestProject",
                DataRetrievalValue = "[System.TeamProject] = 'TestProject'",
                WorkTrackingSystemConnectionId = 1,
                WorkTrackingSystemConnection = connection,
                ParentOverrideAdditionalFieldDefinitionId = 1,
            };

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Feature");

            portfolio.ToDoStates.Clear();
            portfolio.ToDoStates.Add("New");
            portfolio.DoingStates.Clear();
            portfolio.DoingStates.Add("Active");
            portfolio.DoneStates.Clear();
            portfolio.DoneStates.Add("Closed");

            return portfolio;
        }

        private static AdoWorkItem AWorkItemRelatedTo(params WorkItemRelation[] relations)
        {
            return new AdoWorkItem { Relations = relations.ToList() };
        }

        private static WorkItemRelation APredecessorPointingAt(int id)
        {
            return ARelation(Predecessor, TheUrlOf(id), "Predecessor");
        }

        private static WorkItemRelation ASuccessorPointingAt(int id)
        {
            return ARelation(Successor, TheUrlOf(id), "Successor");
        }

        private static WorkItemRelation AParentPointingAt(int id)
        {
            return ARelation(Parent, TheUrlOf(id), "Parent");
        }

        private static WorkItemRelation ARelation(string rel, string? url, string displayName)
        {
            return new WorkItemRelation
            {
                Rel = rel,
                Url = url,
                Attributes = new Dictionary<string, object> { { "name", displayName } },
            };
        }

        private static string TheUrlOf(int id)
        {
            return $"https://dev.azure.com/letpeoplework/Lighthouse/_apis/wit/workItems/{id}";
        }
    }
}
