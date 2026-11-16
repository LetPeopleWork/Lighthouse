using Lighthouse.Backend.Modules.WorkItems.Application.Policies;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.WorkItems.Application.Policies
{
    public class TeamDeletedRefreshLogCleanupHandlerTest
    {
        private Mock<IRefreshLogService> refreshLogServiceMock;

        [SetUp]
        public void Setup()
        {
            refreshLogServiceMock = new Mock<IRefreshLogService>();
        }

        [Test]
        public async Task HandleAsync_RemovesTeamRefreshLogsForDeletedTeam()
        {
            const int teamId = 17;

            var subject = CreateSubject();

            await subject.HandleAsync(new TeamDeleted(teamId, []), CancellationToken.None);

            refreshLogServiceMock.Verify(x => x.RemoveRefreshLogsForEntity(RefreshType.Team, teamId), Times.Once);
        }

        private TeamDeletedRefreshLogCleanupHandler CreateSubject()
        {
            return new TeamDeletedRefreshLogCleanupHandler(refreshLogServiceMock.Object);
        }
    }
}
