using System.Text.RegularExpressions;
using Lighthouse.Backend.Modules.AccessFixtures.DomainReach;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Fixes which types make up Access and Licensing, and the layer each goes to, before any of them moves into
    /// Modules/Access or Modules/Licensing. The types are read from the compiled code: every top-level type in the old
    /// Services Auth, Authorization and Licensing namespaces or under Modules.Access and Modules.Licensing. Each is on one of
    /// the lists in AccessAndLicensingMembership, and a listed name the code no longer holds fails, so the lists hold the same
    /// before and after the move. A type listed as Domain reaches only the shared Models and the base library, and a class
    /// listed as Application reaches more than that, or it would belong in the Domain.
    /// A shared Models type goes with Access only when it is not stored by the database context, not sent by a controller,
    /// not reached by a Models type that stays and not used by any other code that stays outside the modules, because nothing
    /// left behind may have to reach into a module for it.
    /// </summary>
    [TestFixture]
    public class AccessAndLicensingMembershipArchUnitTest
    {
        private const string AccessNamespaces =
            @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.(Auth|Authorization)|Modules\.Access)(\.|$)";

        private const string LicensingNamespaces =
            @"^Lighthouse\.Backend\.(Services\.(Implementation|Interfaces)\.Licensing|Modules\.Licensing)(\.|$)";

        private const string KernelNamespaces = @"^Lighthouse\.Backend\.Models(\.|$)";

        private const string KernelOrBaseLibrary = @"^(System|Lighthouse\.Backend\.Models|Lighthouse\.Backend\.Modules\.Access\.Domain)(\.|$)";

        private const string MadeUpModuleTypes = @"^Lighthouse\.Backend\.Modules\.Access(Fixtures)?\.Membership(Fixtures)?$";

        private const string MadeUpDomainTypes = @"^Lighthouse\.Backend\.Modules\.AccessFixtures\.DomainReach$";

        private const string MadeUpCandidateNamespace = @"^Lighthouse\.Backend\.Models\.AccessMembershipFixtures$";

        private const string Admitted = "admitted";
        private const string Stored = "stored";
        private const string OnTheWire = "on the wire";
        private const string ReachedByTheKernel = "reached by the kernel";
        private const string UsedByCodeThatStays = "used by code that stays";

        private const string LicenceKeyResource = "public_key.pem";

        private static readonly TimeSpan PatternTimeout = TimeSpan.FromSeconds(1);

        // The shared Models types the design named as possibly belonging to Access.
        private static readonly string[] ModelsCandidates =
        [
            "ApiKeyOwnerResolutionState", "ApiKeyValidationResult", "AuthConfigurationValidationResult", "EmbedSessionTokenRedemption",
            "EffectiveAccess", "PermissionRule", "RbacOperationResult",
        ];

        private const string AccessFixHint =
            "Every Access type is on a list on purpose: put it on AccessAndLicensingMembership.AccessDomain, AccessApplication or AccessAdaptersHttp for the layer it goes to in Modules/Access, or on ModelsTypesThatMoveIntoAccess if it is a shared Models type Access takes with it. A listed type the code no longer holds was renamed or deleted: put its new name on the list, or take it off. A name the code holds twice is listed with its namespace, so one type cannot hide behind the other.";

        private const string LicensingFixHint =
            "Every Licensing type is on a list on purpose: put it on AccessAndLicensingMembership.LicensingApplication or LicensingAdaptersHttp for the layer it goes to in Modules/Licensing. A listed type the code no longer holds was renamed or deleted: put its new name on the list, or take it off. A name the code holds twice is listed with its namespace, so one type cannot hide behind the other.";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(BaseMetricsService).Assembly));

        private static readonly Lazy<ModuleDefinition> TestModule = new(() => IlDependencies.ReadModule(typeof(AccessAndLicensingMembershipArchUnitTest).Assembly));

        // Judging reads every type of the module, so it runs once per run.
        private static readonly Lazy<Dictionary<string, CandidateJudgement>> ProductionJudgement =
            new(() => WhenTheModelsCandidatesAreJudged(ProductionModule.Value, IsANamedModelsCandidate));

        private static readonly Lazy<Dictionary<string, CandidateJudgement>> MadeUpJudgement =
            new(() => WhenTheModelsCandidatesAreJudged(TestModule.Value, type => IsIn(type.Namespace, MadeUpCandidateNamespace)));

        private static IReadOnlyList<string> AccessLists =>
        [
            .. AccessAndLicensingMembership.AccessDomain, .. AccessAndLicensingMembership.AccessApplication,
            .. AccessAndLicensingMembership.AccessAdaptersHttp, .. AccessAndLicensingMembership.ModelsTypesThatMoveIntoAccess,
            .. AccessAndLicensingMembership.AccessModuleRoot, .. AccessAndLicensingMembership.AccessContracts,
        ];

        private static IReadOnlyList<string> LicensingLists =>
            [.. AccessAndLicensingMembership.LicensingApplication, .. AccessAndLicensingMembership.LicensingAdaptersHttp, .. AccessAndLicensingMembership.LicensingModuleRoot, .. AccessAndLicensingMembership.LicensingContracts];

        [OneTimeTearDown]
        public void DisposeModules()
        {
            foreach (var module in new[] { ProductionModule, TestModule }.Where(module => module.IsValueCreated))
            {
                module.Value.Dispose();
            }
        }

        [Test]
        public void TheAccessTypesInTheCompiledCode_AreExactlyTheTypesListed()
        {
            var found = WhenTheAccessTypesAreCollectedFrom(ProductionModule.Value);

            ThenTheyMatchTheAccessLists(found);
        }

        [Test]
        public void TheLicensingTypesInTheCompiledCode_AreExactlyTheTypesListed()
        {
            var found = WhenTheLicensingTypesAreCollectedFrom(ProductionModule.Value);

            ThenTheyMatchTheLists(found, LicensingLists, LicensingFixHint);
        }

        [Test]
        public void NoTypeIsOnTwoLists()
        {
            var onTwo = new[] { AccessLists, LicensingLists }
                .SelectMany(names => names)
                .Concat(AccessAndLicensingMembership.AccessDomain.Intersect(AccessAndLicensingMembership.AccessApplication, StringComparer.Ordinal))
                .GroupBy(name => name, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();

            Assert.That(onTwo, Is.Empty,
                $"{string.Join(", ", onTwo)} is on more than one list. A type goes to one module and one layer: keep it on one list, or the move would not know where to put it.");
        }

        [Test]
        public void ATypeListedAsAccessDomain_ReachesOnlyTheSharedModelsAndTheBaseLibrary()
        {
            var reaching = WhenTheDomainTypesReachingBeyondTheKernelAreCollected(ProductionModule.Value, AccessNamespaces, AccessDomainNames);

            ThenNoneReachesBeyond(reaching);
        }

        [Test]
        public void AClassListedAsAccessApplication_ReachesMoreThanTheSharedModelsAndTheBaseLibrary()
        {
            var onlyTheKernel = WhenTheClassesReachingOnlyTheKernelAreCollected(ProductionModule.Value, AccessNamespaces, AccessDomainNames, AccessAndLicensingMembership.AccessApplication);

            ThenNoneBelongsInTheDomain(onlyTheKernel);
        }

        [Test]
        public void AClassListedAsLicensingApplication_ReachesMoreThanTheSharedModelsAndTheBaseLibrary()
        {
            var onlyTheKernel = WhenTheClassesReachingOnlyTheKernelAreCollected(ProductionModule.Value, LicensingNamespaces, [], AccessAndLicensingMembership.LicensingApplication);

            ThenNoneBelongsInTheDomain(onlyTheKernel);
        }

        [Test]
        public void TheLicenceKey_IsEmbeddedOnce_InTheLicensingNamespaces()
        {
            var resources = ProductionModule.Value.Resources.Select(resource => resource.Name);

            ThenTheLicenceKeyIsEmbeddedOnceInLicensing(resources);
        }

        [Test]
        public void TheModelsTypesListedToMove_AreExactlyTheCandidatesNeitherStoredNorOnTheWireNorUsedOutsideTheModules()
        {
            var judged = ProductionJudgement.Value;

            ThenTheAdmittedOnesAreListed(judged);
        }

        [TestCase("ApiKeyOwnerResolutionState", Admitted)]
        [TestCase("ApiKeyValidationResult", Admitted)]
        [TestCase("AuthConfigurationValidationResult", Admitted)]
        [TestCase("EffectiveAccess", Admitted)]
        [TestCase("PermissionRule", ReachedByTheKernel)]
        [TestCase("EmbedSessionTokenRedemption", UsedByCodeThatStays)]
        [TestCase("RbacOperationResult", UsedByCodeThatStays)]
        public void ASharedModelsCandidate_IsJudgedAsTheCompiledCodeShows(string candidate, string expected)
        {
            var judged = ProductionJudgement.Value;

            ThenTheCandidateIsJudged(judged, candidate, expected);
        }

        [TestCase("AMadeUpStoredRecord", Stored)]
        [TestCase("AMadeUpConfiguredRecord", Stored)]
        [TestCase("AMadeUpAnswer", OnTheWire)]
        [TestCase("AMadeUpAnswerPart", OnTheWire)]
        [TestCase("AMadeUpReachedValue", ReachedByTheKernel)]
        [TestCase("AMadeUpValueUsedByCodeThatStays", UsedByCodeThatStays)]
        [TestCase("AMadeUpValueOnlyAccessUses", Admitted)]
        public void AMadeUpModelsCandidate_IsJudgedByWhatTheCompiledCodeShows(string candidate, string expected)
        {
            var judged = MadeUpJudgement.Value;

            ThenTheCandidateIsJudged(judged, candidate, expected);
        }

        [Test]
        public void AMadeUpTypeInTheModule_IsNamedAsUnlisted_AndANamesakeOutsideAndANestedTypeAreNot()
        {
            var found = WhenTheAccessTypesAreCollectedFrom(ProductionModule.Value)
                .Concat(WhenTheAccessTypesAreCollectedFrom(TestModule.Value).Where(name => IsIn(NamespaceOfName(name), MadeUpModuleTypes)));

            var failure = Assert.Throws<AssertionException>(() => ThenTheyMatchTheAccessLists(found))!.Message;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain("AMadeUpAccessServiceInTheModule"),
                    "A made-up type under Modules.Access was not named as unlisted. The collection must take in the module's namespaces, or a type added there during the move would join Access unseen.");
                Assert.That(failure, Does.Not.Contain("ANamesakeOutsideAccess"),
                    "A type in Modules.AccessFixtures was named as Access. The namespace pattern must end at a dot or the end of the name, not take in every namespace that starts with the same letters.");
                Assert.That(failure, Does.Not.Contain("ANestedAccessStep"),
                    "A type nested inside a made-up Access type was named on its own. Only top-level types belong on the lists; a nested type moves with the type that holds it.");
                Assert.That(failure, Does.Not.Contain("On the list, but no longer"),
                    "Adding a made-up type made a listed type look missing. The collection must not lose a real type when another assembly's types are added.");
            }
        }

        [Test]
        public void AListedTypeRenamedInTheCode_IsNamedAsMissing_AndItsNewNameAsUnlisted()
        {
            var found = WhenTheAccessTypesAreCollectedFrom(ProductionModule.Value)
                .Select(name => ShortNameOf(name) == "GroupClaimParser" ? $"{NamespaceOfName(name)}.GroupClaimReader" : name);

            var failure = Assert.Throws<AssertionException>(() => ThenTheyMatchTheAccessLists(found))!.Message;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain("On the list, but no longer offending: GroupClaimParser."),
                    "A listed type the code no longer holds was not named, or other listed types were named with it. A rename during the move must fail until the list follows it.");
                Assert.That(failure, Does.Contain("Not on the list, and offending: GroupClaimReader."),
                    "A renamed type was not named as unlisted under its new name, or other types were named with it.");
            }
        }

        [Test]
        public void ATypeAlreadyInItsNewNamespace_IsStillFoundOnItsList()
        {
            var found = WhenTheLicensingTypesAreCollectedFrom(ProductionModule.Value)
                .Select(name => ShortNameOf(name) == "LicenseVerifier" ? "Lighthouse.Backend.Modules.Licensing.Application.LicenseVerifier" : name);

            Assert.DoesNotThrow(() => ThenTheyMatchTheLists(found, LicensingLists, LicensingFixHint),
                "LicenseVerifier found under Modules.Licensing.Application instead of its old namespace no longer matched its list. The lists name types without their namespace, so a type that has moved must still be found.");
        }

        [Test]
        public void ACopyOfAListedTypeInAnotherNamespace_IsNamedByItsFullName()
        {
            var collected = WhenTheAccessTypesAreCollectedFrom(ProductionModule.Value);
            var original = collected.Single(name => ShortNameOf(name) == "RbacAdministrationService");
            var copy = "Lighthouse.Backend.Services.Implementation.Authorization.RbacAdministrationService";

            var failure = Assert.Throws<AssertionException>(() => ThenTheyMatchTheAccessLists([.. collected, copy]))!.Message;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain(original).And.Contain(copy),
                    "Two types named RbacAdministrationService in two namespaces were not both named by their full names. A move that leaves a copy behind must fail, not count the two as one listed name.");
                Assert.That(failure, Does.Contain("On the list, but no longer offending: RbacAdministrationService."),
                    "The short name of a type the code holds twice still matched the list. A name the code holds twice must be listed by its full names.");
            }
        }

        [Test]
        public void AMadeUpDomainTypeReachingTheApplicationOrThePlatform_IsNamed_AndOneReachingOnlyTheKernelIsNot()
        {
            string[] listedAsDomain =
            [
                nameof(AMadeUpRuleReachingTheApplication), nameof(AMadeUpRuleReachingThePlatform), nameof(AMadeUpRuleReachingOnlyTheKernel),
                nameof(AMadeUpRuleReachingANamesakeOfADomainType),
            ];

            var reaching = WhenTheDomainTypesReachingBeyondTheKernelAreCollected(TestModule.Value, MadeUpDomainTypes, listedAsDomain);

            var failure = Assert.Throws<AssertionException>(() => ThenNoneReachesBeyond(reaching))!.Message;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain($"{nameof(AMadeUpRuleReachingTheApplication)} reaches Lighthouse.Backend.Modules.Access.Contracts.IRbacAdministrationService"),
                    "A made-up Domain type that calls an Application interface was not named with what it reaches. The Domain must not depend on the Application.");
                Assert.That(failure, Does.Contain($"{nameof(AMadeUpRuleReachingThePlatform)} reaches Microsoft.AspNetCore.Http.HttpContext"),
                    "A made-up Domain type that reads the HTTP context was not named with what it reaches. The Domain must not depend on the platform.");
                Assert.That(failure, Does.Contain($"{nameof(AMadeUpRuleReachingANamesakeOfADomainType)} reaches Lighthouse.Backend.Modules.AccessFixtures.Application.{nameof(AMadeUpRuleReachingOnlyTheKernel)}"),
                    "A made-up Domain type that calls an Application type sharing its short name with a Domain-listed type was not named. The types a Domain type may reach must be matched by their full names, or any Application type can hide behind a Domain namesake.");
                Assert.That(failure, Does.Not.Contain($"{nameof(AMadeUpRuleReachingOnlyTheKernel)} reaches"),
                    "A made-up Domain type that reaches only a Models type and the base library was named. The rule must let the Domain use the shared kernel.");
            }
        }

        [Test]
        public void AMadeUpClassReachingOnlyTheKernelButListedAsApplication_IsNamed()
        {
            string[] listedAsApplication = [nameof(AMadeUpRuleReachingOnlyTheKernel), nameof(AMadeUpRuleReachingTheApplication)];

            var onlyTheKernel = WhenTheClassesReachingOnlyTheKernelAreCollected(TestModule.Value, MadeUpDomainTypes, [], listedAsApplication);

            var failure = Assert.Throws<AssertionException>(() => ThenNoneBelongsInTheDomain(onlyTheKernel))!.Message;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(failure, Does.Contain(nameof(AMadeUpRuleReachingOnlyTheKernel)),
                    "A made-up class listed as Application that reaches only a Models type and the base library was not named. Such a class belongs in the Domain.");
                Assert.That(failure, Does.Not.Contain(nameof(AMadeUpRuleReachingTheApplication)),
                    "A made-up class listed as Application that calls an Application interface was named as belonging in the Domain.");
            }
        }

        private static readonly string[] KeyLeftBehindBesideItsMovedCopy =
            ["Lighthouse.Backend.Services.Implementation.Licensing.public_key.pem", "Lighthouse.Backend.Modules.Licensing.Application.public_key.pem"];

        private static readonly string[] KeyOutsideLicensing = ["Lighthouse.Backend.Services.Implementation.Auth.public_key.pem"];

        private static IEnumerable<TestCaseData> LicenceKeysNotEmbeddedOnceInLicensing()
        {
            yield return new TestCaseData((object)Array.Empty<string>()).SetName("AMissingLicenceKey_IsNamed");
            yield return new TestCaseData((object)KeyLeftBehindBesideItsMovedCopy).SetName("ALicenceKeyLeftBehindBesideItsMovedCopy_IsNamed");
            yield return new TestCaseData((object)KeyOutsideLicensing).SetName("ALicenceKeyOutsideLicensing_IsNamed");
        }

        [TestCaseSource(nameof(LicenceKeysNotEmbeddedOnceInLicensing))]
        public void ALicenceKeyNotEmbeddedOnceInLicensing_FailsAndNamesWhatWasFound(string[] resources)
        {
            var failure = Assert.Throws<AssertionException>(() => ThenTheLicenceKeyIsEmbeddedOnceInLicensing(resources))!.Message;

            Assert.That(failure, Does.Contain($"[{string.Join(", ", resources)}]"),
                "The failure did not name the licence key resources it found, so a reader cannot tell which one to fix.");
        }

        private static IReadOnlyList<string> AccessDomainNames =>
            [.. AccessAndLicensingMembership.AccessDomain, .. AccessAndLicensingMembership.ModelsTypesThatMoveIntoAccess];

        private static List<string> WhenTheAccessTypesAreCollectedFrom(ModuleDefinition module)
        {
            var inTheNamespaces = IlDependencies.TypesIn(module, AccessNamespaces).Select(OuterTypeOf);
            var admittedStillInTheKernel = module.Types
                .Where(type => IsIn(type.Namespace, KernelNamespaces) && AccessAndLicensingMembership.ModelsTypesThatMoveIntoAccess.Contains(ShortNameOf(type.FullName)))
                .Select(type => type.FullName);

            return [.. inTheNamespaces.Concat(admittedStillInTheKernel).Distinct(StringComparer.Ordinal)];
        }

        private static List<string> WhenTheLicensingTypesAreCollectedFrom(ModuleDefinition module)
            => [.. IlDependencies.TypesIn(module, LicensingNamespaces).Select(OuterTypeOf).Distinct(StringComparer.Ordinal)];

        private static void ThenTheyMatchTheAccessLists(IEnumerable<string> found)
            => ThenTheyMatchTheLists(found, AccessLists, AccessFixHint);

        private static void ThenTheyMatchTheLists(IEnumerable<string> found, IReadOnlyList<string> listed, string fixHint)
            => InlineBaseline.AssertMatches(ListedNamesOf(found), listed, fixHint);

        // A type is listed by its name alone, unless the code holds that name twice: then each is listed by its full name.
        private static IEnumerable<string> ListedNamesOf(IEnumerable<string> fullNames)
            => fullNames.Distinct(StringComparer.Ordinal)
                .GroupBy(ShortNameOf, StringComparer.Ordinal)
                .SelectMany(group => group.Count() == 1 ? [group.Key] : group.AsEnumerable());

        // What a Domain type may reach: the shared Models, the base library, the module's Domain, and the other types listed as
        // Domain, by their full names as the code holds them, so a namesake elsewhere does not pass for one.
        private static List<string> WhenTheDomainTypesReachingBeyondTheKernelAreCollected(ModuleDefinition module, string namespaces, IReadOnlyCollection<string> listedAsDomain)
        {
            var domainTypes = DomainTypesFoundIn(module, namespaces, listedAsDomain);

            return [.. IlDependencies.DependentsIn(module, namespaces, target => ReachesBeyondTheKernel(target, domainTypes))
                .Where(dependent => listedAsDomain.Contains(ShortNameOf(dependent.TypeName)))
                .Select(dependent => $"{ShortNameOf(dependent.TypeName)} reaches {string.Join(", ", dependent.Targets.Where(target => OuterTypeOf(target) != OuterTypeOf(dependent.TypeName)))}")];
        }

        // Interfaces are left out: they are the ports the Application offers, whatever they reach.
        private static List<string> WhenTheClassesReachingOnlyTheKernelAreCollected(
            ModuleDefinition module, string namespaces, IReadOnlyCollection<string> listedAsDomain, IReadOnlyCollection<string> listedAsApplication)
        {
            var domainTypes = DomainTypesFoundIn(module, namespaces, listedAsDomain);
            var reachingBeyond = IlDependencies.DependentsIn(module, namespaces, target => ReachesBeyondTheKernel(target, domainTypes))
                .Where(dependent => dependent.Targets.Any(target => OuterTypeOf(target) != OuterTypeOf(dependent.TypeName)))
                .Select(dependent => ShortNameOf(dependent.TypeName))
                .ToHashSet(StringComparer.Ordinal);

            return [.. module.Types
                .Where(type => IsIn(type.Namespace, namespaces) && !type.IsInterface && listedAsApplication.Contains(ShortNameOf(type.FullName)))
                .Select(type => ShortNameOf(type.FullName))
                .Where(name => !reachingBeyond.Contains(name))
                .Order(StringComparer.Ordinal)];
        }

        private static HashSet<string> DomainTypesFoundIn(ModuleDefinition module, string namespaces, IReadOnlyCollection<string> listedAsDomain)
            => module.Types
                .Where(type => IsIn(type.Namespace, namespaces) && listedAsDomain.Contains(ShortNameOf(type.FullName)))
                .Select(type => type.FullName)
                .ToHashSet(StringComparer.Ordinal);

        private static bool ReachesBeyondTheKernel(TypeReference target, HashSet<string> domainTypes)
            => target is not GenericParameter
                && !target.Name.StartsWith('<')
                && !IsIn(IlDependencies.NamespaceOf(target), KernelOrBaseLibrary)
                && !domainTypes.Contains(OutermostOf(target.GetElementType()).FullName);

        private static void ThenNoneReachesBeyond(List<string> reaching)
            => Assert.That(reaching, Is.Empty,
                $"Listed as Domain, but reaching beyond the shared Models and the base library: {string.Join("; ", reaching)}. The Domain of a module depends on nothing but the shared kernel and the base library, so it can be tested and reasoned about alone. Move the type to AccessApplication or AccessAdaptersHttp, or take the dependency out of it.");

        private static void ThenNoneBelongsInTheDomain(List<string> onlyTheKernel)
            => Assert.That(onlyTheKernel, Is.Empty,
                $"Listed as Application, but reaching only the shared Models and the base library: {string.Join(", ", onlyTheKernel)}. A class that depends on nothing else belongs in the module's Domain: move it to AccessDomain, or to a Domain of Licensing if one is designed.");

        private static void ThenTheLicenceKeyIsEmbeddedOnceInLicensing(IEnumerable<string> resources)
        {
            var keys = resources.Where(name => name.EndsWith($".{LicenceKeyResource}", StringComparison.Ordinal)).ToList();

            Assert.That(keys.Count == 1 && IsIn(keys[0][..^(LicenceKeyResource.Length + 1)], LicensingNamespaces), Is.True,
                $"The licence key {LicenceKeyResource} must be embedded exactly once, under the Licensing namespaces, but these were found: [{string.Join(", ", keys)}]. LicenseVerifier reads the one resource whose name ends in it, so a missing key fails every licence check and a second copy may be the stale one. Keep one EmbeddedResource for it in Lighthouse.Backend.csproj, next to LicenseVerifier.");
        }

        // A named candidate is looked for in the shared Models and, once it has moved, in the modules' namespaces.
        private static bool IsANamedModelsCandidate(TypeDefinition type)
            => (IsIn(type.Namespace, KernelNamespaces) || IsIn(type.Namespace, AccessNamespaces) || IsIn(type.Namespace, LicensingNamespaces))
                && ModelsCandidates.Contains(ShortNameOf(type.FullName));

        private static Dictionary<string, CandidateJudgement> WhenTheModelsCandidatesAreJudged(ModuleDefinition module, Func<TypeDefinition, bool> isACandidate)
        {
            var candidates = module.Types.Where(isACandidate).ToList();
            var candidateNames = candidates.Select(type => type.FullName).ToHashSet(StringComparer.Ordinal);
            var movingMembers = WhenTheAccessTypesAreCollectedFrom(module).Concat(WhenTheLicensingTypesAreCollectedFrom(module))
                .Where(name => !candidateNames.Contains(name))
                .ToHashSet(StringComparer.Ordinal);
            var stored = StoredTypesIn(module);
            var onTheWire = TypesSentByControllersIn(module);
            var (reachedBy, usedBy) = ReachOf(module, candidateNames, movingMembers);

            var admitted = candidateNames.Where(name => !stored.Contains(name) && !onTheWire.Contains(name)).ToHashSet(StringComparer.Ordinal);
            int refused;
            do
            {
                refused = admitted.RemoveWhere(name => usedBy[name].Count > 0 || reachedBy[name].Any(reacher => !admitted.Contains(reacher)));
            }
            while (refused > 0);

            return candidates.ToDictionary(
                type => ShortNameOf(type.FullName),
                type => new CandidateJudgement(
                    stored.Contains(type.FullName),
                    onTheWire.Contains(type.FullName),
                    [.. reachedBy[type.FullName].Where(reacher => !admitted.Contains(reacher)).Select(ShortNameOf).Distinct(StringComparer.Ordinal)],
                    [.. usedBy[type.FullName].Select(ShortNameOf).Distinct(StringComparer.Ordinal)],
                    admitted.Contains(type.FullName)),
                StringComparer.Ordinal);
        }

        private static void ThenTheAdmittedOnesAreListed(Dictionary<string, CandidateJudgement> judged)
        {
            var refusals = string.Join(" ", judged.Where(entry => !entry.Value.Admitted).Select(entry => entry.Value.Describe(entry.Key)));

            InlineBaseline.AssertMatches(
                judged.Where(entry => entry.Value.Admitted).Select(entry => entry.Key),
                AccessAndLicensingMembership.ModelsTypesThatMoveIntoAccess,
                $"A shared Models type moves with Access only when it is not stored, not sent by a controller, not reached by a Models type that stays and not used by any other code that stays outside the modules: put each admitted type on ModelsTypesThatMoveIntoAccess, and take a refused one off. The refused candidates and why: {refusals}");
        }

        private static void ThenTheCandidateIsJudged(Dictionary<string, CandidateJudgement> judged, string candidate, string expected)
        {
            Assert.That(judged, Does.ContainKey(candidate),
                $"{candidate} was not found among the shared Models types or in the Access and Licensing namespaces of the compiled code, so whether it may move cannot be judged. It was renamed or deleted: tell the design owner.");

            var judgement = judged[candidate];
            var holds = expected switch
            {
                Stored => judgement.Stored,
                OnTheWire => judgement.OnTheWire,
                ReachedByTheKernel => judgement.ReachedBy.Count > 0,
                UsedByCodeThatStays => judgement.UsedBy.Count > 0,
                _ => judgement.Admitted,
            };

            Assert.That(holds, Is.True,
                $"{candidate} should be {expected}, but: {judgement.Describe(candidate)} A type the database context stores, or a controller sends, or a staying Models type reaches, or other staying code uses, cannot leave the shared kernel without breaking the stored model, the wire format or the rule that nothing left behind reaches into a module. The compiled code does not show a type sent through a variable typed object or built by a helper the controller does not call directly.");
        }

        private static HashSet<string> StoredTypesIn(ModuleDefinition module)
        {
            var types = module.Types.SelectMany(TypeAndItsNestedTypes).ToList();

            var inDbSets = types.SelectMany(type => type.Properties)
                .Select(property => property.PropertyType)
                .OfType<GenericInstanceType>()
                .Where(property => property.ElementType.FullName == "Microsoft.EntityFrameworkCore.DbSet`1")
                .Select(property => property.GenericArguments[0]);

            var configured = types.SelectMany(type => type.Methods)
                .Where(method => method.HasBody)
                .SelectMany(method => method.Body.Instructions)
                .Select(instruction => instruction.Operand)
                .OfType<GenericInstanceMethod>()
                .Where(method => method.DeclaringType.FullName == "Microsoft.EntityFrameworkCore.ModelBuilder" && method.Name == "Entity")
                .Select(method => method.GenericArguments[0]);

            return [.. inDbSets.Concat(configured).Select(type => OutermostOf(type).FullName)];
        }

        private static HashSet<string> TypesSentByControllersIn(ModuleDefinition module)
        {
            var controllers = module.Types.Where(IsAController).ToList();

            var inSignatures = controllers.SelectMany(controller => controller.Methods)
                .Where(method => method.IsPublic)
                .SelectMany(method => method.Parameters.Select(parameter => parameter.ParameterType).Prepend(method.ReturnType));

            var handedToResults = controllers.SelectMany(TypeAndItsNestedTypes)
                .SelectMany(type => type.Methods)
                .Where(method => method.HasBody)
                .SelectMany(ValuesHandedToAResult);

            var sent = new HashSet<string>(StringComparer.Ordinal);
            var definitions = module.Types.SelectMany(TypeAndItsNestedTypes).ToDictionary(type => type.FullName, StringComparer.Ordinal);
            var pending = new Stack<TypeReference>(inSignatures.Concat(handedToResults));

            while (pending.Count > 0)
            {
                foreach (var type in Unwrapped(pending.Pop()).Where(type => sent.Add(type.FullName)))
                {
                    if (definitions.TryGetValue(type.FullName, out var definition))
                    {
                        foreach (var property in definition.Properties.Where(property => property.GetMethod is { IsPublic: true, IsStatic: false }))
                        {
                            pending.Push(property.PropertyType);
                        }
                    }
                }
            }

            return sent;
        }

        // An object handed to Ok, BadRequest, StatusCode or an ObjectResult is the value that goes on the wire; the
        // instruction just before the call is the one that produced it.
        private static IEnumerable<TypeReference> ValuesHandedToAResult(MethodDefinition method)
        {
            var instructions = method.Body.Instructions;

            for (var index = 1; index < instructions.Count; index++)
            {
                if (instructions[index].Operand is MethodReference called
                    && IsIn(called.DeclaringType.Namespace, @"^Microsoft\.AspNetCore\.Mvc$")
                    && called.Parameters.Count > 0
                    && called.Parameters[^1].ParameterType.FullName == "System.Object"
                    && ProducedBy(method, instructions[index - 1]) is { } produced)
                {
                    yield return produced;
                }
            }
        }

        private static TypeReference? ProducedBy(MethodDefinition method, Instruction instruction)
            => instruction.OpCode.Code switch
            {
                Code.Box => (TypeReference)instruction.Operand,
                Code.Newobj => ((MethodReference)instruction.Operand).DeclaringType,
                Code.Call or Code.Callvirt => ((MethodReference)instruction.Operand).ReturnType,
                Code.Ldfld or Code.Ldsfld => ((FieldReference)instruction.Operand).FieldType,
                Code.Ldloc_0 => method.Body.Variables[0].VariableType,
                Code.Ldloc_1 => method.Body.Variables[1].VariableType,
                Code.Ldloc_2 => method.Body.Variables[2].VariableType,
                Code.Ldloc_3 => method.Body.Variables[3].VariableType,
                Code.Ldloc_S or Code.Ldloc => ((VariableDefinition)instruction.Operand).VariableType,
                _ => null,
            };

        private static bool IsAController(TypeDefinition type)
        {
            for (var baseType = type.BaseType; baseType is not null;)
            {
                if (baseType.FullName is "Microsoft.AspNetCore.Mvc.ControllerBase" or "Microsoft.AspNetCore.Mvc.Controller")
                {
                    return true;
                }

                baseType = (baseType is GenericInstanceType generic ? generic.ElementType : baseType) is TypeDefinition inThisModule
                    ? inThisModule.BaseType
                    : null;
            }

            return false;
        }

        // Splits who references each candidate into Models types (the kernel) and every other type that stays; the
        // candidate itself, the moving members and the types the compiler makes are left out.
        private static (Dictionary<string, List<string>> ReachedBy, Dictionary<string, List<string>> UsedBy) ReachOf(
            ModuleDefinition module, HashSet<string> candidateNames, HashSet<string> movingMembers)
        {
            var reachedBy = candidateNames.ToDictionary(name => name, _ => new List<string>(), StringComparer.Ordinal);
            var usedBy = candidateNames.ToDictionary(name => name, _ => new List<string>(), StringComparer.Ordinal);

            foreach (var dependent in IlDependencies.DependentsIn(module, "^", target => target is not GenericParameter && candidateNames.Contains(OutermostOf(target).FullName)))
            {
                var reacher = OuterTypeOf(dependent.TypeName);

                if (movingMembers.Contains(reacher) || ShortNameOf(reacher).StartsWith('<'))
                {
                    continue;
                }

                var inTheKernel = candidateNames.Contains(reacher) || IsIn(NamespaceOfName(reacher), KernelNamespaces);

                foreach (var target in dependent.Targets.Select(OuterTypeOf).Where(target => target != reacher).Distinct(StringComparer.Ordinal))
                {
                    (inTheKernel ? reachedBy : usedBy)[target].Add(reacher);
                }
            }

            return (reachedBy, usedBy);
        }

        private static IEnumerable<TypeDefinition> TypeAndItsNestedTypes(TypeDefinition type)
            => [type, .. type.NestedTypes.SelectMany(TypeAndItsNestedTypes)];

        private static IEnumerable<TypeReference> Unwrapped(TypeReference type)
            => type switch
            {
                GenericInstanceType generic => [OutermostOf(generic.ElementType), .. generic.GenericArguments.SelectMany(Unwrapped)],
                TypeSpecification wrapper => Unwrapped(wrapper.ElementType),
                GenericParameter => [],
                _ => [OutermostOf(type)],
            };

        private static TypeReference OutermostOf(TypeReference type)
            => type.DeclaringType is null ? type : OutermostOf(type.DeclaringType);

        private static string OuterTypeOf(string fullName) => fullName.Split(['+', '/'])[0];

        private static string NamespaceOfName(string fullName)
        {
            var outermost = OuterTypeOf(fullName);

            return outermost[..Math.Max(0, outermost.LastIndexOf('.'))];
        }

        private static bool IsIn(string typeNamespace, string pattern)
            => Regex.IsMatch(typeNamespace, pattern, RegexOptions.None, PatternTimeout);

        private static string ShortNameOf(string fullName)
        {
            var outermost = OuterTypeOf(fullName);
            var name = outermost[(outermost.LastIndexOf('.') + 1)..];
            var arity = name.IndexOf('`');

            return arity < 0 ? name : name[..arity];
        }

        private sealed record CandidateJudgement(bool Stored, bool OnTheWire, IReadOnlyList<string> ReachedBy, IReadOnlyList<string> UsedBy, bool Admitted)
        {
            public string Describe(string candidate)
            {
                var reasons = new List<string>();

                if (Stored)
                {
                    reasons.Add("the database context stores it");
                }

                if (OnTheWire)
                {
                    reasons.Add("a controller sends it");
                }

                if (ReachedBy.Count > 0)
                {
                    reasons.Add($"Models types that stay reach it ({string.Join(", ", ReachedBy)})");
                }

                if (UsedBy.Count > 0)
                {
                    reasons.Add($"code that stays outside the modules uses it ({string.Join(", ", UsedBy)})");
                }

                return reasons.Count == 0
                    ? $"{candidate} is admitted: nothing stores it, no controller sends it, and nothing that stays reaches it."
                    : $"{candidate} stays in Models: {string.Join("; ", reasons)}.";
            }
        }
    }
}

