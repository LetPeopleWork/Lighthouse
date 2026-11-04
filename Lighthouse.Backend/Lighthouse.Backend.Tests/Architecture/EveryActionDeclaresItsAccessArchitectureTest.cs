using System.Reflection;
using System.Text.RegularExpressions;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Lighthouse.Backend.Tests.Architecture
{
    [TestFixture]
    public partial class EveryActionDeclaresItsAccessArchitectureTest
    {
        private const string AGoodReason = "Returns only what the caller may read.";

        [Test]
        public void AnActionWithoutADeclarationIsReported()
        {
            AssertReportedOnce<Undeclared>(nameof(Undeclared.Act));
        }

        [Test]
        public void AuthorizeAloneDoesNotCount()
        {
            AssertReportedOnce<AuthorizeOnly>(nameof(AuthorizeOnly.Act));
        }

        [Test]
        public void AGuardThatChecksNothingIsRejected()
        {
            var reported = AccessRule.Violations([typeof(GuardThatChecksNothing)]);

            Assert.That(reported, Is.Not.Empty);
            Assert.That(reported, Has.All.Contain($"{nameof(GuardThatChecksNothing)}.{nameof(GuardThatChecksNothing.Act)}"));
        }

        [Test]
        public void AllowAnonymousWithAGuardIsRejected()
        {
            AssertReportedOnce<AnonymousWithGuard>(nameof(AnonymousWithGuard.Act));
        }

        [Test]
        public void AllowAnonymousWithAMarkerIsRejected()
        {
            AssertReportedOnce<AnonymousWithMarker>(nameof(AnonymousWithMarker.Act));
        }

        [Test]
        public void BothMarkersTogetherAreRejected()
        {
            AssertReportedOnce<BothMarkers>(nameof(BothMarkers.Act));
        }

        [Test]
        public void AMarkerWithAGuardAtOneLevelIsRejected()
        {
            AssertReportedOnce<MarkerAndGuardOnTheAction>(nameof(MarkerAndGuardOnTheAction.Act));
        }

        [Test]
        public void AClassGuardUnderAnActionMarkerIsRejected()
        {
            AssertReportedOnce<ClassGuardUnderActionMarker>(nameof(ClassGuardUnderActionMarker.Act));
        }

        [Test]
        public void ABlankReasonIsRejected()
        {
            AssertReportedOnce<BlankReason>(nameof(BlankReason.Act));
        }

        [Test]
        public void ATooShortReasonIsRejected()
        {
            AssertReportedOnce<TooShortReason>(nameof(TooShortReason.Act));
        }

        [Test]
        public void AScopedGuardWithNoKeyIsRejected()
        {
            AssertReportedOnce<ScopedGuardWithNoKey>(nameof(ScopedGuardWithNoKey.Act));
        }

        [Test]
        public void AScopedGuardWithABlankKeyIsRejected()
        {
            AssertReportedOnce<ScopedGuardWithBlankKey>(nameof(ScopedGuardWithBlankKey.Act));
        }

        [Test]
        public void AScopedGuardWhoseKeyIsNoRouteParameterIsRejected()
        {
            AssertReportedOnce<ScopedGuardWithUnknownKey>(nameof(ScopedGuardWithUnknownKey.Act));
        }

        [Test]
        public void AnActionGuardUnderAClassMarkerIsAccepted()
        {
            Assert.That(AccessRule.Violations([typeof(ActionGuardUnderClassMarker)]), Is.Empty);
        }

        [Test]
        public void StackedClassAndActionGuardsAreAccepted()
        {
            Assert.That(AccessRule.Violations([typeof(StackedGuards)]), Is.Empty);
        }

        [Test]
        public void AGuardWhoseParameterComesFromTheClassRouteIsAccepted()
        {
            Assert.That(AccessRule.Violations([typeof(KeyFromClassRoute)]), Is.Empty);
        }

        [Test]
        public void EachKindOfDeclarationIsAcceptedAndNonActionsAreLeftAlone()
        {
            Assert.That(AccessRule.Violations([typeof(WellDeclared)]), Is.Empty);
        }

        [Test]
        public void TheRuleCanBeHandedAnyListOfControllers()
        {
            var reported = AccessRule.Violations([typeof(WellDeclared), typeof(Undeclared)]);

            Assert.That(reported, Has.Count.EqualTo(1));
        }

        private static void AssertReportedOnce<TController>(string action)
        {
            var reported = AccessRule.Violations([typeof(TController)]);

            Assert.That(reported, Has.Count.EqualTo(1), string.Join(Environment.NewLine, reported));
            Assert.That(reported[0], Does.StartWith($"{typeof(TController).Name}.{action}:"));
        }

        private static partial class AccessRule
        {
            private const int MinimumReasonLength = 25;

            [GeneratedRegex(@"\{\*{0,2}([A-Za-z_][A-Za-z0-9_]*)")]
            private static partial Regex RouteParameter();

            public static List<string> Violations(IEnumerable<Type> controllers)
            {
                return controllers
                    .SelectMany(controller => ActionsOf(controller).SelectMany(action => Problems(controller, action)))
                    .ToList();
            }

            private static IEnumerable<MethodInfo> ActionsOf(Type controller)
            {
                return controller
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(method => method.DeclaringType != typeof(object) && method.DeclaringType != typeof(ControllerBase))
                    .Where(method => !method.IsDefined(typeof(NonActionAttribute), inherit: true))
                    .Where(method => method.GetCustomAttributes<Attribute>(inherit: true).OfType<IActionHttpMethodProvider>().Any());
            }

            private static IEnumerable<string> Problems(Type controller, MethodInfo action)
            {
                var name = $"{controller.Name}.{action.Name}";
                return Describe(controller, action).Select(problem => $"{name}: {problem}");
            }

            private static IEnumerable<string> Describe(Type controller, MethodInfo action)
            {
                var classAttributes = controller.GetCustomAttributes<Attribute>(inherit: true).ToList();
                var actionAttributes = action.GetCustomAttributes<Attribute>(inherit: true).ToList();

                var classGuards = classAttributes.OfType<RbacGuardAttribute>().ToList();
                var actionGuards = actionAttributes.OfType<RbacGuardAttribute>().ToList();
                var classMarkers = classAttributes.Where(IsMarker).ToList();
                var actionMarkers = actionAttributes.Where(IsMarker).ToList();
                var allGuards = classGuards.Concat(actionGuards).ToList();
                var allMarkers = classMarkers.Concat(actionMarkers).ToList();
                var anonymous = classAttributes.Concat(actionAttributes).OfType<AllowAnonymousAttribute>().Any();
                var routeParameters = RouteParametersOf(classAttributes, actionAttributes);

                var checking = allGuards.Where(guard => guard.Check).ToList();
                var declared = checking.Count > 0 || allMarkers.Count > 0 || anonymous;

                if (!declared)
                {
                    yield return "declares no access; add an RbacGuard, a SignedInOnly or CheckedInHandler marker, or AllowAnonymous ([Authorize] alone does not count)";
                }

                if (allGuards.Any(guard => !guard.Check))
                {
                    yield return "an RbacGuard with Check = false checks nothing";
                }

                if (anonymous && (allGuards.Count > 0 || allMarkers.Count > 0))
                {
                    yield return "AllowAnonymous cannot be combined with a guard or a marker";
                }

                if (allMarkers.Select(marker => marker.GetType()).Distinct().Count() > 1)
                {
                    yield return "SignedInOnly and CheckedInHandler cannot both apply";
                }

                if ((classMarkers.Count > 0 && classGuards.Count > 0) || (actionMarkers.Count > 0 && actionGuards.Count > 0))
                {
                    yield return "a marker and an RbacGuard sit at the same level";
                }

                if (actionMarkers.Count > 0 && classGuards.Count > 0)
                {
                    yield return "a class-level RbacGuard still runs under an action-level marker";
                }

                foreach (var marker in allMarkers.Where(marker => ReasonOf(marker).Trim().Length < MinimumReasonLength))
                {
                    yield return $"the reason on {marker.GetType().Name} is blank or shorter than {MinimumReasonLength} characters";
                }

                foreach (var guard in checking.Where(RequiresScope))
                {
                    if (string.IsNullOrWhiteSpace(guard.ScopeIdRouteKey))
                    {
                        yield return $"the {guard.Requirement} guard names no ScopeIdRouteKey";
                    }
                    else if (!routeParameters.Contains(guard.ScopeIdRouteKey))
                    {
                        yield return $"ScopeIdRouteKey '{guard.ScopeIdRouteKey}' is no parameter of the class and action routes";
                    }
                }
            }

            private static bool IsMarker(Attribute attribute)
            {
                return attribute is SignedInOnlyAttribute or CheckedInHandlerAttribute;
            }

            private static string ReasonOf(Attribute marker)
            {
                return marker switch
                {
                    SignedInOnlyAttribute signedInOnly => signedInOnly.Reason,
                    CheckedInHandlerAttribute checkedInHandler => checkedInHandler.Reason,
                    _ => string.Empty,
                } ?? string.Empty;
            }

            private static bool RequiresScope(RbacGuardAttribute guard)
            {
                return guard.Requirement is RbacGuardRequirement.TeamRead
                    or RbacGuardRequirement.TeamContribute
                    or RbacGuardRequirement.TeamWrite
                    or RbacGuardRequirement.PortfolioRead
                    or RbacGuardRequirement.PortfolioWrite;
            }

            private static HashSet<string> RouteParametersOf(IEnumerable<Attribute> classAttributes, IEnumerable<Attribute> actionAttributes)
            {
                return classAttributes.Concat(actionAttributes)
                    .OfType<IRouteTemplateProvider>()
                    .Select(provider => provider.Template)
                    .OfType<string>()
                    .SelectMany(template => RouteParameter().Matches(template).Select(match => match.Groups[1].Value))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            }
        }

        private class Undeclared : ControllerBase
        {
            [HttpGet]
            public string Act() => string.Empty;
        }

        private class AuthorizeOnly : ControllerBase
        {
            [Authorize]
            [HttpGet]
            public string Act() => string.Empty;
        }

        private class GuardThatChecksNothing : ControllerBase
        {
            [RbacGuard(Check = false)]
            [HttpGet]
            public string Act() => string.Empty;
        }

        private class AnonymousWithGuard : ControllerBase
        {
            [AllowAnonymous]
            [RbacGuard]
            [HttpGet]
            public string Act() => string.Empty;
        }

        private class AnonymousWithMarker : ControllerBase
        {
            [AllowAnonymous]
            [SignedInOnly(AGoodReason)]
            [HttpGet]
            public string Act() => string.Empty;
        }

        private class BothMarkers : ControllerBase
        {
            [SignedInOnly(AGoodReason)]
            [CheckedInHandler(AGoodReason)]
            [HttpGet]
            public string Act() => string.Empty;
        }

        private class MarkerAndGuardOnTheAction : ControllerBase
        {
            [SignedInOnly(AGoodReason)]
            [RbacGuard]
            [HttpGet]
            public string Act() => string.Empty;
        }

        [RbacGuard]
        private class ClassGuardUnderActionMarker : ControllerBase
        {
            [CheckedInHandler(AGoodReason)]
            [HttpGet]
            public string Act() => string.Empty;
        }

        private class BlankReason : ControllerBase
        {
            [SignedInOnly("   ")]
            [HttpGet]
            public string Act() => string.Empty;
        }

        private class TooShortReason : ControllerBase
        {
            [CheckedInHandler("checked later")]
            [HttpGet]
            public string Act() => string.Empty;
        }

        private class ScopedGuardWithNoKey : ControllerBase
        {
            [RbacGuard(RbacGuardRequirement.TeamRead)]
            [HttpGet("{teamId}")]
            public string Act() => string.Empty;
        }

        private class ScopedGuardWithBlankKey : ControllerBase
        {
            [RbacGuard(RbacGuardRequirement.PortfolioWrite, ScopeIdRouteKey = " ")]
            [HttpGet("{portfolioId}")]
            public string Act() => string.Empty;
        }

        private class ScopedGuardWithUnknownKey : ControllerBase
        {
            [RbacGuard(RbacGuardRequirement.TeamWrite, ScopeIdRouteKey = "portfolioId")]
            [HttpGet("{teamId}")]
            public string Act() => string.Empty;
        }

        [SignedInOnly(AGoodReason)]
        private class ActionGuardUnderClassMarker : ControllerBase
        {
            [RbacGuard]
            [HttpGet]
            public string Act() => string.Empty;
        }

        [RbacGuard(RbacGuardRequirement.AnyScopedAdmin)]
        private class StackedGuards : ControllerBase
        {
            [RbacGuard]
            [HttpGet]
            public string Act() => string.Empty;
        }

        [Route("api/teams/{teamId:int}")]
        private class KeyFromClassRoute : ControllerBase
        {
            [RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]
            [HttpGet]
            public string Act() => string.Empty;
        }

        private class WellDeclared : ControllerBase
        {
            [RbacGuard(RbacGuardRequirement.PortfolioRead, ScopeIdRouteKey = "id")]
            [HttpGet("{id:int}")]
            public string Guarded() => string.Empty;

            [AllowAnonymous]
            [HttpGet("open")]
            public string Open() => string.Empty;

            [CheckedInHandler(AGoodReason)]
            [HttpPost]
            public string Marked() => string.Empty;

            [NonAction]
            [HttpGet("hidden")]
            public string Hidden() => string.Empty;

            public string NotRoutedAtAll() => string.Empty;
        }
    }
}
