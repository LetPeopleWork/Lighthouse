using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Tests.Architecture.WriteBackMembershipFixtures;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// The write-back round is about to move out of the loose Services namespaces into Forecasting's Application, under
    /// WriteBack. Before anything moves, the rules see the move: the round's membership is measured from the compiled code;
    /// every rule pattern that sees a moving type carries a declared role (a part of the code that must follow the type, an old
    /// location that must not, or a Forecasting pattern that must see the round in its new place); Promises, Access and
    /// Licensing refuse a round type by the one round pattern they are handed, wherever the round lives; and every string of
    /// the test project that names a moving type by its old Services path is on a fixed list of strings the move renames.
    /// </summary>
    [TestFixture]
    public class WriteBackMembershipArchUnitTest
    {
        private static readonly Type[] TheCheckItself =
        [
            typeof(WriteBackMembershipArchUnitTest),
            typeof(WriteBackMembership),
            typeof(WriteBackNeighbourThatStays),
            typeof(WriteBackPatternRoles),
            typeof(WriteBackStringsTheMoveRenames),
            typeof(WriteBackRename),
            typeof(NamespaceMoveLaw),
        ];

        // The map, the roles and this check name the round by its old Services path on purpose, so they hold before, during and
        // after the move; they are the measurement, not rules the move could leave behind.
        private static readonly string[] TheMeasurementFiles =
        [
            $"{nameof(WriteBackMembership)}.cs",
            $"{nameof(WriteBackPatternRoles)}.cs",
            $"{nameof(WriteBackStringsTheMoveRenames)}.cs",
            $"{nameof(WriteBackMembershipArchUnitTest)}.cs",
            $"{nameof(NamespaceMoveLaw)}.cs",
        ];

        private static readonly NamespaceMoveLaw TheLaw = new(
            TheWriteBackMove(WriteBackStringsTheMoveRenames.Entries),
            new PatternRoleMap(WriteBackPatternRoles.PatternRoles, WriteBackPatternRoles.FolderOnlyMatches));

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(LighthouseAppContext).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(WriteBackMembershipArchUnitTest).Assembly));

        private static string FixtureNamespace => typeof(MadeUpRoundRuleKeys).Namespace!;

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void EveryRoundType_IsFoundInItsOldOrItsNewNamespace_AndEveryNeighbourThatStays_IsFoundWithItsOwnerAndReason()
        {
            var map = GivenTheRoundMap();

            var unsound = WhenTheMapIsLookedUpInTheCompiledCode(map, WriteBackMembership.NeighboursThatStay);

            ThenEveryEntryOfTheMapIsSound(unsound);
        }

        [Test]
        public void TheMembershipCheck_NamesAMadeUpRoundTypeFoundInNeitherNamespace_AndAMadeUpNeighbourThatIsNowhereOrHasNoOwner()
        {
            var map = GivenTheRoundMapWithAMadeUpType();

            var unsound = WhenTheMapIsLookedUpInTheCompiledCode(map, GivenMadeUpNeighbours());

            ThenOnlyTheMadeUpEntriesAreNamed(unsound);
        }

        [Test]
        public void EveryRulePatternThatSeesARoundType_IsDeclared_AndKeepsItsRoleOnTheSubjectItsRuleReads()
        {
            var patterns = GivenTheRulePatternsOfTheTestAssembly();

            var check = WhenTheRoleCheckRuns(TheLaw, patterns, WriteBackPatternRoles.PatternRoles, WriteBackPatternRoles.SeeTheRoundInItsNewPlace);

            ThenNoPatternBreaksItsRole(check);
        }

        [Test]
        public void TheRoleCheck_NamesEachMadeUpPatternThatBreaksItsRole_AndPassesTheOnesThatKeepIt()
        {
            var patterns = GivenTheMadeUpRulePatterns();

            var check = WhenTheRoleCheckRuns(MadeUpRoundRuleRoles.TheLaw, patterns, MadeUpRoundRuleRoles.Roles, MadeUpRoundRuleRoles.SeeTheRoundInItsNewPlace);

            ThenTheMadeUpBreaksNamedAre(check);
        }

        [TestCaseSource(nameof(ContextsThatMayNotJoinTheRound))]
        public void AMadeUpTypeOfAContextThatMayNotJoinTheRound_ReachingARoundTypeInItsNewPlace_IsRefusedByTheRoundPattern_AndItsTwinReachingForecastingIsNot(RoundGuard guard)
        {
            var roundPattern = GivenARoundPatternNamingTheMadeUpRound();

            var refused = WhenTheContextsPredicateJudgesItsMadeUpTypes(guard, roundPattern);

            ThenOnlyTheTypeJoiningTheRoundIsRefused(guard, refused);
        }

        [Test]
        public void AMadeUpWorkItemsTypeReachingARoundTypeInItsNewPlace_IsRefusedByExactlyTheWorkItemsRulesTheMembershipRecords()
        {
            var rules = GivenTheWorkItemsOutboundRules();

            var refusing = WhenTheWorkItemsRulesJudgeAWorkItemsTypeReachingTheRoundInItsNewPlace(rules);

            ThenTheRefusingRulesAreTheRecordedOnes(rules, refusing);
        }

        [Test]
        public void EveryTestStringThatNamesARoundTypeByItsOldPath_IsOnTheRenameList_InExactlyOneOfItsForms()
        {
            var sources = GivenTheSourcesOfTheTestProject();

            var comparison = WhenTheOldPathStringsAreComparedWithTheList(TheLaw, WriteBackStringsTheMoveRenames.Entries, sources, GivenThePatternValuesTheRoleCheckGoverns());

            ThenTheOldPathStringsAreExactlyTheList(comparison);
        }

        [Test]
        public void TheRenameListCheck_NamesAnUnlistedOldPathString_AListedStringFoundInNeitherForm_AndOneFoundInBoth()
        {
            var sources = GivenAMadeUpRuleFile();

            var comparison = WhenTheOldPathStringsAreComparedWithTheList(MadeUpRenameList.TheLaw, MadeUpRenameList.Entries, sources, []);

            ThenTheMadeUpFileIsComparedBothWays(comparison);
        }

        private static IEnumerable<TestCaseData> ContextsThatMayNotJoinTheRound()
        {
            yield return new TestCaseData(new RoundGuard("Promises", typeof(PromisesModuleBoundariesArchUnitTest), "IsUpstreamCode", typeof(Lighthouse.Backend.Modules.WriteBackFixtures.Promises.Application.ADeliveryServiceJoiningTheRound), typeof(Lighthouse.Backend.Modules.WriteBackFixtures.Promises.Application.ADeliveryServiceAskingAForecast)))
                .SetName("PromisesMayNotJoinTheRound_ThoughItMayAskForecasting");
            yield return new TestCaseData(new RoundGuard("Access", typeof(AccessModuleBoundariesArchUnitTest), "IsAnotherContext", typeof(Lighthouse.Backend.Modules.WriteBackFixtures.Access.Application.AnAccessServiceJoiningTheRound), typeof(Lighthouse.Backend.Modules.WriteBackFixtures.Access.Application.AnAccessServiceAskingAForecast)))
                .SetName("AccessMayNotJoinTheRound");
            yield return new TestCaseData(new RoundGuard("Licensing", typeof(LicensingModuleBoundariesArchUnitTest), "IsAccessOrAnotherContext", typeof(Lighthouse.Backend.Modules.WriteBackFixtures.Licensing.Application.ALicenceServiceJoiningTheRound), typeof(Lighthouse.Backend.Modules.WriteBackFixtures.Licensing.Application.ALicenceServiceAskingAForecast)))
                .SetName("LicensingMayNotJoinTheRound");
        }

        private static NamespaceMove TheWriteBackMove(IEnumerable<WriteBackRename> renames)
            => new(
                WriteBackMembership.MovingTypes,
                [.. renames.Select(entry => entry.AsRenamedString())],
                $"^{Regex.Escape(WriteBackMembership.TheNewNamespace)}(\\.|$)",
                new Regex(@"(?<=Forecasting\\\.Application\\\.)WriteBack(?![A-Za-z0-9_])", RegexOptions.None, NamespaceMoveLaw.PatternTimeout),
                []);

        private static IReadOnlyList<MovingType> GivenTheRoundMap() => WriteBackMembership.MovingTypes;

        private static IReadOnlyList<MovingType> GivenTheRoundMapWithAMadeUpType()
            => [.. WriteBackMembership.MovingTypes, WriteBackMembership.MovingTypes[^1] with { Name = MadeUpRoundRuleKeys.AMadeUpTypeName }];

        private static IReadOnlyList<WriteBackNeighbourThatStays> GivenMadeUpNeighbours()
            =>
            [
                WriteBackMembership.NeighboursThatStay[0],
                new(MadeUpRoundRuleKeys.AMadeUpNeighbourName, "Forecasting", "a made-up neighbour that is nowhere"),
                WriteBackMembership.NeighboursThatStay[0] with { Owner = " " },
            ];

        private static string GivenARoundPatternNamingTheMadeUpRound()
            => $"^{Regex.Escape(typeof(Lighthouse.Backend.Modules.WriteBackFixtures.Forecasting.Application.WriteBack.IAMadeUpRoundPort).Namespace!)}\\.";

        private static List<RulePattern> GivenTheRulePatternsOfTheTestAssembly()
        {
            var patterns = TheLaw.RulePatternsOf(typeof(WriteBackMembershipArchUnitTest).Assembly.GetTypes().Where(type => !InFixtures(type) && !TheCheckItself.Contains(type)));
            Assert.That(patterns, Has.Count.GreaterThan(50), $"Only {patterns.Count} rule patterns were found in the test assembly, so the check would pass without looking at the rules. How to fix: enumerate the constant and static read-only string fields of every rule class.");
            return patterns;
        }

        private static List<RulePattern> GivenTheMadeUpRulePatterns()
            => TheLaw.RulePatternsOf(typeof(WriteBackMembershipArchUnitTest).Assembly.GetTypes().Where(type => type.Namespace == FixtureNamespace));

        private static HashSet<string> GivenThePatternValuesTheRoleCheckGoverns()
            => TheLaw.PatternValuesItGoverns(GivenTheRulePatternsOfTheTestAssembly().Where(pattern => WriteBackPatternRoles.PatternRoles.ContainsKey(pattern.Field) || WriteBackPatternRoles.SeeTheRoundInItsNewPlace.ContainsKey(pattern.Field)));

        private static List<WorkItemsOutboundRule> GivenTheWorkItemsOutboundRules()
        {
            var rules = typeof(WorkItemsModuleBoundariesArchUnitTest)
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(method => method.ReturnType == typeof(OutboundVerdict))
                .Where(method => method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual([typeof(ModuleDefinition), typeof(string), typeof(IReadOnlyCollection<string>)]))
                .Select(method => new WorkItemsOutboundRule(method.Name, (module, application) => (OutboundVerdict)method.Invoke(null, [module, application, Array.Empty<string>()])!))
                .ToList();

            Assert.That(
                rules,
                Has.Count.GreaterThanOrEqualTo(3),
                $"Only [{string.Join(", ", rules.Select(rule => rule.Name))}] Work Items outbound rules were found, so the check would judge the edge by too few rules. " +
                "Why it matters: the Work Items outbound rules are each a static method taking the module, the Application namespaces and the listed edges, and returning the edges they found; one that is not found cannot refuse the edge. " +
                "How to fix: keep each Work Items outbound rule in that shape.");
            return rules;
        }

        private static List<(string RelativePath, string Source)> GivenTheSourcesOfTheTestProject()
        {
            var root = RepositoryRoot();
            var testProject = Path.Combine(root, "Lighthouse.Backend", "Lighthouse.Backend.Tests");
            var buildOutput = new[] { Path.Combine(testProject, "bin") + Path.DirectorySeparatorChar, Path.Combine(testProject, "obj") + Path.DirectorySeparatorChar };

            var files = Directory.EnumerateFiles(testProject, "*.cs", SearchOption.AllDirectories)
                .Where(path => !buildOutput.Any(folder => path.StartsWith(folder, StringComparison.Ordinal)))
                .Where(path => !TheMeasurementFiles.Contains(Path.GetFileName(path), StringComparer.Ordinal))
                .Select(path => (Path.GetRelativePath(root, path).Replace('\\', '/'), File.ReadAllText(path)))
                .ToList();

            Assert.That(files, Has.Count.GreaterThan(500), $"Only {files.Count} source files were found under {testProject}, so the scan would pass without reading the test project.");
            return files;
        }

        private static List<(string RelativePath, string Source)> GivenAMadeUpRuleFile()
            => [(MadeUpRenameList.TheFile, MadeUpRenameList.TheSource)];

        private static List<string> WhenTheMapIsLookedUpInTheCompiledCode(IReadOnlyList<MovingType> map, IReadOnlyList<WriteBackNeighbourThatStays> staying)
        {
            var module = ProductionModule.Value;
            var movingNotFound = map
                .Where(type => module.GetType(type.OldFullName) is null && module.GetType(type.NewFullName) is null)
                .Select(type => $"{type.Name}: neither {type.OldFullName} nor {type.NewFullName}");
            var movingFoundTwice = map
                .Where(type => module.GetType(type.OldFullName) is not null && module.GetType(type.NewFullName) is not null)
                .Select(type => $"{type.Name}: both {type.OldFullName} and {type.NewFullName}");
            var stayingUnsound = staying
                .Where(neighbour => string.IsNullOrWhiteSpace(neighbour.Owner) || string.IsNullOrWhiteSpace(neighbour.Reason) || module.GetType(neighbour.FullName) is null)
                .Select(neighbour => $"{neighbour.FullName} (owner \"{neighbour.Owner}\", reason \"{neighbour.Reason}\")");

            return [.. movingNotFound, .. movingFoundTwice, .. stayingUnsound];
        }

        // A pattern on the admission set must see the new name of every moving type at every step; whether it sees the old name
        // is left free, so that the round pattern may see both places during the move and only the new one after it.
        private static RoleCheck WhenTheRoleCheckRuns(NamespaceMoveLaw law, List<RulePattern> patterns, IReadOnlyDictionary<string, PatternDeclaration> roles, IReadOnlyDictionary<string, string> seeTheRoundInItsNewPlace)
        {
            var breaks = law.RoleBreaksOf(patterns)
                .Where(found => !(seeTheRoundInItsNewPlace.ContainsKey(found.Pattern.Field) && found.Break is RoleBreak.SeesAnAreaItNeverSaw or RoleBreak.StopsSeeingTheType))
                .Select(found => $"{found.Pattern.Field} = {found.Pattern.Value} ({found.Declared}){Environment.NewLine}{found.Describe()}")
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var unsoundAdmissions = new List<string>();
            foreach (var field in seeTheRoundInItsNewPlace.Keys)
            {
                var pattern = patterns.FirstOrDefault(candidate => candidate.Field == field);
                var subject = roles.TryGetValue(field, out var declaration) && declaration is { Role: PatternRole.Context, Subject: { } declared } ? declared : (PatternSubject?)null;

                if (pattern is null)
                {
                    unsoundAdmissions.Add($"{field}: not a rule pattern of the test assembly");
                }
                else if (subject is null)
                {
                    unsoundAdmissions.Add($"{field}: not declared as a part of the code with the subject its rule reads");
                }
                else
                {
                    unsoundAdmissions.AddRange(WriteBackMembership.MovingTypes
                        .Where(type => !NamespaceMoveLaw.IsMatch(subject == PatternSubject.Namespace ? type.NewNamespace : type.NewFullName, pattern.Value))
                        .Select(type => $"{field} = {pattern.Value}: does not see {type.Name} in its new place {type.NewFullName}"));
                }
            }

            return new RoleCheck(breaks, unsoundAdmissions);
        }

        private static List<string> WhenTheContextsPredicateJudgesItsMadeUpTypes(RoundGuard guard, string roundPattern)
        {
            var predicate = PredicateOf(guard, roundPattern);
            var madeUpNamespace = $"^{Regex.Escape(guard.TypeJoiningTheRound.Namespace!)}$";

            return [.. IlDependencies.DependentsIn(TestModule.Value, madeUpNamespace, predicate)
                .SelectMany(dependent => dependent.Targets.Select(reached => $"{dependent.TypeName} -> {reached}"))];
        }

        // The predicate the context's outbound rule reads, handed the round pattern; a predicate that takes no round pattern is
        // the one that names the round by a list, and is judged as it is, so the made-up round type shows whether it is seen.
        private static Func<TypeReference, bool> PredicateOf(RoundGuard guard, string roundPattern)
        {
            const BindingFlags Statics = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var withThePattern = guard.Rule.GetMethod(guard.Predicate, Statics, [typeof(TypeReference), typeof(string)]);
            if (withThePattern is not null)
            {
                return type => (bool)withThePattern.Invoke(null, [type, roundPattern])!;
            }

            var withTheList = guard.Rule.GetMethod(guard.Predicate, Statics, [typeof(TypeReference)]);
            Assert.That(withTheList, Is.Not.Null, $"{guard.Rule.Name} has no predicate {guard.Predicate}. How to fix: keep the outbound predicate of {guard.Context} under that name, taking the type and the round pattern.");
            return type => (bool)withTheList!.Invoke(null, [type])!;
        }

        private static List<string> WhenTheWorkItemsRulesJudgeAWorkItemsTypeReachingTheRoundInItsNewPlace(List<WorkItemsOutboundRule> rules)
        {
            var madeUpApplication = $"^{Regex.Escape(typeof(Lighthouse.Backend.Modules.WriteBackFixtures.WorkItems.Application.AWorkItemServiceJoiningTheRound).Namespace!)}$";
            var edge = $"{typeof(Lighthouse.Backend.Modules.WriteBackFixtures.WorkItems.Application.AWorkItemServiceJoiningTheRound).FullName} -> {typeof(Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IAMadeUpRoundTypeForTheWorkItemsRules).FullName}";

            return [.. rules
                .Where(rule => rule.Judge(TestModule.Value, madeUpApplication).Edges.Contains(edge, StringComparer.Ordinal))
                .Select(rule => rule.Name)
                .Order(StringComparer.Ordinal)];
        }

        private static OldPathComparison WhenTheOldPathStringsAreComparedWithTheList(NamespaceMoveLaw law, IReadOnlyList<WriteBackRename> list, List<(string RelativePath, string Source)> sources, HashSet<string> governed)
        {
            var unlisted = sources
                .SelectMany(file => law.OldPathStringsIn(file.RelativePath, file.Source, governed))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var byFile = sources.ToDictionary(file => file.RelativePath, file => file.Source, StringComparer.Ordinal);
            var formsFound = list
                .Select(entry => (Entry: entry, Found: byFile.TryGetValue(entry.File, out var source)
                    ? new[] { entry.OldText, entry.NewText }.Count(text => source.Contains($"\"{text}\"", StringComparison.Ordinal))
                    : 0))
                .ToList();
            var missing = formsFound
                .Where(found => found.Found == 0 && !found.Entry.MayLeave)
                .Select(found => $"{found.Entry.File}: \"{found.Entry.OldText}\" / \"{found.Entry.NewText}\"")
                .ToList();
            var both = formsFound
                .Where(found => found.Found == 2 && !found.Entry.MayLeave)
                .Select(found => $"{found.Entry.File}: \"{found.Entry.OldText}\" / \"{found.Entry.NewText}\"")
                .ToList();

            return new OldPathComparison(unlisted, missing, both);
        }

        private static void ThenEveryEntryOfTheMapIsSound(List<string> unsound)
            => Assert.That(
                unsound,
                Is.Empty,
                "These entries of WriteBackMembership do not match the compiled code:" + Environment.NewLine +
                string.Join(Environment.NewLine, unsound) + Environment.NewLine +
                "Why it matters: the rules that follow the move read this map, so a round type found in neither place, or in both, leaves a moved type unwatched or half moved, " +
                "and a neighbour that stays without its owner, its reason or its type is a decision nobody can check. " +
                "How to fix: give the round type its current name, old namespace and the new namespace Lighthouse.Backend.Modules.Forecasting.Application.WriteBack; move each type to one place only; " +
                "give each staying neighbour its current full name, its owner and its reason.");

        private static void ThenOnlyTheMadeUpEntriesAreNamed(List<string> unsound)
            => Assert.That(
                unsound,
                Has.Count.EqualTo(3)
                    .And.Some.StartsWith($"{MadeUpRoundRuleKeys.AMadeUpTypeName}: neither ")
                    .And.Some.StartsWith($"{MadeUpRoundRuleKeys.AMadeUpNeighbourName} ")
                    .And.Some.Contains("owner \" \""),
                $"The membership check named [{string.Join(" | ", unsound)}]. Expected exactly the made-up round type found in neither namespace, the made-up neighbour that is nowhere, " +
                "and the neighbour without an owner, and not the sound entries. Why it matters: a check that names nothing passes a map that has lost a type or a decision. " +
                "How to fix: report every round type found in neither or both of its namespaces, and every staying neighbour without an owner, a reason or a type in the compiled code.");

        private static void ThenNoPatternBreaksItsRole(RoleCheck check)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    check.Breaks,
                    Is.Empty,
                    "These rule patterns would not keep their meaning when the write-back round moves into Lighthouse.Backend.Modules.Forecasting.Application.WriteBack:" + Environment.NewLine +
                    string.Join(Environment.NewLine, check.Breaks) + Environment.NewLine +
                    "Why it matters: a part of the code that does not match the new place stops seeing the round the moment it moves and then passes because it finds nothing; " +
                    "an old location that matches the new place starts judging code that has left it; an undeclared pattern has no known meaning to keep. " +
                    "How to fix: declare the pattern in WriteBackPatternRoles with its subject and role, widen a Context pattern to the round's new name, or keep a Place pattern off the new place.");
                Assert.That(
                    check.UnsoundAdmissions,
                    Is.Empty,
                    "These patterns must see the write-back round in its new place, and do not:" + Environment.NewLine +
                    string.Join(Environment.NewLine, check.UnsoundAdmissions) + Environment.NewLine +
                    "Why it matters: the round pattern is the one owner of which types are the write-back round, and the Promises, Access and Licensing rules refuse the round by it; " +
                    "a round pattern that is missing, or that does not match the new full name of every round type, lets a type of those contexts join the round unseen once it moves. " +
                    @"How to fix: give ModuleRules a field TheWriteBackRound matched against the full name: the eight old full names of the round types, or ^Lighthouse\.Backend\.Modules\.Forecasting\.Application\.WriteBack\. ; declare it as a part of the code on the full name.");
            }
        }

        private static void ThenTheMadeUpBreaksNamedAre(RoleCheck check)
        {
            var named = check.Breaks.Select(found => found.Split(" = ")[0]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
            var unsound = check.UnsoundAdmissions.Select(found => found.Split(':')[0].Split(" = ")[0]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    named,
                    Is.EqualTo(new[]
                    {
                        $"{nameof(MadeUpRoundRuleKeys)}.{nameof(MadeUpRoundRuleKeys.AContextThatSeesOnlyTheOldPlace)}",
                        $"{nameof(MadeUpRoundRuleKeys)}.{nameof(MadeUpRoundRuleKeys.APlaceWidenedIntoTheNewPlace)}",
                        $"{nameof(MadeUpRoundRuleKeys)}.{nameof(MadeUpRoundRuleKeys.AnUndeclaredPatternSeeingAnOldFullName)}",
                        $"{nameof(MadeUpRoundRuleKeys)}.{nameof(MadeUpRoundRuleKeys.AnUndeclaredPatternSeeingTheNewPlace)}",
                    }),
                    $"The role check named [{string.Join(", ", named)}]. Expected the part of the code that sees only the old place, the old location widened into the new place, " +
                    "and the two undeclared patterns, and not the part of the code widened to the new place, the old location left in the old place or the admitted round patterns. " +
                    "Why it matters: a check that lets an undeclared pattern pass, or accepts an old location that sees the new place, lets a rule go blind or wrong at the move. " +
                    "How to fix: evaluate every declared pattern on its subject and role, and every undeclared one on both subjects.");
                Assert.That(
                    unsound,
                    Is.EqualTo(new[]
                    {
                        $"{nameof(MadeUpRoundRuleKeys)}.{nameof(MadeUpRoundRuleKeys.ARoundPatternBlindToTheNewPlace)}",
                        $"{nameof(MadeUpRoundRuleKeys)}.ARoundPatternThatIsNotThere",
                    }),
                    $"The admission check named [{string.Join(", ", unsound)}]. Expected the round pattern that does not see the new place and the admitted entry that is no rule pattern, " +
                    "and not the round pattern of both places or the one of the new place only. Why it matters: an admission that does not demand the new place hides a pattern blind to the moved round. " +
                    "How to fix: require every admitted pattern to exist and to match the new name of every round type, and leave its old name free.");
            }
        }

        private static void ThenOnlyTheTypeJoiningTheRoundIsRefused(RoundGuard guard, List<string> refused)
            => Assert.That(
                refused,
                Is.EqualTo(new[] { $"{guard.TypeJoiningTheRound.FullName} -> {typeof(Lighthouse.Backend.Modules.WriteBackFixtures.Forecasting.Application.WriteBack.IAMadeUpRoundPort).FullName}" }),
                $"{guard.Context}'s outbound predicate {guard.Rule.Name}.{guard.Predicate}, handed a round pattern naming the made-up round, refused [{string.Join(" | ", refused)}]. " +
                $"Expected it to refuse exactly {guard.TypeJoiningTheRound.Name} reaching the made-up round port, and not {guard.TwinAskingForecasting.Name} reaching a made-up Forecasting port outside the round. " +
                $"Why it matters: once the round lives in Forecasting's Application, a {guard.Context} type that takes part in it is seen only if the predicate reads the round pattern it is handed; " +
                "a predicate that still reads its list of old full names lets it join the round unseen, and one that refuses all of Forecasting refuses an edge Promises may make by right. " +
                $"How to fix: give {guard.Predicate} a second parameter, the round pattern matched against the full name of a top-level type, read it instead of the list, and hand it ModuleRules.TheWriteBackRound in the real rule.");

        private static void ThenTheRefusingRulesAreTheRecordedOnes(List<WorkItemsOutboundRule> rules, List<string> refusing)
            => Assert.That(
                refusing,
                Is.EqualTo(WriteBackMembership.WorkItemsRulesRefusingTheRoundInItsNewPlace),
                $"Of the Work Items outbound rules [{string.Join(", ", rules.Select(rule => rule.Name))}], [{string.Join(", ", refusing)}] refuse a made-up Work Items Application type reaching a made-up type " +
                $"in the round's new namespace; WriteBackMembership records [{string.Join(", ", WriteBackMembership.WorkItemsRulesRefusingTheRoundInItsNewPlace)}]. " +
                "Why it matters: Work Items refuses the round today by its old full names; whether a Work Items rule still refuses it in Forecasting decides whether those names may simply be taken away later. " +
                "How to fix: record in WriteBackMembership exactly the Work Items rules that refuse the edge, measured from the rules.");

        private static void ThenTheOldPathStringsAreExactlyTheList(OldPathComparison comparison)
            => Assert.That(
                comparison.Unlisted.Select(found => $"not on the list: {found}")
                    .Concat(comparison.Missing.Select(entry => $"in neither form: {entry}"))
                    .Concat(comparison.Both.Select(entry => $"in both forms: {entry}"))
                    .ToList(),
                Is.Empty,
                "The strings of the test project that name a write-back round type by its old Services path differ from WriteBackStringsTheMoveRenames." + Environment.NewLine +
                $"Not on the list: [{string.Join(" | ", comparison.Unlisted)}]" + Environment.NewLine +
                $"On the list, found in neither form: [{string.Join(" | ", comparison.Missing)}]" + Environment.NewLine +
                $"On the list, found in both forms: [{string.Join(" | ", comparison.Both)}]" + Environment.NewLine +
                "Why it matters: a string the list does not know stays behind when the round moves, and a rule or a list that names the old path then checks nothing; a file holding both forms is half renamed. " +
                "How to fix: rename the string with the move, or build it at run time from its parts; a string the move has not reached yet must be on the list with its new text.");

        private static void ThenTheMadeUpFileIsComparedBothWays(OldPathComparison comparison)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    comparison.Unlisted,
                    Has.Count.EqualTo(1).And.Some.Contains(MadeUpRenameList.TheUnlistedOldName),
                    $"The sweep named [{string.Join(" | ", comparison.Unlisted)}]; it should name only the unlisted old full name. Why it matters: a sweep that names nothing lets an old path stay behind. How to fix: name every literal that names a round type by its old path and is not on the list.");
                Assert.That(
                    comparison.Missing,
                    Has.Count.EqualTo(1).And.Some.Contains(MadeUpRenameList.TheListedButAbsentOldName),
                    $"The sweep reported [{string.Join(" | ", comparison.Missing)}] as found in neither form; it should report only the listed entry found nowhere, and accept the one in its new form and the one that may leave. Why it matters: a list that keeps a string nobody holds hides a rename that went wrong. How to fix: report every entry found in neither form that may not leave.");
                Assert.That(
                    comparison.Both,
                    Has.Count.EqualTo(1).And.Some.Contains(MadeUpRenameList.TheHalfRenamedOldName),
                    $"The sweep reported [{string.Join(" | ", comparison.Both)}] as found in both forms; it should report only the half-renamed entry. Why it matters: a file holding both forms keeps a rule that reads the old place. How to fix: report every entry whose file holds its old and its new text.");
            }
        }

        // Every move check keeps its made-up rule keys in a fixtures namespace under the architecture tests; they stand for no rule.
        private static bool InFixtures(Type type)
            => type.Namespace?.StartsWith(FixtureNamespace, StringComparison.Ordinal) == true
                || (type.Namespace?.StartsWith(typeof(WriteBackMembershipArchUnitTest).Namespace + ".", StringComparison.Ordinal) == true && type.Namespace.EndsWith("Fixtures", StringComparison.Ordinal));

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Lighthouse.Backend", "Lighthouse.sln")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("The repository root, the folder holding Lighthouse.Backend/Lighthouse.sln, was not found above the test directory.");
        }

        public sealed record RoundGuard(string Context, Type Rule, string Predicate, Type TypeJoiningTheRound, Type TwinAskingForecasting)
        {
            public override string ToString() => Context;
        }

        private sealed record WorkItemsOutboundRule(string Name, Func<ModuleDefinition, string, OutboundVerdict> Judge);

        private sealed record RoleCheck(List<string> Breaks, List<string> UnsoundAdmissions);

        private sealed record OldPathComparison(List<string> Unlisted, List<string> Missing, List<string> Both);
    }
}

