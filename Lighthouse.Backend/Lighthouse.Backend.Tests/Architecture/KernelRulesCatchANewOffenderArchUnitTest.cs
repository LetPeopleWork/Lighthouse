using Lighthouse.Backend.Models.KernelFixtures.Clean;
using Lighthouse.Backend.Models.KernelFixtures.Offending;
using Lighthouse.Backend.Models.KernelFixtures.OffendingInAnAsyncBody;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.KernelFixtures;
using Lighthouse.Backend.Services.KernelFixtures;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Tests.Architecture.KernelRowFixtures;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture.KernelRowFixtures
{
    internal interface IUsedByOneContext
    {
    }

    internal interface IUsedByTwoContexts
    {
    }
}

namespace Lighthouse.Backend.Tests.Architecture.KernelRowFixtures.Modules.Alpha
{
    internal sealed class AlphaRecord : IUsedByOneContext, IUsedByTwoContexts
    {
    }
}

namespace Lighthouse.Backend.Tests.Architecture.KernelRowFixtures.Modules.Beta
{
    internal sealed class BetaRecord : IUsedByTwoContexts
    {
    }
}

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The two kernel rules hold on the real code with no list of exceptions, so the real code alone cannot show
    /// that a new offender would fail. Here the same rules run over made-up types: a Model that reaches into
    /// Services or the update queue must fail and be named, and one that does not must pass.
    /// </summary>
    [TestFixture]
    [Category("stories-6367-6369")]
    [Category("slice-s04a")]
    public class KernelRulesCatchANewOffenderArchUnitTest
    {
        private const string OffendingModels = @"^Lighthouse\.Backend\.Models\.KernelFixtures\.Offending(\.|$)";

        private const string CleanModels = @"^Lighthouse\.Backend\.Models\.KernelFixtures\.Clean(\.|$)";

        private const string AsyncOnlyModels = @"^Lighthouse\.Backend\.Models\.KernelFixtures\.OffendingInAnAsyncBody(\.|$)";

        private const string FixtureServices = @"^Lighthouse\.Backend\.Services\.KernelFixtures(\.|$)";

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(OffendingModel).Assembly));

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        // Contexts are the Modules.<X> namespaces only; the API, the data layer, the platform and the loose services are not.
        private static List<string> ContextsUsing(ModuleDefinition module, string modulesRoot, string rowFullName)
        {
            var contextRoot = new Regex($@"^{Regex.Escape(modulesRoot)}\.(?<context>[A-Za-z0-9_]+)(\.|$)", RegexOptions.None, TimeSpan.FromSeconds(1));

            return [.. module.Types
                .Select(type => contextRoot.Match(type.Namespace))
                .Where(found => found.Success)
                .Select(found => found.Groups["context"].Value)
                .Distinct(StringComparer.Ordinal)
                .Where(context => IlDependencies.DependentsIn(module, $@"^{Regex.Escape(modulesRoot)}\.{Regex.Escape(context)}(\.|$)", type => type.FullName == rowFullName).Count > 0)
                .Order(StringComparer.Ordinal)];
        }

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (FixtureModule.IsValueCreated)
            {
                FixtureModule.Value.Dispose();
            }
        }

        [OneTimeTearDown]
        public void DisposeTheProductionModule()
        {
            if (ProductionModule.IsValueCreated)
            {
                ProductionModule.Value.Dispose();
            }
        }

        [Test]
        public void EveryKernelRow_IsUsedByTwoOrMoreContexts()
        {
            var contextsByRow = KernelCatalogue.TheKernelTypes.ToDictionary(
                row => $"{KernelCatalogue.TheModelsRoot}.{row}",
                row => ContextsUsing(ProductionModule.Value, "Lighthouse.Backend.Modules", $"{KernelCatalogue.TheModelsRoot}.{row}"));

            var tooFew = contextsByRow.Where(entry => entry.Value.Count < 2).Select(entry => $"{entry.Key} is used by [{string.Join(", ", entry.Value)}]").ToList();

            Assert.That(tooFew, Is.Empty,
                $"{tooFew.Count} of the {contextsByRow.Count} Kernel rows are used by fewer than two contexts: {string.Join("; ", tooFew)}." + Environment.NewLine +
                "Why it matters: a type only one context uses is that context's own type, and the shared kernel is meant for what several contexts build on. " +
                "How to fix: move the type to its single user, and take its Kernel row off the catalogue.");
        }

        [Test]
        public void AKernelRowUsedByOnlyOneContext_IsNamedAndOneUsedByTwoIsNot()
        {
            var rows = new[] { typeof(IUsedByOneContext).FullName!, typeof(IUsedByTwoContexts).FullName! };
            var root = "Lighthouse.Backend.Tests.Architecture.KernelRowFixtures.Modules";

            var tooFew = rows.Where(row => ContextsUsing(FixtureModule.Value, root, row).Count < 2).ToList();

            Assert.That(tooFew, Is.EqualTo(new[] { typeof(IUsedByOneContext).FullName }),
                $"Of {rows.Length} made-up Kernel rows the rule named [{string.Join("; ", tooFew)}], and should name only the one that a single context uses." + Environment.NewLine +
                "Why it matters: a rule that cannot fail lets a context-owned type sit in the kernel. How to fix: count the distinct Modules contexts whose types reference the row.");
        }

        [Test]
        public void AModelThatReachesIntoServices_FailsTheKernelRuleAndIsNamed()
        {
            var verdict = KernelRules.ModelsDoNotDependOnServices(FixtureModule.Value, OffendingModels, FixtureServices);

            var failure = Assert.Throws<AssertionException>(verdict.Check);

            Assert.That(failure!.Message, Does.Contain(typeof(OffendingModel).FullName));
        }

        [Test]
        public void AModelThatStaysInsideTheKernel_PassesTheKernelRule()
        {
            var verdict = KernelRules.ModelsDoNotDependOnServices(FixtureModule.Value, CleanModels, FixtureServices);

            Assert.That(verdict.Examined, Does.Contain(typeof(CleanModel).FullName), "the rule must have looked at the clean Model, or passing proves nothing");
            Assert.DoesNotThrow(verdict.Check);
        }

        [Test]
        public void AModelThatReachesIntoTheUpdateQueue_FailsTheUpdateQueueRuleAndIsNamed()
        {
            var verdict = KernelRules.ModelsDoNotDependOnTheUpdateQueue(FixtureModule.Value, OffendingModels);

            var failure = Assert.Throws<AssertionException>(verdict.Check);

            Assert.That(
                failure!.Message,
                Does.Contain(typeof(OffendingModel).FullName).And.Contain(typeof(AFixtureUpdateKind).FullName),
                "a Model naming an enum that lives in the update queue's namespace must fail");
        }

        [Test]
        public void AModelThatReachesIntoServicesOnlyInsideAnAsyncMethod_FailsTheKernelRuleAndIsNamed()
        {
            var verdict = KernelRules.ModelsDoNotDependOnServices(FixtureModule.Value, AsyncOnlyModels, FixtureServices);

            var failure = Assert.Throws<AssertionException>(verdict.Check);

            Assert.That(failure!.Message, Does.Contain(typeof(ServicesOnlyInAnAsyncBodyModel).FullName));
        }

        [Test]
        public void AModelThatReachesIntoTheUpdateQueueOnlyInsideAnAsyncMethod_FailsTheUpdateQueueRuleAndIsNamed()
        {
            var verdict = KernelRules.ModelsDoNotDependOnTheUpdateQueue(FixtureModule.Value, AsyncOnlyModels);

            var failure = Assert.Throws<AssertionException>(verdict.Check);

            Assert.That(failure!.Message, Does.Contain(typeof(UpdateQueueOnlyInAnAsyncBodyModel).FullName));
        }

        [Test]
        public void AModelThatStaysInsideTheKernel_PassesTheUpdateQueueRule()
        {
            var verdict = KernelRules.ModelsDoNotDependOnTheUpdateQueue(FixtureModule.Value, CleanModels);

            Assert.That(verdict.Examined, Does.Contain(typeof(CleanModel).FullName), "the rule must have looked at the clean Model, or passing proves nothing");
            Assert.DoesNotThrow(verdict.Check);
        }
    }
}

namespace Lighthouse.Backend.Services.KernelFixtures
{
    internal sealed class AServiceType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.KernelFixtures
{
    internal enum AFixtureUpdateKind
    {
        Team,
        Features,
    }
}

namespace Lighthouse.Backend.Models.KernelFixtures.Offending
{
    internal sealed class OffendingModel(AServiceType service, AFixtureUpdateKind kind)
    {
        public AServiceType Service { get; } = service;

        public AFixtureUpdateKind Kind { get; } = kind;
    }
}

namespace Lighthouse.Backend.Models.KernelFixtures.OffendingInAnAsyncBody
{
    internal sealed class ServicesOnlyInAnAsyncBodyModel
    {
        public async Task<int> AnswerAsync()
        {
            await Task.Yield();
            return new AServiceType().Answer;
        }
    }

    internal sealed class UpdateQueueOnlyInAnAsyncBodyModel
    {
        public async Task<int> CountAsync()
        {
            await Task.Yield();
            return Enum.GetValues<AFixtureUpdateKind>().Length;
        }
    }
}

namespace Lighthouse.Backend.Models.KernelFixtures.Clean
{
    internal sealed class CleanModel
    {
        public int Answer => 42;
    }
}
