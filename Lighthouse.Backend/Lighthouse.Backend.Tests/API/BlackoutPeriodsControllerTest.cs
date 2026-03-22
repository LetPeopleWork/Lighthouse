using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class BlackoutPeriodsControllerTest
    {
        private Mock<IBlackoutPeriodService> serviceMock;
        private BlackoutPeriodsController subject;

        [SetUp]
        public void SetUp()
        {
            serviceMock = new Mock<IBlackoutPeriodService>();
            subject = new BlackoutPeriodsController(serviceMock.Object);
        }

        [Test]
        public void GetAll_ServiceReturnsData_ReturnsOkWithData()
        {
            var periods = new List<BlackoutPeriod>
            {
                new() { Id = 1, Start = new DateOnly(2026, 1, 1), End = new DateOnly(2026, 1, 5), Description = "New Year" },
                new() { Id = 2, Start = new DateOnly(2026, 6, 1), End = new DateOnly(2026, 6, 3), Description = "Summer" }
            };
            serviceMock.Setup(s => s.GetAll()).Returns(periods);

            var result = subject.GetAll();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult!.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.EqualTo(periods));
            }
        }

        [Test]
        public void GetAll_ServiceReturnsEmpty_ReturnsOkWithEmptyList()
        {
            serviceMock.Setup(s => s.GetAll()).Returns([]);

            var result = subject.GetAll();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = result.Result as OkObjectResult;
                Assert.That(okResult!.StatusCode, Is.EqualTo(200));
            }
        }

        [Test]
        public async Task Create_ValidDto_ReturnsCreatedWithPeriod()
        {
            var dto = new BlackoutPeriodDto
            {
                Start = new DateOnly(2026, 4, 10),
                End = new DateOnly(2026, 4, 15),
                Description = "Spring break"
            };

            var created = new BlackoutPeriod
            {
                Id = 1,
                Start = dto.Start,
                End = dto.End,
                Description = dto.Description
            };

            serviceMock.Setup(s => s.Create(dto)).ReturnsAsync(created);

            var result = await subject.Create(dto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<CreatedAtActionResult>());
                var createdResult = result as CreatedAtActionResult;
                Assert.That(createdResult!.StatusCode, Is.EqualTo(201));
                Assert.That(createdResult.Value, Is.EqualTo(created));
            }
        }

        [Test]
        public async Task Create_InvalidDates_ReturnsBadRequest()
        {
            var dto = new BlackoutPeriodDto
            {
                Start = new DateOnly(2026, 4, 15),
                End = new DateOnly(2026, 4, 10)
            };

            serviceMock.Setup(s => s.Create(dto)).ThrowsAsync(new ArgumentException("Start date must be on or before end date."));

            var result = await subject.Create(dto);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Update_ValidDto_ReturnsOkWithUpdatedPeriod()
        {
            var dto = new BlackoutPeriodDto
            {
                Start = new DateOnly(2026, 5, 1),
                End = new DateOnly(2026, 5, 10),
                Description = "Updated"
            };

            var updated = new BlackoutPeriod
            {
                Id = 1,
                Start = dto.Start,
                End = dto.End,
                Description = dto.Description
            };

            serviceMock.Setup(s => s.Update(1, dto)).ReturnsAsync(updated);

            var result = await subject.Update(1, dto);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<OkObjectResult>());
                var okResult = result as OkObjectResult;
                Assert.That(okResult!.Value, Is.EqualTo(updated));
            }
        }

        [Test]
        public async Task Update_NonExistingId_ReturnsNotFound()
        {
            var dto = new BlackoutPeriodDto
            {
                Start = new DateOnly(2026, 5, 1),
                End = new DateOnly(2026, 5, 10)
            };

            serviceMock.Setup(s => s.Update(999, dto)).ThrowsAsync(new KeyNotFoundException("Not found"));

            var result = await subject.Update(999, dto);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task Update_InvalidDates_ReturnsBadRequest()
        {
            var dto = new BlackoutPeriodDto
            {
                Start = new DateOnly(2026, 5, 10),
                End = new DateOnly(2026, 5, 1)
            };

            serviceMock.Setup(s => s.Update(1, dto)).ThrowsAsync(new ArgumentException("Start date must be on or before end date."));

            var result = await subject.Update(1, dto);

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Delete_ExistingId_ReturnsNoContent()
        {
            serviceMock.Setup(s => s.Delete(1)).Returns(Task.CompletedTask);

            var result = await subject.Delete(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<NoContentResult>());
                serviceMock.Verify(s => s.Delete(1), Times.Once);
            }
        }

        [Test]
        public async Task Delete_NonExistingId_ReturnsNotFound()
        {
            serviceMock.Setup(s => s.Delete(999)).ThrowsAsync(new KeyNotFoundException("Not found"));

            var result = await subject.Delete(999);

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }
    }
}