namespace Lighthouse.Backend.Modules.Access.MembershipFixtures
{
    internal static class AMadeUpAccessServiceInTheModule
    {
        public static int Granted(int[] roles) => roles.Count(role => role > 0);

        public static ANestedAccessStep Step(int id) => new(id);

        internal sealed record ANestedAccessStep(int Id);
    }
}

namespace Lighthouse.Backend.Modules.AccessFixtures.Membership
{
    internal static class ANamesakeOutsideAccess
    {
        public static int Granted(int roles) => roles;
    }
}

namespace Lighthouse.Backend.Modules.AccessFixtures.DomainReach
{
    internal sealed class AMadeUpRuleReachingTheApplication(Lighthouse.Backend.Modules.Access.Contracts.IRbacAdministrationService rbac)
    {
        public object Rbac() => rbac;
    }

    internal static class AMadeUpRuleReachingThePlatform
    {
        public static bool IsSignedIn(Microsoft.AspNetCore.Http.HttpContext context) => context.User.Identity?.IsAuthenticated == true;
    }

    internal static class AMadeUpRuleReachingOnlyTheKernel
    {
        public static string Describe(Lighthouse.Backend.Models.Authorization.RbacOperationResult result) => result.ToString() ?? string.Empty;
    }

    internal static class AMadeUpRuleReachingANamesakeOfADomainType
    {
        public static string Describe(int value) => Lighthouse.Backend.Modules.AccessFixtures.Application.AMadeUpRuleReachingOnlyTheKernel.Describe(value);
    }
}