namespace Lighthouse.Backend.Tests.Architecture.WriteBackMembershipFixtures
{
    /// <summary>Made-up rule keys the role check is run over, to show it names a key that breaks its role and passes the others.</summary>
    internal static class MadeUpRoundRuleKeys
    {
        public const string AMadeUpTypeName = "IAMadeUpRoundServiceThatIsNowhere";

        public const string AMadeUpNeighbourName = "Lighthouse.Backend.Models.WriteBack.AMadeUpNeighbourThatIsNowhere";

        public const string AContextThatSeesOnlyTheOldPlace = @"^Lighthouse\.Backend\.Services\.Interfaces\.IWriteBackService$";

        public const string AContextWidenedToTheNewPlace = @"^Lighthouse\.Backend\.(Services\.Interfaces|Modules\.Forecasting\.Application\.WriteBack)\.IWriteBackService$";

        public const string APlaceWidenedIntoTheNewPlace = @"^Lighthouse\.Backend\.(Services\.Implementation|Modules\.Forecasting\.Application\.WriteBack)\.WriteBackRound$";

        public const string APlaceLeftInTheOldPlace = @"^Lighthouse\.Backend\.Services\.Implementation\.WriteBackRound$";

        public const string AnUndeclaredPatternSeeingAnOldFullName = @"^Lighthouse\.Backend\.Services\.Implementation\.WriteBackCollector$";

