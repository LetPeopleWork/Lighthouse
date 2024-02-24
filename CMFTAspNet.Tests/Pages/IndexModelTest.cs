using CMFTAspNet.Models;
using CMFTAspNet.Pages;
using CMFTAspNet.Services.Implementation;
using CMFTAspNet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Moq;

namespace CMFTAspNet.Tests.Pages
{
    public class IndexModelTest
    {
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IMonteCarloService> monteCarloServiceMock;

        [SetUp]
        public void Setup()
        {
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            monteCarloServiceMock = new Mock<IMonteCarloService>();
        }

        [Test]
        public void OnGet_LoadsAllFeatures()
        {
            var features = new List<Feature>
            {
                new Feature { Id = 1, Name = "Project1" },
                new Feature { Id = 2, Name = "SuperImportantProject" },
            };

            featureRepositoryMock.Setup(x => x.GetAll()).Returns(features);

            var subject = CreateSubject();

            var result = subject.OnGet();

            Assert.That(result, Is.InstanceOf<PageResult>());
            CollectionAssert.AreEquivalent(subject.Features, features);
        }

        [Test]
        public async Task OnPost_RecalculatesForecasts_ReturnsPage()
        {
            var features = new List<Feature>
            {
                new Feature { Id = 1, Name = "Project1" },
                new Feature { Id = 2, Name = "SuperImportantProject" },
            };

            featureRepositoryMock.Setup(x => x.GetAll()).Returns(features);

            var subject = CreateSubject();

            var result = await subject.OnPost();

            Assert.That(result, Is.InstanceOf<PageResult>());
            monteCarloServiceMock.Verify(x => x.ForecastFeatures(features));
        }

        private IndexModel CreateSubject()
        {
            return new IndexModel(featureRepositoryMock.Object, monteCarloServiceMock.Object);
        }
    }
}
