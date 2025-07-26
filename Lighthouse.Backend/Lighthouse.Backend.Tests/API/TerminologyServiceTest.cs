using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class TerminologyServiceTest
    {
        private TerminologyService terminologyService;
        private Mock<IRepository<TerminologyEntry>> repositoryMock;

        private readonly List<TerminologyEntry> terminologyEntries = new List<TerminologyEntry>();

        [SetUp]
        public void SetUp()
        {
            terminologyEntries.Clear();

            repositoryMock = new Mock<IRepository<TerminologyEntry>>();
            repositoryMock.Setup(r => r.GetAll())
                .Returns(terminologyEntries.AsQueryable());


            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<TerminologyEntry, bool>>()))
                .Returns((Func<TerminologyEntry, bool> predicate) => terminologyEntries.SingleOrDefault(predicate));

            terminologyService = new TerminologyService(repositoryMock.Object);
        }

        [Test]
        public void GetAllTerminology_NoEntries_ReturnsEmptyDictionary()
        {
            var result = terminologyService.GetAll();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.Empty);
            }
        }

        [Test]
        public void GetAllTerminology_WithEntries_ReturnsDictionary()
        {
            var entry1 = new TerminologyEntry { Key = "feature", Description = "A large work item", DefaultValue = "Feature" };
            var entry2 = new TerminologyEntry { Key = "story", Description = "A small work item", DefaultValue = "User Story" };
            
            terminologyEntries.Add(entry1);
            terminologyEntries.Add(entry2);

            var result = terminologyService.GetAll();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count, Is.EqualTo(2));

                var featureEntry = result.Single(e => e.Key == "feature");
                var storyEntry = result.Single(e => e.Key == "story");

                Assert.That(featureEntry.DefaultValue, Is.EqualTo("Feature"));
                Assert.That(storyEntry.DefaultValue, Is.EqualTo("User Story"));
            }
        }

        [Test]
        public async Task UpdateTerminology_ValidEntry_UpdatesDatabase()
        {
            var originalEntry = new TerminologyEntry { Key = "feature", Description = "Original description", DefaultValue = "Feature" };
            terminologyEntries.Add(originalEntry);

            var updateData = new List<TerminologyEntry>
            {
                new TerminologyEntry { Key = "feature", DefaultValue = "Epic" }
            };

            await terminologyService.UpdateTerminology(updateData);

            var updatedEntry = terminologyService.GetAll().Single(x => x.Key == "feature");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(updatedEntry, Is.Not.Null);
                Assert.That(updatedEntry.DefaultValue, Is.EqualTo("Epic"));
            }
        }

        [Test]
        public async Task UpdateTerminology_NewEntry_IgnoresAsNotSupported()
        {
            var updateData = new List<TerminologyEntry>
            {
                new TerminologyEntry { Key = "newTerm", DefaultValue = "New Term Value" }
            };

            await terminologyService.UpdateTerminology(updateData);

            var newEntry = terminologyService.GetAll().SingleOrDefault(x => x.Key == "newTerm");
            Assert.That(newEntry, Is.Null, "New entries should not be added through UpdateTerminology");
        }

        [Test]
        public async Task UpdateTerminology_MultipleEntries_UpdatesAll()
        {
            var originalEntries = new List<TerminologyEntry>
            {
                new TerminologyEntry { Key = "feature", DefaultValue = "Feature" },
                new TerminologyEntry { Key = "story", DefaultValue = "User Story" },
                new TerminologyEntry { Key = "bug", DefaultValue = "Bug" }
            };
            terminologyEntries.AddRange(originalEntries);


            var updateData = new List<TerminologyEntry>
            {
                new TerminologyEntry { Key = "feature", DefaultValue = "Epic" },
                new TerminologyEntry { Key = "story", DefaultValue = "Task" },
                new TerminologyEntry { Key = "bug", DefaultValue = "Defect" }
            };

            await terminologyService.UpdateTerminology(updateData);

            var allEntries = terminologyService.GetAll().ToList();
            var featureEntry = allEntries.Single(x => x.Key == "feature");
            var storyEntry = allEntries.Single(x => x.Key == "story");
            var bugEntry = allEntries.Single(x => x.Key == "bug");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(featureEntry.DefaultValue, Is.EqualTo("Epic"));
                Assert.That(storyEntry.DefaultValue, Is.EqualTo("Task"));
                Assert.That(bugEntry.DefaultValue, Is.EqualTo("Defect"));
            }
        }
    }
}
