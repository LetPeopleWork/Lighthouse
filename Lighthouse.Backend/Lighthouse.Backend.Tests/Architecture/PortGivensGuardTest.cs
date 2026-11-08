using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API.Integration.Refinement;
using Microsoft.Extensions.DependencyInjection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using static Lighthouse.Backend.Tests.Architecture.TestHostScanning;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A Refinement scenario at the port sets up its Team only through <see cref="RefinementPortGivens"/>, because the
    /// host rows that compare what a port Given stores with what the settings form saves read that builder's inputs
    /// and nothing else. A port class that wrote a Team itself would step around them, and could pass on a Team no
    /// save produces. Blackout days and rules, which a port class may add, go through their own services, which
    /// check them as the routes do. This reads the compiled port classes, with the compiler-made types nested in
    /// them, for any call that writes a Team, its refinement settings, or a blackout table directly.
    /// </summary>
    [TestFixture]
    public class PortGivensGuardTest
    {
        private static readonly string ThePortBase = KeyOf(typeof(RefinementAtThePortTest));

        private static readonly string Planted = KeyOf(typeof(PlantedPortClasses));

        private static readonly HashSet<string> TypesAGivenMustNotSet = new(StringComparer.Ordinal)
        {
            typeof(Team).FullName!,
        };

        private static readonly string TheRefinementSettingsNamespace = typeof(RefinementSettings).Namespace!;

        private static readonly HashSet<string> TablesWrittenOnlyThroughTheirDoors = new(StringComparer.Ordinal)
        {
            typeof(Team).FullName!,
            typeof(BlackoutPeriod).FullName!,
            typeof(RecurringBlackoutRule).FullName!,
        };

        private static readonly HashSet<string> RepositoryWrites = new(StringComparer.Ordinal) { "Add", "AddRange", "Update", "Remove" };

        private static readonly HashSet<string> TheContextsTablesOfThose = new(StringComparer.Ordinal)
        {
            "get_" + nameof(LighthouseAppContext.Teams),
            "get_" + nameof(LighthouseAppContext.BlackoutPeriods),
            "get_" + nameof(LighthouseAppContext.RecurringBlackoutRules),
        };

        private ModuleDefinition module = null!;

        private List<TypeDefinition> everyNamedType = [];

        [OneTimeSetUp]
        public void ReadTheTestAssemblyOnce()
        {
            module = IlDependencies.ReadModule(typeof(PortGivensGuardTest).Assembly);
            everyNamedType = [.. module.Types.SelectMany(TypeAndItsNestedTypes).Where(type => !type.Name.StartsWith('<') && !IsCompilerMade(type))];
        }

        [OneTimeTearDown]
        public void ReleaseTheTestAssembly() => module.Dispose();

        [Test]
        public void No_port_scenario_sets_up_a_Team_except_through_the_builder()
        {
            var portClasses = everyNamedType.Where(IsAPortClass).Where(type => !IsInside(type, Planted)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(portClasses.Select(type => type.FullName), Does.Contain(ThePortBase),
                    "the guard reads the port base, so a port class it finds nothing in was really read");
                Assert.That(portClasses.SelectMany(WritesIn), Is.Empty,
                    $"a port class sets up a Team only through {nameof(RefinementPortGivens)}, and adds blackouts only through their services");
            }
        }

        [TestCase(typeof(PlantedPortClasses.WritesRefinementSettingsItself), "RefinementSettings")]
        [TestCase(typeof(PlantedPortClasses.ChangesTheCadenceItHasRead), "Cadence")]
        [TestCase(typeof(PlantedPortClasses.AddsATeamThroughTheRepository), "Add")]
        [TestCase(typeof(PlantedPortClasses.AddsABlackoutThroughTheRepository), "Add")]
        [TestCase(typeof(PlantedPortClasses.ReachesTheTeamsTableThroughTheContext), "Teams")]
        public void The_guard_sees_a_port_class_that_sets_up_a_Team_itself(Type planted, string whatItCalls)
        {
            var plantedClass = everyNamedType.Single(type => type.FullName == KeyOf(planted));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(IsAPortClass(plantedClass), Is.True, "the planted class derives from the port base");
                Assert.That(WritesIn(plantedClass), Has.Some.Contains(whatItCalls));
            }
        }

        private static bool IsAPortClass(TypeDefinition type)
        {
            for (var current = type; current is not null; current = current.BaseType?.Resolve())
            {
                if (current.FullName == ThePortBase)
                {
                    return true;
                }
            }

            return false;
        }

        private static IEnumerable<string> WritesIn(TypeDefinition portClass)
            => TypeAndItsNestedTypes(portClass)
                .SelectMany(type => type.Methods)
                .Where(method => method.HasBody)
                .SelectMany(method => method.Body.Instructions
                    .Select(instruction => instruction.Operand)
                    .OfType<MethodReference>()
                    .Where(IsAWriteOfASetUpTable)
                    .Select(called => $"{portClass.Name}.{method.Name} calls {called.DeclaringType.Name}.{called.Name}"));

        private static bool IsAWriteOfASetUpTable(MethodReference called)
        {
            var declaringType = called.DeclaringType;
            if (called.Name.StartsWith("set_", StringComparison.Ordinal))
            {
                return TypesAGivenMustNotSet.Contains(declaringType.FullName) || declaringType.Namespace == TheRefinementSettingsNamespace;
            }

            if (declaringType.FullName == typeof(LighthouseAppContext).FullName)
            {
                return TheContextsTablesOfThose.Contains(called.Name);
            }

            return RepositoryWrites.Contains(called.Name)
                && declaringType is GenericInstanceType repository
                && repository.ElementType.FullName == typeof(IRepository<>).FullName
                && TablesWrittenOnlyThroughTheirDoors.Contains(repository.GenericArguments[0].FullName);
        }

        private static bool IsCompilerMade(TypeDefinition type)
            => type.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

        private static bool IsInside(TypeDefinition type, string outer) => type.FullName.StartsWith(outer + "/", StringComparison.Ordinal);

        private static string KeyOf(Type type) => type.FullName!.Replace('+', '/');
    }

    /// <summary>Port classes that set up a Team the way a port class must not, so the guard can be seen to catch each kind. None has a test.</summary>
    public static class PlantedPortClasses
    {
        public sealed class WritesRefinementSettingsItself : RefinementAtThePortTest
        {
            public static void Plant(Team team) => team.RefinementSettings = new RefinementSettings();
        }

        public sealed class ChangesTheCadenceItHasRead : RefinementAtThePortTest
        {
            public static void Plant(RefinementSettings settings) => settings.Cadence = null;
        }

        public sealed class AddsATeamThroughTheRepository : RefinementAtThePortTest
        {
            public void Plant(Team team) => NewScope().ServiceProvider.GetRequiredService<IRepository<Team>>().Add(team);
        }

        public sealed class AddsABlackoutThroughTheRepository : RefinementAtThePortTest
        {
            public void Plant(BlackoutPeriod blackout) => NewScope().ServiceProvider.GetRequiredService<IRepository<BlackoutPeriod>>().Add(blackout);
        }

        public sealed class ReachesTheTeamsTableThroughTheContext : RefinementAtThePortTest
        {
            public int Plant() => NewScope().ServiceProvider.GetRequiredService<LighthouseAppContext>().Teams.Count();
        }
    }
}
