using Lighthouse.Backend.API;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Boards;
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
            
            var boards = new List<Board>
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
                
                var returnedBoards = okResult.Value as List<Board>;
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
        
        [Test]
        public async Task GetJiraBoardInformation_WorkTrackingSystemExists_BoardExists_ReturnsBoardInformation()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection();
            workTrackingSystemConnectionRepoMock.Setup(x => x.GetById(12)).Returns(workTrackingSystemConnection);
            
            const int boardId = 42;

            var boardInformation = new BoardInformation
            {
                DataRetrievalValue = "fixVersion = 1.33.7",
                WorkItemTypes = ["Story", "Bug"],
                ToDoStates = ["Backlog"],
                DoingStates = ["Analyzing", "Development"],
                DoneStates = ["Done"],
            };
            
            jiraWorkTrackingConnectorMock.Setup(x => x.GetBoardInformation(workTrackingSystemConnection, boardId)).ReturnsAsync(boardInformation);

            var subject = CreateSubject();

            var response = await subject.GetJiraBoardInformation(12, boardId);
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Result, Is.InstanceOf<OkObjectResult>());
                var okResult = response.Result as OkObjectResult;
                Assert.That(okResult.StatusCode, Is.EqualTo(200));
                
                var returnedBoardInfo = okResult.Value as BoardInformation;
                Assert.That(returnedBoardInfo.DataRetrievalValue, Is.EqualTo(boardInformation.DataRetrievalValue));
                Assert.That(returnedBoardInfo.WorkItemTypes, Is.EqualTo(boardInformation.WorkItemTypes));
                Assert.That(returnedBoardInfo.ToDoStates, Is.EqualTo(boardInformation.ToDoStates));
                Assert.That(returnedBoardInfo.DoingStates, Is.EqualTo(boardInformation.DoingStates));
                Assert.That(returnedBoardInfo.DoneStates, Is.EqualTo(boardInformation.DoneStates));
            }
        }

        [Test]
        public async Task GetJiraBoardConfiguration_WorkTrackingSystemDoesNotExist_ReturnsNotFound()
        {
            var subject = CreateSubject();

            var response = await subject.GetJiraBoardInformation(12, 42);
            
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