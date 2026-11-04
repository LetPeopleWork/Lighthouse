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
        public void APublicMethodWithNoVerbUnderAClassRouteIsReported()
        {
            AssertReportedOnce<PublicMethodUnderAClassRoute>(nameof(PublicMethodUnderAClassRoute.AnswersAnyVerb));
        }

        [TestCase(typeof(SignedInOnlyAttribute))]
        [TestCase(typeof(CheckedInHandlerAttribute))]
        public void AMarkerCanSitOnlyOnAnAction(Type marker)
        {
            var usage = marker.GetCustomAttribute<AttributeUsageAttribute>();

            Assert.That(usage?.ValidOn, Is.EqualTo(AttributeTargets.Method));
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
        public void AMarkerWithAGuardOnTheActionIsRejected()
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

        [Test]
        public void EveryProductionActionDeclaresItsAccess()
        {
            var controllers = typeof(RbacGuardAttribute).Assembly
                .GetTypes()
                .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract)
                .ToList();
            var actionCount = controllers.Sum(controller => AccessRule.ActionsOf(controller).Count());

            var reported = AccessRule.Violations(controllers);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(controllers, Has.Count.GreaterThanOrEqualTo(40), "the scan found too few controllers");
                Assert.That(actionCount, Is.GreaterThanOrEqualTo(200), "the scan found too few actions");
                Assert.That(reported, Is.Empty, string.Join(Environment.NewLine, reported));
            }
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

            private static readonly Type[] FrameworkTypes = [typeof(object), typeof(ControllerBase), typeof(Controller)];

            [GeneratedRegex(@"\{\*{0,2}([A-Za-z_][A-Za-z0-9_]*)")]
            private static partial Regex RouteParameter();

            public static List<string> Violations(IEnumerable<Type> controllers)
            {
                return controllers
                    .SelectMany(controller => ActionsOf(controller).SelectMany(action => Problems(controller, action)))
                    .ToList();
            }

            public static IEnumerable<MethodInfo> ActionsOf(Type controller)
            {
                return controller
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(method => !method.IsDefined(typeof(NonActionAttribute), inherit: true))
                    .Where(method => !method.IsSpecialName)
                    .Where(method => !FrameworkTypes.Contains(method.GetBaseDefinition().DeclaringType));
            }

            private static IEnumerable<string> Problems(Type controller, MethodInfo action)
            {
                var onClass = controller.GetCustomAttributes<Attribute>(inherit: true).ToList();
                var onAction = action.GetCustomAttributes<Attribute>(inherit: true).ToList();
                var onEither = onClass.Concat(onAction).ToList();

                return DeclarationProblems(onAction, onEither)
                    .Concat(ReasonProblems(onAction))
                    .Concat(ScopeKeyProblems(onEither))
                    .Select(problem => $"{controller.Name}.{action.Name}: {problem}");
            }

            private static IEnumerable<string> DeclarationProblems(List<Attribute> onAction, List<Attribute> onEither)
            {
                var guards = onEither.OfType<RbacGuardAttribute>().ToList();
                var markers = onAction.OfType<AccessMarkerAttribute>().ToList();
                var anonymous = onEither.OfType<AllowAnonymousAttribute>().Any();

                if (!guards.Any(guard => guard.Check) && markers.Count == 0 && !anonymous)
                {
                    yield return "declares no access; add an RbacGuard, a SignedInOnly or CheckedInHandler marker, or AllowAnonymous ([Authorize] alone does not count)";
                }

                if (guards.Any(guard => !guard.Check))
                {
                    yield return "an RbacGuard with Check = false checks nothing";
                }

                if (anonymous && (guards.Count > 0 || markers.Count > 0))
                {
                    yield return "AllowAnonymous cannot be combined with a guard or a marker";
                }

                if (markers.Select(marker => marker.GetType()).Distinct().Count() > 1)
                {
                    yield return "SignedInOnly and CheckedInHandler cannot both apply";
                }

                if (markers.Count > 0 && guards.Count > 0)
                {
                    yield return "an RbacGuard, on the action or on its class, still runs under a marker";
                }
            }

            private static IEnumerable<string> ReasonProblems(List<Attribute> onAction)
            {
                return onAction
                    .OfType<AccessMarkerAttribute>()
                    .Where(marker => marker.Reason.Trim().Length < MinimumReasonLength)
                    .Select(marker => $"the reason on {marker.GetType().Name} is blank or shorter than {MinimumReasonLength} characters");
            }

            private static IEnumerable<string> ScopeKeyProblems(List<Attribute> onEither)
            {
                var routeParameters = RouteParametersOf(onEither);

                foreach (var guard in onEither.OfType<RbacGuardAttribute>().Where(guard => guard.Check && guard.RequiresScope()))
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

            private static HashSet<string> RouteParametersOf(List<Attribute> onEither)
            {
                return onEither
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

            public string AnAccessor => string.Empty;

            public override string ToString() => string.Empty;
        }

        [Route("api/things")]
        private class PublicMethodUnderAClassRoute : ControllerBase
        {
            public string AnswersAnyVerb() => string.Empty;
        }
    }
}
