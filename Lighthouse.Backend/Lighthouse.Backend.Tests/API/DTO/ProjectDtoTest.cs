using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class ProjectDtoTest
    {
        [Test]
        public void CreateProjectDto_GivenLastUpdatedTime_ReturnsDateAsUTC()
        {
            var projectUpdateTime = DateTime.Now;
            var project = new Project
            {
                UpdateTime = projectUpdateTime
            };

            var subject = CreateSubject(project);
            
            Assert.Multiple(() =>
            {
                Assert.That(subject.LastUpdated, Is.EqualTo(projectUpdateTime));
                Assert.That(subject.LastUpdated.Kind, Is.EqualTo(DateTimeKind.Utc));
            });
        }

        private ProjectDto CreateSubject(Project project)
        {
            return new ProjectDto(project);
        }
    }
}
