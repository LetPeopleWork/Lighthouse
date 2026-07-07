using System.Linq;
using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.Loader;
using Lighthouse.Backend.Services.Implementation;
using Microsoft.Extensions.DependencyInjection;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class DomainEventDispatcherSeamArchUnitTest
    {
        private const string DomainEventsImplementationNamespaceFragment = ".Services.Implementation.DomainEvents";
        private const string GetRequiredServiceMemberPrefix = "GetRequiredService(";
        private static readonly string[] RepositorySaveMemberPrefixes = ["Save(", "SaveChangesAsync("];
        private const string AsyncStateMachineBuilderTypeFragment = "AsyncTaskMethodBuilder";

        private static readonly ArchitectureModel Architecture = new ArchLoader()
            .LoadAssemblies(
                typeof(TeamMetricsService).Assembly,
                typeof(ServiceProviderServiceExtensions).Assembly)
            .Build();

        [Test]
        public void DomainEventSeamTypes_DoNotResolveViaServiceLocator()
        {
            var offenders = Architecture.MethodMembers
                .Where(member => member.DeclaringType.FullName.Contains(DomainEventsImplementationNamespaceFragment))
                .SelectMany(member => member.MemberDependencies
                    .OfType<MethodCallDependency>()
                    .Where(dependency => dependency.TargetMember.Name.StartsWith(GetRequiredServiceMemberPrefix))
                    .Select(_ => $"{member.DeclaringType.Name}.{member.Name}"))
                .Distinct()
                .ToList();

            Assert.That(
                offenders,
                Is.Empty,
                "ADR-027 D3: the dispatcher and its handlers must resolve collaborators via typed " +
                "IEnumerable<IDomainEventHandler<TEvent>> (IServiceProvider.GetServices), never " +
                "IServiceProvider.GetRequiredService — otherwise the seam re-introduces the service locator it removes. " +
                "Offenders: " + string.Join(", ", offenders));
        }

        [Test]
        public void DomainEventHandlerMethods_MustAwaitRepositorySaves_NeverFireAndForget()
        {
            // A method that calls a repository Save()/SaveChangesAsync() MUST await it. An awaited call
            // makes the method `async`, which emits AsyncTaskMethodBuilder calls; a fire-and-forget Save
            // (called from a `void`/non-async method that then returns Task.CompletedTask) does not. So the
            // offender is a Save-calling handler method that is NOT async — it returns before its
            // SaveChangesAsync finishes, and the DomainEventDispatcher scope then disposes the DbContext
            // mid-write on the shared Npgsql connection, desyncing the wire protocol (the 2026-07-07
            // verifypostgres flake; BlockedCountSnapshotRecordingHandler.UpsertSnapshot).
            var offenders = Architecture.MethodMembers
                .Where(member => member.DeclaringType.FullName.Contains(DomainEventsImplementationNamespaceFragment))
                // Skip compiler-generated state-machine / closure types (nested `<...>` names).
                .Where(member => !member.DeclaringType.Name.Contains('<'))
                .Where(member => CallsRepositorySave(member) && !IsAsync(member))
                .Select(member => $"{member.DeclaringType.Name}.{member.Name}")
                .Distinct()
                .ToList();

            Assert.That(
                offenders,
                Is.Empty,
                "A domain-event handler that persists via a repository Save()/SaveChangesAsync() MUST await it. " +
                "A direct (non-awaited) Save call remains in the method's own body; awaiting moves it into the " +
                "async state machine. A fire-and-forget Save returns before its SaveChangesAsync finishes, so the " +
                "DomainEventDispatcher scope disposes the DbContext mid-write on the shared Npgsql connection and " +
                "desyncs the wire protocol (Postgres-only; SQLite/InMemory tolerate it). Offenders: " +
                string.Join(", ", offenders));
        }

        private static bool CallsRepositorySave(ArchUnitNET.Domain.MethodMember member)
        {
            return member.MemberDependencies
                .OfType<MethodCallDependency>()
                .Any(dependency => RepositorySaveMemberPrefixes
                    .Any(prefix => dependency.TargetMember.Name.StartsWith(prefix)));
        }

        private static bool IsAsync(ArchUnitNET.Domain.MethodMember member)
        {
            // A C# `async` method emits calls into System.Runtime.CompilerServices.AsyncTaskMethodBuilder
            // (Create/Start/AwaitUnsafeOnCompleted/SetResult). A synchronous method that fires Save() and
            // returns Task.CompletedTask has no such dependency.
            return member.MemberDependencies
                .OfType<MethodCallDependency>()
                .Any(dependency => dependency.TargetMember.DeclaringType.Name
                    .Contains(AsyncStateMachineBuilderTypeFragment));
        }
    }
}
