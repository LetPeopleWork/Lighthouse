using CMFTAspNet.Data;
using CMFTAspNet.Models;
using CMFTAspNet.Services.Implementation.Repositories;
using CMFTAspNet.Tests.TestHelpers;

namespace CMFTAspNet.Tests.Services.Implementation.Repositories
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
            var item = new Feature { Name = "Name" };

            subject.Add(item);
            await subject.Save();

            var items = subject.GetAll();

            Assert.That(items, Contains.Item(item));
        }

        [Test]
        public async Task GetById_ExistingId_RetunrsCorrectEntity()
        {
            var subject = CreateSubject();

            var item = new Feature { Name = "Name" };

            subject.Add(item);
            await subject.Save();

            var foundEntity = subject.GetById(item.Id);

            Assert.That(foundEntity, Is.EqualTo(item));
        }

        [Test]
        public async Task GivenExistingEntity_Remove_RemovesFromList()
        {
            var subject = CreateSubject();
            var item = new Feature { Name = "Name" };

            subject.Add(item);
            await subject.Save();

            // Act
            subject.Remove(item.Id);
            await subject.Save();

            var items = subject.GetAll();
            CollectionAssert.DoesNotContain(items, item);
        }

        [Test]
        public async Task Update_GivenExisting_PersistsChange()
        {
            var subject = CreateSubject();
            var item = new Feature { Name = "Name", Order = 12 };

            subject.Add(item);
            await subject.Save();

            // Act
            item.Order = 42;
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
            var item = new Feature { Name = "Name", Id = 57 };

            subject.Add(item);
            await subject.Save();

            var actual = subject.Exists(57);

            Assert.That(actual, Is.True);
        }

        [Test]
        public async Task Exists_DoesNotExist_ReturnsFalseAsync()
        {

            var subject = CreateSubject();
            var item = new Feature { Name = "Name", Id = 12 };

            subject.Add(item);
            await subject.Save();

            var actual = subject.Exists(37);

            Assert.That(actual, Is.False);
        }

        private RepositoryBaseTestClass CreateSubject()
        {
            return new RepositoryBaseTestClass(DatabaseContext);
        }
    }

    class RepositoryBaseTestClass : RepositoryBase<Feature>
    {
        public RepositoryBaseTestClass(CMFTAspNetContext context) : base(context, (context) => context.Features)
        {
        }
    }
}