namespace Lighthouse.Backend.Modules.AccessFixtures.Application
{
    internal static class AMadeUpRuleReachingOnlyTheKernel
    {
        public static string Describe(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

namespace Lighthouse.Backend.Models.AccessMembershipFixtures
{
    internal sealed record AMadeUpStoredRecord(int Id);

    internal sealed record AMadeUpConfiguredRecord(int Id);

    internal sealed record AMadeUpAnswer(int Id);

    internal sealed record AMadeUpAnswerPart(int Value);

    internal sealed record AMadeUpReachedValue(int Value);

    internal sealed record AMadeUpValueUsedByCodeThatStays(int Value);

    internal sealed record AMadeUpValueOnlyAccessUses(int Value);
}

namespace Lighthouse.Backend.Models.AccessMembershipKernelFixtures
{
    internal sealed record AMadeUpKernelRecord(global::Lighthouse.Backend.Models.AccessMembershipFixtures.AMadeUpReachedValue Reached);
}

namespace Lighthouse.Backend.Data.AccessMembershipFixtures
{
    using Lighthouse.Backend.Models.AccessMembershipFixtures;
    using Microsoft.EntityFrameworkCore;

    internal sealed class AMadeUpDatabaseContext(DbContextOptions<AMadeUpDatabaseContext> options) : DbContext(options)
    {
        public DbSet<AMadeUpStoredRecord> StoredRecords => Set<AMadeUpStoredRecord>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<AMadeUpConfiguredRecord>().HasKey(record => record.Id);
    }
}

namespace Lighthouse.Backend.API.AccessMembershipFixtures
{
    using Lighthouse.Backend.Models.AccessMembershipFixtures;
    using Microsoft.AspNetCore.Mvc;

    internal sealed record AMadeUpEnvelope(AMadeUpAnswerPart Part);

    internal abstract class AMadeUpAccessController : ControllerBase
    {
        public ActionResult<AMadeUpAnswer> Get() => NotFound();

        public IActionResult Wrap() => Ok(new AMadeUpEnvelope(new AMadeUpAnswerPart(1)));
    }
}

namespace Lighthouse.Backend.Services.Implementation.AccessMembershipFixtures
{
    using Lighthouse.Backend.Models.AccessMembershipFixtures;

    internal static class AMadeUpServiceThatStays
    {
        public static AMadeUpValueUsedByCodeThatStays Kept(int value) => new(value);
    }
}

namespace Lighthouse.Backend.Modules.Access.MembershipFixtures
{
    using Lighthouse.Backend.Models.AccessMembershipFixtures;

    internal static class AMadeUpAccessRuleThatTakesAModelsValue
    {
        public static AMadeUpValueOnlyAccessUses Granted(int value) => new(value);
    }
}
