using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Moq;

namespace Lighthouse.Backend.Tests.Services
{
    [TestFixture]
    public class BlackoutPeriodServiceTest
    {
        private BlackoutPeriodService subject;
        private Mock<IRepository<BlackoutPeriod>> repositoryMock;
        private readonly List<BlackoutPeriod> blackoutPeriods = [];

        [SetUp]
        public void SetUp()
        {
            blackoutPeriods.Clear();

            repositoryMock = new Mock<IRepository<BlackoutPeriod>>();
            repositoryMock.Setup(r => r.GetAll())
                .Returns(blackoutPeriods.AsQueryable());
            repositoryMock.Setup(r => r.GetById(It.IsAny<int>()))
                .Returns((int id) => blackoutPeriods.SingleOrDefault(bp => bp.Id == id));

            subject = new BlackoutPeriodService(repositoryMock.Object);
        }

        [Test]
        public void GetAll_NoPeriods_ReturnsEmpty()
        {
            var result = subject.GetAll();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetAll_WithPeriods_ReturnsOrderedByStartThenEnd()
        {
            blackoutPeriods.Add(new BlackoutPeriod { Id = 1, Start = new DateOnly(2026, 6, 1), End = new DateOnly(2026, 6, 5) });
            blackoutPeriods.Add(new BlackoutPeriod { Id = 2, Start = new DateOnly(2026, 3, 1), End = new DateOnly(2026, 3, 3) });
            blackoutPeriods.Add(new BlackoutPeriod { Id = 3, Start = new DateOnly(2026, 3, 1), End = new DateOnly(2026, 3, 2) });

            var result = subject.GetAll().ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Has.Count.EqualTo(3));
                Assert.That(result[0].Id, Is.EqualTo(3));
                Assert.That(result[1].Id, Is.EqualTo(2));
                Assert.That(result[2].Id, Is.EqualTo(1));
            }
        }

        [Test]
        public void GetById_ExistingId_ReturnsPeriod()
        {
            blackoutPeriods.Add(new BlackoutPeriod { Id = 42, Start = new DateOnly(2026, 1, 1), End = new DateOnly(2026, 1, 5) });

            var result = subject.GetById(42);

            Assert.That(result, Is.Not.Null);
            Assert.That(result!.Id, Is.EqualTo(42));
        }

        [Test]
        public void GetById_NonExistingId_ReturnsNull()
        {
            var result = subject.GetById(999);

            Assert.That(result, Is.Null);
        }

        [Test]
        public async Task Create_ValidDto_AddsToPersistence()
        {
            var dto = new BlackoutPeriodDto
            {
                Start = new DateOnly(2026, 4, 10),
                End = new DateOnly(2026, 4, 15),
                Description = "Spring break"
            };

            var result = await subject.Create(dto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Start, Is.EqualTo(new DateOnly(2026, 4, 10)));
                Assert.That(result.End, Is.EqualTo(new DateOnly(2026, 4, 15)));
                Assert.That(result.Description, Is.EqualTo("Spring break"));

                repositoryMock.Verify(r => r.Add(It.Is<BlackoutPeriod>(
                    bp => bp.Start == new DateOnly(2026, 4, 10) &&
                          bp.End == new DateOnly(2026, 4, 15) &&
                          bp.Description == "Spring break")), Times.Once);
                repositoryMock.Verify(r => r.Save(), Times.Once);
            }
        }

        [Test]
        public void Create_StartAfterEnd_ThrowsArgumentException()
        {
            var dto = new BlackoutPeriodDto
            {
                Start = new DateOnly(2026, 4, 15),
                End = new DateOnly(2026, 4, 10),
                Description = "Invalid"
            };

            Assert.ThrowsAsync<ArgumentException>(async () => await subject.Create(dto));
        }

        [Test]
        public async Task Create_StartEqualsEnd_Succeeds()
        {
            var dto = new BlackoutPeriodDto
            {
                Start = new DateOnly(2026, 4, 10),
                End = new DateOnly(2026, 4, 10),
                Description = "Single day"
            };

            var result = await subject.Create(dto);

            Assert.That(result.Start, Is.EqualTo(result.End));
        }

        [Test]
        public async Task Update_ValidDto_UpdatesExistingPeriod()
        {
            var existing = new BlackoutPeriod
            {
                Id = 1,
                Start = new DateOnly(2026, 1, 1),
                End = new DateOnly(2026, 1, 5),
                Description = "Old"
            };
            blackoutPeriods.Add(existing);

            var dto = new BlackoutPeriodDto
            {
                Start = new DateOnly(2026, 2, 1),
                End = new DateOnly(2026, 2, 10),
                Description = "Updated"
            };

            var result = await subject.Update(1, dto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Start, Is.EqualTo(new DateOnly(2026, 2, 1)));
                Assert.That(result.End, Is.EqualTo(new DateOnly(2026, 2, 10)));
                Assert.That(result.Description, Is.EqualTo("Updated"));

                repositoryMock.Verify(r => r.Save(), Times.Once);
            }
        }

        [Test]
        public void Update_NonExistingId_ThrowsKeyNotFoundException()
        {
            var dto = new BlackoutPeriodDto
            {
                Start = new DateOnly(2026, 2, 1),
                End = new DateOnly(2026, 2, 10)
            };

            Assert.ThrowsAsync<KeyNotFoundException>(async () => await subject.Update(999, dto));
        }

        [Test]
        public void Update_StartAfterEnd_ThrowsArgumentException()
        {
            blackoutPeriods.Add(new BlackoutPeriod
            {
                Id = 1,
                Start = new DateOnly(2026, 1, 1),
                End = new DateOnly(2026, 1, 5)
            });

            var dto = new BlackoutPeriodDto
            {
                Start = new DateOnly(2026, 4, 15),
                End = new DateOnly(2026, 4, 10)
            };

            Assert.ThrowsAsync<ArgumentException>(async () => await subject.Update(1, dto));
        }

        [Test]
        public async Task Delete_ExistingId_RemovesFromPersistence()
        {
            blackoutPeriods.Add(new BlackoutPeriod
            {
                Id = 1,
                Start = new DateOnly(2026, 1, 1),
                End = new DateOnly(2026, 1, 5)
            });
            repositoryMock.Setup(r => r.Exists(1)).Returns(true);

            await subject.Delete(1);

            using (Assert.EnterMultipleScope())
            {
                repositoryMock.Verify(r => r.Remove(1), Times.Once);
                repositoryMock.Verify(r => r.Save(), Times.Once);
            }
        }

        [Test]
        public void Delete_NonExistingId_ThrowsKeyNotFoundException()
        {
            repositoryMock.Setup(r => r.Exists(999)).Returns(false);

            Assert.ThrowsAsync<KeyNotFoundException>(async () => await subject.Delete(999));
        }
    }
}
