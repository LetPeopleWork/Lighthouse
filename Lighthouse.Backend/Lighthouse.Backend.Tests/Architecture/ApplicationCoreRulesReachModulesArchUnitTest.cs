using Lighthouse.Backend.Modules.CoreRulesFixtures;
using Lighthouse.Backend.Modules.CoreRulesFixtures.Handlers;
using Lighthouse.Backend.Models.CoreRulesFixtures.ReachingAModule;
using Lighthouse.Backend.Models.CoreRulesFixtures.ReachingAService;
using Lighthouse.Backend.Models.CoreRulesFixtures.ReachingNoCore;
using Lighthouse.Backend.Services.CoreRulesFixtures;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.DomainEvents.CoreRulesFixtures;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The application core is moving out of Services into modules one type at a time. A rule that only looks at
    /// Services would quietly stop checking a type the day it moves, and the real code alone cannot show that,
    /// because it holds no offender. Here the same rules run over made-up offenders: one placed under Services and
    /// the very same one placed under Modules must both be reported, and a compliant twin next to each must not be,
    /// so a rule that simply flags everything it looks at fails too. On the real code, widening a rule must never
    /// make it look at fewer types than before, and every domain-event handler must be looked at, wherever it lives.
    /// </summary>
    [TestFixture]
    public class ApplicationCoreRulesReachModulesArchUnitTest
    {
        private const string ModelsReachingAService = @"^Lighthouse\.Backend\.Models\.CoreRulesFixtures\.(ReachingAService|ReachingNoCore)$";

        private const string ModelsReachingAModule = @"^Lighthouse\.Backend\.Models\.CoreRulesFixtures\.(ReachingAModule|ReachingNoCore)$";

        private static readonly string TypeReachingNoCore = typeof(ATypeReachingNoCore).FullName!;

        private const string ATypeThatIsNotThere = "Lighthouse.Backend.Models.CoreRulesFixtures.ReachingNoCore.ATypeRenamedOrMovedAway";

        private static readonly Lazy<ModuleDefinition> FixtureModule = new(() => IlDependencies.ReadModule(typeof(ApplicationCoreRulesReachModulesArchUnitTest).Assembly));

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(BaseMetricsService).Assembly));

        public enum CorePlacement
        {
            UnderServices,
            UnderModules,
        }

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { FixtureModule, ProductionModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [TestCase(CorePlacement.UnderServices)]
        [TestCase(CorePlacement.UnderModules)]
        public void ACoreTypeThatReachesTheApi_IsReportedWhereverTheCoreTypeLives(CorePlacement placement)
        {
            var offender = CoreTypeReachingTheApi(placement);

            var verdict = ApplicationCoreRules.CoreDoesNotDependOnApi(FixtureModule.Value);

            ThenTheRuleReportsOnlyTheOffender(verdict, offender, CoreTypeReachingNothing(placement), placement, "the application core must not depend back on the API");
        }

        [TestCase(CorePlacement.UnderServices)]
        [TestCase(CorePlacement.UnderModules)]
        public void ACoreTypeThatReachesTheApiOnlyInsideAnAsyncMethod_IsReportedWhereverTheCoreTypeLives(CorePlacement placement)
        {
            var offender = CoreTypeReachingTheApiOnlyInsideAnAsyncMethod(placement);

            var verdict = ApplicationCoreRules.CoreDoesNotDependOnApi(FixtureModule.Value);

            ThenTheRuleReportsOnlyTheOffender(verdict, offender, CoreTypeReachingNothing(placement), placement, "the application core must not depend back on the API, not even inside an async method, which a Release build hides from a dependency graph");
        }

        [TestCase(CorePlacement.UnderServices)]
        [TestCase(CorePlacement.UnderModules)]
        public void AParentPlacementKernelThatReachesTheCore_IsReportedWhereverTheCoreTypeLives(CorePlacement placement)
        {
            var kernelType = TypeReachingTheCore(placement);

            var verdict = ApplicationCoreRules.ParentPlacementKernelDependsOnNoCoreOrData(FixtureModule.Value, [kernelType, TypeReachingNoCore]);

            ThenTheRuleReportsOnlyTheOffender(verdict, kernelType, TypeReachingNoCore, placement, "deciding under which parent a record hangs must not reach the application core");
        }

        [TestCase(CorePlacement.UnderServices)]
        [TestCase(CorePlacement.UnderModules)]
        public void AnArchivedReadPathTypeThatReachesTheCore_IsReportedWhereverTheCoreTypeLives(CorePlacement placement)
        {
            var projectionType = TypeReachingTheCore(placement);

            var verdict = ApplicationCoreRules.ArchivedReadPathReachesNoRecalculation(FixtureModule.Value, [projectionType, TypeReachingNoCore]);

            ThenTheRuleReportsOnlyTheOffender(verdict, projectionType, TypeReachingNoCore, placement, "the view of a retired Delivery must reach nothing it could recalculate from");
        }

        [TestCase(CorePlacement.UnderServices)]
        [TestCase(CorePlacement.UnderModules)]
        public void AModelThatReachesTheCore_IsReportedWhereverTheCoreTypeLives(CorePlacement placement)
        {
            var model = TypeReachingTheCore(placement);

            var verdict = KernelRules.ModelsDoNotDependOnServices(FixtureModule.Value, ModelsReaching(placement));

            ThenTheRuleReportsOnlyTheOffender(verdict, model, TypeReachingNoCore, placement, "the Models are the shared kernel and must not depend on the application core");
        }

        [TestCase(CorePlacement.UnderServices)]
        [TestCase(CorePlacement.UnderModules)]
        public void AHandlerThatUsesTheServiceLocator_IsReportedWhereverItLives(CorePlacement placement)
        {
            var handler = HandlerUsingTheServiceLocator(placement);

            var verdict = ApplicationCoreRules.HandlersUseNoServiceLocator(FixtureModule.Value);

            ThenTheRuleReportsOnlyTheOffender(verdict, handler, CompliantHandler(placement), placement, "a domain-event handler must take its collaborators, not look them up");
        }

        [TestCase(CorePlacement.UnderServices)]
        [TestCase(CorePlacement.UnderModules)]
        public void AHandlerThatSavesWithoutAwaiting_IsReportedWhereverItLives(CorePlacement placement)
        {
            var handler = HandlerSavingWithoutAwaiting(placement);

            var verdict = ApplicationCoreRules.HandlersAwaitRepositorySaves(FixtureModule.Value);

            ThenTheRuleReportsOnlyTheOffender(verdict, handler, CompliantHandler(placement), placement, "a domain-event handler must await its repository save");
        }

        [Test]
        public void TheApiRule_LooksAtEveryRealCoreTypeItLookedAtBefore()
        {
            var widened = ApplicationCoreRules.CoreDoesNotDependOnApi(ProductionModule.Value);
            var servicesOnly = ApplicationCoreRules.CoreDoesNotDependOnApi(ProductionModule.Value, KernelRules.TheServices);

            Assert.That(servicesOnly.Examined, Is.Not.Empty, "The Services-only run looked at no real type. Why it matters: comparing against an empty set proves nothing. How to fix: the Services pattern must match the real Services namespaces.");
            ThenNothingDroppedOut(widened, servicesOnly.Examined, "every type in Services");
        }

        [Test]
        public void AParentPlacementKernelTypeThatIsNotThere_FailsTheRuleAndIsNamed()
        {
            var verdict = ApplicationCoreRules.ParentPlacementKernelDependsOnNoCoreOrData(FixtureModule.Value, [TypeReachingNoCore, ATypeThatIsNotThere]);

            ThenTheCheckFailsNaming(verdict, ATypeThatIsNotThere, "deciding under which parent a record hangs");
        }

        [Test]
        public void AnArchivedReadPathTypeThatIsNotThere_FailsTheRuleAndIsNamed()
        {
            var verdict = ApplicationCoreRules.ArchivedReadPathReachesNoRecalculation(FixtureModule.Value, [TypeReachingNoCore, ATypeThatIsNotThere]);

            ThenTheCheckFailsNaming(verdict, ATypeThatIsNotThere, "showing a retired Delivery");
        }

        [Test]
        public void TheServiceLocatorRule_LooksAtEveryRealTypeItLookedAtBeforeAndAtEveryRealHandler()
        {
            var widened = ApplicationCoreRules.HandlersUseNoServiceLocator(ProductionModule.Value);
            var byNamespace = ApplicationCoreRules.HandlersUseNoServiceLocator(ProductionModule.Value, HandlerSelection.ByDomainEventsNamespace);

            ThenNothingDroppedOut(widened, [.. byNamespace.Examined, .. RealDomainEventHandlers()], "every type in the domain-events namespace, the dispatcher included, and every real domain-event handler");
        }

        [Test]
        public void TheAwaitedSaveRule_LooksAtEveryRealTypeItLookedAtBeforeAndAtEveryRealHandler()
        {
            var widened = ApplicationCoreRules.HandlersAwaitRepositorySaves(ProductionModule.Value);
            var byNamespace = ApplicationCoreRules.HandlersAwaitRepositorySaves(ProductionModule.Value, HandlerSelection.ByDomainEventsNamespace);

            ThenNothingDroppedOut(widened, [.. byNamespace.Examined, .. RealDomainEventHandlers()], "every type in the domain-events namespace, the dispatcher included, and every real domain-event handler");
        }

        private static string CoreTypeReachingTheApi(CorePlacement placement) => placement switch
        {
            CorePlacement.UnderServices => typeof(AServiceTypeThatReachesTheApi).FullName!,
            CorePlacement.UnderModules => typeof(AModuleTypeThatReachesTheApi).FullName!,
            _ => throw new ArgumentOutOfRangeException(nameof(placement)),
        };

        private static string CoreTypeReachingTheApiOnlyInsideAnAsyncMethod(CorePlacement placement) => placement switch
        {
            CorePlacement.UnderServices => typeof(AServiceTypeThatReachesTheApiOnlyInsideAnAsyncMethod).FullName!,
            CorePlacement.UnderModules => typeof(AModuleTypeThatReachesTheApiOnlyInsideAnAsyncMethod).FullName!,
            _ => throw new ArgumentOutOfRangeException(nameof(placement)),
        };

        private static string CoreTypeReachingNothing(CorePlacement placement) => placement switch
        {
            CorePlacement.UnderServices => typeof(AServiceType).FullName!,
            CorePlacement.UnderModules => typeof(AModuleType).FullName!,
            _ => throw new ArgumentOutOfRangeException(nameof(placement)),
        };

        private static string CompliantHandler(CorePlacement placement) => placement switch
        {
            CorePlacement.UnderServices => typeof(ServicesHandlerTakingItsCollaboratorsAndAwaitingItsSave).FullName!,
            CorePlacement.UnderModules => typeof(ModuleHandlerTakingItsCollaboratorsAndAwaitingItsSave).FullName!,
            _ => throw new ArgumentOutOfRangeException(nameof(placement)),
        };

        private static string TypeReachingTheCore(CorePlacement placement) => placement switch
        {
            CorePlacement.UnderServices => typeof(ATypeReachingAService).FullName!,
            CorePlacement.UnderModules => typeof(ATypeReachingAModule).FullName!,
            _ => throw new ArgumentOutOfRangeException(nameof(placement)),
        };

        private static string ModelsReaching(CorePlacement placement) => placement switch
        {
            CorePlacement.UnderServices => ModelsReachingAService,
            CorePlacement.UnderModules => ModelsReachingAModule,
            _ => throw new ArgumentOutOfRangeException(nameof(placement)),
        };

        private static string HandlerUsingTheServiceLocator(CorePlacement placement) => placement switch
        {
            CorePlacement.UnderServices => typeof(ServicesHandlerUsingTheServiceLocator).FullName!,
            CorePlacement.UnderModules => typeof(ModuleHandlerUsingTheServiceLocator).FullName!,
            _ => throw new ArgumentOutOfRangeException(nameof(placement)),
        };

        private static string HandlerSavingWithoutAwaiting(CorePlacement placement) => placement switch
        {
            CorePlacement.UnderServices => typeof(ServicesHandlerSavingWithoutAwaiting).FullName!,
            CorePlacement.UnderModules => typeof(ModuleHandlerSavingWithoutAwaiting).FullName!,
            _ => throw new ArgumentOutOfRangeException(nameof(placement)),
        };

        /// <summary>
        /// Every concrete type in the real code that implements IDomainEventHandler&lt;&gt;, found from the interface
        /// rather than from the namespace it sits in.
        /// </summary>
        private static List<string> RealDomainEventHandlers()
        {
            var handlerInterface = typeof(IDomainEventHandler<>).FullName;

            var handlers = ProductionModule.Value.Types
                .SelectMany(TypeAndItsNestedTypes)
                .Where(type => type is { IsAbstract: false, IsInterface: false })
                .Where(type => type.Interfaces.Any(implemented => implemented.InterfaceType is GenericInstanceType generic && generic.ElementType.FullName == handlerInterface))
                .Select(type => type.FullName.Replace('/', '+'))
                .ToList();

            Assert.That(handlers, Is.Not.Empty, "the real code must hold domain-event handlers, or checking that each is looked at proves nothing");

            return handlers;
        }

        private static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
            => [type, .. type.NestedTypes.SelectMany(TypeAndItsNestedTypes)];

        private static void ThenTheRuleReportsOnlyTheOffender(KernelRuleVerdict verdict, string offender, string compliantTwin, CorePlacement placement, string why)
        {
            var reported = verdict.Offenders.Select(dependent => dependent.TypeName).ToList();

            Assert.That(
                reported,
                Does.Contain(offender),
                $"The made-up offender {offender} ({placement}) was not reported. Why it matters: {why}, and a type moved " +
                "from Services into a module must stay under the same rule. How to fix: the rule's application-core " +
                "pattern, or its handler selection, must take in Lighthouse.Backend.Modules as it takes in Services.");
            Assert.That(
                reported,
                Does.Not.Contain(compliantTwin),
                $"The compliant type {compliantTwin} ({placement}) was reported although it breaks nothing. Why it matters: " +
                "a rule that flags every type it looks at would bury the real offender among false ones. How to fix: " +
                "widen what the rule looks at, not what it counts as a breach.");
        }

        private static void ThenTheCheckFailsNaming(KernelRuleVerdict verdict, string missingSubject, string job)
        {
            var failure = Assert.Throws<AssertionException>(
                verdict.Check,
                $"The rule passed although the named type {missingSubject} does not exist. Why it matters: the rule names " +
                $"the types doing the job of {job}; when one of them is renamed or moved, the rule silently checks fewer " +
                "types, possibly none, and stays green. How to fix: the verdict's Check() must fail when a named type is " +
                "not found, and name that type.");

            Assert.That(
                failure!.Message,
                Does.Contain(missingSubject),
                $"The rule failed without naming the missing type {missingSubject}. Why it matters: the maintainer must " +
                "see which named type to update. How to fix: put the missing type's full name in the failure message.");
        }

        private static void ThenNothingDroppedOut(KernelRuleVerdict widened, IEnumerable<string> mustStillBeExamined, string what)
        {
            var dropped = mustStillBeExamined.Except(widened.Examined, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

            Assert.That(
                dropped,
                Is.Empty,
                $"The rule does not look at {string.Join(", ", dropped)}. Why it matters: it must look at {what}, so " +
                "widening it to the modules can never let a real type escape it. How to fix: select at least the types " +
                "the rule selected before, together with the new ones.");
        }
    }
}

