﻿using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class RepositoryBaseTest : IntegrationTestBase
    {
        public RepositoryBaseTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [Test]
        public void Get_NoItems_ReturnsEmptyList()
        {
            var subject = CreateSubject();

            var items = subject.GetAll();

            Assert.That(items, Is.Empty);
        }

        [Test]
        public async Task Add_StoresAsync()
        {
            var subject = CreateSubject();
            Feature item = CreateFeature();

            subject.Add(item);
            await subject.Save();

            var items = subject.GetAll();

            Assert.That(items, Contains.Item(item));
        }

        [Test]
        public async Task GetById_ExistingId_RetunrsCorrectEntity()
        {
            var subject = CreateSubject();

            var item = CreateFeature();

            subject.Add(item);
            await subject.Save();

            var foundEntity = subject.GetById(item.Id);

            Assert.That(foundEntity, Is.EqualTo(item));
        }

        [Test]
        public async Task GetByPredicate_ExistingItem_RetunrsCorrectEntity()
        {
            var subject = CreateSubject();

            var item = CreateFeature();
            item.ReferenceId = "12";

            subject.Add(item);
            await subject.Save();

            var foundEntity = subject.GetByPredicate(f => f.ReferenceId == "12");

            Assert.That(foundEntity, Is.EqualTo(item));
        }

        [Test]
        public async Task GetByPredicate_NotExistingItem_RetunrsNull()
        {
            var subject = CreateSubject();

            var item = CreateFeature();
            item.ReferenceId = "12";

            subject.Add(item);
            await subject.Save();

            var foundEntity = subject.GetByPredicate(f => f.ReferenceId == "42");

            Assert.That(foundEntity, Is.Null);
        }

        [Test]
        public async Task GivenExistingEntity_Remove_RemovesFromList()
        {
            var subject = CreateSubject();
            var item = CreateFeature();

            subject.Add(item);
            await subject.Save();

            // Act
            subject.Remove(item.Id);
            await subject.Save();

            var items = subject.GetAll();
            Assert.That(items, Does.Not.Contain(item));
        }

        [Test]
        public async Task Update_GivenExisting_PersistsChange()
        {
            var subject = CreateSubject();
            var item = CreateFeature();

            subject.Add(item);
            await subject.Save();

            // Act
            item.Order = "42";
            subject.Update(item);
            await subject.Save();

            // Assert
            var projects = subject.GetAll();
            Assert.That(projects.Single().Order, Is.EqualTo(item.Order));
        }

        [Test]
        public async Task Exists_DoesExist_ReturnsTrueAsync()
        {
            var subject = CreateSubject();
            var item = CreateFeature();

            subject.Add(item);
            await subject.Save();

            var actual = subject.Exists(item.Id);

            Assert.That(actual, Is.True);
        }

        [Test]
        public async Task Exists_DoesNotExist_ReturnsFalseAsync()
        {

            var subject = CreateSubject();
            var item = CreateFeature();

            subject.Add(item);
            await subject.Save();

            var actual = subject.Exists(37);

            Assert.That(actual, Is.False);
        }

        [Test]
        public async Task Exists_UsingPredicate_DoesExist_ReturnsTrueAsync()
        {
            var subject = CreateSubject();
            var item = CreateFeature();
            item.ReferenceId = "32";

            subject.Add(item);
            await subject.Save();

            var actual = subject.Exists(f => f.ReferenceId == "32");

            Assert.That(actual, Is.True);
        }

        [Test]
        public async Task Exists_UsingPredicate_DoesNotExist_ReturnsFalseAsync()
        {

            var subject = CreateSubject();
            var item = CreateFeature();
            item.ReferenceId = "32";

            subject.Add(item);
            await subject.Save();

            var actual = subject.Exists(f => f.ReferenceId == "42");

            Assert.That(actual, Is.False);
        }

        private RepositoryBaseTestClass CreateSubject()
        {
            return new RepositoryBaseTestClass(DatabaseContext);
        }

        private Feature CreateFeature()
        {
            var feature = new Feature { Name = "Name", Order = "12" };

            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var project = new Project { Name = "Project" };
            project.WorkTrackingSystemConnection = workTrackingSystemConnection;
            feature.Projects.Add(project);

            return feature;
        }
    }

    class RepositoryBaseTestClass : RepositoryBase<Feature>
    {
        public RepositoryBaseTestClass(Data.LighthouseAppContext context) : base(context, (context) => context.Features, Mock.Of<ILogger<RepositoryBaseTestClass>>())
        {
        }
    }
}
