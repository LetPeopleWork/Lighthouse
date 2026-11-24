using System.Diagnostics;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The Work Items reactions to domain events (blocked-transition capture and close for Work Items and Features, the
    /// refresh-log clean-up on a deleted Team, the update trigger on a changed fetch shape, and the orphaned-Feature clean-up
    /// on a deleted Portfolio) live in the module's
    /// Application policies, none is left under Services, and the policies hold nothing but domain-event handlers. The types are
    /// named as strings and read from the compiled production assembly, so this class compiles before and after the move, and a
    /// handler that has not moved yet is a failure that names it. The order the handlers run in is pinned by
    /// DomainEventHandlerOrderPinTest, which must stay green as it is. The rules also run on made-up types: an offender must be
    /// reported and its compliant twin must not be.
    /// </summary>
    [TestFixture]
    public class WorkItemsPoliciesLayoutArchUnitTest
    {
        private const string PoliciesNamespace = "Lighthouse.Backend.Modules.WorkItems.Application.Policies";

        private const string PoliciesFolder = "Modules/WorkItems/Application/Policies";

        private const string WorkItemsPolicies = @"^Lighthouse\.Backend\.Modules\.WorkItems\.Application\.Policies(\.|$)";

        private const string FixturePolicies = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.PoliciesLayout\.Policies(\.|$)";

        private const string FixturePoliciesThatHoldNothing = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.PoliciesLayout\.NothingLivesHere(\.|$)";

        private const string FixturePoliciesNamespace = "Lighthouse.Backend.Modules.WorkItemsFixtures.PoliciesLayout.Policies";

        private const string FixtureFormerHome = @"^Lighthouse\.Backend\.Modules\.WorkItemsFixtures\.PoliciesLayout\.Services\.Implementation\.DomainEvents(\.|$)";

        private const string WhereTheHandlersLivedBefore = @"^Lighthouse\.Backend\.Services\.Implementation\.(DomainEvents|BackgroundServices\.Update)(\.|$)";

        private const string TheDomainEventHandlerPort = "Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler`1";

        private const string AnOldNamespacePrefix =
            @"(?<![\w.])(Lighthouse\.Backend\.)?(Services\.Implementation\.(DomainEvents|BackgroundServices\.Update)\.)";

        // Each file declares one top-level type named like the file.
        private static readonly string[] Policies =
        [
            "WorkItemBlockedTransitionCaptureHandler",
            "WorkItemBlockedTransitionCloseHandler",
            "FeatureBlockedTransitionCaptureHandler",
            "FeatureBlockedTransitionCloseHandler",
            "TeamDeletedRefreshLogCleanupHandler",
            "ConnectionFetchShapeChangedUpdateTriggerHandler",
            "PortfolioDeletedOrphanedFeatureCleanupHandler",
        ];

        private static readonly Regex AnOldFullName = new(
            $@"{AnOldNamespacePrefix}(?<type>{string.Join("|", Policies)})\b",
            RegexOptions.None,
            TimeSpan.FromSeconds(1));

        private static readonly Regex AnOldFilePath = new(
            @"Services/Implementation/(DomainEvents|BackgroundServices/Update)/(?<type>" + string.Join("|", Policies) + @")\.cs",
            RegexOptions.None,
            TimeSpan.FromSeconds(1));

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(WorkItemsPoliciesLayoutArchUnitTest).Assembly));

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, FixtureModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheWorkItemsReactions_LiveInTheModulePolicies_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, PoliciesNamespace, Policies, WhereTheHandlersLivedBefore).Check();
        }

        [Test]
        public void TheWorkItemsReactionFiles_SitInTheModulePoliciesFolder_AndNowhereElse()
        {
            ThenEachFileSitsOnlyIn(PoliciesFolder, [.. Policies.Select(name => $"{name}.cs")]);
        }

        [Test]
        public void TheWorkItemsPolicies_HoldNothingButDomainEventHandlers()
        {
            ThenEveryTypeInThePoliciesIsADomainEventHandler(ProductionModule.Value, WorkItemsPolicies);
        }

        [Test]
        public void AReactionLeftUnderServices_IsNamed_AndAWellPlacedTwinIsNot()
        {
            var twinPlaced = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixturePoliciesNamespace, ["AWellPlacedHandler"], FixtureFormerHome);
            var leftBehind = ModuleRules.TypesLiveInTheirLayer(FixtureModule.Value, FixturePoliciesNamespace, ["ALeftBehindHandler"], FixtureFormerHome);

            Assert.DoesNotThrow(twinPlaced.Check, "A handler that lives in the policies and has no copy under Services was reported. Why it matters: a rule that flags a correct move hides the real offenders. How to fix: report only a handler missing from the policies or still present in the former home.");

            var failure = Assert.Throws<AssertionException>(leftBehind.Check, "A handler left under Services passed the layout rule.");
            Assert.That(failure!.Message, Does.Contain("ALeftBehindHandler"), "The failure must name the handler that was left behind.");
        }

        [Test]
        public void APolicyThatIsNoDomainEventHandler_IsNamed_AndAHandlerTwinIsNot()
        {
            var failure = Assert.Throws<AssertionException>(
                () => ThenEveryTypeInThePoliciesIsADomainEventHandler(FixtureModule.Value, FixturePolicies),
                "A policies namespace holding a type that handles no domain event passed. Why it matters: the policies folder is where the module's reactions live, so a service dropped there hides what reacts to what. How to fix: name every type in the policies that does not implement the domain-event handler port.");

            Assert.That(failure!.Message, Does.Contain("ANonHandlerInThePolicies").And.Not.Contain("AWellPlacedHandler"), "The failure must name the non-handler and leave the handler twin alone.");
        }

        [Test]
        public void APoliciesNamespaceThatHoldsNoType_FailsTheRuleInsteadOfPassingUnseen()
        {
            var failure = Assert.Throws<AssertionException>(
                () => ThenEveryTypeInThePoliciesIsADomainEventHandler(FixtureModule.Value, FixturePoliciesThatHoldNothing),
                "A handler rule that looked at no type passed. Why it matters: a rule pointed at a namespace that holds nothing stays green forever and guards nothing. How to fix: fail when the policies it was given hold no type.");

            Assert.That(failure!.Message, Does.Contain("NothingLivesHere"), "The failure must name the pattern that held nothing.");
        }

        [Test]
        public void NoTrackedTestOrProductionSource_NamesAMovedReactionAtItsOldHome()
        {
            var stale = ProjectFilesNamingAMovedTypeAtItsOldHome();

            Assert.That(
                stale,
                Is.Empty,
                $"These tracked source files still name a moved Work Items reaction at its old home: [{string.Join("; ", stale)}]. Why it matters: a rule list or a path that " +
                "names the old full name checks a type that no longer lives there, so the rule is green over nothing while the moved handler goes unguarded. " +
                "How to fix: write each full name with the handler's new home under Modules/WorkItems/Application/Policies.");
        }

        [TestCase("\"Lighthouse.Backend.Services.Implementation.DomainEvents.WorkItemBlockedTransitionCaptureHandler\",", "Lighthouse.Backend.Services.Implementation.DomainEvents.WorkItemBlockedTransitionCaptureHandler")]
        [TestCase("\"Lighthouse.Backend.Services.Implementation.DomainEvents.TeamDeletedRefreshLogCleanupHandler\"", "Lighthouse.Backend.Services.Implementation.DomainEvents.TeamDeletedRefreshLogCleanupHandler")]
        [TestCase("\"Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.ConnectionFetchShapeChangedUpdateTriggerHandler\",", "Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.ConnectionFetchShapeChangedUpdateTriggerHandler")]
        [TestCase("\"**/Services/Implementation/DomainEvents/FeatureBlockedTransitionCloseHandler.cs\",", "Services/Implementation/DomainEvents/FeatureBlockedTransitionCloseHandler.cs")]
        [TestCase("    /// <see cref=\"Services.Implementation.DomainEvents.FeatureBlockedTransitionCaptureHandler\"/>, and every", "Services.Implementation.DomainEvents.FeatureBlockedTransitionCaptureHandler")]
        public void AnOldHomeInARuleListOrAPath_IsFound(string line, string expectedHit)
        {
            Assert.That(
                StaleNamesIn(line),
                Is.EqualTo(new[] { expectedHit }),
                "The scan missed an old full name or file path. Why it matters: a scan that cannot see an old home cannot fail when a list keeps one. How to fix: match the old namespace and the old folder of every moved handler.");
        }

        [TestCase("\"Lighthouse.Backend.Modules.WorkItems.Application.Policies.WorkItemBlockedTransitionCaptureHandler\",")]
        [TestCase("\"Lighthouse.Backend.Services.Implementation.DomainEvents.SomeHandlerThatDidNotMove\",")]
        [TestCase("\"Lighthouse.Backend.Services.Implementation.DomainEvents.WorkItemBlockedTransitionCaptureHandlerTest\",")]
        [TestCase("\"**/Modules/WorkItems/Application/Policies/FeatureBlockedTransitionCloseHandler.cs\",")]
        public void ANewHomeOrAHandlerThatStays_IsNotFound(string line)
        {
            Assert.That(
                StaleNamesIn(line),
                Is.Empty,
                "The scan reported a name that is correct. Why it matters: a scan that flags the new home makes the move impossible to finish. How to fix: report only the old namespace or folder of a handler that moved.");
        }

        private static void ThenEveryTypeInThePoliciesIsADomainEventHandler(ModuleDefinition module, string policies)
        {
            var types = IlDependencies.TypesIn(module, policies);

            Assert.That(
                types,
                Is.Not.Empty,
                $"The policies matching \"{policies}\" hold no type, so the rule looked at nothing. Why it matters: a rule pointed at a namespace that holds nothing stays green forever. " +
                "How to fix: move the handlers into the policies namespace the rule names.");

            var notHandlers = types
                .Where(fullName => module.GetType(fullName) is { } type
                    && !type.Interfaces.Any(port => port.InterfaceType.FullName.StartsWith(TheDomainEventHandlerPort, StringComparison.Ordinal)))
                .ToList();

            Assert.That(
                notHandlers,
                Is.Empty,
                $"These types in the policies matching \"{policies}\" handle no domain event: [{string.Join(", ", notHandlers)}]. Why it matters: the policies folder is where the module's reactions live, " +
                "so a type that reacts to nothing hides what reacts to what. How to fix: move the type to the layer it belongs to, or make it implement the domain-event handler port.");
        }

        private static List<string> StaleNamesIn(string text)
            => [.. AnOldFullName.Matches(text).Select(match => match.Value), .. AnOldFilePath.Matches(text).Select(match => match.Value)];

        private static List<string> ProjectFilesNamingAMovedTypeAtItsOldHome()
        {
            var repositoryRoot = Path.GetDirectoryName(SolutionDirectory())!;
            var thisFile = $"{nameof(WorkItemsPoliciesLayoutArchUnitTest)}.cs";

            var files = TrackedFiles(repositoryRoot)
                .Where(path => path.EndsWith(".cs", StringComparison.Ordinal)
                    && (path.StartsWith("Lighthouse.Backend/Lighthouse.Backend.Tests/", StringComparison.Ordinal)
                        || path.StartsWith("Lighthouse.Backend/Lighthouse.Backend/", StringComparison.Ordinal))
                    && Path.GetFileName(path) != thisFile)
                .ToList();

            Assert.That(files, Is.Not.Empty, "git listed no tracked source file of the backend, so the scan would pass without looking at anything.");

            return
            [
                .. files.SelectMany(file => StaleNamesIn(File.ReadAllText(Path.Combine(repositoryRoot, file))).Distinct(StringComparer.Ordinal)
                    .Select(name => $"{file} names {name}")),
            ];
        }

        // Only tracked files count: a build output or a local config is no part of the committed code.
        private static List<string> TrackedFiles(string repositoryRoot)
        {
            var start = new ProcessStartInfo("git")
            {
                WorkingDirectory = repositoryRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            start.ArgumentList.Add("ls-files");

            Process? process = null;
            try
            {
                process = Process.Start(start);
            }
            catch (System.ComponentModel.Win32Exception exception)
            {
                Assert.Fail($"The scan needs git on the machine to list the tracked files, and it could not be started: {exception.Message}. Why it matters: without it the test cannot tell which files are committed. How to fix: install git and put it on the PATH.");
            }

            using var started = process!;
            var outputRead = started.StandardOutput.ReadToEndAsync();
            var errorsRead = started.StandardError.ReadToEndAsync();
            Task.WaitAll(outputRead, errorsRead);
            started.WaitForExit();

            Assert.That(started.ExitCode, Is.Zero, $"git ls-files failed: {errorsRead.Result}");
            return [.. outputRead.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line => line.Trim())];
        }

        private static string SolutionDirectory()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to anchor the repository root.");
            return directory!.FullName;
        }

        private static void ThenEachFileSitsOnlyIn(string folder, IReadOnlyCollection<string> fileNames)
        {
            var paths = ProductionSource.Files().Select(file => file.RelativePath).ToList();

            var misplaced = fileNames
                .SelectMany(fileName => paths.Where(path => Path.GetFileName(path) == fileName && path != $"{folder}/{fileName}"))
                .ToList();
            var missing = fileNames.Where(fileName => !paths.Contains($"{folder}/{fileName}", StringComparer.Ordinal)).ToList();

            Assert.That(
                missing.Concat(misplaced),
                Is.Empty,
                $"Expected each of {string.Join(", ", fileNames)} to sit in the production project's {folder} folder and nowhere else; " +
                $"missing there: [{string.Join(", ", missing)}], found elsewhere: [{string.Join(", ", misplaced)}]. Why it matters: a " +
                "module shows its layers only when the folder matches the namespace. How to fix: move the file with git mv into " +
                $"{folder}, leaving no copy behind.");
        }
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.PoliciesLayout.Policies
{
    internal sealed record APolicyEvent : IDomainEvent;

    internal sealed class AWellPlacedHandler : global::Lighthouse.Backend.Platform.DomainEvents.IDomainEventHandler<APolicyEvent>
    {
        public Task HandleAsync(APolicyEvent domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    internal sealed class ANonHandlerInThePolicies
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.WorkItemsFixtures.PoliciesLayout.Services.Implementation.DomainEvents
{
    internal sealed class ALeftBehindHandler
    {
        public int Answer => 42;
    }
}