namespace Lighthouse.Backend.API.CoreRulesFixtures
{
    internal sealed class AnApiType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Services.CoreRulesFixtures
{
    using Lighthouse.Backend.API.CoreRulesFixtures;

    internal sealed class AServiceTypeThatReachesTheApi(AnApiType api)
    {
        public AnApiType Api { get; } = api;
    }

    internal sealed class AServiceType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Modules.CoreRulesFixtures
{
    using Lighthouse.Backend.API.CoreRulesFixtures;

    internal sealed class AModuleTypeThatReachesTheApi(AnApiType api)
    {
        public AnApiType Api { get; } = api;
    }

    internal sealed class AModuleType
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Models.CoreRulesFixtures.ReachingAService
{
    using Lighthouse.Backend.Services.CoreRulesFixtures;

    internal sealed class ATypeReachingAService(AServiceType service)
    {
        public AServiceType Service { get; } = service;
    }
}

namespace Lighthouse.Backend.Models.CoreRulesFixtures.ReachingAModule
{
    using Lighthouse.Backend.Modules.CoreRulesFixtures;

    internal sealed class ATypeReachingAModule(AModuleType module)
    {
        public AModuleType Module { get; } = module;
    }
}

namespace Lighthouse.Backend.Models.CoreRulesFixtures.Events
{
    using Lighthouse.Backend.Models.Events;

