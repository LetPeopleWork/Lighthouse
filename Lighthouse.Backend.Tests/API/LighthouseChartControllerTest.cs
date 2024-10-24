using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO.LighthouseChart;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.History;
using Lighthouse.Backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;

namespace Lighthouse.Backend.Tests.API
{
    public class LighthouseChartControllerTest
    {
        private Mock<IRepository<Project>> projectRepositoryMock;
        private Mock<IRepository<FeatureHistoryEntry>> featureHistoryRepositoryMock;

        [SetUp]
        public void Setup()
        {
            projectRepositoryMock = new Mock<IRepository<Project>>();
            featureHistoryRepositoryMock = new Mock<IRepository<FeatureHistoryEntry>>();
        }

        [Test]
        public void GetLighthouseChartData_ProjectDoesNotExist_ReturnsNotFound()
        {
            projectRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns((Project)null);

            var subject = CreateSubject();
            var response = subject.GetLighthouseChartData(1, new LighthouseChartController.LighthouseChartDataInput { StartDate = DateTime.Today.AddDays(-30), SampleRate = 1 });

            Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void GetLighthouseChartData_ProjectExists_NoFeatures_NoMilestones_ReturnsEmptyDto()
        {
            var project = new Project();
            projectRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns(project);

            var subject = CreateSubject();
            var response = subject.GetLighthouseChartData(1, new LighthouseChartController.LighthouseChartDataInput());

            Assert.Multiple(() =>
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());

                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                Assert.That(okResult.Value, Is.InstanceOf<LighthouseChartDto>());

                var lighthouseChartDto = okResult.Value as LighthouseChartDto;

                Assert.That(lighthouseChartDto.Features, Has.Count.EqualTo(0));
                Assert.That(lighthouseChartDto.Milestones, Has.Count.EqualTo(0));
            });
        }

        [Test]
        public void GetLighthouseChartData_ProjectExists_HasMilestones_ReturnsDtoWithMilestones()
        {
            var milestone = new Milestone { Name = "Milestone", Date = new DateTime(2099, 04, 08, 0, 0, 0, DateTimeKind.Utc) };
            var project = new Project();
            project.Milestones.Add(milestone);

            projectRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns(project);

            var subject = CreateSubject();
            var response = subject.GetLighthouseChartData(1, new LighthouseChartController.LighthouseChartDataInput());

            Assert.Multiple(() =>
            {
                var okResult = response.Result as OkObjectResult;
                var lighthouseChartDto = okResult.Value as LighthouseChartDto;
                Assert.That(lighthouseChartDto.Milestones, Has.Count.EqualTo(1));

                var milestoneDto = lighthouseChartDto.Milestones.Single();
                Assert.That(milestoneDto.Name, Is.EqualTo(milestone.Name));
                Assert.That(milestoneDto.Date, Is.EqualTo(milestone.Date));
            });
        }

        [Test]
        public void GetLighthouseChartData_HasMilestones_IgnoresMilestonesInThePast()
        {
            var futureMilestone = new Milestone { Name = "Future Milestone", Date = new DateTime(2099, 04, 08, 0, 0, 0, DateTimeKind.Utc) };
            var pastMilestone = new Milestone { Name = "Past Milestone", Date = new DateTime(1991, 04, 08, 0, 0, 0, DateTimeKind.Utc) };
            
            var project = new Project();
            project.Milestones.Add(futureMilestone);
            project.Milestones.Add(pastMilestone);

            projectRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns(project);

            var subject = CreateSubject();
            var response = subject.GetLighthouseChartData(1, new LighthouseChartController.LighthouseChartDataInput());

            Assert.Multiple(() =>
            {
                var okResult = response.Result as OkObjectResult;
                var lighthouseChartDto = okResult.Value as LighthouseChartDto;
                Assert.That(lighthouseChartDto.Milestones, Has.Count.EqualTo(1));

                var milestoneDto = lighthouseChartDto.Milestones.Single();
                Assert.That(milestoneDto.Name, Is.EqualTo(futureMilestone.Name));
                Assert.That(milestoneDto.Date, Is.EqualTo(futureMilestone.Date));
            });
        }

