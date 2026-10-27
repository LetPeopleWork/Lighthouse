using ArchUnitNET.Loader;
using ArchUnitNET.NUnit;
using Lighthouse.Backend.Models.KernelFixtures.Offending;
using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.KernelFixtures;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The two kernel rules hold on the real code with no list of exceptions, so the real code alone cannot show
    /// that a new offender would fail. Here the same rules run over made-up types: a Model that reaches into
    /// Services must fail and be named, and one that does not must pass.
    /// </summary>
    [TestFixture]
    [Category("stories-6367-6369")]
    [Category("slice-s04a")]
    public class KernelRulesCatchANewOffenderArchUnitTest
    {
        private const string OffendingModels = @"^Lighthouse\.Backend\.Models\.KernelFixtures\.Offending(\.|$)";

        private const string CleanModels = @"^Lighthouse\.Backend\.Models\.KernelFixtures\.Clean(\.|$)";

        private const string FixtureServices = @"^Lighthouse\.Backend\.Services\.KernelFixtures(\.|$)";

        private static readonly Lazy<ArchitectureModel> FixtureArchitecture = new(() => new ArchLoader()
            .LoadAssemblies(typeof(OffendingModel).Assembly)
            .Build());

        [Test]
        [Ignore("pending: S04a-6")]
        public void AModelThatReachesIntoServices_FailsTheKernelRuleAndIsNamed()
        {
            var rule = KernelRules.ModelsDoNotDependOnServices(OffendingModels, FixtureServices);

            var failure = Assert.Throws<AssertionException>(() => rule.Check(FixtureArchitecture.Value));

            Assert.That(failure!.Message, Does.Contain(typeof(OffendingModel).FullName));
        }

        [Test]
        [Ignore("pending: S04a-6")]
        public void AModelThatStaysInsideTheKernel_PassesTheKernelRule()
        {
            var rule = KernelRules.ModelsDoNotDependOnServices(CleanModels, FixtureServices);

            Assert.DoesNotThrow(() => rule.Check(FixtureArchitecture.Value));
        }

        [Test]
        [Ignore("pending: S04a-4")]
        public void AModelThatReachesIntoTheUpdateQueue_FailsTheUpdateQueueRuleAndIsNamed()
        {
            var rule = KernelRules.ModelsDoNotDependOnTheUpdateQueue(OffendingModels);

            var failure = Assert.Throws<AssertionException>(() => rule.Check(FixtureArchitecture.Value));

            Assert.That(
                failure!.Message,
                Does.Contain(typeof(OffendingModel).FullName).And.Contain(typeof(AFixtureUpdateKind).FullName),
                "a Model naming an enum that lives in the update queue's namespace must fail");
        }

        [Test]
        [Ignore("pending: S04a-4")]
        public void AModelThatStaysInsideTheKernel_PassesTheUpdateQueueRule()
        {
            var rule = KernelRules.ModelsDoNotDependOnTheUpdateQueue(CleanModels);

            Assert.DoesNotThrow(() => rule.Check(FixtureArchitecture.Value));
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
    internal sealed class OffendingModel(
        Lighthouse.Backend.Services.KernelFixtures.AServiceType service,
        Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.KernelFixtures.AFixtureUpdateKind kind)
    {
        public Lighthouse.Backend.Services.KernelFixtures.AServiceType Service { get; } = service;

        public Lighthouse.Backend.Services.Implementation.BackgroundServices.Update.KernelFixtures.AFixtureUpdateKind Kind { get; } = kind;
    }
}

namespace Lighthouse.Backend.Models.KernelFixtures.Clean
{
    internal sealed class CleanModel
    {
        public int Answer => 42;
    }
}
