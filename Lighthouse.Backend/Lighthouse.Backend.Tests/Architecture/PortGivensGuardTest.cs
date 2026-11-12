using System.Reflection;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API.Integration.Refinement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mono.Cecil;
using static Lighthouse.Backend.Tests.Architecture.TestHostScanning;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// A Refinement scenario at the port sets up its Team only through <see cref="RefinementPortGivens"/>, because the
    /// host rows that compare what a port Given stores with what the settings form saves read that builder's inputs
    /// and nothing else. A port class that wrote a Team itself would step around them, and could pass on a Team no
    /// save produces. Blackout days and rules, which a port class may add, go through their own services, which
    /// check them as the routes do. This reads the compiled port classes, with the compiler-made types nested in
    /// them, for any call that writes a Team or anything a Team holds: a setter on it, a read of one of its lists,
    /// or a write of a Team or a blackout table through a repository or the database context.
    /// </summary>
    [TestFixture]
    public class PortGivensGuardTest
    {
        private static readonly string ThePortBase = KeyOf(typeof(RefinementAtThePortTest));

        private static readonly string Planted = KeyOf(typeof(PlantedPortClasses));

        /// <summary>A Team and every type it holds through its properties and theirs, with the types they derive from.</summary>
        private static readonly HashSet<string> TypesATeamHolds = ReachableFrom(typeof(Team));

        private static readonly HashSet<string> TablesWrittenOnlyThroughTheirDoors = new(StringComparer.Ordinal)
        {
            typeof(Team).FullName!,
            typeof(BlackoutPeriod).FullName!,
            typeof(RecurringBlackoutRule).FullName!,
        };

        private static readonly HashSet<string> RepositoryWrites = new(StringComparer.Ordinal) { "Add", "AddRange", "Update", "Remove" };

        private static readonly HashSet<string> TheContextTypes = new(StringComparer.Ordinal)
        {
            typeof(DbContext).FullName!,
            typeof(LighthouseAppContext).FullName!,
        };

        private static readonly HashSet<string> TheContextsTablesOfThose = new(StringComparer.Ordinal)
        {
            "get_" + nameof(LighthouseAppContext.Teams),
            "get_" + nameof(LighthouseAppContext.BlackoutPeriods),
            "get_" + nameof(LighthouseAppContext.RecurringBlackoutRules),
        };

        private static readonly HashSet<string> ContextWrites = new(StringComparer.Ordinal)
        {
            nameof(DbContext.Set), nameof(DbContext.Add), nameof(DbContext.AddAsync), nameof(DbContext.AddRange),
            nameof(DbContext.Update), nameof(DbContext.UpdateRange), nameof(DbContext.Remove), nameof(DbContext.RemoveRange),
            nameof(DbContext.Attach), nameof(DbContext.AttachRange),
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
                    $"a port class sets up a Team, and anything a Team holds, only through {nameof(RefinementPortGivens)}, and adds blackouts only through their services");
            }
        }

        [TestCase(typeof(PlantedPortClasses.WritesRefinementSettingsItself), "RefinementSettings")]
        [TestCase(typeof(PlantedPortClasses.ChangesTheCadenceItHasRead), "Cadence")]
        [TestCase(typeof(PlantedPortClasses.AddsATeamThroughTheRepository), "Add")]
        [TestCase(typeof(PlantedPortClasses.AddsABlackoutThroughTheRepository), "Add")]
        [TestCase(typeof(PlantedPortClasses.ReachesTheTeamsTableThroughTheContext), "Teams")]
        [TestCase(typeof(PlantedPortClasses.ChangesTheModeOfAReadyRule), "set_Mode")]
        [TestCase(typeof(PlantedPortClasses.AddsAToDoStateToATeam), "get_ToDoStates")]
        [TestCase(typeof(PlantedPortClasses.TakesTheTeamsTableFromTheContext), "Set<Team>")]
        [TestCase(typeof(PlantedPortClasses.AddsATeamThroughTheContext), "Add<Team>")]
        [TestCase(typeof(PlantedPortClasses.UpdatesATeamThroughTheRepository), "Update")]
        [TestCase(typeof(PlantedPortClasses.HandsATeamsToDoStatesToAHelperThatAddsOne), "get_ToDoStates")]
        [TestCase(typeof(PlantedPortClasses.AddsADoingStateInALambdaLater), "get_DoingStates")]
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
                .SelectMany(method => WritesInTheBodyOf(method).Select(write => $"{portClass.Name}.{method.Name} {write}"));

        // A list a Team holds can be changed wherever it is handed on to: a helper, a lambda, a field. Following it
        // there is not possible from the calls alone, so a port class may not take such a list off a Team at all.
        private static IEnumerable<string> WritesInTheBodyOf(MethodDefinition method)
            => method.Body.Instructions
                .Select(instruction => instruction.Operand)
                .OfType<MethodReference>()
                .Select(called => IsAWriteOfASetUpTable(called) ? $"calls {NameOf(called)}"
                    : IsAListATeamHolds(called) ? $"calls {NameOf(called)}, a list a Team holds, which it could change"
                    : null)
                .OfType<string>();

        private static bool IsAWriteOfASetUpTable(MethodReference called)
        {
            var declaringType = called.DeclaringType;
            if (called.Name.StartsWith("set_", StringComparison.Ordinal))
            {
                return TypesATeamHolds.Contains(declaringType.GetElementType().FullName);
            }

            if (TheContextTypes.Contains(declaringType.FullName))
            {
                return TheContextsTablesOfThose.Contains(called.Name)
                    || (ContextWrites.Contains(called.Name)
                        && called is GenericInstanceMethod generic
                        && generic.GenericArguments.Any(argument => TablesWrittenOnlyThroughTheirDoors.Contains(argument.FullName)));
            }

            return RepositoryWrites.Contains(called.Name)
                && declaringType is GenericInstanceType repository
                && repository.ElementType.FullName == typeof(IRepository<>).FullName
                && TablesWrittenOnlyThroughTheirDoors.Contains(repository.GenericArguments[0].FullName);
        }

        private static bool IsAListATeamHolds(MethodReference called)
            => called.Name.StartsWith("get_", StringComparison.Ordinal)
                && TypesATeamHolds.Contains(called.DeclaringType.GetElementType().FullName)
                && called.ReturnType is GenericInstanceType { Namespace: "System.Collections.Generic" };

        private static string NameOf(MethodReference called)
            => called is GenericInstanceMethod generic
                ? $"{called.DeclaringType.Name}.{called.Name}<{string.Join(", ", generic.GenericArguments.Select(argument => argument.Name))}>"
                : $"{called.DeclaringType.Name}.{called.Name}";

        private static HashSet<string> ReachableFrom(Type root)
        {
            var found = new HashSet<Type>();
            var waiting = new Queue<Type>([root]);

            while (waiting.TryDequeue(out var type))
            {
                if (type.Assembly != root.Assembly || type.IsEnum || !found.Add(type))
                {
                    continue;
                }

                if (type.BaseType is { } baseType)
                {
                    waiting.Enqueue(baseType);
                }

                foreach (var held in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).SelectMany(property => TypesIn(property.PropertyType)))
                {
                    waiting.Enqueue(held);
                }
            }

            return new HashSet<string>(found.Select(KeyOf), StringComparer.Ordinal);
        }

        private static IEnumerable<Type> TypesIn(Type type)
        {
            if (type.IsArray)
            {
                return TypesIn(type.GetElementType()!);
            }

            return type.IsGenericType ? type.GetGenericArguments().SelectMany(TypesIn) : [type];
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

        public sealed class ChangesTheModeOfAReadyRule : RefinementAtThePortTest
        {
            public static void Plant(StageRules rules) => rules.Ready!.Mode = WorkItemRuleSet.ModeOr;
        }

        public sealed class AddsAToDoStateToATeam : RefinementAtThePortTest
        {
            public static void Plant(Team team) => team.ToDoStates.Add("Ready for refinement");
        }

        public sealed class HandsATeamsToDoStatesToAHelperThatAddsOne : RefinementAtThePortTest
        {
            public static void Plant(Team team) => AddReady(team.ToDoStates);

            private static void AddReady(List<string> states) => states.Add("Ready for refinement");
        }

        public sealed class AddsADoingStateInALambdaLater : RefinementAtThePortTest
        {
            public static Action Plant(Team team)
            {
                var states = team.DoingStates;
                return () => states.Add("Ready for refinement");
            }
        }

        public sealed class TakesTheTeamsTableFromTheContext : RefinementAtThePortTest
        {
            public int Plant() => NewScope().ServiceProvider.GetRequiredService<LighthouseAppContext>().Set<Team>().Count();
        }

        public sealed class AddsATeamThroughTheContext : RefinementAtThePortTest
        {
            public void Plant(Team team) => NewScope().ServiceProvider.GetRequiredService<LighthouseAppContext>().Add(team);
        }

        public sealed class UpdatesATeamThroughTheRepository : RefinementAtThePortTest
        {
            public void Plant(Team team) => NewScope().ServiceProvider.GetRequiredService<IRepository<Team>>().Update(team);
        }
    }
}
