using Lighthouse.Backend.Tests.PlacementFixtures.Platform;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The unit tests of the platform lane sit under Platform/&lt;Area&gt; in this project, in the namespace of that folder,
    /// and no production file is left in Services/Interfaces or Services/Implementation unless it is named, file by file,
    /// with its owner. A platform type is a type of the lane's membership map or any type in the production Platform
    /// namespace; a unit test of it is a class named after it and ending in Test or Tests. Where a test lives is read from
    /// the symbols of the compiled test assembly, which say which file each class was compiled from, so the namespace a
    /// class claims cannot hide where its file sits. The tests under the API, Acceptance, Architecture, Contracts and
    /// Integration roots go through a host or the compiled code as a whole and are not judged. Both lists are compared
    /// both ways: a file that is not on its list is named, and an entry whose file is gone, or that no longer needs to be
    /// there, is named too. Each rule also runs on made-up trees.
    /// </summary>
    [TestFixture]
    public class PlatformLaneUnitTestPlacementTest
    {
        private const string PlatformTestFolder = "Platform";

        private const string ThisFile = "Architecture/PlatformLaneUnitTestPlacementTest.cs";

        private const string TheFixtureFile = "PlacementFixtures/Platform/PlatformPlacementFixtures.cs";

        private const string AMadeUpArea = "Lanterns";

        private static readonly string[] BuildOutputFolders = ["bin/", "obj/"];

        private static readonly string[] TestRootsNotJudged = ["API", "Acceptance", "Architecture", "Contracts", "Integration"];

        private static readonly string[] FoldersWithStayingProductionFiles = ["Services/Interfaces", "Services/Implementation"];

        // Test files outside the Platform folders that test a platform type and stay where they are, each with why.
        private static readonly (string Path, string Reason)[] TestFilesThatStay =
        [
        ];

        // Every production file left in Services/Interfaces and Services/Implementation, each with the owner that keeps it.
        private static readonly (string Path, string Owner)[] ProductionFilesThatStay =
        [
            ("Services/Implementation/AppSettingService.cs", "Organisation set-up"),
            ("Services/Implementation/BackgroundServices/Update/PortfolioUpdater.cs", "The update pipelines"),
            ("Services/Implementation/BackgroundServices/Update/TeamUpdater.cs", "The update pipelines"),
            ("Services/Implementation/BackgroundServices/Update/UpdateServiceBase.cs", "The update pipelines"),
            ("Services/Implementation/Connections/ChangedConnectionDefault.cs", "Organisation set-up"),
            ("Services/Implementation/Connections/ConnectionSaveService.cs", "Organisation set-up"),
            ("Services/Implementation/EstimateNormalizer.cs", "Work Items"),
            ("Services/Implementation/FeatureListActiveWork.cs", "Work Items"),
            ("Services/Implementation/FeatureListOwners.cs", "Work Items"),
            ("Services/Implementation/FeatureListService.cs", "Work Items"),
            ("Services/Implementation/FeaturePositionMap.cs", "Organisation set-up"),
            ("Services/Implementation/InProgressFeatureResolution.cs", "Work Items"),
            ("Services/Implementation/OrphanedFeatureCleanupService.cs", "Work Items"),
            ("Services/Implementation/Repositories/ApiKeyPermissionRepository.cs", "Access, its persistence"),
            ("Services/Implementation/Repositories/ApiKeyRepository.cs", "Access, its persistence"),
            ("Services/Implementation/Repositories/AppSettingRepository.cs", "Organisation set-up, its persistence"),
            ("Services/Implementation/Repositories/BlackoutPeriodRepository.cs", "Organisation set-up, its persistence"),
            ("Services/Implementation/Repositories/ConnectionHealthVerdictRepository.cs", "Tracker integration, its persistence"),
            ("Services/Implementation/Repositories/DeliveryMetricSnapshotRepository.cs", "Promises, its persistence"),
            ("Services/Implementation/Repositories/DeliveryRepository.cs", "Promises, its persistence"),
            ("Services/Implementation/Repositories/EmbedSessionTokenRepository.cs", "Access, its persistence"),
            ("Services/Implementation/Repositories/FeatureBlockedTransitionRepository.cs", "Work Items, its persistence"),
            ("Services/Implementation/Repositories/FeatureRepository.cs", "Work Items, its persistence"),
            ("Services/Implementation/Repositories/FeatureStateTransitionRepository.cs", "Work Items, its persistence"),
            ("Services/Implementation/Repositories/LicenseInformationRepository.cs", "Licensing, its persistence"),
            ("Services/Implementation/Repositories/OAuthCredentialRepository.cs", "Tracker integration, its persistence"),
            ("Services/Implementation/Repositories/OptionalFeatureRepository.cs", "Organisation set-up, its persistence"),
            ("Services/Implementation/Repositories/PortfolioRepository.cs", "Organisation set-up, its persistence"),
            ("Services/Implementation/Repositories/RecurringBlackoutRuleRepository.cs", "Organisation set-up, its persistence"),
            ("Services/Implementation/Repositories/RefreshLogRepository.cs", "Work Items, its persistence"),
            ("Services/Implementation/Repositories/ReportRepository.cs", "Reports, its persistence"),
            ("Services/Implementation/Repositories/RepositoryBase.cs", "The repository base every context's persistence builds on"),
            ("Services/Implementation/Repositories/SizingLogRepository.cs", "Refinement, its persistence"),
            ("Services/Implementation/Repositories/TeamRepository.cs", "Organisation set-up, its persistence"),
            ("Services/Implementation/Repositories/TerminologyRepository.cs", "Organisation set-up, its persistence"),
            ("Services/Implementation/Repositories/UsageDataConsentRepository.cs", "Platform usage data, its persistence adapter kept with the repositories"),
            ("Services/Implementation/Repositories/UserProfileLookup.cs", "Access, its persistence"),
            ("Services/Implementation/Repositories/UserProfileRepository.cs", "Access, its persistence"),
            ("Services/Implementation/Repositories/WorkItemBlockedTransitionRepository.cs", "Work Items, its persistence"),
            ("Services/Implementation/Repositories/WorkItemRepository.cs", "Work Items, its persistence"),
            ("Services/Implementation/Repositories/WorkItemStateTransitionRepository.cs", "Work Items, its persistence"),
            ("Services/Implementation/Repositories/WorkTrackingSystemConnectionRepository.cs", "Tracker integration, its persistence"),
            ("Services/Implementation/Seeding/ApiKeyOwnerReconciliationSeeder.cs", "Access"),
            ("Services/Implementation/Seeding/AppSettingSeeder.cs", "Organisation set-up"),
            ("Services/Implementation/Seeding/CategoryListNormalisationSeeder.cs", "Organisation set-up"),
            ("Services/Implementation/Seeding/EveryoneSignedInSeeder.cs", "Access"),
            ("Services/Implementation/Seeding/OptionalFeatureSeeder.cs", "Organisation set-up"),
            ("Services/Implementation/Seeding/RefreshLogSeeder.cs", "Work Items"),
            ("Services/Implementation/Seeding/TerminologySeeder.cs", "Organisation set-up"),
            ("Services/Implementation/TerminologyService.cs", "Organisation set-up"),
            ("Services/Implementation/WorkTrackingSystemConnectionUsage.cs", "Organisation set-up"),
            ("Services/Implementation/WriteBackCollector.cs", "Tracker integration, the write-back round"),
            ("Services/Implementation/WriteBackRound.cs", "Tracker integration, the write-back round"),
            ("Services/Implementation/WriteBackRoundContext.cs", "Tracker integration, the write-back round"),
            ("Services/Implementation/WriteBackService.cs", "Tracker integration, the write-back round"),
            ("Services/Implementation/WriteBackTriggerService.cs", "Tracker integration, the write-back round"),
            ("Services/Interfaces/IAppSettingService.cs", "Organisation set-up"),
            ("Services/Interfaces/IConnectionSaveService.cs", "Organisation set-up"),
            ("Services/Interfaces/IFeatureListService.cs", "Work Items"),
            ("Services/Interfaces/IFeaturePositionMap.cs", "Organisation set-up"),
            ("Services/Interfaces/IOrphanedFeatureCleanupService.cs", "Work Items"),
            ("Services/Interfaces/ITerminologyService.cs", "Organisation set-up"),
            ("Services/Interfaces/IWorkTrackingSystemConnectionUsage.cs", "Organisation set-up"),
            ("Services/Interfaces/IWriteBackCollector.cs", "Tracker integration, the write-back round"),
            ("Services/Interfaces/IWriteBackService.cs", "Tracker integration, the write-back round"),
            ("Services/Interfaces/IWriteBackTriggerService.cs", "Tracker integration, the write-back round"),
            ("Services/Interfaces/Repositories/IApiKeyRepository.cs", "Access, its persistence"),
            ("Services/Interfaces/Repositories/IDeliveryMetricSnapshotRepository.cs", "Promises, its persistence"),
            ("Services/Interfaces/Repositories/IDeliveryRepository.cs", "Promises, its persistence"),
            ("Services/Interfaces/Repositories/IEmbedSessionTokenRepository.cs", "Access, its persistence"),
            ("Services/Interfaces/Repositories/IFeatureBlockedTransitionRepository.cs", "Work Items, its persistence"),
            ("Services/Interfaces/Repositories/IFeatureListReader.cs", "Work Items, its persistence"),
            ("Services/Interfaces/Repositories/IFeatureRepository.cs", "Work Items, its persistence"),
            ("Services/Interfaces/Repositories/IFeatureStateTransitionRepository.cs", "Work Items, its persistence"),
            ("Services/Interfaces/Repositories/IPortfolioRepository.cs", "Organisation set-up, its persistence"),
            ("Services/Interfaces/Repositories/IRepository.cs", "The repository base every context's persistence builds on"),
            ("Services/Interfaces/Repositories/ISizingLogRepository.cs", "Refinement, its persistence"),
            ("Services/Interfaces/Repositories/IUsageDataConsentRepository.cs", "Platform usage data, its persistence adapter kept with the repositories"),
            ("Services/Interfaces/Repositories/IUserProfileLookup.cs", "Access, its persistence"),
            ("Services/Interfaces/Repositories/IWorkItemBlockedTransitionRepository.cs", "Work Items, its persistence"),
            ("Services/Interfaces/Repositories/IWorkItemRepository.cs", "Work Items, its persistence"),
            ("Services/Interfaces/Repositories/IWorkItemStateTransitionRepository.cs", "Work Items, its persistence"),
            ("Services/Interfaces/Repositories/RecordableDeliveries.cs", "Promises, its persistence"),
            ("Services/Interfaces/Update/IPortfolioUpdater.cs", "The update pipelines"),
            ("Services/Interfaces/Update/ITeamUpdater.cs", "The update pipelines"),
            ("Services/Interfaces/Update/IUpdateService.cs", "The update pipelines"),
        ];

        private static readonly Lazy<List<string>> TestFilesOnDisk = new(ReadTestFilesOnDisk);

        private static readonly Lazy<List<CompiledClass>> CompiledTestClasses = new(ReadCompiledTestClasses);

        private static readonly Lazy<List<PlatformSubject>> PlatformTypes = new(ReadPlatformTypes);

        private static readonly Lazy<List<string>> ProductionFilesInTheOldFolders = new(() =>
            [.. ProductionSource.Files().Select(file => file.RelativePath).Where(IsInAnOldServicesFolder)]);

        private static readonly PlatformSubject TheMadeUpSubject = new(nameof(AMadeUpLanternKeeper), AMadeUpArea);

        private static IEnumerable<TestCaseData> MadeUpTestTrees()
        {
            const string inAnOldFolder = "Services/Implementation/AMadeUpLanternKeeperTest.cs";
            const string inItsFolder = $"{PlatformTestFolder}/{AMadeUpArea}/AMadeUpLanternKeeperTest.cs";
            const string inAnotherArea = $"{PlatformTestFolder}/Host/AMadeUpLanternKeeperTest.cs";
            const string inAFolderThatOnlyLooksAlike = $"{PlatformTestFolder}Neighbour/{AMadeUpArea}/AMadeUpLanternKeeperTest.cs";
            const string atTheTestRoot = "AMadeUpLanternKeeperTests.cs";
            const string anIntegrationTest = "API/Integration/AMadeUpLanternKeeperTest.cs";
            const string aStayingTest = "Services/Implementation/AMadeUpLanternKeeperStayingTest.cs";

            yield return new TestCaseData(
                    new[] { Class("Services.Implementation", "AMadeUpLanternKeeperTest", inAnOldFolder) }, Array.Empty<string>(), new[] { inAnOldFolder })
                .SetArgDisplayNames("a test of a platform type in an old folder");
            yield return new TestCaseData(
                    new[] { Class($"Platform.{AMadeUpArea}", "AMadeUpLanternKeeperTest", inItsFolder) }, Array.Empty<string>(), Array.Empty<string>())
                .SetArgDisplayNames("a test of a platform type in its Platform folder");
            yield return new TestCaseData(
                    new[] { Class("Services.Implementation", "AMadeUpLanternKeeperTest", inItsFolder) }, Array.Empty<string>(), new[] { inItsFolder })
                .SetArgDisplayNames("a test in its Platform folder that kept its old namespace");
            yield return new TestCaseData(
                    new[] { Class($"Platform.{AMadeUpArea}", "AMadeUpLanternKeeperTest", inAnOldFolder) }, Array.Empty<string>(), new[] { inAnOldFolder })
                .SetArgDisplayNames("a test in an old folder that claims the Platform namespace");
            yield return new TestCaseData(
                    new[] { Class("Platform.Host", "AMadeUpLanternKeeperTest", inAnotherArea) }, Array.Empty<string>(), new[] { inAnotherArea })
                .SetArgDisplayNames("a test in the folder of another area");
            yield return new TestCaseData(
                    new[]
                    {
                        Class($"PlatformNeighbour.{AMadeUpArea}", "AMadeUpLanternKeeperTest", inAFolderThatOnlyLooksAlike),
                        Class(string.Empty, "AMadeUpLanternKeeperTests", atTheTestRoot),
                    },
                    Array.Empty<string>(),
                    new[] { inAFolderThatOnlyLooksAlike, atTheTestRoot })
                .SetArgDisplayNames("tests in a folder that only looks alike and at the test root");
            yield return new TestCaseData(
                    new[]
                    {
                        Class("API.Integration", "AMadeUpLanternKeeperTest", anIntegrationTest),
                        Class("Services.Implementation", "AMadeUpLanternKeeperStayingTest", aStayingTest),
                        Class("Services.Implementation", "AMadeUpLanternKeeperLikeHelper", inAnOldFolder),
                        Class("Services.Implementation", "AMadeUpLanternKeeperishTest", inAnOldFolder),
                        Class("Services.Implementation", "AMadeUpLanternTest", inAnOldFolder),
                    },
                    new[] { aStayingTest },
                    Array.Empty<string>())
                .SetArgDisplayNames("an integration test, a staying test and names that only look alike");
        }

        private static IEnumerable<TestCaseData> MadeUpProductionTrees()
        {
            const string aStayer = "Services/Implementation/AMadeUpStayer.cs";
            const string itsInterface = "Services/Interfaces/IAMadeUpStayer.cs";
            const string aNewcomerAmongTheInterfaces = "Services/Interfaces/IAMadeUpNewcomer.cs";
            const string aNewcomerAmongTheImplementations = "Services/Implementation/Deeper/AMadeUpNewcomer.cs";
            const string aPlatformTypeLeftBehind = "Services/Implementation/AMadeUpLanternKeeper.cs";
            (string Path, string Owner)[] theList = [(aStayer, "a made-up owner"), (itsInterface, "a made-up owner")];

            yield return new TestCaseData(new[] { aStayer, itsInterface }, theList, Array.Empty<string>())
                .SetArgDisplayNames("every file listed with its owner");
            yield return new TestCaseData(new[] { aStayer, itsInterface, aNewcomerAmongTheInterfaces }, theList, new[] { aNewcomerAmongTheInterfaces })
                .SetArgDisplayNames("an unlisted newcomer among the interfaces");
            yield return new TestCaseData(new[] { aStayer, itsInterface, aNewcomerAmongTheImplementations }, theList, new[] { aNewcomerAmongTheImplementations })
                .SetArgDisplayNames("an unlisted newcomer deeper among the implementations");
            yield return new TestCaseData(new[] { aStayer }, theList, new[] { itsInterface })
                .SetArgDisplayNames("a listed file that no longer exists");
            yield return new TestCaseData(
                    new[] { aStayer, itsInterface },
                    new[] { (aStayer, "a made-up owner"), (itsInterface, " ") },
                    new[] { itsInterface })
                .SetArgDisplayNames("a listed file without an owner");
            yield return new TestCaseData(
                    new[] { aStayer, itsInterface, aPlatformTypeLeftBehind },
                    theList.Append((aPlatformTypeLeftBehind, "a made-up owner")).ToArray(),
                    new[] { aPlatformTypeLeftBehind })
                .SetArgDisplayNames("a platform type listed as staying");
            yield return new TestCaseData(
                    new[]
                    {
                        aStayer,
                        itsInterface,
                        "Services/ImplementationNeighbour/AThing.cs",
                        "Services/Interfacing/AThing.cs",
                        "Modules/Lanterns/Services/Implementation/AThing.cs",
                        "Services/AThing.cs",
                    },
                    theList,
                    Array.Empty<string>())
                .SetArgDisplayNames("files in folders that only look alike");
        }

        private static IEnumerable<TestCaseData> EveryListedProductionFile()
            => ProductionFilesThatStay.Select((entry, position) => new TestCaseData(position).SetArgDisplayNames(entry.Path));

        private static IEnumerable<TestCaseData> EveryListedTestFile()
            => TestFilesThatStay.Select((entry, position) => new TestCaseData(position).SetArgDisplayNames(entry.Path));

        [Test]
        public void NoUnitTestOfAPlatformType_SitsOutsideItsPlatformFolder_ApartFromTheNamedStayingTests()
        {
            var platformTests = UnitTestsOfAPlatformType(CompiledTestClasses.Value, PlatformTypes.Value);

            Assert.That(
                platformTests.Where(test => test.Path.StartsWith($"{PlatformTestFolder}/", StringComparison.Ordinal)),
                Is.Not.Empty,
                $"No compiled test class of a platform type was found under {PlatformTestFolder}. Why it matters: a placement rule " +
                "that finds no platform test passes without looking at anything. How to fix: read the platform types from the " +
                "membership map and the production Platform namespace, and the test classes with the files they were compiled from.");

            ThenNothingIsMisplaced(Misplacements(platformTests, StayingTestPaths(TestFilesThatStay)));
        }

        [Test]
        public void EveryNamedStayingTestFile_ExistsAndTestsAPlatformType()
        {
            var problems = StayingTestListProblems(TestFilesThatStay, TestFilesOnDisk.Value, UnitTestsOfAPlatformType(CompiledTestClasses.Value, PlatformTypes.Value));

            Assert.That(
                problems,
                Is.Empty,
                $"[{string.Join("; ", problems)}]. Why it matters: a staying test is an exception to the placement rule, and an " +
                "exception whose file is gone, that tests no platform type, or that gives no reason covers nothing and lets a " +
                "misplaced test back in unseen. How to fix: take the entry off the list of test files that stay, or give its reason.");
        }

        [Test]
        public void TheProductionFilesInServicesInterfacesAndImplementation_AreExactlyTheNamedStayingFiles()
        {
            ThenTheStayingFilesMatchTheList(ProductionFilesInTheOldFolders.Value, ProductionFilesThatStay, PlatformTypes.Value);
        }

        [TestCaseSource(nameof(EveryListedProductionFile))]
        public void AListedProductionFileDroppedFromTheList_IsNamed(int position)
        {
            var dropped = ProductionFilesThatStay[position].Path;
            var shorterList = ProductionFilesThatStay.Where((_, index) => index != position).ToArray();

            var failure = Assert.Throws<AssertionException>(() => ThenTheStayingFilesMatchTheList(ProductionFilesInTheOldFolders.Value, shorterList, PlatformTypes.Value))!.Message;

            Assert.That(
                failure,
                Does.Contain(dropped),
                $"Without the entry {dropped} the rule said: {failure}. Why it matters: every entry of the list must be load-bearing, " +
                "so a file in Services/Interfaces or Services/Implementation that nobody named must fail the rule. How to fix: " +
                "compare every file found in both folders with the list, both ways.");
        }

        [TestCaseSource(nameof(EveryListedTestFile))]
        public void AStayingTestFileDroppedFromTheList_IsNamed(int position)
        {
            var dropped = TestFilesThatStay[position].Path;
            var shorterList = TestFilesThatStay.Where((_, index) => index != position).ToArray();

            var misplaced = Misplacements(UnitTestsOfAPlatformType(CompiledTestClasses.Value, PlatformTypes.Value), StayingTestPaths(shorterList));

            Assert.That(
                misplaced.Select(test => test.Path),
                Does.Contain(dropped),
                $"Without the entry {dropped} the placement rule reported [{string.Join("; ", misplaced)}]. Why it matters: a staying " +
                "test is on the list only because the rule would otherwise name it; an entry the rule does not need covers " +
                "nothing. How to fix: take the entry off the list, or name the file when it is not on the list.");
        }

        [Test]
        public void AGoneFileAddedToTheProductionList_IsNamed()
        {
            const string aGoneFile = "Services/Implementation/AMadeUpFileThatIsGone.cs";
            var longerList = ProductionFilesThatStay.Append((aGoneFile, "a made-up owner")).ToArray();

            var failure = Assert.Throws<AssertionException>(() => ThenTheStayingFilesMatchTheList(ProductionFilesInTheOldFolders.Value, longerList, PlatformTypes.Value))!.Message;

            Assert.That(
                failure,
                Does.Contain(aGoneFile),
                $"With the made-up entry {aGoneFile} the rule said: {failure}. Why it matters: a listed file that no longer exists " +
                "covers nothing and would let a newcomer of that name in unseen. How to fix: name every listed file that holds no file.");
        }

        [TestCaseSource(nameof(MadeUpTestTrees))]
        public void AMadeUpTestOfAPlatformTypeOutsideItsFolder_IsNamedWithItsFolder_AndOneInItsFolderIsNot(
            CompiledClass[] classes, string[] stayingTests, string[] expectedNamed)
        {
            var misplaced = Misplacements(UnitTestsOfAPlatformType(classes, [TheMadeUpSubject]), [.. stayingTests]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    misplaced.Select(test => test.Path),
                    Is.EquivalentTo(expectedNamed),
                    $"For the classes [{string.Join(", ", classes.AsEnumerable())}] the rule reported [{string.Join("; ", misplaced)}]. " +
                    "Why it matters: a test of a platform type in an old folder, in the wrong area, in a look-alike folder or with a " +
                    "namespace that disagrees with its file must each be named, and a test in its folder, an integration test, a " +
                    "staying test and a class that only looks like a test of the type must not be. How to fix: judge each test " +
                    "class by the files it was compiled from and its namespace, against the Platform folder of its subject.");

                Assert.That(
                    misplaced.Select(test => test.ExpectedFolder).Distinct(),
                    Is.EqualTo(expectedNamed.Length == 0 ? Array.Empty<string>() : new[] { $"{PlatformTestFolder}/{AMadeUpArea}" }),
                    $"The rule reported [{string.Join("; ", misplaced)}]. Why it matters: the message must say where the test " +
                    "belongs, or the reader has to work it out. How to fix: name the Platform folder of the subject's area.");
            }
        }

        [Test]
        public void TheMadeUpTestCompiledFromTheFixtureFile_IsNamedWithItsPlatformFolder()
        {
            var fromTheFixtureFile = CompiledTestClasses.Value.Where(compiled => compiled.Files.Contains(TheFixtureFile)).ToList();

            var misplaced = Misplacements(UnitTestsOfAPlatformType(fromTheFixtureFile, [TheMadeUpSubject]), []);

            Assert.That(
                misplaced.Select(test => $"{test.Path} -> {test.ExpectedFolder}"),
                Is.EqualTo(new[] { $"{TheFixtureFile} -> {PlatformTestFolder}/{AMadeUpArea}" }),
                $"The compiled classes from {TheFixtureFile} were [{string.Join(", ", fromTheFixtureFile)}] and the rule reported " +
                $"[{string.Join("; ", misplaced)}]. Why it matters: {nameof(AMadeUpLanternKeeperTest)} is compiled from a file " +
                "outside the Platform folders, so the rule must find it through the symbols of the compiled assembly and name " +
                "its file with the folder it belongs in. How to fix: tie every compiled class to the files its methods came from.");
        }

        [TestCaseSource(nameof(MadeUpProductionTrees))]
        public void AMadeUpProductionTree_NamesEveryUnlistedGoneOwnerlessOrPlatformFile_AndNothingElse(
            string[] productionPaths, (string Path, string Owner)[] list, string[] expectedNamed)
        {
            var found = productionPaths.Where(IsInAnOldServicesFolder).ToList();

            if (expectedNamed.Length == 0)
            {
                Assert.DoesNotThrow(
                    () => ThenTheStayingFilesMatchTheList(found, list, [TheMadeUpSubject]),
                    $"For the tree [{string.Join(", ", productionPaths)}] the rule failed although every file in Services/Interfaces " +
                    "and Services/Implementation is listed with its owner. Why it matters: a file in a folder that only looks alike " +
                    "is not in the old folders. How to fix: read only the paths that start with those two folders.");
                return;
            }

            var failure = Assert.Throws<AssertionException>(() => ThenTheStayingFilesMatchTheList(found, list, [TheMadeUpSubject]))!.Message;

            Assert.That(
                failure,
                Does.Contain(expectedNamed[0]),
                $"For the tree [{string.Join(", ", productionPaths)}] the rule said: {failure}. Why it matters: an unlisted file in " +
                "either folder, a listed file that is gone, an entry without an owner and a platform type on the list must each " +
                "be named. How to fix: compare the files with the list both ways and check every entry's owner and type.");
        }

        private static List<PlatformTest> UnitTestsOfAPlatformType(IEnumerable<CompiledClass> classes, IReadOnlyCollection<PlatformSubject> subjects)
            => [.. classes.SelectMany(compiled => subjects
                .Where(subject => IsATestOf(compiled.Name, subject.Name))
                .Take(1)
                .SelectMany(subject => compiled.Files
                    .Where(path => !IsInsideAny(path, TestRootsNotJudged))
                    .Select(path => new PlatformTest(path, compiled.FullName, compiled.Namespace, subject.Name, $"{PlatformTestFolder}/{subject.Area}"))))];

        private static List<PlatformTest> Misplacements(IEnumerable<PlatformTest> platformTests, IReadOnlyCollection<string> stayingTests)
            => [.. platformTests
                .Where(test => !stayingTests.Contains(test.Path, StringComparer.Ordinal))
                .Where(test => !IsInsideAny(test.Path, [test.ExpectedFolder]) || test.Namespace != NamespaceOfFolder(FolderOf(test.Path)))];

        private static List<string> StayingTestListProblems(
            IEnumerable<(string Path, string Reason)> list, IReadOnlyCollection<string> filesOnDisk, IReadOnlyCollection<PlatformTest> platformTests)
            => [.. list.SelectMany(entry => new[]
            {
                string.IsNullOrWhiteSpace(entry.Reason) ? $"{entry.Path} is named as staying without a reason" : null,
                !filesOnDisk.Contains(entry.Path, StringComparer.Ordinal) ? $"{entry.Path} is named as staying but holds no file" : null,
                filesOnDisk.Contains(entry.Path, StringComparer.Ordinal) && !platformTests.Any(test => test.Path == entry.Path)
                    ? $"{entry.Path} is named as staying but holds no unit test of a platform type"
                    : null,
            }).OfType<string>()];

        private static void ThenNothingIsMisplaced(IReadOnlyCollection<PlatformTest> misplaced)
        {
            Assert.That(
                misplaced,
                Is.Empty,
                $"These unit tests of a platform type sit outside their Platform folder: [{string.Join("; ", misplaced)}]. Why it " +
                "matters: the platform lane's tests sit next to the lane, and a test of a platform type left in or written into " +
                "an old folder splits the lane again. How to fix: git mv the file into the Platform folder named for it and change " +
                "its namespace to that folder's; when it must stay, name it with its reason among the test files that stay.");
        }

        private static void ThenTheStayingFilesMatchTheList(
            IReadOnlyCollection<string> foundInTheOldFolders, IReadOnlyCollection<(string Path, string Owner)> list, IReadOnlyCollection<PlatformSubject> platformTypes)
        {
            var platformTypeNames = platformTypes.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
            var badEntries = list
                .Where(entry => string.IsNullOrWhiteSpace(entry.Owner)).Select(entry => $"{entry.Path} is listed without an owner")
                .Concat(list.Where(entry => platformTypeNames.Contains(TypeNameOf(entry.Path))).Select(entry => $"{entry.Path} holds a platform type"))
                .ToList();

            Assert.That(
                badEntries,
                Is.Empty,
                $"[{string.Join("; ", badEntries)}]. Why it matters: every file left in Services/Interfaces and " +
                "Services/Implementation stays only with a named owner, and no platform type stays there. How to fix: give the " +
                "entry its owner, or git mv the platform type into its Platform area and take it off the list.");

            InlineBaseline.AssertMatches(
                foundInTheOldFolders,
                list.Select(entry => entry.Path),
                "Why it matters: nothing may be left in, or added to, Services/Interfaces or Services/Implementation unseen. " +
                "How to fix: move a platform file into its Platform area; when a file deliberately stays, name it with its owner " +
                "among the production files that stay.");
        }

        private static bool IsATestOf(string className, string subjectName)
        {
            var name = WithoutArity(className);
            var stem = name.EndsWith("Tests", StringComparison.Ordinal) ? name[..^5]
                : name.EndsWith("Test", StringComparison.Ordinal) ? name[..^4]
                : null;
            var subject = WithoutArity(subjectName);

            return stem is not null
                   && (stem.Equals(subject, StringComparison.OrdinalIgnoreCase)
                       || stem.Equals($"{subject}s", StringComparison.OrdinalIgnoreCase)
                       || $"{stem}s".Equals(subject, StringComparison.OrdinalIgnoreCase)
                       || (stem.Length > subject.Length
                           && stem.StartsWith(subject, StringComparison.OrdinalIgnoreCase)
                           && char.IsUpper(stem[subject.Length])));
        }

        private static string WithoutArity(string name)
        {
            var tick = name.IndexOf('`');
            return tick < 0 ? name : name[..tick];
        }

        private static IReadOnlyCollection<string> StayingTestPaths(IEnumerable<(string Path, string Reason)> list)
            => [.. list.Select(entry => entry.Path)];

        private static bool IsInAnOldServicesFolder(string path) => IsInsideAny(path, FoldersWithStayingProductionFiles);

        private static bool IsInsideAny(string path, IEnumerable<string> folders)
            => folders.Any(folder => path.StartsWith($"{folder}/", StringComparison.Ordinal));

        private static CompiledClass Class(string folderNamespace, string name, string file)
        {
            var ns = NamespaceOfFolder(folderNamespace.Replace('.', '/'));
            return new CompiledClass($"{ns}.{name}", ns, name, [file]);
        }

        private static string NamespaceOfFolder(string folder)
        {
            const string testRootNamespace = "Lighthouse.Backend.Tests";
            return folder.Length == 0 ? testRootNamespace : $"{testRootNamespace}.{folder.Replace('/', '.')}";
        }

        private static string FolderOf(string path)
        {
            var lastSlash = path.LastIndexOf('/');
            return lastSlash < 0 ? string.Empty : path[..lastSlash];
        }

        private static string FileNameOf(string path) => path[(path.LastIndexOf('/') + 1)..];

        private static string TypeNameOf(string path) => FileNameOf(path).Split('.')[0];

        // A platform type is a type of the lane's membership map, or any top-level type the production assembly declares in
        // the Platform namespace; the area is the namespace segment after Platform.
        private static List<PlatformSubject> ReadPlatformTypes()
        {
            const string platformNamespace = "Lighthouse.Backend.Platform";

            var declared = typeof(Program).Assembly.GetTypes()
                .Where(type => !type.IsNested && type.Namespace is not null && !type.Name.StartsWith('<'))
                .Where(type => type.Namespace!.StartsWith($"{platformNamespace}.", StringComparison.Ordinal))
                .Select(type => new PlatformSubject(WithoutArity(type.Name), type.Namespace![(platformNamespace.Length + 1)..].Split('.')[0]));

            var subjects = PlatformLaneMembership.MovingTypes
                .Select(type => new PlatformSubject(WithoutArity(type.Name), type.Area))
                .Concat(declared)
                .Distinct()
                .ToList();

            Assert.That(subjects, Is.Not.Empty, "No platform type was found, so a placement rule over them would pass without looking at anything.");
            return subjects;
        }

        private static List<string> ReadTestFilesOnDisk()
        {
            var projectRoot = Path.Combine(SolutionRoot(), "Lighthouse.Backend.Tests");

            var paths = Directory.EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(projectRoot, file).Replace('\\', '/'))
                .Where(path => !BuildOutputFolders.Any(folder => path.StartsWith(folder, StringComparison.Ordinal)))
                .Order(StringComparer.Ordinal)
                .ToList();

            Assert.That(paths, Does.Contain(ThisFile), $"The file scan of {projectRoot} did not find {ThisFile}, so it is not looking at the test project.");
            return paths;
        }

        private static List<CompiledClass> ReadCompiledTestClasses()
        {
            var assemblyPath = typeof(PlatformLaneUnitTestPlacementTest).Assembly.Location;
            using var module = ModuleDefinition.ReadModule(assemblyPath, new ReaderParameters { InMemory = true, ReadSymbols = true });

            var classes = module.Types
                .Where(type => !type.Name.StartsWith('<'))
                .Select(type => new CompiledClass(type.FullName, type.Namespace, type.Name, FilesOf(type)))
                .Where(compiled => compiled.Files.Count > 0)
                .ToList();

            Assert.That(
                classes.Where(compiled => compiled.Files.Contains(ThisFile)).Select(compiled => compiled.FullName),
                Does.Contain(typeof(PlatformLaneUnitTestPlacementTest).FullName),
                $"The symbols of {assemblyPath} did not tie this very class to {ThisFile}, so no class could be tied to its file.");
            return classes;
        }

        // The files a class was compiled from, read from the symbols of its methods and of the types nested in it, such as
        // the state machines the compiler writes for async test methods.
        private static List<string> FilesOf(TypeDefinition type)
            => [.. TypeAndItsNestedTypes(type)
                .SelectMany(each => each.Methods)
                .Where(method => method.DebugInformation.HasSequencePoints)
                .SelectMany(method => method.DebugInformation.SequencePoints)
                .Select(point => TestProjectRelativePath(point.Document.Url))
                .OfType<string>()
                .Where(path => !BuildOutputFolders.Any(folder => path.StartsWith(folder, StringComparison.Ordinal)))
                .Distinct(StringComparer.Ordinal)];

        private static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
            => new[] { type }.Concat(type.NestedTypes.SelectMany(TypeAndItsNestedTypes));

        private static string? TestProjectRelativePath(string documentPath)
        {
            const string marker = "/Lighthouse.Backend.Tests/";
            var normalised = documentPath.Replace('\\', '/');
            var start = normalised.LastIndexOf(marker, StringComparison.Ordinal);
            return start < 0 ? null : normalised[(start + marker.Length)..];
        }

        private static string SolutionRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the test file scan.");
            return directory!.FullName;
        }

        public sealed record CompiledClass(string FullName, string Namespace, string Name, IReadOnlyList<string> Files)
        {
            public override string ToString() => $"{FullName} from {string.Join(", ", Files)}";
        }

        private sealed record PlatformSubject(string Name, string Area);

        private sealed record PlatformTest(string Path, string ClassFullName, string Namespace, string Subject, string ExpectedFolder)
        {
            public override string ToString()
                => $"{Path} holds {ClassFullName}, a unit test of the platform type {Subject}; it belongs in {ExpectedFolder}, namespace {NamespaceOfFolder(ExpectedFolder)}";
        }
    }
}
