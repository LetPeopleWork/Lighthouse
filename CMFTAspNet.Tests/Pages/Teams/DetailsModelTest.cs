using CMFTAspNet.Models;
using CMFTAspNet.Models.Forecast;
using CMFTAspNet.Pages.Teams;
using CMFTAspNet.Services.Implementation;
using CMFTAspNet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

namespace CMFTAspNet.Tests.Pages.Teams
{
    public class DetailsModelTest
    {
        private Mock<IRepository<Team>> teamRepositoryMock;
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IThroughputService> throughputServiceMock;
        private Mock<IMonteCarloService> monteCarloServiceMock;

        [SetUp]
        public void Setup()
        {
            teamRepositoryMock = new Mock<IRepository<Team>>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            throughputServiceMock = new Mock<IThroughputService>();
            monteCarloServiceMock = new Mock<IMonteCarloService>();
        }

        [Test]
        public void OnGet_ExistingTeam_GetsFeaturesForTeam()
        {
            var team = new Team { Id = 12, Name = "MyTeam" };

            var features = new List<Feature>();
            var featureForTeam = CreateFeatureForTeam(team, 37);

            features.Add(featureForTeam);
            features.Add(new Feature { Name = "Other Feature" });

            featureRepositoryMock.Setup(x => x.GetAll()).Returns(features);
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(team);

            var subject = CreateSubject();

            // Act
            subject.OnGet(12);

            Assert.Multiple(() =>
            {
                Assert.That(subject.Features, Has.Count.EqualTo(1));
                Assert.That(subject.Features.Single(), Is.EqualTo(featureForTeam));
            });
        }

        [Test]
        public async Task OnPostUpdateForecast_GivenFeaturesForTeam_UpdatesForecastsAsync()
        {
            var team = new Team { Id = 12, Name = "My Team" };

            var featuresForTeam = new List<Feature>
            {
                CreateFeatureForTeam(team, 37),
                CreateFeatureForTeam(team, 12),
                CreateFeatureForTeam(team, 57),
            };

            var features = new List<Feature> { new Feature() };
            features.AddRange(featuresForTeam);
            features.Add(new Feature());

            featureRepositoryMock.Setup(x => x.GetAll()).Returns(features);
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(team);

            var subject = CreateSubject();

            // Act
            await subject.OnPostUpdateForecast(12);

            monteCarloServiceMock.Verify(x => x.ForecastFeatures(featuresForTeam));
        }

        [Test]
        public async Task OnPost_IdIsNull_ReturnsNotFoundAsync()
        {
            var subject = CreateSubject();

            var result = await subject.OnPostUpdateThroughput(null);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
            teamRepositoryMock.Verify(x => x.GetById(It.IsAny<int>()), Times.Never());
        }

        [Test]
        public async Task OnPost_TeamDoesNotExist_ReturnsNotFoundAsync()
        {
            teamRepositoryMock.Setup(x => x.GetById(12)).Returns((Team)null);
            var subject = CreateSubject();

            var result = await subject.OnPostUpdateThroughput(12);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task OnPost_TeamExists_UpdatesThroughputAndSavesAsync()
        {
            var team = new Team { Id = 2, Name = "Team", ProjectName = "Project" };

            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(team);
            var subject = CreateSubject();

            var result = await subject.OnPostUpdateThroughput(12);

            Assert.That(result, Is.InstanceOf<PageResult>());
            throughputServiceMock.Verify(x => x.UpdateThroughput(team), Times.Once());
            teamRepositoryMock.Verify(x => x.Update(team), Times.Once());
            teamRepositoryMock.Verify(x => x.Save(), Times.Once());
        }

        [Test]
        public async Task OnPostWhenForecast_RunsForecast_SetsPropertyAsync()
        {
            var team = new Team { Id = 2, Name = "Team", ProjectName = "Project" };

            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(team);
            var subject = CreateSubject();

            var expectedForecast = new WhenForecast();
            monteCarloServiceMock.Setup(x => x.When(team, 42)).Returns(expectedForecast);

            var result = await subject.OnPostWhenForecast(12, 42);

            Assert.That(subject.WhenForecast, Is.EqualTo(expectedForecast));
        }

        [Test]
        public async Task OnPostHowManyForecast_RunsForecast_SetsPropertyAsync()
        {
            var team = new Team { Id = 2, Name = "Team", ProjectName = "Project" };

            teamRepositoryMock.Setup(x => x.GetById(12)).Returns(team);
            var subject = CreateSubject();

            var expectedForecast = new HowManyForecast();
            monteCarloServiceMock.Setup(x => x.HowMany(It.IsAny<Throughput>(), 32)).Returns(expectedForecast);

            var result = await subject.OnPostHowManyForecast(12, DateTime.Now.AddDays(32));

            Assert.That(subject.HowManyForecast, Is.EqualTo(expectedForecast));
        }

        private DetailsModel CreateSubject()
        {
            return new DetailsModel(teamRepositoryMock.Object, throughputServiceMock.Object, featureRepositoryMock.Object, monteCarloServiceMock.Object);
        }

        private Feature CreateFeatureForTeam(Team team, int featureId)
        {
            var featureForTeam = new Feature { Name = "FeatureForTeam " };
            featureForTeam.RemainingWork.Add(new RemainingWork(team, featureId, featureForTeam));
            return featureForTeam;
        }
    }
}
