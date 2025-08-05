using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Licensing;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Licensing;

public class LicenseGuardAttributeTest
{
    private Mock<ILicenseService> licenseServiceMock = null!;
    private Mock<IRepository<Team>> teamRepositoryMock = null!;
    private Mock<IRepository<Project>> projectRepositoryMock = null!;
    private AuthorizationFilterContext context = null!;

    [SetUp]
    public void Setup()
    {
        licenseServiceMock = new Mock<ILicenseService>();
        teamRepositoryMock = new Mock<IRepository<Team>>();
        projectRepositoryMock = new Mock<IRepository<Project>>();

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
            Assert.That(((ObjectResult)context.Result).Value?.ToString(), Does.Contain("Access denied: premium features required"));
            Assert.That(((ObjectResult)context.Result).StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        }
    }

    [Test]
    public async Task LicenseGuard_TeamLimitExceeded_ReturnsForbid()
    {
        licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
        teamRepositoryMock.Setup(r => r.GetAll()).Returns(GetTeams(5)); // more than allowed

        var attribute = new LicenseGuardAttribute { MaxAllowedTeams = 3 };

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

        var attribute = new LicenseGuardAttribute { MaxAllowedTeams = 3 };

        await attribute.OnAuthorizationAsync(context);

        Assert.That(context.Result, Is.Null);
    }

    [Test]
    public async Task LicenseGuard_ProjectLimitExceeded_ReturnsBadRequest()
    {
        licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
        projectRepositoryMock.Setup(r => r.GetAll()).Returns(GetProjects(4));

        var attribute = new LicenseGuardAttribute { MaxAllowedProjects = 2 };

        await attribute.OnAuthorizationAsync(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Result, Is.InstanceOf<ObjectResult>());
            Assert.That(((ObjectResult)context.Result).Value?.ToString(), Does.Contain("Free users can only use up to 2 Project"));
            Assert.That(((ObjectResult)context.Result).StatusCode, Is.EqualTo(StatusCodes.Status403Forbidden));
        }
    }

    private IQueryable<Team> GetTeams(int count) =>
        Enumerable.Range(1, count).Select(i => new Team { Id = i, Name = $"Team {i}" }).AsQueryable();

    private IQueryable<Project> GetProjects(int count) =>
        Enumerable.Range(1, count).Select(i => new Project { Id = i, Name = $"Project {i}" }).AsQueryable();
}