    internal sealed record AFixtureEvent : IDomainEvent;

    internal interface IFixtureRepository
    {
        void Save();
    }
}

namespace Lighthouse.Backend.Services.Implementation.DomainEvents.CoreRulesFixtures
{
    using Lighthouse.Backend.Models.CoreRulesFixtures.Events;
    using Lighthouse.Backend.Services.Interfaces.DomainEvents;
    using Microsoft.Extensions.DependencyInjection;

    internal sealed class ServicesHandlerUsingTheServiceLocator(IServiceProvider services) : IDomainEventHandler<AFixtureEvent>
    {
        public Task HandleAsync(AFixtureEvent domainEvent, CancellationToken cancellationToken)
        {
            _ = services.GetRequiredService<IFixtureRepository>();
            return Task.CompletedTask;
        }
    }

    internal sealed class ServicesHandlerSavingWithoutAwaiting(IFixtureRepository repository) : IDomainEventHandler<AFixtureEvent>
    {
        public Task HandleAsync(AFixtureEvent domainEvent, CancellationToken cancellationToken)
        {
            repository.Save();
            return Task.CompletedTask;
        }
    }
}

namespace Lighthouse.Backend.Modules.CoreRulesFixtures.Handlers
{
    using Lighthouse.Backend.Models.CoreRulesFixtures.Events;
    using Lighthouse.Backend.Services.Interfaces.DomainEvents;
    using Microsoft.Extensions.DependencyInjection;

