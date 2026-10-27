using ArchUnitNET.Loader;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.Architecture.RatchetFixtures;
using Microsoft.Extensions.DependencyInjection;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Two leaks of the persistence layer are held where they are today, by name, and may only shrink:
    /// code outside the repositories folder that asks a repository for a query to build on, and
    /// driving-adapter types that depend on a repository. A list that held only a count would let one
    /// offender leave and another arrive unseen, so each list names its entries and is compared in both
    /// directions: a newcomer fails, and an entry that no longer offends fails until it is deleted.
    ///
    /// The comparison is a small pure step, driven here with made-up lists, because the two failure
    /// directions cannot be shown on the real code without breaking it.
    /// </summary>
    [TestFixture]
    [Category("stories-6367-6369")]
    [Category("slice-s05a")]
    public class RepositoryLeakRatchetArchUnitTest
    {
        private const string PendingGetAllByPredicate = "pending: S05a-1";

        private const string PendingApiDependency = "pending: S05a-2";

        private const string RepositoriesFolder = "Services/Implementation/Repositories/";

        private const string ApiNamespace = @"^Lighthouse\.Backend\.API(\.|$)";

        private const string FixtureNamespace = @"^Lighthouse\.Backend\.Tests\.Architecture\.RatchetFixtures$";

        /// <summary>
        /// Files outside the repositories folder that call GetAllByPredicate today. Delete an entry when
        /// its file stops calling; never add one.
        /// </summary>
        private static readonly string[] FilesThatCallGetAllByPredicate =
        [
            "API/FeaturesController.cs",
            "API/TeamMetricsController.cs",
            "Services/Implementation/BackgroundServices/OverTimeFillTarget.cs",
            "Services/Implementation/BlockedCountSeriesBuilder.cs",
            "Services/Implementation/Connections/ConnectionSaveService.cs",
            "Services/Implementation/DomainEvents/DemoBlockedHistoryBackfillHandler.cs",
            "Services/Implementation/DomainEvents/DemoPercentilesBackfillHandler.cs",
            "Services/Implementation/PortfolioMetricsService.cs",
            "Services/Implementation/Refinement/RefinementList.cs",
            "Services/Implementation/RefreshLogService.cs",
            "Services/Implementation/TeamMetricsService.cs",
            "Services/Implementation/WorkItems/WorkItemService.cs",
            "Services/Implementation/WorkTrackingSystemConnectionUsage.cs",
            "Services/Implementation/WriteBackTriggerService.cs",
        ];

        /// <summary>
        /// Driving-adapter types that depend on a repository today. Delete an entry when its type stops
        /// depending; never add one.
        /// </summary>
        private static readonly string[] ApiTypesThatDependOnARepository =
        [
            "Lighthouse.Backend.API.APIHelpers",
            "Lighthouse.Backend.API.DeliveriesController",
            "Lighthouse.Backend.API.DeliveryNotesController",
            "Lighthouse.Backend.API.DeliveryRulesController",
            "Lighthouse.Backend.API.DeliverySourcesController",
            "Lighthouse.Backend.API.DemoController",
            "Lighthouse.Backend.API.FeaturesController",
            "Lighthouse.Backend.API.ForecastController",
            "Lighthouse.Backend.API.ForecastRealityCheckController",
            "Lighthouse.Backend.API.Helpers.OwnerSettingRefusals",
            "Lighthouse.Backend.API.Helpers.PortfolioExtensions",
            "Lighthouse.Backend.API.OptionalFeaturesController",
            "Lighthouse.Backend.API.PortfolioController",
            "Lighthouse.Backend.API.PortfolioMetricsController",
            "Lighthouse.Backend.API.PortfoliosController",
            "Lighthouse.Backend.API.SuggestionsController",
            "Lighthouse.Backend.API.TeamController",
            "Lighthouse.Backend.API.TeamFeatureSizeMetricsController",
            "Lighthouse.Backend.API.TeamMetricsController",
            "Lighthouse.Backend.API.TeamsController",
            "Lighthouse.Backend.API.WizardsController",
            "Lighthouse.Backend.API.WorkTrackingSystemConnectionController",
            "Lighthouse.Backend.API.WorkTrackingSystemConnectionsController",
        ];

        private static readonly string[] TheOneCallSite = ["Services/Implementation/Somewhere.cs:5"];

        private static readonly string[] TheLinkyCallSite = ["Services/Implementation/Linky.cs:5"];

        private static readonly string[] TheQuotedCallSite = ["Services/Implementation/Quoted.cs:5"];

        private static readonly string[] TheWrappedCallSite = ["Services/Implementation/Wrapped.cs:7"];

        private static readonly Lazy<ArchitectureModel> FixtureArchitecture = new(() => new ArchLoader()
            .LoadAssemblies(typeof(ConstructorDependent).Assembly)
            .Build());

        // ---- The real code: the lists equal what was verified when they were written ----

        [Test]
        [Ignore(PendingGetAllByPredicate)]
        public void GetAllByPredicate_IsCalledOnlyFromTheFilesOnTheList()
        {
            Assert.That(FilesThatCallGetAllByPredicate, Has.Length.EqualTo(14).And.Unique);

            var callers = GetAllByPredicateCallSites(ProductionSource.Files())
                .Select(site => site[..site.LastIndexOf(':')])
                .Distinct();

            AssertMatchesBaseline(
                callers,
                FilesThatCallGetAllByPredicate,
                "A repository query belongs inside the repositories folder: add a method to the repository instead of calling GetAllByPredicate from here.");
        }

        [Test]
        [Ignore(PendingApiDependency)]
        public void TheApi_DependsOnARepositoryOnlyInTheTypesOnTheList()
        {
            Assert.That(ApiTypesThatDependOnARepository, Has.Length.EqualTo(23).And.Unique);

            AssertMatchesBaseline(
                RepositoryDependentsIn(LighthouseArchitecture.Production, ApiNamespace),
                ApiTypesThatDependOnARepository,
                "The driving adapter asks a service, not a repository: move the data access behind a service.");
        }

        // ---- The two-way comparison, driven with made-up lists ----

        [Test]
        public void AListThatMatchesTheCodeExactly_Passes()
        {
            Assert.DoesNotThrow(() => AssertMatchesBaseline(["a.cs", "b.cs"], ["a.cs", "b.cs"], "hint"));
        }

        [Test]
        public void ANewOffenderNotOnTheList_FailsAndNamesIt()
        {
            var failure = Assert.Throws<AssertionException>(
                () => AssertMatchesBaseline(["a.cs", "Services/NewOne.cs"], ["a.cs"], "Use the repository."));

            Assert.That(
                failure!.Message,
                Does.Contain("Services/NewOne.cs").And.Contain("Use the repository.").And.Not.Contain("a.cs"),
                "an entry that matches must not be reported");
        }

        [Test]
        public void AListedEntryThatNoLongerOffends_FailsAndNamesItUntilItIsDeleted()
        {
            var failure = Assert.Throws<AssertionException>(
                () => AssertMatchesBaseline(["a.cs"], ["a.cs", "Services/Cleaned.cs"], "hint"));

            Assert.That(failure!.Message, Does.Contain("Services/Cleaned.cs").And.Contain("delete"));
        }

        [Test]
        public void ANewOffenderAndAStaleEntryAtOnce_AreBothNamedInOneFailure()
        {
            var failure = Assert.Throws<AssertionException>(
                () => AssertMatchesBaseline(["Services/Newcomer.cs"], ["Services/Gone.cs"], "hint"));

            Assert.That(failure!.Message, Does.Contain("Services/Newcomer.cs").And.Contain("Services/Gone.cs"));
        }

        [Test]
        public void TheOrderOfTheListsAndRepeatedEntries_DoNotMatter()
        {
            Assert.DoesNotThrow(() => AssertMatchesBaseline(["b.cs", "a.cs", "a.cs"], ["a.cs", "b.cs"], "hint"));
        }

        [Test]
        public void NothingOffendingAgainstAnEmptyList_Passes()
        {
            Assert.DoesNotThrow(() => AssertMatchesBaseline([], [], "hint"));
        }

        // ---- The source scan for GetAllByPredicate callers ----

        [Test]
        [Ignore(PendingGetAllByPredicate)]
        public void ACallInCode_IsReportedWithItsFileAndLine()
        {
            var source = new ProductionSourceFile(
                "Services/Implementation/Somewhere.cs",
                "class Somewhere\n{\n    void Run()\n    {\n        var all = teams.GetAllByPredicate(t => t.Id == 1);\n    }\n}");

            Assert.That(GetAllByPredicateCallSites([source]), Is.EqualTo(TheOneCallSite));
        }

        [Test]
        [Ignore(PendingGetAllByPredicate)]
        public void AMentionInALineComment_IsNotACall()
        {
            var source = new ProductionSourceFile(
                "Services/Implementation/Chatty.cs",
                "class Chatty\n{\n    // do not call teams.GetAllByPredicate(t => true) from here\n}");

            Assert.That(GetAllByPredicateCallSites([source]), Is.Empty);
        }

        [Test]
        [Ignore(PendingGetAllByPredicate)]
        public void AMentionInABlockComment_IsNotACall()
        {
            var source = new ProductionSourceFile(
                "Services/Implementation/Chatty.cs",
                "class Chatty\n{\n    /* teams.GetAllByPredicate(t => true)\n       is what this used to do */\n}");

            Assert.That(GetAllByPredicateCallSites([source]), Is.Empty);
        }

        [Test]
        [Ignore(PendingGetAllByPredicate)]
        public void ASlashPairInsideAStringLiteral_DoesNotHideACallOnTheSameLine()
        {
            var source = new ProductionSourceFile(
                "Services/Implementation/Linky.cs",
                "class Linky\n{\n    void Run()\n    {\n        var all = Open(\"http://example.com/*docs*/\").GetAllByPredicate(t => t.Id == 1);\n    }\n}");

            Assert.That(GetAllByPredicateCallSites([source]), Is.EqualTo(TheLinkyCallSite));
        }

        [Test]
        [Ignore(PendingGetAllByPredicate)]
        public void AnEscapedQuoteBeforeASlashPairInsideAString_DoesNotHideACallOnTheSameLine()
        {
            var source = new ProductionSourceFile(
                "Services/Implementation/Quoted.cs",
                "class Quoted\n{\n    void Run()\n    {\n        var all = Say(\"she said \\\" // still the string\").GetAllByPredicate(t => t.Id == 1);\n    }\n}");

            Assert.That(GetAllByPredicateCallSites([source]), Is.EqualTo(TheQuotedCallSite));
        }

        [Test]
        [Ignore(PendingGetAllByPredicate)]
        public void ACallWrittenOnTheLineAfterItsReceiver_IsReportedAtTheCallsLine()
        {
            var source = new ProductionSourceFile(
                "Services/Implementation/Wrapped.cs",
                "class Wrapped\n{\n    // GetAllByPredicate (not GetByPredicate) so the query stays lazy\n    void Run()\n    {\n        var all = teams\n            .GetAllByPredicate(t => t.Id == 1);\n    }\n}");

            Assert.That(GetAllByPredicateCallSites([source]), Is.EqualTo(TheWrappedCallSite));
        }

        [Test]
        [Ignore(PendingGetAllByPredicate)]
        public void TheDeclarationOnTheRepositoryContract_IsNotACall()
        {
            var source = new ProductionSourceFile(
                "Services/Interfaces/Repositories/IRepository.cs",
                "interface IRepository<T>\n{\n    IQueryable<T> GetAllByPredicate(Expression<Func<T, bool>> predicate);\n}");

            Assert.That(GetAllByPredicateCallSites([source]), Is.Empty);
        }

        [Test]
        [Ignore(PendingGetAllByPredicate)]
        public void ACallInsideTheRepositoriesFolder_IsAllowed()
        {
            var source = new ProductionSourceFile(
                RepositoriesFolder + "TeamRepository.cs",
                "class TeamRepository\n{\n    void Run() => GetAllByPredicate(t => t.Id == 1);\n}");

            Assert.That(GetAllByPredicateCallSites([source]), Is.Empty);
        }

        // ---- The repository dependency rule, read from the compiled types ----

        [Test]
        [Ignore(PendingApiDependency)]
        public void AConstructorParameterOnARepository_IsADependency()
        {
            Assert.That(RepositoryDependentsIn(FixtureArchitecture.Value, FixtureNamespace), Does.Contain(typeof(ConstructorDependent).FullName));
        }

        [Test]
        [Ignore(PendingApiDependency)]
        public void ARepositoryNamedOnlyAsAGenericArgument_IsADependency()
        {
            Assert.That(RepositoryDependentsIn(FixtureArchitecture.Value, FixtureNamespace), Does.Contain(typeof(GenericArgumentDependent).FullName));
        }

        [Test]
        [Ignore(PendingApiDependency)]
        public void AConcreteRepositoryClass_IsADependency()
        {
            Assert.That(RepositoryDependentsIn(FixtureArchitecture.Value, FixtureNamespace), Does.Contain(typeof(ConcreteRepositoryDependent).FullName));
        }

        [Test]
        [Ignore(PendingApiDependency)]
        public void ALambdaThatAsksForARepository_IsReportedAgainstTheTypeThatWroteIt()
        {
            var dependents = RepositoryDependentsIn(FixtureArchitecture.Value, FixtureNamespace);

            Assert.That(dependents, Does.Contain(typeof(LambdaDependent).FullName));
            Assert.That(dependents, Has.None.Contain("+<"), "a compiler-made nested type must never be reported by its own name");
        }

        [Test]
        [Ignore(PendingApiDependency)]
        public void AnAsyncMethodThatAsksForARepository_IsReportedAgainstTheTypeThatWroteIt()
        {
            var dependents = RepositoryDependentsIn(FixtureArchitecture.Value, FixtureNamespace);

            Assert.That(dependents, Does.Contain(typeof(AsyncDependent).FullName));
            Assert.That(dependents, Has.None.Contain("+<"), "a compiler-made nested type must never be reported by its own name");
        }

        [Test]
        [Ignore(PendingApiDependency)]
        public void ATypeThatReadsThroughAServiceContractInTheRepositoriesNamespace_IsNotADependency()
        {
            var dependents = RepositoryDependentsIn(FixtureArchitecture.Value, FixtureNamespace);

            Assert.That(dependents, Does.Not.Contain(typeof(ReadContractDependent).FullName));
            Assert.That(dependents, Does.Not.Contain(typeof(UnrelatedType).FullName));
        }

        // The nested type used here is a real one the compiler made, so the test fails if the
        // normalisation does nothing: the input differs from the expected answer.
        [Test]
        [Ignore(PendingApiDependency)]
        public void ACompilerMadeNestedType_NormalisesToItsDeclaringType()
        {
            var nested = typeof(LambdaDependent).GetNestedTypes(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                .Concat(typeof(AsyncDependent).GetNestedTypes(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public))
                .Where(type => type.Name.StartsWith('<'))
                .ToList();

            Assert.That(nested, Is.Not.Empty, "the fixtures no longer produce a compiler-made nested type, so this check proves nothing");

            foreach (var type in nested)
            {
                Assert.That(DeclaringTypeOf(type.FullName!), Is.EqualTo(type.DeclaringType!.FullName), type.FullName);
            }
        }

        // A type the code's author nested on purpose is a type in its own right; only the names the compiler
        // makes stand for the method that caused them.
        [Test]
        [Ignore(PendingApiDependency)]
        public void ANestedTypeSomeoneWrote_KeepsItsOwnName()
        {
            Assert.That(DeclaringTypeOf(typeof(OuterWithAnInnerType.Inner).FullName!), Is.EqualTo(typeof(OuterWithAnInnerType.Inner).FullName));
        }

        [Test]
        [Ignore(PendingApiDependency)]
        public void AnOrdinaryTypeName_IsLeftAlone()
        {
            Assert.That(DeclaringTypeOf(typeof(ConstructorDependent).FullName!), Is.EqualTo(typeof(ConstructorDependent).FullName));
        }

        // ---- Scaffolds: built in the slice named on each pending test ----

        private static void AssertMatchesBaseline(IEnumerable<string> actual, IEnumerable<string> baseline, string fixHint)
        {
            var offending = actual.ToHashSet(StringComparer.Ordinal);
            var listed = baseline.ToHashSet(StringComparer.Ordinal);

            var newcomers = offending.Except(listed).Order(StringComparer.Ordinal).ToList();
            var stale = listed.Except(offending).Order(StringComparer.Ordinal).ToList();

            var problems = new List<string>();

            if (newcomers.Count > 0)
            {
                problems.Add($"Not on the list, and offending: {string.Join(", ", newcomers)}. {fixHint}");
            }

            if (stale.Count > 0)
            {
                problems.Add($"On the list, but no longer offending: {string.Join(", ", stale)}. Good - delete {(stale.Count == 1 ? "it" : "them")} from the list so it cannot come back unseen.");
            }

            if (problems.Count > 0)
            {
                Assert.Fail(string.Join(Environment.NewLine, problems));
            }
        }

        // SCAFFOLD: true
        private static IReadOnlyList<string> GetAllByPredicateCallSites(IEnumerable<ProductionSourceFile> sources)
            => throw new AssertionException("SCAFFOLD: GetAllByPredicateCallSites is not built yet");

        // SCAFFOLD: true
        private static IReadOnlyCollection<string> RepositoryDependentsIn(ArchitectureModel architecture, string namespacePattern)
            => throw new AssertionException("SCAFFOLD: RepositoryDependentsIn is not built yet");

        // SCAFFOLD: true
        private static string DeclaringTypeOf(string typeFullName)
            => throw new AssertionException("SCAFFOLD: DeclaringTypeOf is not built yet");
    }
}

namespace Lighthouse.Backend.Tests.Architecture.RatchetFixtures
{
    internal sealed class ConstructorDependent(IRepository<Team> teams)
    {
        public IRepository<Team> Teams { get; } = teams;
    }

    internal sealed class GenericArgumentDependent
    {
        public object Resolve(IServiceProvider services) => services.GetRequiredService<IRepository<Team>>();
    }

    internal sealed class ConcreteRepositoryDependent(TeamRepository teams)
    {
        public TeamRepository Teams { get; } = teams;
    }

    internal sealed class LambdaDependent
    {
        public Func<IServiceProvider, object> Factory { get; } = services => services.GetRequiredService<IRepository<Team>>();
    }

    internal sealed class AsyncDependent
    {
        public async Task<object> ResolveAsync(IServiceProvider services)
        {
            await Task.Yield();
            return services.GetRequiredService<IRepository<Team>>();
        }
    }

    internal sealed class ReadContractDependent(IFeatureListReader reader)
    {
        public IFeatureListReader Reader { get; } = reader;
    }

    internal sealed class OuterWithAnInnerType
    {
        public sealed class Inner
        {
            public int Answer => 42;
        }
    }

    internal sealed class UnrelatedType
    {
        public int Answer => 42;
    }
}
