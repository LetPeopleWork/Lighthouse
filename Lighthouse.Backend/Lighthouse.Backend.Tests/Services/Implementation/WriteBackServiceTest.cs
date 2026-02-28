using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class WriteBackServiceTest
    {
        private Mock<IWorkTrackingConnectorFactory> connectorFactoryMock;
        private Mock<IWorkTrackingConnector> connectorMock;
        private Mock<ILogger<WriteBackService>> loggerMock;

        [SetUp]
        public void Setup()
        {
            connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorMock = new Mock<IWorkTrackingConnector>();
            loggerMock = new Mock<ILogger<WriteBackService>>();

            connectorFactoryMock
                .Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(connectorMock.Object);
        }

        [Test]
        public async Task WriteFieldsToWorkItems_ResolvesConnectorFromFactory()
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);
            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = "42", TargetFieldReference = "Custom.Age", Value = "5" }
            };

            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(connection, updates))
                .ReturnsAsync(new WriteBackResult
                {
                    ItemResults = [new WriteBackItemResult { WorkItemId = "42", TargetFieldReference = "Custom.Age", Success = true }]
                });

            var service = CreateService();

            await service.WriteFieldsToWorkItems(connection, updates);

            connectorFactoryMock.Verify(f => f.GetWorkTrackingConnector(WorkTrackingSystems.AzureDevOps), Times.Once);
        }

        [Test]
        public async Task WriteFieldsToWorkItems_DelegatesUpdatesToConnector()
        {
            var connection = CreateConnection(WorkTrackingSystems.Jira);
            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = "PROJ-1", TargetFieldReference = "customfield_10001", Value = "7" },
                new() { WorkItemId = "PROJ-2", TargetFieldReference = "customfield_10001", Value = "3" }
            };

            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(connection, updates))
                .ReturnsAsync(new WriteBackResult
                {
                    ItemResults =
                    [
                        new WriteBackItemResult { WorkItemId = "PROJ-1", TargetFieldReference = "customfield_10001", Success = true },
                        new WriteBackItemResult { WorkItemId = "PROJ-2", TargetFieldReference = "customfield_10001", Success = true }
                    ]
                });

            var service = CreateService();

            var result = await service.WriteFieldsToWorkItems(connection, updates);

            connectorMock.Verify(c => c.WriteFieldsToWorkItems(connection, updates), Times.Once);
        }

        [Test]
        public async Task WriteFieldsToWorkItems_ReturnsResultFromConnector()
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);
            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = "42", TargetFieldReference = "Custom.Age", Value = "5" },
                new() { WorkItemId = "43", TargetFieldReference = "Custom.Age", Value = "10" }
            };

            var expectedResult = new WriteBackResult
            {
                ItemResults =
                [
                    new WriteBackItemResult { WorkItemId = "42", TargetFieldReference = "Custom.Age", Success = true },
                    new WriteBackItemResult { WorkItemId = "43", TargetFieldReference = "Custom.Age", Success = false, ErrorMessage = "Field not found" }
                ]
            };

            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(connection, updates))
                .ReturnsAsync(expectedResult);

            var service = CreateService();

            var result = await service.WriteFieldsToWorkItems(connection, updates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.SuccessCount, Is.EqualTo(1));
                Assert.That(result.FailureCount, Is.EqualTo(1));
                Assert.That(result.AllSucceeded, Is.False);
            }
        }

        [Test]
        public async Task WriteFieldsToWorkItems_AllSucceed_ReturnsAllSucceeded()
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);
            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = "42", TargetFieldReference = "Custom.Age", Value = "5" }
            };

            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(connection, updates))
                .ReturnsAsync(new WriteBackResult
                {
                    ItemResults = [new WriteBackItemResult { WorkItemId = "42", TargetFieldReference = "Custom.Age", Success = true }]
                });

            var service = CreateService();

            var result = await service.WriteFieldsToWorkItems(connection, updates);

            Assert.That(result.AllSucceeded, Is.True);
        }

        [Test]
        public async Task WriteFieldsToWorkItems_EmptyUpdates_ReturnsEmptyResult()
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);
            var updates = new List<WriteBackFieldUpdate>();

            var service = CreateService();

            var result = await service.WriteFieldsToWorkItems(connection, updates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ItemResults, Is.Empty);
                Assert.That(result.AllSucceeded, Is.True);
                Assert.That(result.SuccessCount, Is.Zero);
                Assert.That(result.FailureCount, Is.Zero);
            }

            connectorMock.Verify(c => c.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()), Times.Never);
        }

        [Test]
        public async Task WriteFieldsToWorkItems_ConnectorThrows_WrapsInFailedResult()
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);
            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = "42", TargetFieldReference = "Custom.Age", Value = "5" }
            };

            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(connection, updates))
                .ThrowsAsync(new HttpRequestException("Service unavailable"));

            var service = CreateService();

            var result = await service.WriteFieldsToWorkItems(connection, updates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.AllSucceeded, Is.False);
                Assert.That(result.FailureCount, Is.EqualTo(1));
                Assert.That(result.ItemResults[0].ErrorMessage, Does.Contain("Service unavailable"));
            }
        }

        [Test]
        public async Task WriteFieldsToWorkItems_LogsStartedAndCompleted()
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);
            connection.Name = "My ADO Connection";
            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = "42", TargetFieldReference = "Custom.Age", Value = "5" }
            };

            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(connection, updates))
                .ReturnsAsync(new WriteBackResult
                {
                    ItemResults = [new WriteBackItemResult { WorkItemId = "42", TargetFieldReference = "Custom.Age", Success = true }]
                });

            var service = CreateService();

            await service.WriteFieldsToWorkItems(connection, updates);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting write-back")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Completed write-back")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task WriteFieldsToWorkItems_WithFailures_LogsFailuresAtDebugLevel()
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);
            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = "42", TargetFieldReference = "Custom.Age", Value = "5" }
            };

            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(connection, updates))
                .ReturnsAsync(new WriteBackResult
                {
                    ItemResults =
                    [
                        new WriteBackItemResult { WorkItemId = "42", TargetFieldReference = "Custom.Age", Success = false, ErrorMessage = "Permission denied" }
                    ]
                });

            var service = CreateService();

            await service.WriteFieldsToWorkItems(connection, updates);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("42") && v.ToString()!.Contains("Permission denied")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task WriteFieldsToWorkItems_ConnectorThrows_LogsErrorWithException()
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);
            var updates = new List<WriteBackFieldUpdate>
            {
                new() { WorkItemId = "42", TargetFieldReference = "Custom.Age", Value = "5" }
            };

            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(connection, updates))
                .ThrowsAsync(new HttpRequestException("Service unavailable"));

            var service = CreateService();

            await service.WriteFieldsToWorkItems(connection, updates);

            var errorInvocations = loggerMock.Invocations
                .Where(i => (LogLevel)i.Arguments[0] == LogLevel.Error)
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(errorInvocations, Has.Count.EqualTo(1));
                Assert.That(errorInvocations[0].Arguments[3], Is.InstanceOf<HttpRequestException>());
                Assert.That(((Exception)errorInvocations[0].Arguments[3]!).Message, Does.Contain("Service unavailable"));
            }
        }

        private WriteBackService CreateService()
        {
            return new WriteBackService(connectorFactoryMock.Object, loggerMock.Object);
        }

        private static WorkTrackingSystemConnection CreateConnection(WorkTrackingSystems system)
        {
            return new WorkTrackingSystemConnection
            {
                Id = 1,
                Name = "Test Connection",
                WorkTrackingSystem = system
            };
        }
    }
}
