using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Factories
{
    public class DemoDataFactoryTest
    {
        [Test]
        public void CreateDemoWorkTrackingSystemConnection_CreatesWorkTrackingSystemConnectionWithCorrectDetails()
        {
            var subject = CreateSubject();

            var workTrackingSystemConnection = subject.CreateDemoWorkTrackingSystemConnection();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workTrackingSystemConnection.Name, Is.EqualTo("Demo Data CSV Connector"));
                Assert.That(workTrackingSystemConnection.WorkTrackingSystem, Is.EqualTo(WorkTrackingSystems.Csv));

                var options = workTrackingSystemConnection.Options;
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.Delimiter, options), Is.EqualTo(","));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.DateTimeFormat, options), Is.EqualTo("yyyy-MM-dd"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.TagSeparator, options), Is.EqualTo("|"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.IdHeader, options), Is.EqualTo("ID"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.NameHeader, options), Is.EqualTo("Name"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.StateHeader, options), Is.EqualTo("State"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.TypeHeader, options), Is.EqualTo("Type"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.ParentReferenceIdHeader, options), Is.EqualTo("Parent"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.StartedDateHeader, options), Is.EqualTo("StartedDate"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.ClosedDateHeader, options), Is.EqualTo("ClosedDate"));
                Assert.That(GetWorkTrackingSystemOptionValue(CsvWorkTrackingOptionNames.TagsHeader, options), Is.EqualTo("Tags"));
            }
        }

        [Test]
        [TestCase("Team Zenith")]
        public void CreateDemoTeam_CreatesTeamWithCorrectSettings(string teamName)
        {
            var subject = CreateSubject();

            var demoTeam = subject.CreateDemoTeam(teamName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(demoTeam.Name, Is.EqualTo(teamName));

                Assert.That(demoTeam.ToDoStates, Has.Count.EqualTo(1));
                Assert.That(demoTeam.ToDoStates, Contains.Item("Backlog"));

                Assert.That(demoTeam.DoingStates, Has.Count.EqualTo(4));
                Assert.That(demoTeam.DoingStates, Contains.Item("Next"));
                Assert.That(demoTeam.DoingStates, Contains.Item("Analysing"));
                Assert.That(demoTeam.DoingStates, Contains.Item("Implementation"));
                Assert.That(demoTeam.DoingStates, Contains.Item("Verification"));

                Assert.That(demoTeam.DoneStates, Has.Count.EqualTo(1));
                Assert.That(demoTeam.DoneStates, Contains.Item("Done"));

                Assert.That(demoTeam.WorkItemTypes, Has.Count.EqualTo(2));
                Assert.That(demoTeam.WorkItemTypes, Contains.Item("User Story"));
                Assert.That(demoTeam.WorkItemTypes, Contains.Item("Bug"));

                Assert.That(demoTeam.BlockedTags, Has.Count.EqualTo(1));
                Assert.That(demoTeam.BlockedTags, Contains.Item("Blocked"));

                Assert.That(demoTeam.WorkItemQuery, Is.Not.Empty);
                Assert.That(demoTeam.WorkItemQuery, Does.Not.Contain("{"));
                Assert.That(demoTeam.WorkItemQuery, Does.Not.Contain("}"));
            }
        }

        [Test]
        [TestCase("Project Apollo")]
        public void CreateDemoProject_CreatesProjectWithCorrectSettings(string projectName)
        {
            var subject = CreateSubject();

            var demoProject = subject.CreateDemoProject(projectName);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(demoProject.Name, Is.EqualTo(projectName));

                Assert.That(demoProject.ToDoStates, Has.Count.EqualTo(1));
                Assert.That(demoProject.ToDoStates, Contains.Item("Backlog"));

                Assert.That(demoProject.DoingStates, Has.Count.EqualTo(4));
                Assert.That(demoProject.DoingStates, Contains.Item("Next"));
                Assert.That(demoProject.DoingStates, Contains.Item("Analysing"));
                Assert.That(demoProject.DoingStates, Contains.Item("Implementation"));
                Assert.That(demoProject.DoingStates, Contains.Item("Verification"));

                Assert.That(demoProject.DoneStates, Has.Count.EqualTo(1));
                Assert.That(demoProject.DoneStates, Contains.Item("Done"));

                Assert.That(demoProject.WorkItemTypes, Has.Count.EqualTo(1));
                Assert.That(demoProject.WorkItemTypes, Contains.Item("Epic"));

                Assert.That(demoProject.BlockedTags, Has.Count.EqualTo(1));
                Assert.That(demoProject.BlockedTags, Contains.Item("Blocked"));

                Assert.That(demoProject.WorkItemQuery, Is.Not.Empty);
                Assert.That(demoProject.WorkItemQuery, Does.Not.Contain("{"));
                Assert.That(demoProject.WorkItemQuery, Does.Not.Contain("}"));
            }
        }

        private string GetWorkTrackingSystemOptionValue(string optionName, IEnumerable<WorkTrackingSystemConnectionOption> options)
        {
            return options.Single(o => o.Key == optionName).Value;
        }

        private DemoDataFactory CreateSubject()
        {
            return new DemoDataFactory(new WorkTrackingSystemFactory(Mock.Of<ILogger<WorkTrackingSystemFactory>>()));
        }
    }
}