        [Test]
        [TestCase(15, 1, 30, 15)]
        [TestCase(30, 1, 30, 30)]
        [TestCase(40, 1, 30, 30)]
        [TestCase(30, 5, 30, 6)]
        [TestCase(30, 7, 30, 5)]
        [TestCase(360, 30, 360, 12)]
        public void GetLighthouseChartData_OneFeature_ReturnsTrendAccordingToResolution_ReturnsAllAvailableItems(int historyEntries, int sampleRate, int historyInDays, int expectedItems)
        {
            var project = new Project();
            projectRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns(project);

            var feature = new Feature { ReferenceId = "Feature1" };
            project.UpdateFeatures([feature]);

            var featureHistoryEntries = SetupFeatureHistoryForFeature(feature, historyEntries, 10);
            SetupFeatureHistoryRepositoryWithFeatureHistoryEntries(featureHistoryEntries);

            var subject = CreateSubject();
            var response = subject.GetLighthouseChartData(1, new LighthouseChartController.LighthouseChartDataInput { SampleRate = sampleRate, StartDate = DateTime.Today.AddDays(-historyInDays) });

            Assert.Multiple(() =>
            {
                var okResult = response.Result as OkObjectResult;
                var lighthouseChartDto = okResult.Value as LighthouseChartDto;
                Assert.That(lighthouseChartDto.Features, Has.Count.EqualTo(1));

                var featureDto = lighthouseChartDto.Features.Single();
                Assert.That(featureDto.RemainingItemsTrend, Has.Count.EqualTo(expectedItems));
            });
        }

        [Test]
        public void GetLighthouseChartData_MissingSnapshot_ReturnsRemainingWorkFromPreviousEntry()
        {
            var project = new Project();
            projectRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns(project);

            var feature = new Feature { ReferenceId = "Feature1" };
            project.UpdateFeatures([feature]);

            var featureHistoryEntries = SetupFeatureHistoryForFeature(feature, 30, 10);

            for (var index = 1; index < featureHistoryEntries.Count; index += 2)
            {
                featureHistoryEntries.RemoveAt(index);
            }

            SetupFeatureHistoryRepositoryWithFeatureHistoryEntries(featureHistoryEntries);

            var subject = CreateSubject();
            var response = subject.GetLighthouseChartData(1, new LighthouseChartController.LighthouseChartDataInput { SampleRate = 1, StartDate = DateTime.Today.AddDays(-30) });

            Assert.Multiple(() =>
            {
                var okResult = response.Result as OkObjectResult;
                var lighthouseChartDto = okResult.Value as LighthouseChartDto;
                Assert.That(lighthouseChartDto.Features, Has.Count.EqualTo(1));

                var featureDto = lighthouseChartDto.Features.Single();
                Assert.That(featureDto.RemainingItemsTrend, Has.Count.EqualTo(30));

                foreach (var remainingItem in featureDto.RemainingItemsTrend)
                {
                    Assert.That(remainingItem.RemainingItems, Is.EqualTo(10));
                }
            });
        }

        private void SetupFeatureHistoryRepositoryWithFeatureHistoryEntries(List<FeatureHistoryEntry> featureHistoryEntries)
        {
            featureHistoryRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Func<FeatureHistoryEntry, bool>>())).Returns((Func<FeatureHistoryEntry, bool> predicate) => featureHistoryEntries.Where(predicate));
        }

        private List<FeatureHistoryEntry> SetupFeatureHistoryForFeature(Feature feature, int days, int remainingItems)
        {
            var featureHistoryEntries = new List<FeatureHistoryEntry>();

            for (var index = 0; index < days; index++)
            {
                var time = DateOnly.FromDateTime(DateTime.Now.AddDays(-index));
                var featureHistoryEntry = new FeatureHistoryEntry
                {
                    FeatureReferenceId = feature.ReferenceId,
                    FeatureId = feature.Id,
                    Snapshot = time,
                };

                featureHistoryEntry.FeatureWork.Add(new FeatureWorkHistoryEntry { RemainingWorkItems = remainingItems });

                featureHistoryEntries.Add(featureHistoryEntry);
            }

            return featureHistoryEntries;
        }

        private LighthouseChartController CreateSubject()
        {
            return new LighthouseChartController(projectRepositoryMock.Object, featureHistoryRepositoryMock.Object);
        }
    }
}