        public const string AnUndeclaredPatternSeeingTheNewPlace = @"^Lighthouse\.Backend\.Modules\.Forecasting\.Application\.WriteBack(\.|$)";

        public const string ARoundPatternOfBothPlaces = @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.(I?WriteBack(Collector|Round|RoundContext|Service|TriggerService))$|Modules\.Forecasting\.Application\.WriteBack\.)";

        public const string ARoundPatternOfTheNewPlaceOnly = @"^Lighthouse\.Backend\.Modules\.Forecasting\.Application\.WriteBack\.";

        public const string ARoundPatternBlindToTheNewPlace = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.I?WriteBack(Collector|Round|RoundContext|Service|TriggerService)$";
    }

    /// <summary>The subject and role of each made-up key, as the real rules have theirs in WriteBackPatternRoles.</summary>
    internal static class MadeUpRoundRuleRoles
    {
        public static IReadOnlyDictionary<string, PatternDeclaration> Roles { get; } = new Dictionary<string, PatternDeclaration>(StringComparer.Ordinal)
        {
            ["MadeUpRoundRuleKeys.AContextThatSeesOnlyTheOldPlace"] = PatternDeclaration.Context(PatternSubject.FullName, "a part of the code that was never widened"),
            ["MadeUpRoundRuleKeys.AContextWidenedToTheNewPlace"] = PatternDeclaration.Context(PatternSubject.FullName, "a part of the code widened with the type's new full name"),
            ["MadeUpRoundRuleKeys.APlaceWidenedIntoTheNewPlace"] = PatternDeclaration.Place(PatternSubject.FullName, "an old location that was given the new place"),
            ["MadeUpRoundRuleKeys.APlaceLeftInTheOldPlace"] = PatternDeclaration.Place(PatternSubject.FullName, "an old location left in the old place"),
            ["MadeUpRoundRuleKeys.ARoundPatternOfBothPlaces"] = PatternDeclaration.Context(PatternSubject.FullName, "the round while it moves"),
            ["MadeUpRoundRuleKeys.ARoundPatternOfTheNewPlaceOnly"] = PatternDeclaration.Context(PatternSubject.FullName, "the round after it moved"),
            ["MadeUpRoundRuleKeys.ARoundPatternBlindToTheNewPlace"] = PatternDeclaration.Context(PatternSubject.FullName, "the round by its old names only"),
        };

        public static IReadOnlyDictionary<string, string> SeeTheRoundInItsNewPlace { get; } = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MadeUpRoundRuleKeys.ARoundPatternOfBothPlaces"] = "admitted while it sees both places",
            ["MadeUpRoundRuleKeys.ARoundPatternOfTheNewPlaceOnly"] = "admitted once narrowed",
            ["MadeUpRoundRuleKeys.ARoundPatternBlindToTheNewPlace"] = "admitted, but blind to the new place",
            ["MadeUpRoundRuleKeys.ARoundPatternThatIsNotThere"] = "admitted, but no such pattern",
        };

        public static NamespaceMoveLaw TheLaw { get; } = new(
            new NamespaceMove(
                WriteBackMembership.MovingTypes,
                [],
                @"^Lighthouse\.Backend\.Modules\.Forecasting\.Application\.WriteBack(\.|$)",
                new System.Text.RegularExpressions.Regex(@"(?<=Forecasting\\\.Application\\\.)WriteBack(?![A-Za-z0-9_])", System.Text.RegularExpressions.RegexOptions.None, NamespaceMoveLaw.PatternTimeout),
                []),
            new PatternRoleMap(Roles, new Dictionary<(string Field, string Type), string>()));
    }

    /// <summary>A made-up rule file and its rename list, to show the sweep compares the two both ways.</summary>
    internal static class MadeUpRenameList
    {
        public const string TheFile = "Lighthouse.Backend/Lighthouse.Backend.Tests/Architecture/AMadeUpRoundRuleFile.cs";

        public static string TheUnlistedOldName => string.Join('.', "Lighthouse.Backend.Services.Implementation", "WriteBackCollector");

        public static string TheListedButAbsentOldName => string.Join('.', "Lighthouse.Backend.Services.Interfaces", "IWriteBackCollector");

        public static string TheHalfRenamedOldName => string.Join('.', "Lighthouse.Backend.Services.Implementation", "WriteBackRound");

        private static string TheNewPlace => string.Join('.', "Lighthouse.Backend.Modules.Forecasting.Application", "WriteBack");

        public static string TheSource => string.Join(
            Environment.NewLine,
            $"var listedAndStillOld = \"{string.Join('.', "Lighthouse.Backend.Services.Interfaces", "IWriteBackService")}\";",
            $"var listedAndAlreadyRenamed = \"{string.Join('.', TheNewPlace, "IWriteBackTriggerService")}\";",
            $"var halfRenamedOld = \"{TheHalfRenamedOldName}\";",
            $"var halfRenamedNew = \"{string.Join('.', TheNewPlace, "WriteBackRound")}\";",
            $"var unlisted = \"{TheUnlistedOldName}\";",
            "var aTestNamespace = \"Lighthouse.Backend.Tests.Services.Implementation.WriteBackServiceTest\";");

        public static IReadOnlyList<WriteBackRename> Entries { get; } =
        [
            new(TheFile, string.Join('.', "Lighthouse.Backend.Services.Interfaces", "IWriteBackService"), string.Join('.', TheNewPlace, "IWriteBackService")),
            new(TheFile, string.Join('.', "Lighthouse.Backend.Services.Interfaces", "IWriteBackTriggerService"), string.Join('.', TheNewPlace, "IWriteBackTriggerService")),
            new(TheFile, TheHalfRenamedOldName, string.Join('.', TheNewPlace, "WriteBackRound")),
            new(TheFile, TheListedButAbsentOldName, string.Join('.', TheNewPlace, "IWriteBackCollector")),
            new(TheFile, string.Join('.', "Lighthouse.Backend.Services.Implementation", "WriteBackService"), string.Join('.', TheNewPlace, "WriteBackService"), MayLeave: true),
        ];

        public static NamespaceMoveLaw TheLaw { get; } = new(
            new NamespaceMove(
                WriteBackMembership.MovingTypes,
                [.. Entries.Select(entry => entry.AsRenamedString())],
                @"^Lighthouse\.Backend\.Modules\.Forecasting\.Application\.WriteBack(\.|$)",
                new System.Text.RegularExpressions.Regex("(?!)", System.Text.RegularExpressions.RegexOptions.None, NamespaceMoveLaw.PatternTimeout),
                []),
            new PatternRoleMap(new Dictionary<string, PatternDeclaration>(), new Dictionary<(string Field, string Type), string>()));
    }
}

