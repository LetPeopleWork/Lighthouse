using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    [Category("Integration")]
    public class JiraWorkTrackingConnectorTest
    {
        [Test]
        public async Task GetWorkItemsForTeam_GetsAllItemsThatMatchQuery()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ AND labels = ExistingLabel");

            team.ResetUpdateTime();

            var matchingItems = await subject.GetWorkItemsForTeam(team);

            Assert.That(matchingItems.Count(), Is.EqualTo(2));
        }
        
        [Test]
        [TestCase(0, 1)]
        [TestCase(10, 0)]
        [TestCase(5500, 1)]
        public async Task GetWorkItemsForTeam_CutOffDateSet_SkipsItemsIfBeyondCutOff(int cutOffDays, int expectedItems)
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ AND labels = \"LabelOfItemThatIsClosed\"");
            
            team.DoneItemsCutoffDays =  cutOffDays;

            var actualItems = await subject.GetWorkItemsForTeam(team);

            Assert.That(actualItems.ToList(), Has.Count.EqualTo(expectedItems));
        }

        [Test]
        public async Task GetWorkItemsForTeam_OrCaseInWorkItemQuery_HandlesCorrectly()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ OR project = DUMMY");

            team.UseFixedDatesForThroughput = true;
            team.ThroughputHistoryStartDate = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            team.ThroughputHistoryEndDate = new DateTime(2025, 4, 1, 0, 0, 0, DateTimeKind.Utc);
            
            var startDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            team.DoneItemsCutoffDays =  (DateTime.UtcNow - startDate).Days;

            var closedItems = await subject.GetWorkItemsForTeam(team);

            Assert.That(closedItems.Count(), Is.EqualTo(18));
        }

        [Test]
        [TestCase("PROJ-18", "")]
        [TestCase("PROJ-15", "PROJ-8")]
        public async Task GetWorkItemsForTeam_SetsParentRelationCorrect(string issueKey, string expectedParentReference)
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ AND issueKey = {issueKey}");

            var workItems = await subject.GetWorkItemsForTeam(team);
            var workItem = workItems.Single(wi => wi.ReferenceId == issueKey);

            Assert.That(workItem.ParentReferenceId, Is.EqualTo(expectedParentReference));
        }

        [Test]
        public async Task GetWorkItemsForTeam_UseParentOverride_SetsParentRelationCorrect()
        {
            var subject = CreateSubject();
            var team = CreateTeam("project = LGHTHSDMO AND labels = NoProperParentLink AND issuekey = LGHTHSDMO-1726");

            team.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = 1,
                DisplayName = "Parent Link Override",
                Reference = "customrelationfield"
            });

            team.ParentOverrideAdditionalFieldDefinitionId = 1;

            var workItems = await subject.GetWorkItemsForTeam(team);
            var workItem = workItems.Single(wi => wi.ReferenceId == "LGHTHSDMO-1726");

            Assert.That(workItem.ParentReferenceId, Is.EqualTo("LGHTHSDMO-1724"));
        }

        [Test]
        public async Task GetFeaturesForProject_UseParentOverride_SetsParentRelationCorrect()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio("project = LGHTHSDMO AND labels = NoProperParentLink AND issuekey = LGHTHSDMO-1726");
            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Story");
            
            portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = 1,
                DisplayName = "Parent Link Override",
                Reference = "customfield_10038"
            });

            portfolio.ParentOverrideAdditionalFieldDefinitionId = 1;

            var workItems = await subject.GetFeaturesForProject(portfolio);
            var workItem = workItems.Single(wi => wi.ReferenceId == "LGHTHSDMO-1726");

            Assert.That(workItem.ParentReferenceId, Is.EqualTo("LGHTHSDMO-1724"));
        }

        [Test]
        public async Task GetWorkItemsForTeam_QueryContainsAmpersand_EscapesCorrectly()
        {
            var subject = CreateSubject();
            var team = CreateTeam("project = LGHTHSDMO");

            team.ToDoStates.Clear();
            team.DoneStates.Clear();
            team.DoingStates.Clear();
            team.DoingStates.Add("QA&Testing");

            var workItems = await subject.GetWorkItemsForTeam(team);

            Assert.That(workItems.ToList(), Has.Count.EqualTo(1));
        }

        [Test]
        public async Task GetWorkItemsForTeam_GetsLabelsAndStoresAsTag()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ AND key = PROJ-23");

            var workItems = await subject.GetWorkItemsForTeam(team);
            var workItem = workItems.Single(wi => wi.ReferenceId == "PROJ-23");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItem.Tags, Has.Count.EqualTo(3));
                Assert.That(workItem.Tags, Contains.Item("TagTest"));
                Assert.That(workItem.Tags, Contains.Item("ExistingLabel"));
                Assert.That(workItem.Tags, Contains.Item("Flagged"));
            }
        }

        [Test]
        [TestCase("Flagged")]
        [TestCase("FLAGGED")]
        [TestCase("flagged")]
        public async Task GetWorkItemsForTeam_ItemIsFlagged_FlaggedConfiguredAsBlockingTag_TreatsAsBlocked(string flaggedTag)
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ AND key = PROJ-23");
            
            team.BlockedTags.Clear();
            team.BlockedTags.Add(flaggedTag);

            var workItems = await subject.GetWorkItemsForTeam(team);
            var workItem = workItems.Single(wi => wi.ReferenceId == "PROJ-23");

            Assert.That(workItem.IsBlocked, Is.True);
        }

        [Test]
        public async Task GetWorkItemsForTeam_ItemIsNotFlagged_FlaggedConfiguredAsBlockingTag_TreatsNotBlocked()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ AND key = PROJ-18");
            
            team.BlockedTags.Clear();
            team.BlockedTags.Add("Flagged");

            var workItems = await subject.GetWorkItemsForTeam(team);
            var workItem = workItems.Single(wi => wi.ReferenceId == "PROJ-18");

            Assert.That(workItem.IsBlocked, Is.False);
        }

        [Test]
        public async Task GetWorkItemsForTeam_ItemIsFlagged_FlaggedNotConfiguredAsBlockingTag_TreatsNotBlocked()
        {
            var subject = CreateSubject();
            var team = CreateTeam($"project = PROJ AND key = PROJ-23");

            var workItems = await subject.GetWorkItemsForTeam(team);
            var workItem = workItems.Single(wi => wi.ReferenceId == "PROJ-23");

            Assert.That(workItem.IsBlocked, Is.False);
        }

        [Test]
        public async Task GetFeaturesForProject_LabelDoesNotExist_ReturnsNoItems()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"project = PROJ AND labels = \"NotExistingLabel\"");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Story");

            var itemsByTag = await subject.GetFeaturesForProject(portfolio);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsCorrectAmountOfItems()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"project = \"LGHTHSDMO\" AND labels = \"Phoenix\"");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Epic");
            
            var startDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            portfolio.DoneItemsCutoffDays =  (DateTime.UtcNow - startDate).Days;
            
            var itemsByTag = await subject.GetFeaturesForProject(portfolio);

            Assert.That(itemsByTag, Has.Count.EqualTo(4));
        }


        [Test]
        public async Task GetFeaturesForProject_TagExists_ReturnsCorrectNumberOfItems()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"project = PROJ AND labels = \"ExistingLabel\"");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Story");

            var itemsByTag = await subject.GetFeaturesForProject(portfolio);

            Assert.That(itemsByTag, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetFeaturesForProject_TagExists_WorkItemTypeDoesNotMatch_ReturnsNoItems()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"project = PROJ AND labels = \"ExistingLabel\"");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Bug");

            var itemsByTag = await subject.GetFeaturesForProject(portfolio);

            Assert.That(itemsByTag, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_ItemIsClosed_ReturnsItem()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"project = PROJ AND labels = \"LabelOfItemThatIsClosed\"");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Story");
            
            var startDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            portfolio.DoneItemsCutoffDays =  (DateTime.UtcNow - startDate).Days;

            var actualItems = await subject.GetFeaturesForProject(portfolio);

            Assert.That(actualItems, Has.Count.EqualTo(1));
        }   
        
        [Test]
        [TestCase(0, 1)]
        [TestCase(10, 0)]
        [TestCase(5500, 1)]
        public async Task GetFeaturesForProject_CutOffDateSet_SkipsItemsIfBeyondCutOff(int cutOffDays, int expectedItems)
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"project = PROJ AND labels = \"LabelOfItemThatIsClosed\"");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Story");
            
            portfolio.DoneItemsCutoffDays =  cutOffDays;

            var actualItems = await subject.GetFeaturesForProject(portfolio);

            Assert.That(actualItems, Has.Count.EqualTo(expectedItems));
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsCorrectFeatureProperties()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"project = PROJ AND issueKey = PROJ-18");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Story");

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.Single(f => f.ReferenceId == "PROJ-18");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.Name, Is.EqualTo("Test 32523"));
                Assert.That(feature.Order, Is.EqualTo("0|i00037:9"));
                Assert.That(feature.State, Is.EqualTo("In Progress"));
                Assert.That(feature.StateCategory, Is.EqualTo(StateCategories.Doing));
                Assert.That(feature.Url, Is.EqualTo("https://letpeoplework.atlassian.net/browse/PROJ-18"));
            }
        }

        [Test]
        public async Task WorkTrackingSystemContainsTrailingSlash_IgnoresInUrl()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"project = PROJ AND issueKey = PROJ-18");

            portfolio.WorkItemTypes.Clear();
            portfolio.WorkItemTypes.Add("Story");

            portfolio.WorkTrackingSystemConnection.Options[0].Value = "https://letpeoplework.atlassian.net/";

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.Single(f => f.ReferenceId == "PROJ-18");

            Assert.That(feature.Url, Is.EqualTo("https://letpeoplework.atlassian.net/browse/PROJ-18"));
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsCorrectStartedDateBasedOnStateMapping()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"project = PROJ AND issueKey = PROJ-21");

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.Single(f => f.ReferenceId == "PROJ-21");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.StartedDate.HasValue, Is.True);
                Assert.That(feature.StartedDate?.Date, Is.EqualTo(new DateTime(2025, 4, 5, 0, 0, 0, DateTimeKind.Utc)));

                Assert.That(feature.ClosedDate.HasValue, Is.False);
            }
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsCorrectClosedDateBasedOnStateMapping()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"project = PROJ AND issueKey = PROJ-21");
            portfolio.DoingStates.Remove("In Progress");
            portfolio.DoneStates.Add("In Progress");

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.Single(f => f.ReferenceId == "PROJ-21");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.ClosedDate.HasValue, Is.True);
                Assert.That(feature.ClosedDate?.Date, Is.EqualTo(new DateTime(2025, 4, 5, 0, 0, 0, DateTimeKind.Utc)));
            }
        }

        [Test]
        public async Task GetFeaturesForProject_ClosedDateButNoStartedDate_SetsStartedDateToClosedDate()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"project = PROJ AND issueKey = PROJ-21");
            portfolio.DoingStates.Clear();
            portfolio.DoneStates.Clear();
            portfolio.DoneStates.Add("In Progress");
            
            var startDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            portfolio.DoneItemsCutoffDays =  (DateTime.UtcNow - startDate).Days;

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.Single(f => f.ReferenceId == "PROJ-21");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.StartedDate, Is.EqualTo(feature.ClosedDate));
            }
        }

        [Test]
        [TestCase("", "LGHTHSDMO-9", 0)]
        [TestCase("MambooJamboo", "LGHTHSDMO-9", 0)]
        [TestCase("customfield_10037", "LGHTHSDMO-9", 12)]
        [TestCase("customfield_10037", "LGHTHSDMO-1724", 0)]
        [TestCase("customfield_10037", "LGHTHSDMO-8", 2)]
        public async Task GetFeaturesForProject_ReadsEstimatedSizeCorrect(string fieldName, string issueKey, int expectedEstimatedSize)
        {
            var subject = CreateSubject();

            var additionalFieldDefs = new List<AdditionalFieldDefinition>();
            
            var sizeField = new AdditionalFieldDefinition { Id = 1, DisplayName = "Size", Reference = fieldName };
            additionalFieldDefs.Add(sizeField);

            var portfolio = CreatePortfolioWithAdditionalFields($"project = LGHTHSDMO AND issuekey = {issueKey}", additionalFieldDefs);
            portfolio.SizeEstimateAdditionalFieldDefinitionId = 1;

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.Single(f => f.ReferenceId == issueKey);

            Assert.That(feature.EstimatedSize, Is.EqualTo(expectedEstimatedSize));
        }

        [Test]
        [TestCase("", "LGHTHSDMO-9", "")]
        [TestCase("MambooJamboo", "LGHTHSDMO-9", "")]
        [TestCase("customfield_10037", "LGHTHSDMO-9", "12.0")]
        [TestCase("fixVersions", "LGHTHSDMO-9", "Elixir Project")]
        [TestCase("labels", "LGHTHSDMO-5", "Phoenix")]
        [TestCase("labels", "LGHTHSDMO-5", "RebelRevolt")]
        public async Task GetFeaturesForProject_ReadsFeatureOwnerFieldCorrect(string fieldName, string issueKey, string expectedFeatureOwnerFieldValue)
        {
            var subject = CreateSubject();

            var additionalFieldDefs = new List<AdditionalFieldDefinition>();
            
            var ownerField = new AdditionalFieldDefinition { Id = 2, DisplayName = "Owner", Reference = fieldName };
            additionalFieldDefs.Add(ownerField);

            var portfolio = CreatePortfolioWithAdditionalFields($"project = LGHTHSDMO AND issuekey = {issueKey}", additionalFieldDefs);
            portfolio.FeatureOwnerAdditionalFieldDefinitionId = 2;

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.Single(f => f.ReferenceId == issueKey);
            
            Assert.That(feature.OwningTeam, Contains.Substring(expectedFeatureOwnerFieldValue));
        }

        [Test]
        public async Task GetFeaturesForProject_GetsLabelsAndStoresAsTag()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"project = PROJ AND key = PROJ-7");

            var features = await subject.GetFeaturesForProject(portfolio);
            var feature = features.Single(f => f.ReferenceId == "PROJ-7");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.Tags, Has.Count.EqualTo(2));
                Assert.That(feature.Tags, Contains.Item("TagTest"));
                Assert.That(feature.Tags, Contains.Item("ExistingLabel"));
            }
        }

        [Test]
        public async Task GetParentFeaturesDetails_ReturnsCorrectDetails()
        {
            var subject = CreateSubject();
            var portfolio = CreatePortfolio($"project = LGHTHSDMO");

            var parentItems = await subject.GetParentFeaturesDetails(portfolio, ["LGHTHSDMO-2377"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parentItems, Has.Count.EqualTo(1));
                var parentItem = parentItems.Single();

                Assert.That(parentItem.ReferenceId, Is.EqualTo("LGHTHSDMO-2377"));
                Assert.That(parentItem.Name, Is.EqualTo("Delivery for Agnieszka"));
                Assert.That(parentItem.Url, Is.EqualTo("https://letpeoplework.atlassian.net/browse/LGHTHSDMO-2377"));
            }
        }

        [Test]
        public async Task ValidateConnection_GivenValidSettings_ReturnsTrue()
        {
            var subject = CreateSubject();

            var organizationUrl = "https://letpeoplework.atlassian.net";
            var username = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestUsername") ?? "atlassian.pushchair@huser-berta.com";
            var apiToken = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'JiraLighthouseIntegrationTestToken' is set!");

            var connectionSetting = new WorkTrackingSystemConnection 
            { 
                WorkTrackingSystem = WorkTrackingSystems.Jira, 
                Name = "Test Setting",
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud
            };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken, IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "100", IsSecret = false },
                ]);

            var isValid = await subject.ValidateConnection(connectionSetting);

            Assert.That(isValid, Is.True);
        }

        [Test]
        [TestCase("https://letpeoplework.atlassian.net", "Yah-yah-yah, Coco Jamboo, yah-yah-yeh", "atlassian.pushchair@huser-berta.com")]
        [TestCase("https://letpeoplework.atlassian.net", "", "atlassian.pushchair@huser-berta.com")]
        [TestCase("https://letpeoplework.atlassian.net", "PATPATPAT", "")]
        [TestCase("", "PATPATPAT", "atlassian.pushchair@huser-berta.com")]
        [TestCase("https://not.valid", "PATPATPAT", "atlassian.pushchair@huser-berta.com")]
        [TestCase("asdfasdfasdfasdf", "PATPATPAT", "atlassian.pushchair@huser-berta.com")]
        public async Task ValidateConnection_GivenInvalidSettings_ReturnsFalse(string organizationUrl, string apiToken, string username)
        {
            var subject = CreateSubject();

            var connectionSetting = new WorkTrackingSystemConnection 
            { 
                WorkTrackingSystem = WorkTrackingSystems.Jira, 
                Name = "Test Setting",
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud
            };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken, IsSecret = true },
                ]);

            var isValid = await subject.ValidateConnection(connectionSetting);

            Assert.That(isValid, Is.False);
        }

        [Test]
        [TestCase(new[] { "fixVersions" }, true)]
        [TestCase(new[] { "Fix versions" }, true)]
        [TestCase(new[] { "fixVersions", "components" }, true)]
        [TestCase(new[] { "customfield_10038" }, true)]
        [TestCase(new[] { "customrelationfield" }, true)]
        [TestCase(new[] { "MamboJambo" }, false)]
        [TestCase(new[] { "fixVersions", "Custom.NOTEXISTING" }, false)]
        public async Task ValidateConnection_GivenAdditionalFields_ReturnsTrueOnlyIfFieldsExist(string[] additionalFields, bool expectedValidationResult)
        {
            var subject = CreateSubject();

            var organizationUrl = "https://letpeoplework.atlassian.net";
            var username = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestUsername") ?? "atlassian.pushchair@huser-berta.com";
            var apiToken = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'JiraLighthouseIntegrationTestToken' is set!");

            var connectionSetting = new WorkTrackingSystemConnection 
            { 
                WorkTrackingSystem = WorkTrackingSystems.Jira, 
                Name = "Test Setting",
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud
            };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken, IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "100", IsSecret = false },
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
        [TestCase("project = LGHTHSDMO AND issueKey = LGHTHSDMO-11", true)]
        [TestCase("project = LGHTHSDMO AND labels = 'NotExisting'", false)]
        [TestCase("project = SomethingThatDoesNotExist", false)]
        public async Task ValidateTeamSettings_ValidConnectionSettings_ReturnsTrueIfTeamHasThroughput(string query, bool expectedValue)
        {
            var team = CreateTeam(query);

            var subject = CreateSubject();

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task ValidateTeamSettings_InvalidConnectionSettings_ReturnsFalse()
        {
            var subject = CreateSubject();
            var team = CreateTeam("project = LGHTHSDMO");

            var connectionSetting = new WorkTrackingSystemConnection 
            { 
                WorkTrackingSystem = WorkTrackingSystems.Jira, 
                Name = "Test Setting",
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud
            };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = "https://letpeoplework.atlassian.net", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = "Benji", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = "JennifferAniston", IsSecret = true },
                ]);

            team.WorkTrackingSystemConnection = connectionSetting;

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.False);
        }

        [Test]
        [TestCase("project = LGHTHSDMO", true)]
        [TestCase("project = LGHTHSDMO AND labels = 'NotExisting'", false)]
        [TestCase("project = SomethingThatDoesNotExist", false)]
        public async Task ValidateProjectSettings_ValidConnectionSettings_ReturnsTrueIfFeaturesAreFound(string query, bool expectedValue)
        {
            var team = CreateTeam("project = LGHTHSDMO");
            var portfolio = CreatePortfolio(query, team);

            var subject = CreateSubject();

            var isValid = await subject.ValidatePortfolioSettings(portfolio);

            Assert.That(isValid, Is.EqualTo(expectedValue));
        }

        [Test]
        public async Task ValidateProjectSettings_InvalidConnectionSettings_ReturnsFalse()
        {
            var portfolio = CreatePortfolio("project = LGHTHSDMO");
            var subject = CreateSubject();

            var connectionSetting = new WorkTrackingSystemConnection 
            { 
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps, 
                Name = "Test Setting",
                AuthenticationMethodKey = AuthenticationMethodKeys.AzureDevOpsPat
            };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = "https://letpeoplework.atlassian.net", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = "Benji", IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = "JennifferAniston", IsSecret = true },
                ]);

            portfolio.WorkTrackingSystemConnection = connectionSetting;

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

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("Story");
            team.WorkItemTypes.Add("Bug");
            team.WorkItemTypes.Add("Task");

            team.DoneStates.Clear();
            team.DoneStates.Add("Done");

            team.DoingStates.Clear();
            team.DoingStates.Add("In Progress");

            team.ToDoStates.Clear();
            team.ToDoStates.Add("To Do");

            var connectionSetting = CreateWorkTrackingSystemConnection();
            team.WorkTrackingSystemConnection = connectionSetting;

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
            portfolio.WorkItemTypes.Add("Epic");

            portfolio.DoneStates.Clear();
            portfolio.DoneStates.Add("Done");

            portfolio.DoingStates.Clear();
            portfolio.DoingStates.Add("In Progress");

            portfolio.ToDoStates.Clear();
            portfolio.ToDoStates.Add("To Do");

            portfolio.UpdateTeams(teams);

            var workTrackingSystemConnection = CreateWorkTrackingSystemConnection();
            workTrackingSystemConnection.AdditionalFieldDefinitions.AddRange(additionalFieldDefs);
            portfolio.WorkTrackingSystemConnection = workTrackingSystemConnection;

            return portfolio;
        }

        private WorkTrackingSystemConnection CreateWorkTrackingSystemConnection()
        {
            var organizationUrl = "https://letpeoplework.atlassian.net";
            var username = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestUsername") ?? "atlassian.pushchair@huser-berta.com";
            var apiToken = Environment.GetEnvironmentVariable("JiraLighthouseIntegrationTestToken") ?? throw new NotSupportedException("Can run test only if Environment Variable 'JiraLighthouseIntegrationTestToken' is set!");

            var connectionSetting = new WorkTrackingSystemConnection 
            { 
                WorkTrackingSystem = WorkTrackingSystems.Jira, 
                Name = "Test Setting",
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud
            };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = organizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken, IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "100", IsSecret = false },
                ]);

            return connectionSetting;
        }

        private static JiraWorkTrackingConnector CreateSubject()
        {
            return new JiraWorkTrackingConnector(
                new IssueFactory(Mock.Of<ILogger<IssueFactory>>()), Mock.Of<ILogger<JiraWorkTrackingConnector>>(), new FakeCryptoService());
        }
    }
}
