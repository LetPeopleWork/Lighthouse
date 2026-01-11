using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class WizardsControllerTest
    {
        private Mock<IJiraWorkTrackingConnector> jiraWorkTrackingConnectorMock;
        
        private Mock<IRepository<WorkTrackingSystemConnection>> workTrackingSystemConnectionRepoMock;

        [SetUp]
        public void Setup()
        {
            jiraWorkTrackingConnectorMock = new Mock<IJiraWorkTrackingConnector>();
            workTrackingSystemConnectionRepoMock = new Mock<IRepository<WorkTrackingSystemConnection>>();
        }
        
        [Test]
        public async Task GetJiraBoards_WorkTrackingSystemExists_ReturnsAvailableBoards()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection();
            workTrackingSystemConnectionRepoMock.Setup(x => x.GetById(12)).Returns(workTrackingSystemConnection);
            
            var boards = new List<JiraBoard>
            {
                new() { Id = 1, Name = "My Board" },
                new() { Id = 2, Name = "My Other Board" }
            };
            
            jiraWorkTrackingConnectorMock.Setup(x => x.GetBoards(workTrackingSystemConnection)).ReturnsAsync(boards);

            var subject = CreateSubject();

            var response = await subject.GetJiraBoards(12);
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                
                var returnedBoards = okResult.Value as List<JiraBoard>;
                Assert.That(returnedBoards, Has.Count.EqualTo(2));
                Assert.That(returnedBoards[0].Name, Is.EqualTo("My Board"));
                Assert.That(returnedBoards[0].Id, Is.EqualTo(1));
                Assert.That(returnedBoards[1].Name, Is.EqualTo("My Other Board"));
                Assert.That(returnedBoards[1].Id, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task GetJiraBoards_WorkTrackingSystemDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = await subject.GetJiraBoards(12);
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<NotFoundResult>());

                var notFoundResult = response.Result as NotFoundResult;
                Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
            }
        }
        
        private WizardsController CreateSubject()
        {
            return new WizardsController(jiraWorkTrackingConnectorMock.Object, workTrackingSystemConnectionRepoMock.Object);
        }
    }
}