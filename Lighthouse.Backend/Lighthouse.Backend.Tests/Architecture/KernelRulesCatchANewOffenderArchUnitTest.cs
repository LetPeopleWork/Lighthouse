using Lighthouse.Backend.Models.KernelFixtures.Clean;
using Lighthouse.Backend.Models.KernelFixtures.Offending;
using Lighthouse.Backend.Models.KernelFixtures.OffendingInAnAsyncBody;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.KernelFixtures;
using Lighthouse.Backend.Services.KernelFixtures;
using Mono.Cecil;

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

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (FixtureModule.IsValueCreated)
            {
                FixtureModule.Value.Dispose();
            }
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
