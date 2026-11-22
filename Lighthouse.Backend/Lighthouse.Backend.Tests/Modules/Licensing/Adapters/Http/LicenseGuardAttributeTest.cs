using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.Licensing.Adapters.Http;
using Lighthouse.Backend.Modules.Licensing.Application;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.Licensing.Adapters.Http;

public class LicenseGuardAttributeTest
{
    private Mock<ILicenseService> licenseServiceMock = null!;
    private Mock<IRepository<Team>> teamRepositoryMock = null!;
    private Mock<IRepository<Portfolio>> projectRepositoryMock = null!;
    private AuthorizationFilterContext context = null!;

    [SetUp]
    public void Setup()
    {
        licenseServiceMock = new Mock<ILicenseService>();
        teamRepositoryMock = new Mock<IRepository<Team>>();
        projectRepositoryMock = new Mock<IRepository<Portfolio>>();

        var services = new ServiceCollection();
        services.AddSingleton(licenseServiceMock.Object);
        services.AddSingleton(teamRepositoryMock.Object);
        services.AddSingleton(projectRepositoryMock.Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };

        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        context = new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    [Test]
    public async Task LicenseGuard_RequirePremium_UserIsPremium_AllowsExecution()
    {
        licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

        var attribute = new LicenseGuardAttribute { RequirePremium = true };

        await attribute.OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public async Task LicenseGuard_RequirePremium_UserIsNotPremium_ReturnsForbid()
    {
        licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);

        var attribute = new LicenseGuardAttribute { RequirePremium = true };

        await attribute.OnAuthorizationAsync(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Result, Is.InstanceOf<ObjectResult>());
            Assert.That(((ObjectResult)context.Result).Value?.ToString(), Does.Contain("Access Denied: Premium Features Required"));
            Assert.That(((ObjectResult)context.Result).StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        }
    }

    [Test]
    public async Task LicenseGuard_TeamLimitExceeded_ReturnsForbid()
    {
        licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
        teamRepositoryMock.Setup(r => r.GetAll()).Returns(GetTeams(5)); // more than allowed

        var attribute = new LicenseGuardAttribute { CheckTeamConstraint = true };

        await attribute.OnAuthorizationAsync(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Result, Is.InstanceOf<ObjectResult>());
            Assert.That(((ObjectResult)context.Result).Value?.ToString(), Does.Contain("Free users can only use up to 3 Team"));
            Assert.That(((ObjectResult)context.Result).StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        }
    }

    [Test]
    public async Task LicenseGuard_TeamLimitNotExceeded_AllowsExecution()
    {
        licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
        teamRepositoryMock.Setup(r => r.GetAll()).Returns(GetTeams(2));

        var attribute = new LicenseGuardAttribute { CheckTeamConstraint = true };

        await attribute.OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public async Task LicenseGuard_ProjectLimitExceeded_ReturnsBadRequest()
    {
        licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
        projectRepositoryMock.Setup(r => r.GetAll()).Returns(GetProjects(2));

        var attribute = new LicenseGuardAttribute { CheckPortfolioConstraint = true };

        await attribute.OnAuthorizationAsync(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Result, Is.InstanceOf<ObjectResult>());
            Assert.That(((ObjectResult)context.Result).Value?.ToString(), Does.Contain("Free users can only use up to 1 Portfolio"));
            Assert.That(((ObjectResult)context.Result).StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        }
    }

    [Test]
    public async Task LicenseGuard_WithoutLimitFlags_AllowsExecutionWhateverTheCounts()
    {
        licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
        teamRepositoryMock.Setup(r => r.GetAll()).Returns(GetTeams(50));
        projectRepositoryMock.Setup(r => r.GetAll()).Returns(GetProjects(50));

        await new LicenseGuardAttribute().OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public async Task LicenseGuard_TeamCountExactlyAtTheLimit_AllowsExecution()
    {
        licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
        teamRepositoryMock.Setup(r => r.GetAll()).Returns(GetTeams(3));

        await new LicenseGuardAttribute { CheckTeamConstraint = true }.OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public async Task LicenseGuard_PortfolioCountExactlyAtTheLimit_AllowsExecution()
    {
        licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
        projectRepositoryMock.Setup(r => r.GetAll()).Returns(GetProjects(1));

        await new LicenseGuardAttribute { CheckPortfolioConstraint = true }.OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public async Task LicenseGuard_RepositoryNotRegistered_ReturnsServerError()
    {
        licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
        var services = new ServiceCollection();
        services.AddSingleton(licenseServiceMock.Object);
        context.HttpContext.RequestServices = services.BuildServiceProvider();

        await new LicenseGuardAttribute { CheckTeamConstraint = true }.OnAuthorizationAsync(context);

        Assert.That(((StatusCodeResult)context.Result!).StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
    }

    private IQueryable<Team> GetTeams(int count) =>
        Enumerable.Range(1, count).Select(i => new Team { Id = i, Name = $"Team {i}" }).AsQueryable();

    private IQueryable<Portfolio> GetProjects(int count) =>
        Enumerable.Range(1, count).Select(i => new Portfolio { Id = i, Name = $"Project {i}" }).AsQueryable();
}
