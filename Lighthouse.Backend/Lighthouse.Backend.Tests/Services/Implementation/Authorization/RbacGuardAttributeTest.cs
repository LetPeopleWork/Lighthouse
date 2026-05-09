using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Security.Claims;

namespace Lighthouse.Backend.Tests.Services.Implementation.Authorization;

public class RbacGuardAttributeTest
{
    private Mock<IRbacAdministrationService> rbacAdministrationServiceMock = null!;
    private AuthorizationFilterContext context = null!;

    [SetUp]
    public void SetUp()
    {
        rbacAdministrationServiceMock = new Mock<IRbacAdministrationService>();

        var services = new ServiceCollection();
        services.AddSingleton(rbacAdministrationServiceMock.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "auth0|user")], "TestAuth")),
        };

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Test]
    public async Task OnAuthorizationAsync_CheckDisabled_AllowsExecution()
    {
        var attribute = new RbacGuardAttribute { Check = false };

        await attribute.OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public async Task OnAuthorizationAsync_ServiceUnavailable_AllowsExecution()
    {
        var services = new ServiceCollection();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "auth0|user")], "TestAuth")),
        };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var localContext = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());

        var attribute = new RbacGuardAttribute(RbacGuardRequirement.SystemAdmin);

        await attribute.OnAuthorizationAsync(localContext);

        Assert.That(localContext.Result, Is.Null);
    }

    [Test]
    public async Task OnAuthorizationAsync_SystemAdminRequirementDenied_ReturnsForbid()
    {
        rbacAdministrationServiceMock
            .Setup(x => x.CanSatisfyRequirementAsync(
                It.IsAny<ClaimsPrincipal>(),
                RbacGuardRequirement.SystemAdmin,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var attribute = new RbacGuardAttribute(RbacGuardRequirement.SystemAdmin);

        await attribute.OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.InstanceOf<ForbidResult>());
    }

    [Test]
    public async Task OnAuthorizationAsync_TeamReadRequirementDenied_ReturnsNotFound()
    {
        context.RouteData.Values["teamId"] = "42";
        rbacAdministrationServiceMock
            .Setup(x => x.CanSatisfyRequirementAsync(
                It.IsAny<ClaimsPrincipal>(),
                RbacGuardRequirement.TeamRead,
                42,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var attribute = new RbacGuardAttribute(RbacGuardRequirement.TeamRead)
        {
            ScopeIdRouteKey = "teamId",
        };

        await attribute.OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task OnAuthorizationAsync_PortfolioWriteRequirementAllowed_AllowsExecution()
    {
        context.RouteData.Values["portfolioId"] = 7;
        rbacAdministrationServiceMock
            .Setup(x => x.CanSatisfyRequirementAsync(
                It.IsAny<ClaimsPrincipal>(),
                RbacGuardRequirement.PortfolioWrite,
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var attribute = new RbacGuardAttribute(RbacGuardRequirement.PortfolioWrite)
        {
            ScopeIdRouteKey = "portfolioId",
        };

        await attribute.OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public async Task OnAuthorizationAsync_ScopedRequirementWithoutRouteValue_ReturnsInternalServerError()
    {
        var attribute = new RbacGuardAttribute(RbacGuardRequirement.TeamWrite)
        {
            ScopeIdRouteKey = "teamId",
        };

        await attribute.OnAuthorizationAsync(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Result, Is.InstanceOf<StatusCodeResult>());
            Assert.That(((StatusCodeResult)context.Result!).StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
        }
    }
}