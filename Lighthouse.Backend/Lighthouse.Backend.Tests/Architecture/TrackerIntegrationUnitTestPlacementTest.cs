using Lighthouse.Backend.Tests.PlacementFixtures.Modules.TrackerIntegration.Domain;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The unit tests of Tracker integration sit next to the module: under Modules/TrackerIntegration in this project, in
    /// the folder that mirrors the module folder of their subject, in the namespace of that folder, and with their class
    /// names kept. No file of the context is left in the folders it came from (the connector, OAuth and connection-health
    /// folders under Services, and Factories), apart from the demo data, which stays; the old Work Items and Forecast
    /// source folders under Services hold nothing either. Files are found on disk, and the classes are read from the
    /// compiled test assembly together with its symbols, which say which file each class was compiled from, so no source
    /// text is pinned. Each rule also runs on made-up trees, so a file left in an old folder, a copy left behind, a folder
    /// that mirrors nothing and an empty or missing module folder must each be reported, while a file in its place, the
    /// demo data, the staying repositories and the integration tests under API/Integration must not be.
    /// </summary>
    [TestFixture]
    public class TrackerIntegrationUnitTestPlacementTest
    {
        private const string TestRootNamespace = "Lighthouse.Backend.Tests";

        private const string TestProjectDirectory = "Lighthouse.Backend.Tests";

        private const string ProductionProjectDirectory = "Lighthouse.Backend";

        private const string ModuleFolder = "Modules/TrackerIntegration";

        private const string MadeUpModuleTestFolder = "PlacementFixtures/Modules/TrackerIntegration";

        private const string ThisFile = "Architecture/TrackerIntegrationUnitTestPlacementTest.cs";

        private const string AMadeUpTestFile = "AMadeUpConnectorTest";

        private static readonly string[] BuildOutputFolders = ["bin/", "obj/"];

        // The folders Tracker integration came from, in the production project and, mirrored, in this one. Every file the
        // context had there moved into the module; only the demo data stays in Factories.
        private static readonly string[] FoldersTheContextLeft =
        [
            "Services/Implementation/WorkTrackingConnectors",
            "Services/Interfaces/WorkTrackingConnectors",
            "Services/Implementation/OAuth",
            "Services/Interfaces/OAuth",
            "Services/Implementation/ConnectionHealth",
            "Services/Interfaces/ConnectionHealth",
            "Factories",
        ];

        // Types of the context also sat in shared folders such as Services/Implementation/Dependencies, Parents and
        // BackgroundServices, so a file named after a type that moved is looked for anywhere below these roots.
        private static readonly string[] RootsTheContextLeft = ["Services", "Factories"];

        // The tests of the demo data, which stays in Factories; their names do not follow a type of their own.
        private static readonly string[] DemoDataTestsThatStay =
        [
            "Factories/DemoDataFactoryTest.cs",
            "Factories/DemoDataReferenceIdUniquenessTest.cs",
        ];

        // The Work Items and Forecasting modules emptied these production folders; nothing new belongs in them.
        private static readonly string[] FoldersTheWorkItemsAndForecastSourcesLeft =
        [
            "Services/Implementation/WorkItems",
            "Services/Interfaces/WorkItems",
            "Services/Implementation/TeamData",
            "Services/Interfaces/TeamData",
            "Services/Implementation/WorkItemRules",
            "Services/Interfaces/WorkItemRules",
            "Services/Implementation/Forecast",
            "Services/Interfaces/Forecast",
        ];

        // Each Tracker integration unit-test file, without its .cs, and the module folder of its subject.
        private static readonly (string TestFile, string Folder)[] TrackerIntegrationUnitTestFiles =
        [
            ("ConnectorsWithoutDependenciesTest", "Adapters/Connectors"),
            ("ServiceNowBasicAuthStrategyTest", "Adapters/Connectors/Auth"),
            ("WorkTrackingAuthStrategyTest", "Adapters/Connectors/Auth"),
            ("AzureDevOpsBatchedWriteBackTest", "Adapters/Connectors/AzureDevOps"),
            ("AzureDevOpsBoardReadFailureTest", "Adapters/Connectors/AzureDevOps"),
            ("AzureDevOpsCancellationGranularityTest", "Adapters/Connectors/AzureDevOps"),
            ("AzureDevOpsDependencyRelationTest", "Adapters/Connectors/AzureDevOps"),
            ("AzureDevOpsFetchRefusalTest", "Adapters/Connectors/AzureDevOps"),
            ("AzureDevOpsHistoryReadTest", "Adapters/Connectors/AzureDevOps"),
            ("AzureDevOpsIncrementalSyncTest", "Adapters/Connectors/AzureDevOps"),
            ("AzureDevOpsNamedRelationEndTest", "Adapters/Connectors/AzureDevOps"),
            ("AzureDevOpsRawListsTest", "Adapters/Connectors/AzureDevOps"),
            ("AzureDevOpsSuppressionOutcomeTest", "Adapters/Connectors/AzureDevOps"),
            ("AzureDevOpsWorkTrackingConnectorTest", "Adapters/Connectors/AzureDevOps"),
            ("AzureDevOpsWriteBackTest", "Adapters/Connectors/AzureDevOps"),
            ("CsvCancellationGranularityTest", "Adapters/Connectors/Csv"),
            ("CsvDemoJourneyWithStateMappingTest", "Adapters/Connectors/Csv"),
            ("CsvWorkTrackingConnectorTest", "Adapters/Connectors/Csv"),
            ("IssueExtensionsParentLinkTest", "Adapters/Connectors/Jira"),
            ("IssueFactoryLinkDatesTest", "Adapters/Connectors/Jira"),
            ("IssueFactoryOriginalNamesTest", "Adapters/Connectors/Jira"),
            ("IssueFactoryRawListsTest", "Adapters/Connectors/Jira"),
            ("IssueFactoryTest", "Adapters/Connectors/Jira"),
            ("JiraBatchedWriteBackTest", "Adapters/Connectors/Jira"),
            ("JiraBoardOriginalNamesTest", "Adapters/Connectors/Jira"),
            ("JiraBoardQueryAssemblyTest", "Adapters/Connectors/Jira"),
            ("JiraBoardReadFailureTest", "Adapters/Connectors/Jira"),
            ("JiraCancellationGranularityTest", "Adapters/Connectors/Jira"),
            ("JiraDeliveryForecastPublisherTest", "Adapters/Connectors/Jira"),
            ("JiraDeliverySourceProviderTest", "Adapters/Connectors/Jira"),
            ("JiraDependencyDogfoodTest", "Adapters/Connectors/Jira"),
            ("JiraDependencyLinkTest", "Adapters/Connectors/Jira"),
            ("JiraFieldLookupTest", "Adapters/Connectors/Jira"),
            ("JiraIncrementalSyncTest", "Adapters/Connectors/Jira"),
            ("JiraIssuesPerRequestTest", "Adapters/Connectors/Jira"),
            ("JiraLinkTypeDogfoodTest", "Adapters/Connectors/Jira"),
            ("JiraNamedDependencyLinkTest", "Adapters/Connectors/Jira"),
            ("JiraOriginalNamesLiveTest", "Adapters/Connectors/Jira"),
            ("JiraOriginalNamesRefusedListTest", "Adapters/Connectors/Jira"),
            ("JiraOriginalNamesTest", "Adapters/Connectors/Jira"),
            ("JiraParentLinkDirectionDogfoodTest", "Adapters/Connectors/Jira"),
            ("JiraParentLinkDogfoodTest", "Adapters/Connectors/Jira"),
            ("JiraParentReadingsTest", "Adapters/Connectors/Jira"),
            ("JiraQuietWriteBackTest", "Adapters/Connectors/Jira"),
            ("JiraRefreshOriginalNamesTest", "Adapters/Connectors/Jira"),
            ("JiraRefreshRefusalTest", "Adapters/Connectors/Jira"),
            ("JiraReleaseSweepCostTest", "Adapters/Connectors/Jira"),
            ("JiraReleaseSweepLineTest", "Adapters/Connectors/Jira"),
            ("JiraScopedTokenIntegrationTest", "Adapters/Connectors/Jira"),
            ("JiraSiteAddressTest", "Adapters/Connectors/Jira"),
            ("JiraSuppressionRetryIntegrationTest", "Adapters/Connectors/Jira"),
            ("JiraWorkTrackingConnectorAuthDelegationTest", "Adapters/Connectors/Jira"),
            ("JiraWorkTrackingConnectorTest", "Adapters/Connectors/Jira"),
            ("JiraWriteBackTest", "Adapters/Connectors/Jira"),
            ("LinearCancellationGranularityTest", "Adapters/Connectors/Linear"),
            ("LinearDependencyDogfoodTest", "Adapters/Connectors/Linear"),
            ("LinearDependencyRelationTest", "Adapters/Connectors/Linear"),
            ("LinearFetchRefusalTest", "Adapters/Connectors/Linear"),
            ("LinearWorkTrackingConnectorHistoryParsingTest", "Adapters/Connectors/Linear"),
            ("LinearWorkTrackingConnectorTest", "Adapters/Connectors/Linear"),
            ("OAuthSchemaExtensionsTest", "Adapters/Connectors/OAuth"),
            ("ServiceNowBoardPickerTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowBoardVerdictTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowCancellationGranularityTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowClassLabelsTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowConnectionConfigurationTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowHistoryQueryTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowHistoryVerdictTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowRecordClassTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowStateSpanMapperRawListsTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowStateSpanMapperTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowTeamQueryVerdictTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowTeamSyncTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowTransitionHistoryTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowValidationVerdictTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowWorkItemMapperTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowWorkTrackingConnectorIntegrationTest", "Adapters/Connectors/ServiceNow"),
            ("ServiceNowWorkTrackingConnectorTest", "Adapters/Connectors/ServiceNow"),
            ("AdoOAuthProviderTest", "Adapters/OAuth"),
            ("JiraOAuthProviderTest", "Adapters/OAuth"),
            ("AuthenticationMethodKeysTest", "Application"),
            ("AuthenticationMethodSchemaTest", "Application"),
            ("DependencySourceSelectorTest", "Application"),
            ("IncrementalSourceCapabilityTest", "Application"),
            ("WorkItemCategoryCrossingTest", "Application"),
            ("WorkTrackingConnectorFactoryTest", "Application"),
            ("WorkTrackingSystemFactoryTest", "Application"),
            ("Bug5023JiraRefreshRotationTest", "Application/OAuth"),
            ("OAuthCredentialConcurrentLoadTest", "Application/OAuth"),
            ("OAuthProviderRegistryTest", "Application/OAuth"),
            ("OAuthRefreshSingleFlightTest", "Application/OAuth"),
            ("OAuthServiceTest", "Application/OAuth"),
            ("OAuthStateSecretPersistenceTest", "Application/OAuth"),
            ("OAuthStateTokenIssuerTest", "Application/OAuth"),
            ("DependencyFieldReferencesTest", "Domain"),
            ("ParentSourceSelectorOrderTest", "Domain"),
            ("ParentSourceSelectorOwnParentTest", "Domain"),
            ("ParentSourceSelectorTest", "Domain"),
            ("WorkItemStateTransitionMapperTest", "Domain"),
        ];

        private static readonly Lazy<List<string>> TestSourcePaths = new(() => ReadSourcePaths(TestProjectDirectory, ThisFile));

        private static readonly Lazy<List<string>> ProductionSourcePaths = new(() => ReadSourcePaths(ProductionProjectDirectory, "Program.cs"));

        private static readonly Lazy<List<CompiledClass>> CompiledTestClasses = new(ReadCompiledTestClasses);

        private static IEnumerable<TestCaseData> EveryTrackerIntegrationUnitTestFile()
            => TrackerIntegrationUnitTestFiles.Select(entry => new TestCaseData(entry.TestFile, entry.Folder).SetArgDisplayNames(entry.TestFile, entry.Folder));

        private static IEnumerable<TestCaseData> MadeUpTestTrees()
        {
            const string moved = $"{ModuleFolder}/Adapters/Connectors/Jira/{AMadeUpTestFile}.cs";
            const string leftInTheOldConnectorFolder = $"Services/Implementation/WorkTrackingConnectors/Jira/{AMadeUpTestFile}.cs";
            const string leftInASharedFolder = $"Services/Implementation/Dependencies/{AMadeUpTestFile}.cs";
            const string inAnotherTrackersFolder = $"{ModuleFolder}/Adapters/Connectors/Linear/{AMadeUpTestFile}.cs";
            const string aNeighbour = $"{ModuleFolder}/Adapters/Connectors/Jira/ANeighbourTest.cs";
            const string anIntegrationTest = "API/Integration/AJiraConnectionControllerIntegrationTest.cs";
            const string aDemoDataTest = "Factories/DemoDataFactoryTest.cs";

            yield return new TestCaseData(new[] { leftInTheOldConnectorFolder }, new[] { moved, leftInTheOldConnectorFolder }).SetArgDisplayNames("left in the old connector folder");
            yield return new TestCaseData(new[] { moved, leftInASharedFolder }, new[] { leftInASharedFolder }).SetArgDisplayNames("moved with a copy left in a shared folder");
            yield return new TestCaseData(new[] { inAnotherTrackersFolder }, new[] { moved, inAnotherTrackersFolder }).SetArgDisplayNames("moved into the folder of another tracker");
            yield return new TestCaseData(new[] { moved, aNeighbour, anIntegrationTest, aDemoDataTest }, Array.Empty<string>())
                .SetArgDisplayNames("moved into its module folder, with the integration and demo data tests left where they are");
        }

        private static IEnumerable<TestCaseData> MadeUpTreesAroundTheOldTestFolders()
        {
            const string aNewcomerAmongTheConnectors = "Services/Implementation/WorkTrackingConnectors/Jira/ANewcomerConnectorTest.cs";
            const string aNewcomerAmongTheOAuthInterfaces = "Services/Interfaces/OAuth/ANewcomerOAuthTest.cs";
            const string aNewcomerBesideTheDemoData = "Factories/ANewcomerFactoryTest.cs";
            const string aFolderWithASimilarName = "Services/Implementation/WorkTrackingConnectorsNeighbour/ANeighbourTest.cs";
            const string aStayingRepositoryTest = "Services/Implementation/Repositories/WorkTrackingSystemConnectionRepositoryTest.cs";
            const string anIntegrationTest = "API/Integration/AJiraConnectionControllerIntegrationTest.cs";
            const string aModuleTest = $"{ModuleFolder}/Adapters/Connectors/Jira/ANewcomerConnectorTest.cs";

            yield return new TestCaseData(DemoDataTestsThatStay.Append(aNewcomerAmongTheConnectors).Append(aModuleTest).ToArray(), new[] { aNewcomerAmongTheConnectors })
                .SetArgDisplayNames("a newcomer in the old connector folder");
            yield return new TestCaseData(DemoDataTestsThatStay.Append(aNewcomerAmongTheOAuthInterfaces).ToArray(), new[] { aNewcomerAmongTheOAuthInterfaces })
                .SetArgDisplayNames("a newcomer in the old OAuth interfaces folder");
            yield return new TestCaseData(DemoDataTestsThatStay.Append(aNewcomerBesideTheDemoData).ToArray(), new[] { aNewcomerBesideTheDemoData })
                .SetArgDisplayNames("a newcomer beside the demo data tests");
            yield return new TestCaseData(new[] { DemoDataTestsThatStay[0] }, new[] { DemoDataTestsThatStay[1] })
                .SetArgDisplayNames("a demo data test named as staying that is gone");
            yield return new TestCaseData(DemoDataTestsThatStay.Concat([aFolderWithASimilarName, aStayingRepositoryTest, anIntegrationTest, aModuleTest]).ToArray(), Array.Empty<string>())
                .SetArgDisplayNames("the demo data tests and tests in other folders only");
        }

        private static IEnumerable<TestCaseData> MadeUpSourceTrees()
        {
            const string anInterfaceAmongTheConnectors = "Services/Interfaces/WorkTrackingConnectors/IAMadeUpConnector.cs";
            const string aProviderAmongTheOAuthServices = "Services/Implementation/OAuth/Providers/AMadeUpOAuthProvider.cs";
            const string aProbeAmongTheHealthChecks = "Services/Implementation/ConnectionHealth/AMadeUpHealthProbe.cs";
            const string aNewcomerBesideTheDemoData = "Factories/AMadeUpFactory.cs";
            const string aMovedTypeInASharedFolder = "Services/Implementation/Dependencies/DependencySourceSelector.cs";
            const string theProberInItsOldFolder = "Services/Implementation/BackgroundServices/ConnectionHealthProber.cs";
            const string theDemoDataFactory = "Factories/DemoDataFactory.cs";
            const string theDemoDataPort = "Factories/IDemoDataFactory.cs";
            const string aStayingRepository = "Services/Implementation/Repositories/WorkTrackingSystemConnectionRepository.cs";
            const string theConnectionSave = "Services/Implementation/Connections/ConnectionSaveService.cs";
            const string theSharedExtensionsOfTheSameShortName = "Extensions/WorkItemExtensions.cs";
            const string aFolderWithASimilarName = "Services/Implementation/OAuthNeighbour/AThing.cs";
            const string aModuleConnector = $"{ModuleFolder}/Adapters/Connectors/Jira/JiraWorkTrackingConnector.cs";

            yield return new TestCaseData(new[] { anInterfaceAmongTheConnectors, aModuleConnector }, new[] { anInterfaceAmongTheConnectors }).SetArgDisplayNames("an interface left among the old connector interfaces");
            yield return new TestCaseData(new[] { aProviderAmongTheOAuthServices }, new[] { aProviderAmongTheOAuthServices }).SetArgDisplayNames("a provider left among the old OAuth services");
            yield return new TestCaseData(new[] { aProbeAmongTheHealthChecks }, new[] { aProbeAmongTheHealthChecks }).SetArgDisplayNames("a probe left among the old health checks");
            yield return new TestCaseData(new[] { theDemoDataFactory, aNewcomerBesideTheDemoData }, new[] { aNewcomerBesideTheDemoData }).SetArgDisplayNames("a newcomer beside the demo data");
            yield return new TestCaseData(new[] { aMovedTypeInASharedFolder, theProberInItsOldFolder }, new[] { aMovedTypeInASharedFolder, theProberInItsOldFolder }).SetArgDisplayNames("a moved type left in a shared folder");
            yield return new TestCaseData(
                    new[] { theDemoDataFactory, theDemoDataPort, aStayingRepository, theConnectionSave, theSharedExtensionsOfTheSameShortName, aFolderWithASimilarName, aModuleConnector },
                    Array.Empty<string>())
                .SetArgDisplayNames("the demo data, the staying repositories, the connection save and the module only");
        }

        private static IEnumerable<TestCaseData> MadeUpModuleTestFolders()
        {
            const string inATrackerFolder = $"{ModuleFolder}/Adapters/Connectors/Jira/ATest.cs";
            const string inTheFolderAboveTheTrackers = $"{ModuleFolder}/Adapters/Connectors/ATest.cs";
            const string inAFolderOfNoTracker = $"{ModuleFolder}/Adapters/Connectors/Trello/ATest.cs";
            const string inAHelpersFolder = $"{ModuleFolder}/Helpers/ATest.cs";
            const string atTheModuleRoot = $"{ModuleFolder}/ATest.cs";

            yield return new TestCaseData(new[] { inATrackerFolder, inTheFolderAboveTheTrackers }, Array.Empty<string>()).SetArgDisplayNames("folders that mirror module folders");
            yield return new TestCaseData(new[] { inATrackerFolder, inAFolderOfNoTracker }, new[] { inAFolderOfNoTracker }).SetArgDisplayNames("a folder of a tracker the module does not have");
            yield return new TestCaseData(new[] { inAHelpersFolder, atTheModuleRoot }, new[] { inAHelpersFolder, atTheModuleRoot }).SetArgDisplayNames("a helpers folder and the module root");
        }

        private static IEnumerable<TestCaseData> MadeUpListedFolders()
        {
            const string aDomainTest = $"{ModuleFolder}/Domain/ATest.cs";
            const string anOAuthTest = $"{ModuleFolder}/Application/OAuth/ATest.cs";
            const string aTestOneFolderDown = $"{ModuleFolder}/Application/OAuth/Deeper/ATest.cs";

            yield return new TestCaseData(new[] { aDomainTest, anOAuthTest }, Array.Empty<string>()).SetArgDisplayNames("both listed folders hold a test");
            yield return new TestCaseData(new[] { aDomainTest }, new[] { $"{ModuleFolder}/Application/OAuth" }).SetArgDisplayNames("a listed folder is missing");
            yield return new TestCaseData(new[] { aDomainTest, aTestOneFolderDown }, new[] { $"{ModuleFolder}/Application/OAuth" }).SetArgDisplayNames("a listed folder holds only a subfolder");
        }

        private static IEnumerable<TestCaseData> MadeUpTreesAroundTheOldWorkItemsAndForecastFolders()
        {
            const string aNewcomerAmongTheWorkItems = "Services/Implementation/WorkItems/ANewcomerWorkItemService.cs";
            const string aNewcomerAmongTheForecastInterfaces = "Services/Interfaces/Forecast/IANewcomerForecast.cs";
            const string aNewcomerInASubfolder = "Services/Implementation/TeamData/Refresh/ANewcomerRefresh.cs";
            const string aModuleFile = "Modules/WorkItems/Application/WorkItemService.cs";
            const string aFolderWithASimilarName = "Services/Implementation/WorkItemsNeighbour/AThing.cs";

            yield return new TestCaseData(new[] { aNewcomerAmongTheWorkItems, aModuleFile }, new[] { aNewcomerAmongTheWorkItems }).SetArgDisplayNames("a newcomer in the old Work Items folder");
            yield return new TestCaseData(new[] { aNewcomerAmongTheForecastInterfaces, aNewcomerInASubfolder }, new[] { aNewcomerAmongTheForecastInterfaces, aNewcomerInASubfolder }).SetArgDisplayNames("newcomers in the old Forecast interfaces and a subfolder");
            yield return new TestCaseData(new[] { aModuleFile, aFolderWithASimilarName }, Array.Empty<string>()).SetArgDisplayNames("files in other folders only");
        }

        private static IEnumerable<TestCaseData> MadeUpClassesCompiledFromATestFile()
        {
            const string inItsFolder = $"{TestRootNamespace}.Modules.TrackerIntegration.Adapters.Connectors.Jira.{AMadeUpTestFile}";
            const string aHelperBesideIt = $"{TestRootNamespace}.Modules.TrackerIntegration.Adapters.Connectors.Jira.AHelperBesideIt";
            const string withAStaleNamespace = $"{TestRootNamespace}.Services.Implementation.WorkTrackingConnectors.Jira.{AMadeUpTestFile}";
            const string inAnotherTrackersNamespace = $"{TestRootNamespace}.Modules.TrackerIntegration.Adapters.Connectors.Linear.{AMadeUpTestFile}";
            const string renamed = $"{TestRootNamespace}.Modules.TrackerIntegration.Adapters.Connectors.Jira.ARenamedConnectorTest";

            yield return new TestCaseData(new[] { inItsFolder, aHelperBesideIt }, Array.Empty<string>()).SetArgDisplayNames("kept its name in its folder namespace");
            yield return new TestCaseData(new[] { withAStaleNamespace }, new[] { withAStaleNamespace, inItsFolder }).SetArgDisplayNames("kept a stale namespace");
            yield return new TestCaseData(new[] { inAnotherTrackersNamespace }, new[] { inAnotherTrackersNamespace, inItsFolder }).SetArgDisplayNames("took the namespace of another tracker");
            yield return new TestCaseData(new[] { renamed }, new[] { inItsFolder }).SetArgDisplayNames("renamed its class");
            yield return new TestCaseData(Array.Empty<string>(), new[] { $"{AMadeUpTestFile}.cs" }).SetArgDisplayNames("compiled into no class");
        }

        [TestCaseSource(nameof(EveryTrackerIntegrationUnitTestFile))]
        public void ATrackerIntegrationUnitTestFile_SitsOnlyInItsModuleFolder_AndKeepsItsClassInThatFolder(string testFile, string folder)
        {
            var misplacedFiles = FilePlacementProblems(testFile, folder, TestSourcePaths.Value);
            var misplacedClasses = ClassPlacementProblems(testFile, folder, CompiledTestClasses.Value);

            ThenNothingIsMisplaced(testFile, folder, misplacedFiles.Concat(misplacedClasses));
        }

        [Test]
        public void EveryClassOfTheTrackerIntegrationModuleTests_HasTheNamespaceOfItsFolder()
        {
            var moduleTestClasses = ClassesOfTheModuleTests(CompiledTestClasses.Value, ModuleFolder);

            Assert.That(
                moduleTestClasses,
                Is.Not.Empty,
                $"The compiled tests hold no class from a file under {ModuleFolder} or in its namespace. Why it matters: the " +
                "Tracker integration unit tests belong next to the module, and a namespace rule over nothing passes without " +
                $"looking at anything. How to fix: move the Tracker integration unit-test files under {ModuleFolder} with git mv.");

            ThenEachClassHasTheNamespaceOfItsFolder(moduleTestClasses);
        }

        [Test]
        public void EveryTrackerIntegrationModuleTestFolder_MirrorsAFolderOfTheModule()
        {
            var unmirrored = TestFilesInFoldersThatMirrorNoModuleFolder(TestSourcePaths.Value, ProductionSourcePaths.Value);

            Assert.That(
                unmirrored,
                Is.Empty,
                $"These test files sit in a folder under {ModuleFolder} that the module itself does not have: " +
                $"[{string.Join(", ", unmirrored)}]. Why it matters: a unit test sits in the folder that mirrors the module folder of " +
                "its subject, so a reader finds the tests of a type beside the path of the type. How to fix: git mv the file into " +
                "the folder that mirrors its subject's file and change its namespace to match.");
        }

        [Test]
        public void EveryListedTrackerIntegrationModuleTestFolder_HoldsATestFile()
        {
            var emptyOrMissing = EmptyOrMissingFolders(ListedModuleTestFolders(), TestSourcePaths.Value);

            Assert.That(
                emptyOrMissing,
                Is.Empty,
                $"These module test folders hold no test file: [{string.Join(", ", emptyOrMissing)}]. Why it matters: a placement " +
                "rule over an empty or missing folder passes without looking at anything, so the tests that should sit there " +
                "are somewhere else. How to fix: move the unit tests of that folder's types into it with git mv, or take the " +
                "folder's files off the list when the tests were deliberately removed.");
        }

        [Test]
        public void TheOldTrackerIntegrationTestFolders_HoldNoTestFile_ApartFromTheDemoDataTests()
        {
            var problems = TestFilesLeftInTheOldFolders(TestSourcePaths.Value);

            Assert.That(
                problems,
                Is.Empty,
                $"[{string.Join("; ", problems)}]. Why it matters: [{string.Join(", ", FoldersTheContextLeft)}] held the Tracker " +
                "integration tests, so a test that sits there, moved or new, is a Tracker integration test outside the module, " +
                "and a demo data test named as staying that is gone leaves an exception that covers nothing. How to fix: git mv " +
                $"each test into the folder mirroring its subject under {ModuleFolder}, or take a gone demo data test off the list.");
        }

        [Test]
        public void TheOldTrackerIntegrationSourceFolders_HoldNoFileOfTheContext_ApartFromTheTypesThatStay()
        {
            var leftBehind = SourceFilesTheContextLeftBehind(ProductionSourcePaths.Value);

            Assert.That(
                leftBehind,
                Is.Empty,
                $"These source files of Tracker integration are left outside the module: [{string.Join(", ", leftBehind)}]. Why it " +
                $"matters: every type of the context lives under {ModuleFolder} apart from the types named as staying, and a file " +
                $"left in [{string.Join(", ", FoldersTheContextLeft)}] or named after a type that moved splits the context again. " +
                $"How to fix: git mv the file into its module folder under {ModuleFolder}, or, when the type deliberately stays, " +
                "name it among the staying types of the Tracker integration membership.");
        }

        [Test]
        public void TheOldWorkItemsAndForecastSourceFolders_HoldNoSourceFile()
        {
            var leftOver = FilesInsideAny(ProductionSourcePaths.Value, FoldersTheWorkItemsAndForecastSourcesLeft);

            Assert.That(
                leftOver,
                Is.Empty,
                $"[{string.Join(", ", FoldersTheWorkItemsAndForecastSourcesLeft)}] still hold [{string.Join(", ", leftOver)}]. Why it " +
                "matters: the Work Items and Forecasting modules emptied these folders, and a file there lives outside every module. " +
                "How to fix: git mv the file into the layer folder of its module under Modules.");
        }

        [TestCaseSource(nameof(MadeUpTestTrees))]
        public void AMadeUpTestFileOutsideItsModuleFolder_IsReported_AndOneThatSitsThereIsNot(string[] sourcePaths, string[] expectedReportedPaths)
        {
            var problems = FilePlacementProblems(AMadeUpTestFile, "Adapters/Connectors/Jira", sourcePaths);

            Assert.That(
                problems.Select(problem => problem.Path),
                Is.EquivalentTo(expectedReportedPaths),
                $"For the test tree [{string.Join(", ", sourcePaths)}] the placement rule reported [{string.Join("; ", problems)}]. " +
                "Why it matters: a test file left in an old folder, a copy left behind or a file in another tracker's folder must " +
                "each be named, and a file in its module folder must not be, nor the integration and demo data tests that stay " +
                "where they are. How to fix: report the expected path when it holds no file, and every other path that holds a " +
                "file of the same name.");
        }

        [TestCaseSource(nameof(MadeUpTreesAroundTheOldTestFolders))]
        public void AMadeUpTestFileInAnOldTrackerIntegrationFolder_IsReported_AndTheDemoDataTestsAreNot(string[] sourcePaths, string[] expectedReportedPaths)
        {
            var problems = TestFilesLeftInTheOldFolders(sourcePaths);

            Assert.That(
                problems.Select(problem => problem.Path),
                Is.EquivalentTo(expectedReportedPaths),
                $"For the test tree [{string.Join(", ", sourcePaths)}] the rule reported [{string.Join("; ", problems)}]. Why it " +
                $"matters: a test written into one of [{string.Join(", ", FoldersTheContextLeft)}] or below it is a Tracker " +
                "integration test outside the module and must be named, a demo data test named as staying must not be while it " +
                "exists and must be named when it is gone, and tests in other folders, including one whose name only starts the " +
                "same, must not be. How to fix: report every path inside those folders except the demo data tests, and every " +
                "demo data test that holds no file.");
        }

        [TestCaseSource(nameof(MadeUpSourceTrees))]
        public void AMadeUpSourceFileOfTheContextLeftInAnOldFolder_IsReported_AndATypeThatStaysIsNot(string[] sourcePaths, string[] expectedReportedPaths)
        {
            var leftBehind = SourceFilesTheContextLeftBehind(sourcePaths);

            Assert.That(
                leftBehind,
                Is.EquivalentTo(expectedReportedPaths),
                $"For the source tree [{string.Join(", ", sourcePaths)}] the rule reported [{string.Join(", ", leftBehind)}]. Why it " +
                "matters: a file in an old connector, OAuth, connection-health or factories folder, or a file named after a moved " +
                "type in a shared folder, is the context left outside the module and must be named, while the demo data, the " +
                "staying repositories, the connection save and a shared type that only shares a short name must not be. How to " +
                "fix: report every file in those folders and every file under Services, Factories or Models named after a type " +
                "that moved, unless its type is named as staying.");
        }

        [TestCaseSource(nameof(MadeUpModuleTestFolders))]
        public void AMadeUpModuleTestFolderThatMirrorsNoModuleFolder_IsReported_AndAMirroringOneIsNot(string[] testPaths, string[] expectedReportedPaths)
        {
            string[] productionPaths =
            [
                $"{ModuleFolder}/Adapters/Connectors/Jira/JiraWorkTrackingConnector.cs",
                $"{ModuleFolder}/Domain/ParentSourceSelector.cs",
            ];

            var unmirrored = TestFilesInFoldersThatMirrorNoModuleFolder(testPaths, productionPaths);

            Assert.That(
                unmirrored,
                Is.EquivalentTo(expectedReportedPaths),
                $"For the tests [{string.Join(", ", testPaths)}] beside the module files [{string.Join(", ", productionPaths)}] the " +
                $"rule reported [{string.Join(", ", unmirrored)}]. Why it matters: a test in a folder the module does not have, or " +
                "at the module root, hides which type it covers, and a test in a module folder or a folder above one must pass. " +
                "How to fix: take every folder that holds a module file, and the folders above it inside the module, as mirrored.");
        }

        [TestCaseSource(nameof(MadeUpListedFolders))]
        public void AnEmptyOrMissingListedModuleTestFolder_IsReported_AndOneHoldingATestIsNot(string[] testPaths, string[] expectedReportedFolders)
        {
            string[] listedFolders = [$"{ModuleFolder}/Domain", $"{ModuleFolder}/Application/OAuth"];

            var emptyOrMissing = EmptyOrMissingFolders(listedFolders, testPaths);

            Assert.That(
                emptyOrMissing,
                Is.EquivalentTo(expectedReportedFolders),
                $"For the listed folders [{string.Join(", ", listedFolders)}] and the tests [{string.Join(", ", testPaths)}] the rule " +
                $"reported [{string.Join(", ", emptyOrMissing)}]. Why it matters: a listed folder that holds no test of its own " +
                "lets a placement rule pass over nothing. How to fix: report every listed folder that holds no file directly.");
        }

        [TestCaseSource(nameof(MadeUpTreesAroundTheOldWorkItemsAndForecastFolders))]
        public void AMadeUpSourceFileInAnOldWorkItemsOrForecastFolder_IsReported_AndOneElsewhereIsNot(string[] sourcePaths, string[] expectedReportedPaths)
        {
            var leftOver = FilesInsideAny(sourcePaths, FoldersTheWorkItemsAndForecastSourcesLeft);

            Assert.That(
                leftOver,
                Is.EquivalentTo(expectedReportedPaths),
                $"For the source tree [{string.Join(", ", sourcePaths)}] the rule reported [{string.Join(", ", leftOver)}]. Why it " +
                "matters: a file in an old Work Items or Forecast folder, or below one, lives outside every module and must be " +
                "named, while files in other folders, including one whose name only starts the same, must not be. How to fix: " +
                "report every path inside those folders and no other.");
        }

        [TestCaseSource(nameof(MadeUpClassesCompiledFromATestFile))]
        public void AMadeUpTestFileWhoseClassLostItsNameOrFolderNamespace_IsReported_AndOneThatKeptThemIsNot(string[] classFullNames, string[] expectedReported)
        {
            const string theFile = $"{ModuleFolder}/Adapters/Connectors/Jira/{AMadeUpTestFile}.cs";
            var classes = classFullNames
                .Select(fullName => new CompiledClass(fullName, fullName[..fullName.LastIndexOf('.')], [theFile]))
                .ToList();

            var problems = ClassPlacementProblems(AMadeUpTestFile, "Adapters/Connectors/Jira", classes);

            Assert.That(
                problems.Select(problem => problem.Path),
                Is.EquivalentTo(expectedReported),
                $"For the classes [{string.Join(", ", classFullNames)}] compiled from {theFile} the rule reported " +
                $"[{string.Join("; ", problems)}]. Why it matters: a moved test keeps its class name and takes the namespace of its " +
                "folder, so a test filter and a reader still find it under the same name; a stale namespace, another tracker's " +
                "namespace, a renamed class or a file that compiles into nothing must each be named, and a helper beside the test " +
                "must not be. How to fix: report each class out of the folder namespace, the expected class name when no class " +
                "carries it, and the file when it compiles into no class.");
        }

        [Test]
        public void AMadeUpClassDeclaredInTheModuleNamespaceOutsideItsFolder_IsReported_AndItsCompliantTwinIsNot()
        {
            var classesInThisFile = ClassesCompiledFrom(CompiledTestClasses.Value, path => path == ThisFile);
            var madeUpModuleClasses = ClassesOfTheModuleTests(CompiledTestClasses.Value, MadeUpModuleTestFolder);

            var mismatches = NamespaceFolderMismatches(classesInThisFile);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    classesInThisFile.Select(compiled => compiled.FullName),
                    Does.Contain(typeof(ACompliantTwinOfTheTrackerIntegrationPlacementRule).FullName).And.Contain(typeof(ATrackerIntegrationTestDeclaredOutsideItsFolder).FullName),
                    $"The compiled tests do not tie both made-up classes to {ThisFile}. Why it matters: a twin the rule never looked " +
                    "at proves nothing by passing. How to fix: read every class with the files its methods were compiled from.");

                Assert.That(
                    madeUpModuleClasses.Select(compiled => compiled.FullName),
                    Is.EqualTo(new[] { typeof(ATrackerIntegrationTestDeclaredOutsideItsFolder).FullName }),
                    $"The classes taken as tests of the made-up module {MadeUpModuleTestFolder} were " +
                    $"[{string.Join(", ", madeUpModuleClasses.Select(compiled => compiled.FullName))}]. Why it matters: a newcomer " +
                    "declared in the module's namespace from a file outside the module folder must still be looked at, or it slips " +
                    "past the folder rule. How to fix: take every class that is compiled from a file under the module folder or " +
                    "declared in the module's namespace.");

                Assert.That(
                    mismatches.Select(mismatch => mismatch.FullName),
                    Is.EqualTo(new[] { typeof(ATrackerIntegrationTestDeclaredOutsideItsFolder).FullName }),
                    $"Expected exactly {typeof(ATrackerIntegrationTestDeclaredOutsideItsFolder).FullName} to be reported, got " +
                    $"[{string.Join("; ", mismatches)}]. Why it matters: a class whose namespace names a module folder while its file " +
                    "sits elsewhere hides where the test really lives, and a class whose namespace matches its folder must pass, or " +
                    "the rule flags everything. How to fix: compare each class's namespace with the namespace its folder gives.");
            }
        }

        private static List<Misplacement> FilePlacementProblems(string testFile, string folder, IReadOnlyCollection<string> sourcePaths)
        {
            var fileName = $"{testFile}.cs";
            var expectedPath = $"{ModuleFolder}/{folder}/{fileName}";
            var problems = new List<Misplacement>();

            if (!sourcePaths.Contains(expectedPath, StringComparer.Ordinal))
            {
                problems.Add(new Misplacement(expectedPath, "holds no such file"));
            }

            problems.AddRange(sourcePaths
                .Where(path => FileNameOf(path) == fileName && path != expectedPath)
                .Select(path => new Misplacement(path, "still holds the file")));

            return problems;
        }

        private static List<Misplacement> ClassPlacementProblems(string testFile, string folder, IReadOnlyCollection<CompiledClass> classes)
        {
            var fileName = $"{testFile}.cs";
            var expectedNamespace = NamespaceOfFolder($"{ModuleFolder}/{folder}");
            var expectedClass = $"{expectedNamespace}.{testFile}";
            var fromTheFile = ClassesCompiledFrom(classes, path => FileNameOf(path) == fileName);

            if (fromTheFile.Count == 0)
            {
                return [new Misplacement(fileName, "was compiled into no class")];
            }

            var problems = fromTheFile
                .Where(compiled => compiled.Namespace != expectedNamespace)
                .Select(compiled => new Misplacement(compiled.FullName, $"lives in namespace {compiled.Namespace} instead of {expectedNamespace}"))
                .ToList();

            if (!fromTheFile.Any(compiled => compiled.FullName == expectedClass))
            {
                problems.Add(new Misplacement(expectedClass, $"is not a class compiled from {fileName}"));
            }

            return problems;
        }

        private static List<Misplacement> TestFilesLeftInTheOldFolders(IReadOnlyCollection<string> testPaths)
        {
            var leftBehind = FilesInsideAny(testPaths, FoldersTheContextLeft)
                .Where(path => !DemoDataTestsThatStay.Contains(path, StringComparer.Ordinal))
                .Select(path => new Misplacement(path, "is a test file left in a folder Tracker integration left"));

            var goneExceptions = DemoDataTestsThatStay
                .Where(path => !testPaths.Contains(path, StringComparer.Ordinal))
                .Select(path => new Misplacement(path, "is named as a staying demo data test but holds no file"));

            return [.. leftBehind.Concat(goneExceptions)];
        }

        private static List<string> SourceFilesTheContextLeftBehind(IEnumerable<string> productionPaths)
        {
            var typesThatMoved = TrackerIntegrationMembership.Moves.Concat(TrackerIntegrationMembership.ModelsTypesThatMove).ToHashSet(StringComparer.Ordinal);
            string[] rootsTheMovedTypesLeft = [.. RootsTheContextLeft, "Models"];

            return [.. productionPaths.Where(path =>
                !TrackerIntegrationMembership.Stays.ContainsKey(TypeNameOf(path))
                && (IsInsideAny(path, FoldersTheContextLeft)
                    || (IsInsideAny(path, rootsTheMovedTypesLeft) && typesThatMoved.Contains(TypeNameOf(path)))))];
        }

        private static List<string> TestFilesInFoldersThatMirrorNoModuleFolder(IEnumerable<string> testPaths, IEnumerable<string> productionPaths)
        {
            var moduleFolders = productionPaths
                .Where(path => path.StartsWith($"{ModuleFolder}/", StringComparison.Ordinal))
                .SelectMany(path => FolderAndItsParentsInsideTheModule(FolderOf(path)))
                .ToHashSet(StringComparer.Ordinal);

            return [.. testPaths
                .Where(path => path.StartsWith($"{ModuleFolder}/", StringComparison.Ordinal))
                .Where(path => !moduleFolders.Contains(FolderOf(path)))];
        }

        private static IEnumerable<string> FolderAndItsParentsInsideTheModule(string folder)
        {
            for (var current = folder; current.StartsWith($"{ModuleFolder}/", StringComparison.Ordinal); current = FolderOf(current))
            {
                yield return current;
            }
        }

        private static string[] ListedModuleTestFolders()
            => [.. TrackerIntegrationUnitTestFiles.Select(entry => $"{ModuleFolder}/{entry.Folder}").Distinct(StringComparer.Ordinal)];

        private static List<string> EmptyOrMissingFolders(IEnumerable<string> listedFolders, IReadOnlyCollection<string> paths)
            => [.. listedFolders.Where(folder => !paths.Any(path => FolderOf(path) == folder))];

        private static List<string> FilesInsideAny(IEnumerable<string> paths, IReadOnlyCollection<string> folders)
            => [.. paths.Where(path => IsInsideAny(path, folders))];

        private static bool IsInsideAny(string path, IEnumerable<string> folders)
            => folders.Any(folder => path.StartsWith($"{folder}/", StringComparison.Ordinal));

        private static List<CompiledClass> ClassesOfTheModuleTests(IEnumerable<CompiledClass> classes, string moduleFolder)
        {
            var moduleNamespace = NamespaceOfFolder(moduleFolder);

            return [.. classes.Where(compiled =>
                compiled.Files.Any(path => path.StartsWith($"{moduleFolder}/", StringComparison.Ordinal))
                || compiled.Namespace == moduleNamespace
                || compiled.Namespace.StartsWith($"{moduleNamespace}.", StringComparison.Ordinal))];
        }

        private static List<NamespaceFolderMismatch> NamespaceFolderMismatches(IEnumerable<CompiledClass> classes)
            => [.. classes.SelectMany(compiled => compiled.Files
                .Select(path => (Path: path, FolderNamespace: NamespaceOfFolder(FolderOf(path))))
                .Where(file => file.FolderNamespace != compiled.Namespace)
                .Select(file => new NamespaceFolderMismatch(compiled.FullName, compiled.Namespace, file.Path, file.FolderNamespace)))];

        private static void ThenNothingIsMisplaced(string testFile, string folder, IEnumerable<Misplacement> problems)
        {
            var found = problems.ToList();

            Assert.That(
                found,
                Is.Empty,
                $"{testFile} should sit only in {ModuleFolder}/{folder} with its class {testFile} and every other class in it in " +
                $"namespace {NamespaceOfFolder($"{ModuleFolder}/{folder}")}; instead: [{string.Join("; ", found)}]. Why it matters: " +
                "the Tracker integration unit tests sit next to the module only when each one is in the folder mirroring its " +
                "subject, with the namespace of that folder, its class name kept and no copy left in an old place. How to fix: " +
                "git mv the file into that folder and change its namespace and using lines; keep the class names.");
        }

        private static void ThenEachClassHasTheNamespaceOfItsFolder(IEnumerable<CompiledClass> classes)
        {
            var mismatches = NamespaceFolderMismatches(classes);

            Assert.That(
                mismatches,
                Is.Empty,
                $"These classes do not have the namespace of the folder their file sits in: [{string.Join("; ", mismatches)}]. " +
                "Why it matters: a reader and a test filter find a test by its namespace, so a namespace that disagrees with the " +
                "folder hides where the test lives. How to fix: move the file into the folder its namespace names, or change the " +
                "namespace line to the folder's namespace.");
        }

        private static List<CompiledClass> ClassesCompiledFrom(IEnumerable<CompiledClass> classes, Func<string, bool> isWanted)
            => [.. classes.Where(compiled => compiled.Files.Any(isWanted))];

        private static string NamespaceOfFolder(string folder)
            => folder.Length == 0 ? TestRootNamespace : $"{TestRootNamespace}.{folder.Replace('/', '.')}";

        private static string FolderOf(string path)
        {
            var lastSlash = path.LastIndexOf('/');
            return lastSlash < 0 ? string.Empty : path[..lastSlash];
        }

        private static string FileNameOf(string path) => path[(path.LastIndexOf('/') + 1)..];

        // A file named Foo.cs or Foo.Part.cs holds the type Foo.
        private static string TypeNameOf(string path) => FileNameOf(path).Split('.')[0];

        private static List<string> ReadSourcePaths(string projectDirectory, string aFileItHolds)
        {
            var projectRoot = Path.Combine(SolutionRoot(), projectDirectory);

            var paths = Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(projectRoot, file).Replace('\\', '/'))
                .Where(path => !IsBuildOutput(path))
                .Order(StringComparer.Ordinal)
                .ToList();

            Assert.That(paths, Does.Contain(aFileItHolds), $"The source scan of {projectRoot} did not find {aFileItHolds}, so it is not looking at that project.");
            return paths;
        }

        private static List<CompiledClass> ReadCompiledTestClasses()
        {
            var assemblyPath = typeof(TrackerIntegrationUnitTestPlacementTest).Assembly.Location;
            using var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters { InMemory = true, ReadSymbols = true });

            var classes = module.Types
                .Where(type => !type.Name.StartsWith('<'))
                .Select(type => new CompiledClass(type.FullName, type.Namespace, FilesOf(type)))
                .Where(compiled => compiled.Files.Count > 0)
                .ToList();

            Assert.That(
                ClassesCompiledFrom(classes, path => path == ThisFile).Select(compiled => compiled.FullName),
                Does.Contain(typeof(TrackerIntegrationUnitTestPlacementTest).FullName),
                $"The symbols of {assemblyPath} did not tie this very class to {ThisFile}, so no class could be tied to its file.");
            return classes;
        }

        // The files a class was compiled from, read from the symbols of its methods and of the types nested in it, such
        // as the state machines the compiler writes for async test methods.
        private static List<string> FilesOf(TypeDefinition type)
            => [.. TypeAndItsNestedTypes(type)
                .SelectMany(each => each.Methods)
                .Where(method => method.DebugInformation.HasSequencePoints)
                .SelectMany(method => method.DebugInformation.SequencePoints)
                .Select(point => TestProjectRelativePath(point.Document.Url))
                .OfType<string>()
                .Where(path => !IsBuildOutput(path))
                .Distinct(StringComparer.Ordinal)];

        private static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
            => new[] { type }.Concat(type.NestedTypes.SelectMany(TypeAndItsNestedTypes));

        private static string? TestProjectRelativePath(string documentPath)
        {
            var normalised = documentPath.Replace('\\', '/');
            var marker = $"/{TestProjectDirectory}/";
            var start = normalised.LastIndexOf(marker, StringComparison.Ordinal);
            return start < 0 ? null : normalised[(start + marker.Length)..];
        }

        private static bool IsBuildOutput(string path) => BuildOutputFolders.Any(folder => path.StartsWith(folder, StringComparison.Ordinal));

        private static string SolutionRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the source scans.");
            return directory!.FullName;
        }

        private sealed record CompiledClass(string FullName, string Namespace, IReadOnlyList<string> Files);

        private sealed record Misplacement(string Path, string What)
        {
            public override string ToString() => $"{Path} {What}";
        }

        private sealed record NamespaceFolderMismatch(string FullName, string Namespace, string Path, string FolderNamespace)
        {
            public override string ToString() => $"{FullName} is in namespace {Namespace} but its file {Path} sits in the folder of namespace {FolderNamespace}";
        }
    }

    internal static class ACompliantTwinOfTheTrackerIntegrationPlacementRule
    {
        public static int Answer() => 42;
    }
}

namespace Lighthouse.Backend.Tests.PlacementFixtures.Modules.TrackerIntegration.Domain
{
    internal static class ATrackerIntegrationTestDeclaredOutsideItsFolder
    {
        public static int Answer() => 42;
    }
}