    internal sealed class ModuleHandlerUsingTheServiceLocator(IServiceProvider services) : IDomainEventHandler<AFixtureEvent>
    {
        public Task HandleAsync(AFixtureEvent domainEvent, CancellationToken cancellationToken)
        {
            _ = services.GetRequiredService<IFixtureRepository>();
            return Task.CompletedTask;
        }
    }

    internal sealed class ModuleHandlerSavingWithoutAwaiting(IFixtureRepository repository) : IDomainEventHandler<AFixtureEvent>
    {
        public Task HandleAsync(AFixtureEvent domainEvent, CancellationToken cancellationToken)
        {
            repository.Save();
            return Task.CompletedTask;
        }
    }
}

namespace Lighthouse.Backend.Services.CoreRulesFixtures
{
    using Lighthouse.Backend.API.CoreRulesFixtures;

    internal sealed class AServiceTypeThatReachesTheApiOnlyInsideAnAsyncMethod
    {
        public async Task<int> AnswerAsync()
        {
            await Task.Yield();
            return new AnApiType().Answer;
        }
    }
}

namespace Lighthouse.Backend.Modules.CoreRulesFixtures
{
    using Lighthouse.Backend.API.CoreRulesFixtures;

    internal sealed class AModuleTypeThatReachesTheApiOnlyInsideAnAsyncMethod
    {
        public async Task<int> AnswerAsync()
        {
            await Task.Yield();
            return new AnApiType().Answer;
        }
    }
}

namespace Lighthouse.Backend.Models.CoreRulesFixtures.ReachingNoCore
{
    internal sealed class ATypeReachingNoCore
    {
        public int Answer => 42;
    }
}

namespace Lighthouse.Backend.Services.Implementation.DomainEvents.CoreRulesFixtures
{
    using Lighthouse.Backend.Models.CoreRulesFixtures.Events;
    using Lighthouse.Backend.Services.Interfaces.DomainEvents;

    internal sealed class ServicesHandlerTakingItsCollaboratorsAndAwaitingItsSave(IFixtureRepository repository) : IDomainEventHandler<AFixtureEvent>
    {
        public async Task HandleAsync(AFixtureEvent domainEvent, CancellationToken cancellationToken)
        {
            await Task.Yield();
            repository.Save();
        }
    }
}

namespace Lighthouse.Backend.Modules.CoreRulesFixtures.Handlers
{
    using Lighthouse.Backend.Models.CoreRulesFixtures.Events;
    using Lighthouse.Backend.Services.Interfaces.DomainEvents;

    internal sealed class ModuleHandlerTakingItsCollaboratorsAndAwaitingItsSave(IFixtureRepository repository) : IDomainEventHandler<AFixtureEvent>
    {
        public async Task HandleAsync(AFixtureEvent domainEvent, CancellationToken cancellationToken)
        {
            await Task.Yield();
            repository.Save();
        }
    }
}
