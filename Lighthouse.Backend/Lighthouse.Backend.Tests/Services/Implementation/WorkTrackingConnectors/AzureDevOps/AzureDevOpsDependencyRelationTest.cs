using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
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

        private static readonly string[] TheOtherItemItWaitsOn = ["1799"];

        private const int TheFeature = 42;

        private const int TheItemTheFeatureWaitsOn = 1801;

        private const string TheParentFieldTheProjectNames = "Custom.RemoteFeatureID";

        private const string TheDependencyFieldTheProjectNames = "Custom.WaitsOn";

        private const int TheDependencyFieldId = 2;

        private const string TheParentThatFieldPointsAt = "4711";

        private const string TheFilterTheProjectIsFetchedBy = "[System.TeamProject] = 'TestProject'";

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

        [Test]
        public async Task GetWorkItemsForTeam_ATeamThatNamesItsOwnParentFieldIsCompletelyUnaffected()
        {
            var (subject, team, payloadReads) = ATeamWhoseParentComesFromACustomField();

            var workItem = (await subject.GetWorkItemsForTeam(team)).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(payloadReads.Select(read => read.Expand), Has.None.EqualTo(WorkItemExpand.Relations),
                    "A dependency runs between two features, so nothing on a team's refresh ever reads one - and the "
                    + "relations are the only place one could be read from. A team that names a field to read the "
                    + "parent from still skips them outright, as it always did, rather than buying a request per "
                    + "refresh for something no screen shows.");
                Assert.That(workItem.ParentReferenceId, Is.EqualTo(TheParentThatFieldPointsAt),
                    "The named field stays the parent's only source on a team, exactly as before.");
            }
        }

        [Test]
        public async Task GetFeaturesForProject_ReadingTheDependenciesCostsTheRefreshNoRequestOfItsOwn()
        {
            var (subject, payloadReads) = AnAzureDevOpsHoldingOneItemOfType("Feature");

            var feature = (await subject.GetFeaturesForProject(APortfolio())).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.DependsOnReferences.Select(reference => reference.ReferenceId), Is.EqualTo(TheOneItemItWaitsOn),
                    "A refresh that read no dependency at all would satisfy every count below for free.");
                Assert.That(payloadReads.Count(read => read.Expand == WorkItemExpand.Relations), Is.EqualTo(1),
                    "The relations are read once per refresh, for the parent, and the dependencies were already in "
                    + "that answer. Reading them a second time doubles the most expensive request a refresh makes, "
                    + "and a portfolio of a few hundred features feels that in minutes, not milliseconds.");
                Assert.That(payloadReads.SelectMany(read => read.Ids), Has.None.EqualTo(TheItemTheFeatureWaitsOn),
                    "A relation names the item it points at by id, so saying which feature this one waits on is a "
                    + "lookup among the features already in hand. Fetching the target instead would cost one "
                    + "request per dependency, on top of the refresh, every time.");
            }
        }

        [Test]
        public async Task GetFeaturesForProject_WhenTheDependenciesComeFromANamedField_YieldsOneEdgePerReferenceInIt()
        {
            var (subject, _) = AnAzureDevOpsHoldingOneItemOfType("Feature", whatTheDependencyFieldSays: "1801;1799");

            var feature = (await subject.GetFeaturesForProject(APortfolioReadingItsDependenciesFromAField())).Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.DependsOnReferences.Select(reference => reference.ReferenceId), Is.EqualTo(TheTwoItemsItWaitsOn));
                Assert.That(feature.DependsOnReferences.Select(reference => reference.Source), Has.All.EqualTo(DependencySource.PortfolioField),
                    "Where an edge was read from is what lets a reader tell a link somebody drew in the tracker from a "
                    + "reference somebody typed into a column, and the two are maintained by different people.");
            }
        }

        /// <summary>
        /// Naming a field is a declaration that the field is authoritative, which is the posture the parent
        /// setting beside it already takes. Adding to the tracker's own link instead would leave nobody able to
        /// say where an edge on the screen came from, or how to remove one.
        /// </summary>
        [Test]
        public async Task GetFeaturesForProject_WhenTheDependenciesComeFromANamedField_IgnoresTheTrackersOwnLink()
        {
            var (subject, _) = AnAzureDevOpsHoldingOneItemOfType("Feature", whatTheDependencyFieldSays: "1799");

            var feature = (await subject.GetFeaturesForProject(APortfolioReadingItsDependenciesFromAField())).Single();

            Assert.That(feature.DependsOnReferences.Select(reference => reference.ReferenceId), Is.EqualTo(TheOtherItemItWaitsOn),
                $"The relations name {TheItemTheFeatureWaitsOn} as a predecessor, and the named field does not.");
        }

        [Test]
        public async Task GetFeaturesForProject_WhenBothTheParentAndTheDependenciesComeFromNamedFields_ReadsNoRelationsAtAll()
        {
            var (subject, payloadReads) = AnAzureDevOpsHoldingOneItemOfType("Feature", whatTheDependencyFieldSays: "1799");
            var portfolio = APortfolioReadingItsDependenciesFromAField();
            portfolio.ParentOverrideAdditionalFieldDefinitionId = 1;

            await subject.GetFeaturesForProject(portfolio);

            Assert.That(payloadReads.Select(read => read.Expand), Has.None.EqualTo(WorkItemExpand.Relations),
                "The relations carry the parent link and the dependency links and nothing else a refresh wants. "
                + "Once both come from a field, the most expensive request a refresh makes buys nothing.");
        }

        /// <summary>
        /// The failure this guards against is silent: a Portfolio reading its dependencies from a field but its
        /// parent from the tracker would lose the whole parent hierarchy, and an empty hierarchy is a believable
        /// answer with nothing about it that looks wrong.
        /// </summary>
        [Test]
        public async Task GetFeaturesForProject_WhenOnlyTheDependenciesComeFromANamedField_StillReadsTheRelationsForTheParent()
        {
            var (subject, payloadReads) = AnAzureDevOpsHoldingOneItemOfType("Feature", whatTheDependencyFieldSays: "1799");

            await subject.GetFeaturesForProject(APortfolioReadingItsDependenciesFromAField());

            Assert.That(payloadReads.Count(read => read.Expand == WorkItemExpand.Relations), Is.EqualTo(1),
                "The relations are still the only place the parent link can be read from.");
        }

        [Test]
        public async Task GetFeaturesForProject_WhenTheNamedFieldIsEmpty_YieldsNoDependenciesAndNoError()
        {
            var (subject, _) = AnAzureDevOpsHoldingOneItemOfType("Feature", whatTheDependencyFieldSays: "");

            var feature = (await subject.GetFeaturesForProject(APortfolioReadingItsDependenciesFromAField())).Single();

            Assert.That(feature.DependsOnReferences, Is.Empty);
        }

        private static Portfolio APortfolioReadingItsDependenciesFromAField()
        {
            var portfolio = APortfolio();
            portfolio.DependencyOverrideAdditionalFieldDefinitionId = TheDependencyFieldId;

            return portfolio;
        }

        private static (AzureDevOpsWorkTrackingConnector Subject, Portfolio Portfolio) APortfolioWhoseParentComesFromACustomField()
        {
            var (subject, _) = AnAzureDevOpsHoldingOneItemOfType("Feature");

            return (subject, APortfolioReadingItsParentFromAField());
        }

        private static (AzureDevOpsWorkTrackingConnector Subject, Team Team, List<PayloadRead> PayloadReads) ATeamWhoseParentComesFromACustomField()
        {
            var (subject, payloadReads) = AnAzureDevOpsHoldingOneItemOfType("User Story");

            return (subject, ATeamReadingItsParentFromAField(), payloadReads);
        }

        /// <summary>
        /// One work item, whose relations name a predecessor, in an organisation that records every read it is
        /// asked for - so a test can say which reads happened and not only what came back.
        /// </summary>
        private static (AzureDevOpsWorkTrackingConnector Subject, List<PayloadRead> PayloadReads) AnAzureDevOpsHoldingOneItemOfType(
            string workItemType, string whatTheDependencyFieldSays = "")
        {
            var payloadReads = new List<PayloadRead>();
            var clientMock = new Mock<WorkItemTrackingHttpClient>(new Uri("https://dev.azure.com/lighthouse-test"), new VssCredentials());

            clientMock
                .Setup(client => client.QueryByWiqlAsync(It.IsAny<Wiql>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new WorkItemQueryResult { WorkItems = [new WorkItemReference { Id = TheFeature }] });

            clientMock
                .Setup(client => client.GetWorkItemFieldsAsync(It.IsAny<GetFieldsExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([
                    new WorkItemField2 { Name = TheParentFieldTheProjectNames, ReferenceName = TheParentFieldTheProjectNames },
                    new WorkItemField2 { Name = TheDependencyFieldTheProjectNames, ReferenceName = TheDependencyFieldTheProjectNames },
                ]);

            clientMock
                .Setup(client => client.GetWorkItemsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<DateTime?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<WorkItemErrorPolicy?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .Returns((IEnumerable<int> ids, IEnumerable<string> fields, DateTime? _, WorkItemExpand? expand, WorkItemErrorPolicy? _, object _, CancellationToken _) =>
                {
                    var askedFor = ids.ToList();
                    payloadReads.Add(new PayloadRead(askedFor, fields?.ToList() ?? [], expand));

                    return Task.FromResult<List<AdoWorkItem>>(
                        [.. askedFor.Select(id => expand == WorkItemExpand.Relations ? TheRelationsOf(id) : ThePayloadOf(id, workItemType, whatTheDependencyFieldSays))]);
                });

            clientMock
                .Setup(client => client.GetRevisionsAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            return (new RecordedAzureDevOpsConnector(clientMock.Object), payloadReads);
        }

        private static AdoWorkItem ThePayloadOf(int id, string workItemType, string whatTheDependencyFieldSays)
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
                    [AzureDevOpsFieldNames.WorkItemType] = workItemType,
                    [AzureDevOpsFieldNames.CreatedDate] = new DateTime(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc),
                    [AzureDevOpsFieldNames.StackRank] = $"{id}",
                    [TheParentFieldTheProjectNames] = TheParentThatFieldPointsAt,
                    [TheDependencyFieldTheProjectNames] = whatTheDependencyFieldSays,
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
            var portfolio = APortfolio();
            portfolio.ParentOverrideAdditionalFieldDefinitionId = 1;

            return portfolio;
        }

        private static Portfolio APortfolio()
        {
            var portfolio = new Portfolio
            {
                Id = 1,
                Name = "TestProject",
                DataRetrievalValue = TheFilterTheProjectIsFetchedBy,
                WorkTrackingSystemConnectionId = 1,
                WorkTrackingSystemConnection = AConnectionDefiningTheParentField(),
            };

            TheTypeAndStatesItFetches(portfolio, "Feature");

            return portfolio;
        }

        private static Team ATeamReadingItsParentFromAField()
        {
            var team = new Team
            {
                Id = 1,
                Name = "TestTeam",
                DataRetrievalValue = TheFilterTheProjectIsFetchedBy,
                WorkTrackingSystemConnectionId = 1,
                WorkTrackingSystemConnection = AConnectionDefiningTheParentField(),
                ParentOverrideAdditionalFieldDefinitionId = 1,
            };

            TheTypeAndStatesItFetches(team, "User Story");

            return team;
        }

        private static WorkTrackingSystemConnection AConnectionDefiningTheParentField()
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

            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = TheDependencyFieldId,
                DisplayName = "Waits On",
                Reference = TheDependencyFieldTheProjectNames,
            });

            return connection;
        }

        private static void TheTypeAndStatesItFetches(IWorkItemQueryOwner owner, string workItemType)
        {
            owner.WorkItemTypes.Clear();
            owner.WorkItemTypes.Add(workItemType);

            owner.ToDoStates.Clear();
            owner.ToDoStates.Add("New");
            owner.DoingStates.Clear();
            owner.DoingStates.Add("Active");
            owner.DoneStates.Clear();
            owner.DoneStates.Add("Closed");
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
