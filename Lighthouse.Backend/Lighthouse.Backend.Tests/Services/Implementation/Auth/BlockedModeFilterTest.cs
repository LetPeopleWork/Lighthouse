using Lighthouse.Backend.Models.Auth;
using Lighthouse.Backend.Services.Implementation.Auth;
using Lighthouse.Backend.Services.Interfaces.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Auth
{
    [TestFixture]
    public class BlockedModeFilterTest
    {
        private Mock<IAuthModeResolver> authModeResolverMock;
        private BlockedModeFilter subject;

        [SetUp]
        public void SetUp()
        {
            authModeResolverMock = new Mock<IAuthModeResolver>();
            var loggerMock = new Mock<ILogger<BlockedModeFilter>>();
            subject = new BlockedModeFilter(authModeResolverMock.Object, loggerMock.Object);
        }

        [Test]
        public async Task OnActionExecutionAsync_AuthDisabled_AllowsRequest()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Disabled });
            var context = CreateContext("/api/teams");

            var nextCalled = false;
            await subject.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(context, [], null!));
            });

            Assert.That(nextCalled, Is.True);
            Assert.That(context.Result, Is.Null);
        }

        [Test]
        public async Task OnActionExecutionAsync_AuthEnabled_AllowsRequest()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Enabled });
            var context = CreateContext("/api/teams");

            var nextCalled = false;
            await subject.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(context, [], null!));
            });

            Assert.That(nextCalled, Is.True);
            Assert.That(context.Result, Is.Null);
        }

        [Test]
        public async Task OnActionExecutionAsync_Blocked_AllowsAuthEndpoints()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Blocked });
            var context = CreateContext("/api/auth/mode");

            var nextCalled = false;
            await subject.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(context, [], null!));
            });

            Assert.That(nextCalled, Is.True);
            Assert.That(context.Result, Is.Null);
        }

        [Test]
        public async Task OnActionExecutionAsync_Blocked_AllowsLicenseEndpoints()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Blocked });
            var context = CreateContext("/api/license/import");

            var nextCalled = false;
            await subject.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(context, [], null!));
            });

            Assert.That(nextCalled, Is.True);
            Assert.That(context.Result, Is.Null);
        }

        [Test]
        public async Task OnActionExecutionAsync_Blocked_AllowsVersionEndpoints()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Blocked });
            var context = CreateContext("/api/version/current");

            var nextCalled = false;
            await subject.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(context, [], null!));
            });

            Assert.That(nextCalled, Is.True);
            Assert.That(context.Result, Is.Null);
        }

        [Test]
        public async Task OnActionExecutionAsync_Blocked_DeniesTeamEndpoints()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Blocked });
            var context = CreateContext("/api/teams");

            await subject.OnActionExecutionAsync(context, () =>
            {
                Assert.Fail("Next should not be called for blocked non-allowlisted endpoints.");
                return Task.FromResult(new ActionExecutedContext(context, [], null!));
            });

            Assert.That(context.Result, Is.InstanceOf<ObjectResult>());
            var result = (ObjectResult)context.Result!;
            Assert.That(result.StatusCode, Is.EqualTo(403));
        }

        [Test]
        public async Task OnActionExecutionAsync_Blocked_DeniesPortfolioEndpoints()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Blocked });
            var context = CreateContext("/api/portfolios/1");

            await subject.OnActionExecutionAsync(context, () =>
            {
                Assert.Fail("Next should not be called for blocked non-allowlisted endpoints.");
                return Task.FromResult(new ActionExecutedContext(context, [], null!));
            });

            Assert.That(context.Result, Is.InstanceOf<ObjectResult>());
            var result = (ObjectResult)context.Result!;
            Assert.That(result.StatusCode, Is.EqualTo(403));
        }

        [Test]
        public async Task OnActionExecutionAsync_Blocked_DeniesSettingsEndpoints()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Blocked });
            var context = CreateContext("/api/appsettings/FeatureRefresh");

            await subject.OnActionExecutionAsync(context, () =>
            {
                Assert.Fail("Next should not be called for blocked non-allowlisted endpoints.");
                return Task.FromResult(new ActionExecutedContext(context, [], null!));
            });

            Assert.That(context.Result, Is.InstanceOf<ObjectResult>());
            var result = (ObjectResult)context.Result!;
            Assert.That(result.StatusCode, Is.EqualTo(403));
        }

        [Test]
        public async Task OnActionExecutionAsync_Misconfigured_AllowsRequest()
        {
            authModeResolverMock.Setup(r => r.Resolve()).Returns(new RuntimeAuthStatus { Mode = AuthMode.Misconfigured });
            var context = CreateContext("/api/teams");

            var nextCalled = false;
            await subject.OnActionExecutionAsync(context, () =>
            {
                nextCalled = true;
                return Task.FromResult(new ActionExecutedContext(context, [], null!));
            });

            Assert.That(nextCalled, Is.True);
            Assert.That(context.Result, Is.Null);
        }

        private static ActionExecutingContext CreateContext(string path)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Path = new PathString(path);

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

            return new ActionExecutingContext(
                actionContext,
                [],
                new Dictionary<string, object?>(),
                null!);
        }
    }
}