namespace Lighthouse.Backend.Modules.WriteBackFixtures.Forecasting.Application.WriteBack
{
    /// <summary>A made-up port of the write-back round, in a made-up round namespace.</summary>
    internal interface IAMadeUpRoundPort
    {
        int Stage(int fields);
    }
}

namespace Lighthouse.Backend.Modules.WriteBackFixtures.Forecasting.Application
{
    /// <summary>A made-up Forecasting port outside the round, which Promises may reach by right.</summary>
    internal interface IAMadeUpForecastPort
    {
        int Forecast(int days);
    }
}

namespace Lighthouse.Backend.Modules.WriteBackFixtures.Promises.Application
{
    internal sealed class ADeliveryServiceJoiningTheRound(global::Lighthouse.Backend.Modules.WriteBackFixtures.Forecasting.Application.WriteBack.IAMadeUpRoundPort round)
    {
        public int Promise(int fields) => round.Stage(fields);
    }

    internal sealed class ADeliveryServiceAskingAForecast(global::Lighthouse.Backend.Modules.WriteBackFixtures.Forecasting.Application.IAMadeUpForecastPort forecast)
    {
        public int Promise(int days) => forecast.Forecast(days);
    }
}

namespace Lighthouse.Backend.Modules.WriteBackFixtures.Access.Application
{
    internal sealed class AnAccessServiceJoiningTheRound(global::Lighthouse.Backend.Modules.WriteBackFixtures.Forecasting.Application.WriteBack.IAMadeUpRoundPort round)
    {
        public int Allow(int fields) => round.Stage(fields);
    }

