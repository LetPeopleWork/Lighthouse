using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Models.Validation;
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
    /// A Dependency Field that names one end of an Azure DevOps relation type rather than a field, read
    /// against the relation types the organisation lists: what Validate says about such a reference, which
    /// end a refresh reads, and what it reports when what was named reads nothing.
    /// </summary>
    [TestFixture]
    public class AzureDevOpsNamedRelationEndTest
    {
        private const string DependsOn = "Contoso.DependsOn";

        private const string DependsOnForward = "Contoso.DependsOn-Forward";

        private const string DependsOnReverse = "Contoso.DependsOn-Reverse";

        private const string Related = "System.LinkTypes.Related";

        private const string Predecessor = "System.LinkTypes.Dependency-Reverse";

        private const string HierarchyForward = "System.LinkTypes.Hierarchy-Forward";

        private const string HierarchyReverse = "System.LinkTypes.Hierarchy-Reverse";

        private const string OnlyItsForwardEndIsListed = "Contoso.HalfListed";

        private const string AnEndListedWithoutAttributes = "Contoso.Plain-Forward";

        private const string AttachedFile = "AttachedFile";

        private const string ARealField = "Custom.Size";

        private const string NotAnythingAtAll = "Contoso.Missing";

        private const string AlsoNotAnything = "Contoso.AlsoMissing";

        private const string ARefusal = "TF401027: You need the 'Read work items' permission to perform this action.";

        private const string ThePermissionHint =
            "Give the Personal Access Token permission to read work items, or check whether a proxy in front of Azure DevOps is turning that request down.";

        private const int TheFeature = 42;

        private const int TheOtherFeature = 43;

        private const int TheItemItWaitsOn = 1801;

        private const int TheItemItsPredecessorLinkPointsAt = 1799;

        private const int TheRealFieldId = 1;

        private const int TheDependencyFieldId = 2;

        private const int TheSecondReferenceId = 3;

        private static readonly string[] TheOneItemItWaitsOn = [$"{TheItemItWaitsOn}"];

        private static readonly string[] TheOnlyWorkItemLinkItCarries = [Related];

        private static readonly string[] BothEndsAsListed = [DependsOnForward, DependsOnReverse];

        private static readonly WorkItemRelationType[] TheListedTypes =
        [
            ARelationType(DependsOnForward, ("directional", true)),
            ARelationType(DependsOnReverse, ("directional", true)),
            ARelationType(Related, ("directional", false)),
            ARelationType($"{OnlyItsForwardEndIsListed}-Forward", ("directional", true)),
            new WorkItemRelationType { ReferenceName = AnEndListedWithoutAttributes, Name = "Plain" },
            ARelationType(AttachedFile, ("usage", "resourceLink")),
        ];

        [Test]
        public async Task ValidateConnection_WhenEveryReferenceIsAField_DoesNotListTheRelationTypes()
        {
            var organisation = new Organisation();

            var verdict = await organisation.Connector.ValidateConnection(AConnectionNaming(ARealField));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.True);
                organisation.Client.Verify(client => client.GetRelationTypesAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never,
                    "A connection whose references are all fields has nothing to look up among the relation types.");
            }
        }

        [TestCase(DependsOnForward)]
        [TestCase("contoso.dependson-reverse")]
        [TestCase(DependsOn)]
        [TestCase(Related)]
        [TestCase(AnEndListedWithoutAttributes)]
        public async Task ValidateConnection_WhenAReferenceNamesARelationType_AcceptsIt(string reference)
        {
            var organisation = new Organisation();

            var verdict = await organisation.Connector.ValidateConnection(AConnectionNaming(reference));

            Assert.That(verdict.IsValid, Is.True, verdict.Message);
        }

        [TestCase(OnlyItsForwardEndIsListed)]
        [TestCase(AttachedFile)]
        [TestCase(NotAnythingAtAll)]
        public async Task ValidateConnection_WhenAReferenceNamesNoFieldAndNoRelationType_RefusesIt(string reference)
        {
            var organisation = new Organisation();

            var verdict = await organisation.Connector.ValidateConnection(AConnectionNaming(reference));

            Assert.That(verdict.Code, Is.EqualTo("additional_fields_invalid"),
                "A base name names a relation type only when both its ends are listed, and an attachment links no work item to another.");
        }

        [Test]
        public async Task ValidateConnection_WhenReferencesNameNothing_ListsThemAndTheRelationTypesThatDoExist()
        {
            var organisation = new Organisation();

            var verdict = await organisation.Connector.ValidateConnection(AConnectionNaming(NotAnythingAtAll, AlsoNotAnything));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Message, Is.EqualTo(
                    $"Some additional fields could not be found: {NotAnythingAtAll}, {AlsoNotAnything}. This Azure DevOps organisation defines no field "
                    + "and no relation type of that name. The relation types it does define are: "
                    + $"{DependsOnForward}, {DependsOnReverse}, {Related}, {OnlyItsForwardEndIsListed}-Forward, {AnEndListedWithoutAttributes}."));
                Assert.That(verdict.TechnicalDetails, Is.EqualTo("Verify field names or references in Azure DevOps and update the additional field configuration."));
                Assert.That(verdict.FieldName, Is.EqualTo(AzureDevOpsReadException.AdditionalFieldsFieldName));
            }
        }

        [Test]
        public async Task ValidateConnection_WhenTheOrganisationListsNoRelationTypes_SaysSo()
        {
            var organisation = new Organisation(relationTypes: []);

            var verdict = await organisation.Connector.ValidateConnection(AConnectionNaming(NotAnythingAtAll));

            Assert.That(verdict.Message, Does.EndWith("of that name. It listed no relation types at all to check them against."));
        }

        [Test]
        public async Task ValidateConnection_WhenTheRelationTypesAreRefused_SaysTheReferenceCouldNotBeChecked()
        {
            var organisation = new Organisation(listingFailure: new VssServiceException(ARefusal));

            var verdict = await organisation.Connector.ValidateConnection(AConnectionNaming(NotAnythingAtAll, AlsoNotAnything));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.Code, Is.EqualTo("relation_types_unreadable"));
                Assert.That(verdict.Message, Is.EqualTo(
                    "Azure DevOps would not list its relation types, so the relation types could not be read and a reference naming one could not "
                    + $"be checked at all. The references waiting on it are: {NotAnythingAtAll}, {AlsoNotAnything}."));
                Assert.That(verdict.TechnicalDetails, Is.EqualTo(
                    $"{ThePermissionHint} Nothing about the additional field configuration is known to be wrong."));
                Assert.That(verdict.FieldName, Is.EqualTo(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken));
            }
        }

        [Test]
        public async Task ValidateConnection_WhenListingTheRelationTypesTimesOut_ReadsItAsUnreadable()
        {
            var organisation = new Organisation(listingFailure: new TaskCanceledException("The request timed out."));

            var verdict = await organisation.Connector.ValidateConnection(AConnectionNaming(NotAnythingAtAll));

            Assert.That(verdict.Code, Is.EqualTo("relation_types_unreadable"),
                "Nobody cancelled anything, so a request that ran out of time is a listing that could not be read.");
        }

        [Test]
        public async Task ValidateConnection_WhenListingTheRelationTypesFailsUnexpectedly_DoesNotPassItOffAsARefusal()
        {
            var organisation = new Organisation(listingFailure: new InvalidOperationException("Something nobody expected."));

            var verdict = await organisation.Connector.ValidateConnection(AConnectionNaming(NotAnythingAtAll));

            Assert.That(verdict.Code, Is.EqualTo("validation_failed"));
        }

        [Test]
        public async Task GetFeaturesForProject_WhenTheNamedEndIsCarried_ReadsThatEndAloneAsALinkDrawnInTheTracker()
        {
            var organisation = new Organisation(relations: new()
            {
                [TheFeature] = [ARelation(DependsOnForward, TheItemItWaitsOn), ARelation(Predecessor, TheItemItsPredecessorLinkPointsAt)],
                [TheOtherFeature] = [ARelation(Related, TheItemItWaitsOn)],
            });

            var features = await organisation.Connector.GetFeaturesForProject(APortfolioWhoseDependencyFieldNames(DependsOnForward), CancellationToken.None);

            var feature = features.Single(f => f.ReferenceId == $"{TheFeature}");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.DependsOnReferences.Select(reference => reference.ReferenceId), Is.EqualTo(TheOneItemItWaitsOn),
                    "Once an end is named, the Predecessor link no longer counts.");
                Assert.That(feature.DependsOnReferences.Select(reference => reference.Source), Has.All.EqualTo(DependencySource.TrackerLink));
                Assert.That(features.SelectMany(f => f.RefreshFindings), Is.Empty,
                    "One Feature carrying the named end is enough to show the setting reads something.");
            }
        }

        [Test]
        public async Task GetFeaturesForProject_WhenNoFeatureCarriesTheNamedEnd_SaysSoNamingOnlyTheWorkItemLinksSeen()
        {
            var organisation = new Organisation(relations: new()
            {
                [TheFeature] =
                [
                    ARelation(Related, TheItemItWaitsOn),
                    ARelation(AttachedFile, TheItemItWaitsOn),
                    ARelation("Hyperlink", TheItemItWaitsOn),
                    ARelation("ArtifactLink", TheItemItWaitsOn),
                    ARelation(HierarchyForward, TheItemItWaitsOn),
                    ARelation(HierarchyReverse, TheItemItsPredecessorLinkPointsAt),
                    ARelation(null, TheItemItWaitsOn),
                    ARelation(string.Empty, TheItemItWaitsOn),
                ],
                [TheOtherFeature] = [ARelation("system.linktypes.related", TheItemItWaitsOn)],
            });

            var features = await organisation.Connector.GetFeaturesForProject(APortfolioWhoseDependencyFieldNames(DependsOnForward), CancellationToken.None);

            var expected = new RefreshFinding(RefreshFindingKind.NamedLinkMatchedNothing, RefreshFindingSetting.DependencyField, DependsOnForward, TheOnlyWorkItemLinkItCarries);
            using (Assert.EnterMultipleScope())
            {
                foreach (var feature in features)
                {
                    Assert.That(feature.RefreshFindings, Has.Count.EqualTo(1));
                    var finding = feature.RefreshFindings.Single();
                    Assert.That((finding.Kind, finding.Setting, finding.Named), Is.EqualTo((expected.Kind, expected.Setting, expected.Named)));
                    Assert.That(finding.Seen, Is.EqualTo(expected.Seen),
                        "Attachments, hyperlinks, artifact links and the hierarchy could never have matched, so naming them would only mislead.");
                }
            }
        }

        [Test]
        public async Task GetFeaturesForProject_WhenTheFeaturesCarryNoRelationsAtAll_SaysNothing()
        {
            var organisation = new Organisation(relations: []);

            var features = await organisation.Connector.GetFeaturesForProject(APortfolioWhoseDependencyFieldNames(DependsOnForward), CancellationToken.None);

            Assert.That(features.SelectMany(f => f.RefreshFindings), Is.Empty,
                "Having no dependencies is the ordinary case, and says nothing about the setting.");
        }

        [Test]
        public async Task GetFeaturesForProject_WhenNoRelationEndIsNamed_SaysNothingAboutThePredecessorLinkGoingUnmatched()
        {
            var organisation = new Organisation(relations: new() { [TheFeature] = [ARelation(Related, TheItemItWaitsOn)] });

            var features = await organisation.Connector.GetFeaturesForProject(APortfolio(), CancellationToken.None);

            Assert.That(features.SelectMany(f => f.RefreshFindings), Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_WhenTheBaseNameIsNamed_ReadsNothingAndNamesBothEnds()
        {
            var organisation = new Organisation(relations: new() { [TheFeature] = [ARelation(DependsOnForward, TheItemItWaitsOn)] });

            var feature = (await organisation.Connector.GetFeaturesForProject(APortfolioWhoseDependencyFieldNames(DependsOn), CancellationToken.None))
                .Single(f => f.ReferenceId == $"{TheFeature}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.DependsOnReferences, Is.Empty, "A type named without its end cannot say which of its items waits.");
                Assert.That(feature.RefreshFindings.Select(finding => (finding.Kind, finding.Setting, finding.Named)), Is.EqualTo(new[]
                {
                    (RefreshFindingKind.RelationTypeNamedWithoutItsEnd, RefreshFindingSetting.DependencyField, DependsOn),
                }));
                Assert.That(feature.RefreshFindings.Single().Seen, Is.EqualTo(BothEndsAsListed));
            }
        }

        [Test]
        public async Task GetFeaturesForProject_WhenARelationTypeWithoutDirectionIsNamed_ReadsNothingAndSaysWhy()
        {
            var organisation = new Organisation(relations: new() { [TheFeature] = [ARelation(Related, TheItemItWaitsOn)] });

            var feature = (await organisation.Connector.GetFeaturesForProject(APortfolioWhoseDependencyFieldNames(Related), CancellationToken.None))
                .Single(f => f.ReferenceId == $"{TheFeature}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.DependsOnReferences, Is.Empty);
                Assert.That(feature.RefreshFindings, Has.Count.EqualTo(1));
                Assert.That(feature.RefreshFindings.Select(finding => (finding.Kind, finding.Named, finding.Seen.Count)), Has.All.EqualTo(
                    (RefreshFindingKind.RelationTypeHasNoDirection, Related, 0)));
            }
        }

        [Test]
        public async Task GetFeaturesForProject_WhenTheParentComesFromAFieldAndTheDependencyFieldReadsNothing_ReadsNoRelations()
        {
            var organisation = new Organisation(relations: new() { [TheFeature] = [ARelation(DependsOnForward, TheItemItWaitsOn)] });
            var portfolio = APortfolioWhoseDependencyFieldNames(DependsOn);
            portfolio.OwnParentOverride(TheRealFieldId);

            await organisation.Connector.GetFeaturesForProject(portfolio, CancellationToken.None);

            Assert.That(organisation.PayloadReads.Select(read => read.Expand), Has.None.EqualTo(WorkItemExpand.Relations),
                "The relations would be read for nothing: the parent comes from a field and the Dependency Field reads no end.");
        }

        [Test]
        public async Task GetFeaturesForProject_WhenSettingsThatReadAFieldNameARelationType_SaysSoForEachAndListsTheTypesOnce()
        {
            var organisation = new Organisation();
            var portfolio = APortfolio();
            var connection = portfolio.WorkTrackingSystemConnection;
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 10, DisplayName = "Parent", Reference = DependsOnForward });
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 11, DisplayName = "Size", Reference = DependsOnReverse });
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 12, DisplayName = "Owner", Reference = ARealField });
            portfolio.OwnParentOverride(10);
            portfolio.OwnSizeEstimateField(11);
            portfolio.OwnFeatureOwnerField(12);
            portfolio.OwningEstimationField(12);

            var feature = (await organisation.Connector.GetFeaturesForProject(portfolio, CancellationToken.None)).Single(f => f.ReferenceId == $"{TheFeature}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.RefreshFindings.Select(finding => (finding.Kind, finding.Setting, finding.Named, finding.Seen.Count)), Is.EqualTo(new[]
                {
                    (RefreshFindingKind.RelationTypeCannotBeReadHere, RefreshFindingSetting.ParentOverrideField, DependsOnForward, 0),
                    (RefreshFindingKind.RelationTypeCannotBeReadHere, RefreshFindingSetting.SizeEstimateField, DependsOnReverse, 0),
                }), "A setting naming a field says nothing; only the two naming a relation type are reported.");
                organisation.Client.Verify(client => client.GetRelationTypesAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once,
                    "The relation types are listed once per refresh, however many settings need them.");
            }
        }

        [Test]
        public async Task GetFeaturesForProject_WhenTheParentOverrideFieldNamesARelationTypeWithoutItsEnd_SaysItReadsNoParentFromIt()
        {
            var organisation = new Organisation();
            var portfolio = APortfolio();
            portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 10, DisplayName = "Parent", Reference = DependsOn });
            portfolio.OwnParentOverride(10);

            var feature = (await organisation.Connector.GetFeaturesForProject(portfolio, CancellationToken.None)).Single(f => f.ReferenceId == $"{TheFeature}");

            Assert.That(feature.RefreshFindings.Select(finding => (finding.Kind, finding.Setting, finding.Named)), Is.EqualTo(new[]
            {
                (RefreshFindingKind.RelationTypeCannotBeReadHere, RefreshFindingSetting.ParentOverrideField, DependsOn),
            }), "A base name whose two ends are listed is still a relation type, which a Parent Override Field cannot read.");
        }

        [Test]
        public async Task GetFeaturesForProject_WhenOnlyAnUnusedDefinitionNamesNoField_DoesNotListTheRelationTypes()
        {
            var organisation = new Organisation();
            var portfolio = APortfolio();
            portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(
                new AdditionalFieldDefinition { Id = 10, DisplayName = "Stale", Reference = NotAnythingAtAll });

            await organisation.Connector.GetFeaturesForProject(portfolio, CancellationToken.None);

            organisation.Client.Verify(client => client.GetRelationTypesAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never,
                "No setting of this Portfolio names the stale entry, so nothing it reads depends on what it is.");
        }

        [Test]
        public async Task GetFeaturesForProject_WhenTheDependencyFieldCannotBeLookedUp_StopsWithASentenceAnOperatorCanActOn()
        {
            var organisation = new Organisation(listingFailure: new VssServiceException(ARefusal));
            var portfolio = APortfolioWhoseDependencyFieldNames(DependsOnForward);

            var stopped = await Assert.ThrowsAsync<AzureDevOpsReadException>(
                async () => await organisation.Connector.GetFeaturesForProject(portfolio, CancellationToken.None));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stopped!.Verdict.Code, Is.EqualTo("relation_types_unreadable"));
                Assert.That(stopped.Verdict.Message, Is.EqualTo(
                    $"The Dependency Field of {portfolio.Name} names '{DependsOnForward}', which is not a field, and the relation types could not be listed, "
                    + "so the refresh was stopped. Nothing Lighthouse already holds was changed."));
                Assert.That(stopped.Verdict.TechnicalDetails, Is.EqualTo(ThePermissionHint));
                Assert.That(stopped.Verdict.FieldName, Is.EqualTo(AzureDevOpsReadException.AdditionalFieldsFieldName));
            }
        }

        private static Portfolio APortfolioWhoseDependencyFieldNames(string reference)
        {
            var portfolio = APortfolio();
            portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(
                new AdditionalFieldDefinition { Id = TheDependencyFieldId, DisplayName = "Depends On", Reference = reference });
            portfolio.OwnDependencyField(TheDependencyFieldId);

            return portfolio;
        }

        private static Portfolio APortfolio()
        {
            var connection = AConnection();
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = TheRealFieldId, DisplayName = "Size", Reference = ARealField });

            var portfolio = new Portfolio
            {
                Id = 1,
                Name = "TestProject",
                DataRetrievalValue = "[System.TeamProject] = 'TestProject'",
                WorkTrackingSystemConnectionId = 1,
                WorkTrackingSystemConnection = connection,
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

        private static WorkTrackingSystemConnection AConnectionNaming(params string[] references)
        {
            var connection = AConnection();
            connection.AdditionalFieldDefinitions.AddRange(references.Select((reference, index) =>
                new AdditionalFieldDefinition { Id = TheSecondReferenceId + index, DisplayName = reference, Reference = reference }));

            return connection;
        }

        private static WorkTrackingSystemConnection AConnection()
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
            ]);

            return connection;
        }

        private static WorkItemRelationType ARelationType(string referenceName, (string Key, object Value) attribute)
            => new()
            {
                ReferenceName = referenceName,
                Name = referenceName,
                Attributes = new Dictionary<string, object> { [attribute.Key] = attribute.Value },
            };

        private static WorkItemRelation ARelation(string? rel, int pointingAt)
            => new()
            {
                Rel = rel,
                Url = $"https://dev.azure.com/letpeoplework/Lighthouse/_apis/wit/workItems/{pointingAt}",
                Attributes = new Dictionary<string, object> { ["name"] = rel ?? string.Empty },
            };

        /// <summary>
        /// An organisation holding two Features, which defines one field, lists <see cref="TheListedTypes"/>
        /// unless told otherwise, and answers a read of the relations with what each Feature was given.
        /// </summary>
        private sealed class Organisation
        {
            public Organisation(
                WorkItemRelationType[]? relationTypes = null,
                Exception? listingFailure = null,
                Dictionary<int, List<WorkItemRelation>>? relations = null)
            {
                var relationsOf = relations ?? [];
                Client = new Mock<WorkItemTrackingHttpClient>(new Uri("https://dev.azure.com/lighthouse-test"), new VssCredentials());

                Client
                    .Setup(client => client.QueryByWiqlAsync(It.IsAny<Wiql>(), It.IsAny<bool?>(), It.IsAny<int?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new WorkItemQueryResult { WorkItems = [new WorkItemReference { Id = TheFeature }, new WorkItemReference { Id = TheOtherFeature }] });

                Client
                    .Setup(client => client.GetWorkItemFieldsAsync(It.IsAny<GetFieldsExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync([new WorkItemField2 { Name = "Size", ReferenceName = ARealField }]);

                Client
                    .Setup(client => client.GetWorkItemsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<DateTime?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<WorkItemErrorPolicy?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .Returns((IEnumerable<int> ids, IEnumerable<string> fields, DateTime? _, WorkItemExpand? expand, WorkItemErrorPolicy? _, object _, CancellationToken _) =>
                    {
                        var askedFor = ids.ToList();
                        PayloadReads.Add(new PayloadRead(askedFor, fields?.ToList() ?? [], expand));

                        return Task.FromResult<List<AdoWorkItem>>(
                            [.. askedFor.Select(id => expand == WorkItemExpand.Relations
                                ? new AdoWorkItem { Id = id, Relations = relationsOf.TryGetValue(id, out var carried) ? carried : null }
                                : ThePayloadOf(id))]);
                    });

                Client
                    .Setup(client => client.GetRevisionsAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<WorkItemExpand?>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync([]);

                var listing = Client.Setup(client => client.GetRelationTypesAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()));
                if (listingFailure is null)
                {
                    listing.ReturnsAsync(new List<WorkItemRelationType>(relationTypes ?? TheListedTypes));
                }
                else
                {
                    listing.ThrowsAsync(listingFailure);
                }

                Connector = new RecordedAzureDevOpsConnector(Client.Object);
            }

            public Mock<WorkItemTrackingHttpClient> Client { get; }

            public RecordedAzureDevOpsConnector Connector { get; }

            public List<PayloadRead> PayloadReads { get; } = [];

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
                    },
                };

                item.Links.AddLink(AzureDevOpsFieldNames.UrlPropertyName, $"https://dev.azure.com/lighthouse-test/_workitems/edit/{id}");

                return item;
            }
        }
    }
}
