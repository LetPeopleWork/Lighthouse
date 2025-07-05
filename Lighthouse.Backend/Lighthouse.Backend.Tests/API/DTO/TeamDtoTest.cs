using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class TeamDtoTest
    {
        [Test]
        public void CreateTeamDto_GivenLastUpdatedTime_ReturnsDateAsUTC()
        {
            var teamUpdateTime = DateTime.Now;
            var team = new Team
            {
                UpdateTime = teamUpdateTime
            };

            var subject = CreateSubject(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.LastUpdated, Is.EqualTo(teamUpdateTime));
                Assert.That(subject.LastUpdated.Kind, Is.EqualTo(DateTimeKind.Utc));
            };
        }

        private TeamDto CreateSubject(Team team)
        {
            return new TeamDto(team);
        }
    }
}