    internal sealed class AnAccessServiceAskingAForecast(global::Lighthouse.Backend.Modules.WriteBackFixtures.Forecasting.Application.IAMadeUpForecastPort forecast)
    {
        public int Allow(int days) => forecast.Forecast(days);
    }
}

namespace Lighthouse.Backend.Modules.WriteBackFixtures.Licensing.Application
{
    internal sealed class ALicenceServiceJoiningTheRound(global::Lighthouse.Backend.Modules.WriteBackFixtures.Forecasting.Application.WriteBack.IAMadeUpRoundPort round)
    {
        public int Allow(int fields) => round.Stage(fields);
    }

    internal sealed class ALicenceServiceAskingAForecast(global::Lighthouse.Backend.Modules.WriteBackFixtures.Forecasting.Application.IAMadeUpForecastPort forecast)
    {
        public int Allow(int days) => forecast.Forecast(days);
    }
}

namespace Lighthouse.Backend.Modules.WriteBackFixtures.WorkItems.Application
{
    internal sealed class AWorkItemServiceJoiningTheRound(global::Lighthouse.Backend.Modules.Forecasting.Application.WriteBack.IAMadeUpRoundTypeForTheWorkItemsRules round)
    {
        public int Sync(int fields) => round.Stage(fields);
    }
}

namespace Lighthouse.Backend.Modules.Forecasting.Application.WriteBack
{
    /// <summary>
    /// A made-up type in the round's real new namespace, so the Work Items rules, which read real namespaces, judge an edge into
    /// the round in its new place.
    /// </summary>
    internal interface IAMadeUpRoundTypeForTheWorkItemsRules
    {
        int Stage(int fields);
    }
}
