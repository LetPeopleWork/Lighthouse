﻿using Lighthouse.Models;
using Lighthouse.Pages.Teams;
using Lighthouse.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

namespace Lighthouse.Tests.Pages.Teams
{
    public class CreateModelTest
    {
        private Mock<IRepository<Team>> repositoryMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<Team>>();
        }

        [Test]
        public void OnGet_CreateNew_ReturnsPageAsync()
        {
            var subject = CreateSubject();

            var result = subject.OnGet(null);

            Assert.That(result, Is.InstanceOf<PageResult>());
        }

        [Test]
        public void OnGet_EditExisting_IdExists_ReturnsPageAsync()
        {
            var subject = CreateSubject();
            var team = new Team { Name = "Team" };

            repositoryMock.Setup(x => x.GetById(12)).Returns(team);

            var result = subject.OnGet(12);
            
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<PageResult>());
                Assert.That(subject.Team, Is.EqualTo(team));
            });
        }

        [Test]
        public void OnGet_EditExisting_IdDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            repositoryMock.Setup(x => x.GetById(12)).Returns((Team)null);

            var result = subject.OnGet(12);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task OnPost_CreateNew_ModelNotValid_ReturnsPage()
        {
            var subject = CreateSubject();
            subject.ModelState.AddModelError("Error", "Error");

            var result = await subject.OnPostAsync();

            Assert.That(result, Is.InstanceOf<PageResult>());

            repositoryMock.Verify(x => x.Add(It.IsAny<Team>()), Times.Never);
            repositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task OnPost_CreateNew_ModelValid_AddsTeamAndSaves()
        {
            var subject = CreateSubject();
            subject.Team = new Team { Name = "Test" };

            var result = await subject.OnPostAsync();

            repositoryMock.Verify(x => x.Add(subject.Team));
            repositoryMock.Verify(x => x.Save());
        }

        [Test]
        public async Task OnPost_EditExisting_ModelValid_UpdatesTeamAndSaves()
        {
            var subject = CreateSubject();
            subject.Team = new Team { Name = "Test", Id = 12 };

            var result = await subject.OnPostAsync();
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.InstanceOf<RedirectToPageResult>());

                Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
                var pageResult = (RedirectToPageResult)result;

                var routeValue = pageResult.RouteValues?.Single() ?? throw new InvalidOperationException("RoutedValues not set");
                Assert.That(routeValue.Key, Is.EqualTo("id"));
                Assert.That(routeValue.Value, Is.EqualTo(12));

                Assert.That(pageResult.PageName, Is.EqualTo("./Details"));
                Assert.That(pageResult.RouteValues?.Count, Is.EqualTo(1));
            });

            repositoryMock.Verify(x => x.Update(subject.Team));
            repositoryMock.Verify(x => x.Save());
        }

        private CreateModel CreateSubject()
        {
            return new CreateModel(repositoryMock.Object);
        }
    }
}
