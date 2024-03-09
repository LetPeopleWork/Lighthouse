using Lighthouse.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Lighthouse.Services.Interfaces;
using Lighthouse.Pages;

namespace Lighthouse.Tests.Pages
{
    public class PageModelBaseTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
        }

        [Test]
        public void OnGet_UnknownTeam_ReturnsNotFound()
        {
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns((Team)null);
            var subject = CreateSubject();

            var result = subject.OnGet(12);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void OnGet_IdIsNull_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var result = subject.OnGet(null);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void OnGet_ExistingTeamId_SetsTeam()
        {
            var team = new Team { Id = 12 };

            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(team);
            var subject = CreateSubject();

            var result = subject.OnGet(12);
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<PageResult>());
                Assert.That(subject.Entity, Is.EqualTo(team));
            });
        }

        private TeamPageModelBaseTestClass CreateSubject()
        {
            return new TeamPageModelBaseTestClass(teamRepositoryMock.Object);
        }
    }

    class TeamPageModelBaseTestClass : PageModelBase<Team>
    {
        public TeamPageModelBaseTestClass(IRepository<Team> teamRepository) : base(teamRepository)
        {
        }
    }
}
