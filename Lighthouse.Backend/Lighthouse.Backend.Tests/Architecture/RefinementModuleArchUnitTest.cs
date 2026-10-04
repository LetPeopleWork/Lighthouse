using System.Reflection;
using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;
using ArchUnitNET.NUnit;
using Lighthouse.Backend.API;
using Lighthouse.Backend.API.Helpers;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Implementation.Refinement;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Lighthouse.Backend.Services.Interfaces.Refinement;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using ArchitectureModel = ArchUnitNET.Domain.Architecture;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public class RefinementModuleArchUnitTest
    {
        private const string ServicesImplementationPattern = @"^Lighthouse\.Backend\.Services\.Implementation($|\..*)";

        private const string ApiLayerPattern = @"^Lighthouse\.Backend\.API($|\..*)";

        private const string CompositionRootPattern = @"^Lighthouse\.Backend\.Program($|\+.*)";

        private const string RefinementImplementationNamespace = "Lighthouse.Backend.Services.Implementation.Refinement";

        private const string RefinementResolutionAndItsNestedTypesPattern = @"^Lighthouse\.Backend\.Services\.Implementation\.Refinement\.RefinementResolution($|\+.*)";

        private const string WeeklyRecurrenceAndItsNestedTypesPattern = @"^Lighthouse\.Backend\.Services\.Implementation\.WeeklyRecurrence($|\+.*)";

        private const string RefinementCadenceCalendarAndItsNestedTypesPattern = @"^Lighthouse\.Backend\.Services\.Implementation\.Refinement\.RefinementCadenceCalendar($|\+.*)";

        private const string RepositoriesNamespace = "Lighthouse.Backend.Services.Interfaces.Repositories";

        private static readonly string[] RepositoryWriters = ["Save", "Add", "Append", "Update", "Remove", "Delete", "Apply"];

        private static readonly string[] SizingLogRewrites = ["ExecuteUpdate", "ExecuteDelete", "Remove"];

        private static readonly string[] EditingMemberPrefixes = ["Update", "Remove", "Delete"];

        private static readonly Type[] WriteVerbs = [typeof(HttpPostAttribute), typeof(HttpPutAttribute), typeof(HttpPatchAttribute), typeof(HttpDeleteAttribute)];

        // Everything in the module serves the tab's read except what adds to the sizing log.
        private static readonly string[] WritePath = [nameof(SizingLogCommands)];

        private static readonly ArchitectureModel Architecture = LighthouseArchitecture.Production;

        // The rules that decide what a Team may save as its refinement setup are plain functions of the values
        // being saved. Kept that way, every rule can be tested by handing it values, and none of them can quietly
        // start reading the database or the trackers while it judges a save.
        [Test]
        public void TheRefinementSettingsValidator_IsStatic()
        {
            var validator = typeof(RefinementSettingsValidator);

            Assert.That(validator.IsAbstract && validator.IsSealed, Is.True,
                "The refinement settings validator must stay a static class: it judges the values it is given and holds nothing.");
        }

        [Test]
        public void TheRefinementSettingsValidator_ReferencesNothingInServicesImplementation()
        {
            Types().That().HaveFullName(typeof(RefinementSettingsValidator).FullName!)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ServicesImplementationPattern))
                .Because("a validator that reaches into a service can no longer be tested by handing it values.")
                .Check(Architecture);
        }

        // How the votes on a row resolve is decided from the log's entries alone, so every answer it gives can be
        // checked by handing it entries.
        [Test]
        public void TheRefinementResolution_IsStatic()
        {
            var resolution = typeof(RefinementResolution);

            Assert.That(resolution.IsAbstract && resolution.IsSealed, Is.True,
                "The refinement resolution must stay a static class: it resolves the entries it is given and holds nothing.");
        }

        // Its own lambdas compile into nested types beside it, which are not a reach into another service.
        [Test]
        public void TheRefinementResolution_ReferencesNothingInServicesImplementation()
        {
            Types().That().HaveFullName(typeof(RefinementResolution).FullName!)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ServicesImplementationPattern)
                    .And().DoNotHaveFullNameMatching(RefinementResolutionAndItsNestedTypesPattern))
                .Because("a resolution that reaches into a service can no longer be tested by handing it entries.")
                .Check(Architecture);
        }

        // Which day a cadence falls on is worked out from the cadence and the day alone, so every answer can be
        // checked against a fixed calendar, and the blackout rules and the Refinement cadence count weeks the same way.
        [TestCase(typeof(WeeklyRecurrence))]
        [TestCase(typeof(RefinementCadenceCalendar))]
        public void The_cadence_arithmetic_IsStatic(Type calendar)
        {
            Assert.That(calendar.IsAbstract && calendar.IsSealed, Is.True,
                $"{calendar.Name} must stay a static class: it answers from the days it is given and holds nothing.");
        }

        [Test]
        public void The_weekly_recurrence_ReferencesNothingElseInServicesImplementation()
        {
            Types().That().HaveFullName(typeof(WeeklyRecurrence).FullName!)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ServicesImplementationPattern)
                    .And().DoNotHaveFullNameMatching(WeeklyRecurrenceAndItsNestedTypesPattern))
                .Because("a recurrence rule that reaches into a service can no longer be tested by handing it days.")
                .Check(Architecture);
        }

        [Test]
        public void The_Refinement_cadence_calendar_ReferencesNothingInServicesImplementationButTheWeeklyRecurrence()
        {
            Types().That().HaveFullName(typeof(RefinementCadenceCalendar).FullName!)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ServicesImplementationPattern)
                    .And().DoNotHaveFullNameMatching(WeeklyRecurrenceAndItsNestedTypesPattern)
                    .And().DoNotHaveFullNameMatching(RefinementCadenceCalendarAndItsNestedTypesPattern))
                .Because("a calendar that reaches into a service can no longer be tested by handing it a cadence and a day.")
                .Check(Architecture);
        }

        // Stages come from the same rule engine every other Work Item rule uses, reached through its port, so a
        // stage rule can never mean something different from the identical blocked-items rule.
        [Test]
        public void The_stage_rule_matcher_reaches_the_rule_engine_only_through_its_port()
        {
            Types().That().HaveFullName(typeof(StageRuleMatcher).FullName!)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ServicesImplementationPattern)
                    .And().DoNotResideInNamespace(RefinementImplementationNamespace))
                .AndShould().NotDependOnAny(Types().That().ResideInNamespace(RepositoriesNamespace))
                .Because("matching rules is the rule engine's job; the matcher only hands it the Work Items already in refinement.")
                .Check(Architecture);
        }

        [Test]
        public void Nothing_but_the_API_and_the_composition_root_depends_on_the_Refinement_module()
        {
            Types().That().DoNotResideInNamespaceMatching(ModuleBoundariesArchUnitTest.RefinementPattern)
                .And().DoNotResideInNamespaceMatching(ApiLayerPattern)
                .And().DoNotHaveFullNameMatching(CompositionRootPattern)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ModuleBoundariesArchUnitTest.RefinementPattern))
                .Because("refinement sits on top of the Team and its Work Items; anything below reaching up into it would tie forecasting and syncing to a coaching screen.")
                .Check(Architecture);
        }

        [Test]
        public void The_Refinement_module_reaches_neither_the_trackers_nor_the_background_updates()
        {
            Types().That().ResideInNamespaceMatching(ModuleBoundariesArchUnitTest.RefinementPattern)
                .Should().NotDependOnAny(Types().That().ResideInNamespaceMatching(ModuleBoundariesArchUnitTest.WorkTrackingIntegrationPattern)
                    .Or().ResideInNamespaceMatching(ModuleBoundariesArchUnitTest.PortfolioDeliveryPattern))
                .Because("refinement reads what Lighthouse already holds; it never asks a tracker or starts an update to answer.")
                .Check(Architecture);
        }

        // Read off the call dependencies directly: the fluent call rule does not match a method declared on the
        // generic repository interface, so it would pass a read that saves.
        [Test]
        public void Reading_the_Refinement_tab_calls_no_repository_member_that_writes()
        {
            var readPath = ReadPath();
            var writes = readPath
                .SelectMany(type => type.Dependencies.OfType<MethodCallDependency>())
                .Where(call => call.TargetMember.DeclaringType.Namespace.FullName == RepositoriesNamespace
                    && RepositoryWriters.Any(writer => call.TargetMember.Name.StartsWith(writer, StringComparison.Ordinal)))
                .Select(call => $"{call.Origin.FullName} calls {call.TargetMember.DeclaringType.FullName}.{call.TargetMember.Name}")
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(readPath.Select(type => type.Name), Does.Contain("RefinementViewQuery"),
                    "the rule finds the read path by its namespace; without it the rule guards nothing");
                Assert.That(readPath.Select(type => type.Name), Does.Contain("SleYardstickResolver"),
                    "the yardstick is part of the tab's read, so it must sit where the rule looks");
                Assert.That(writes, Is.Empty,
                    "opening the Refinement tab is a read; saving, adding, updating or removing through a repository would make it write");
            }
        }

        // Asking for the caller's profile creates it when it is missing, so a read that asked would write.
        [Test]
        public void Reading_the_Refinement_tab_neither_resolves_a_profile_nor_reaches_the_sizing_log_commands()
        {
            var reaches = ReadPath()
                .SelectMany(type => type.Dependencies)
                .Where(dependency => dependency.Target.FullName == typeof(ICurrentUserProfileService).FullName
                    || dependency.Target.FullName == typeof(ISizingLogCommands).FullName)
                .Select(dependency => $"{dependency.Origin.FullName} depends on {dependency.Target.FullName}")
                .Distinct()
                .ToList();

            Assert.That(reaches, Is.Empty);
        }

        [Test]
        public void The_sizing_log_port_offers_no_way_to_edit_an_entry()
        {
            var editing = typeof(ISizingLogRepository).GetMembers()
                .Select(member => member.Name)
                .Where(name => EditingMemberPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
                .ToList();

            Assert.That(editing, Is.Empty, "the sizing log is appended to; a changed mind is a later entry, never an edit");
        }

        [Test]
        public void Nothing_that_touches_the_sizing_log_table_updates_or_deletes_in_it()
        {
            var touching = Architecture.Types
                .Where(type => type.Dependencies.OfType<MethodCallDependency>()
                    .Any(call => call.TargetMember.Name.StartsWith("get_SizingLogEntries", StringComparison.Ordinal)))
                .ToList();
            var rewrites = touching
                .SelectMany(type => type.Dependencies.OfType<MethodCallDependency>())
                .Where(call => SizingLogRewrites.Any(rewrite => call.TargetMember.Name.StartsWith(rewrite, StringComparison.Ordinal)))
                .Select(call => $"{call.Origin.FullName} calls {call.TargetMember.DeclaringType.FullName}.{call.TargetMember.Name}")
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(touching.Select(type => type.Name), Does.Contain(nameof(SizingLogRepository)),
                    "the rule finds the writers by their use of the table; without the repository it guards nothing");
                Assert.That(rewrites, Is.Empty);
            }
        }

        // A write that needs only Team read is unusual; keeping every one of them in one controller means a
        // second one cannot appear without somebody deciding it should.
        [Test]
        public void Every_action_open_to_Team_readers_for_writing_lives_in_the_votes_controller()
        {
            var contributing = typeof(Program).Assembly.GetTypes()
                .Where(type => typeof(ControllerBase).IsAssignableFrom(type))
                .Where(controller => CarriesTeamContribute(controller)
                    || controller.GetMethods().Any(CarriesTeamContribute))
                .Select(controller => controller.Name)
                .ToList();

            Assert.That(contributing, Is.EqualTo(new List<string> { nameof(RefinementVotesController) }));
        }

        [Test]
        public void The_Refinement_tab_controller_only_reads()
        {
            var writes = typeof(RefinementController).GetMethods()
                .Where(action => WriteVerbs.Any(verb => action.IsDefined(verb, inherit: true)))
                .Select(action => action.Name)
                .ToList();

            Assert.That(writes, Is.Empty);
        }

        // Teams and profiles are deleted by code that knows nothing about votes, so the clean-up has to be
        // the database's own, on both providers.
        [TestCase("Lighthouse.Migrations.Sqlite", "TeamId", "Cascade")]
        [TestCase("Lighthouse.Migrations.Postgres", "TeamId", "Cascade")]
        [TestCase("Lighthouse.Migrations.Sqlite", "VoterProfileId", "SetNull")]
        [TestCase("Lighthouse.Migrations.Postgres", "VoterProfileId", "SetNull")]
        public void The_sizing_log_migration_makes_the_database_clean_up_after_a_deleted_Team_or_profile(string project, string column, string onDelete)
        {
            var up = ExpandOnlyMigrationGuard.ExtractUpMethodBody(File.ReadAllText(SizingLogMigrationIn(project)));

            Assert.That(OnDeleteOf(up, column), Is.EqualTo(onDelete));
        }

        private static List<IType> ReadPath()
            => [.. Architecture.Types
                .Where(type => type.Namespace.FullName == RefinementImplementationNamespace)
                .Where(type => !WritePath.Contains(type.Name))];

        private static bool CarriesTeamContribute(MemberInfo member)
            => member.GetCustomAttributes<RbacGuardAttribute>(inherit: true)
                .Any(guard => guard.Requirement == RbacGuardRequirement.TeamContribute);

        private static string OnDeleteOf(string up, string column)
        {
            const string onDeleteMarker = "onDelete: ReferentialAction.";

            var foreignKey = up.IndexOf($"column: x => x.{column},", StringComparison.Ordinal);
            if (foreignKey < 0)
            {
                return "no foreign key";
            }

            var start = up.IndexOf(onDeleteMarker, foreignKey, StringComparison.Ordinal) + onDeleteMarker.Length;
            return up[start..up.IndexOf(')', start)];
        }

        private static string SizingLogMigrationIn(string project)
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            Assert.That(directory, Is.Not.Null, "Could not locate Lighthouse.sln to find the migrations.");

            return Directory.EnumerateFiles(Path.Combine(directory!.FullName, project, "Migrations"), "*_AddSizingLogEntries.cs").Single();
        }
    }
}
