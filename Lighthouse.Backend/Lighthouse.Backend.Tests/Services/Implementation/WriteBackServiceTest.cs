using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Factories;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
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

        private Mock<IWorkItemRepository> workItemRepositoryMock;
        private Mock<IRepository<Feature>> featureRepositoryMock;

        private List<WorkItem> workItems;

        private List<Feature> features;

        [SetUp]
        public void Setup()
        {
            connectorFactoryMock = new Mock<IWorkTrackingConnectorFactory>();
            connectorMock = new Mock<IWorkTrackingConnector>();
            loggerMock = new Mock<ILogger<WriteBackService>>();

            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();

            workItems = [];
            features = [];

            connectorFactoryMock
                .Setup(f => f.GetWorkTrackingConnector(It.IsAny<WorkTrackingSystems>()))
                .Returns(connectorMock.Object);

            workItemRepositoryMock.Setup(x => x.GetAll()).Returns(workItems);
            featureRepositoryMock.Setup(x => x.GetAll()).Returns(features);
        }

        [Test]
        public async Task WriteFieldsToWorkItems_ResolvesConnectorFromFactory()
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);
            var updates = new List<WriteBackFieldUpdate>
            {
                CreateBackFieldUpdate("42", "Custom.Age", "5", null, connection),
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
                CreateBackFieldUpdate("PROJ-1", "customfield_10001", "7", null, connection),
                CreateBackFieldUpdate("PROJ-2", "customfield_10001", "3", null, connection),
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

            _ = await service.WriteFieldsToWorkItems(connection, updates);

            connectorMock.Verify(c => c.WriteFieldsToWorkItems(connection, updates), Times.Once);
        }

        [Test]
        public async Task WriteFieldsToWorkItems_ReturnsResultFromConnector()
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);
            var updates = new List<WriteBackFieldUpdate>
            {
                CreateBackFieldUpdate("42", "Custom.Age", "5", string.Empty, connection),
                CreateBackFieldUpdate("43", "Custom.Age", "10", null, connection),
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
                CreateBackFieldUpdate("42", "Custom.Age", "5", "3", connection),
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

            var service = CreateService();

            var result = await service.WriteFieldsToWorkItems(connection, new List<WriteBackFieldUpdate>());

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
                CreateBackFieldUpdate("42", "Custom.Age", "5", null, connection),
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
                CreateBackFieldUpdate("42", "Custom.Age", "5", null, connection),
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
                CreateBackFieldUpdate("42", "Custom.Age", "5", null, connection),
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
                CreateBackFieldUpdate("42", "Custom.Age", "5", null, connection),
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

        [Test]
        public async Task WriteFieldsToWorkItems_AdditionalFieldHasSameValue_Skips()
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);
            var updates = new List<WriteBackFieldUpdate>
            {
                CreateBackFieldUpdate("42", "Custom.Age", "5", "5", connection),
            };

            connection.AdditionalFieldDefinitions.Clear();

            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(connection, updates))
                .ReturnsAsync(new WriteBackResult
                {
                    ItemResults = [new WriteBackItemResult { WorkItemId = "42", TargetFieldReference = "Custom.Age", Success = true }]
                });

            var service = CreateService();

            _ = await service.WriteFieldsToWorkItems(connection, updates);

            connectorMock.Verify(c => c.WriteFieldsToWorkItems(connection, updates), Times.Never);
        }

        [Test]
        public async Task WriteFieldsToWorkItems_AdditionalFieldDoesNotExistForItem_Skips()
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);
            var updates = new List<WriteBackFieldUpdate>
            {
                CreateBackFieldUpdate("42", "Custom.Age", "5", null, connection),
            };

            workItems.Single().AdditionalFieldValues.Clear();

            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(connection, updates))
                .ReturnsAsync(new WriteBackResult
                {
                    ItemResults = [new WriteBackItemResult { WorkItemId = "42", TargetFieldReference = "Custom.Age", Success = true }]
                });

            var service = CreateService();

            _ = await service.WriteFieldsToWorkItems(connection, updates);

            connectorMock.Verify(c => c.WriteFieldsToWorkItems(connection, updates), Times.Never);
        }

        [Test]
        public async Task WriteFieldsToWorkItems_AdditionalFieldDoesNotExistOnConnection_Skips()
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);
            var updates = new List<WriteBackFieldUpdate>
            {
                CreateBackFieldUpdate("42", "Custom.Age", "5", null, connection),
            };

            connection.AdditionalFieldDefinitions.Clear();

            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(connection, updates))
                .ReturnsAsync(new WriteBackResult
                {
                    ItemResults = [new WriteBackItemResult { WorkItemId = "42", TargetFieldReference = "Custom.Age", Success = true }]
                });

            var service = CreateService();

            _ = await service.WriteFieldsToWorkItems(connection, updates);

            connectorMock.Verify(c => c.WriteFieldsToWorkItems(connection, updates), Times.Never);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public async Task WriteFieldsToWorkItems_AdditionalFieldIsNull_WritesValue(bool isFeature)
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);
            var updates = new List<WriteBackFieldUpdate>
            {
                CreateBackFieldUpdate("42", "Custom.Age", "5", null, connection, isFeature),
            };

            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(connection, updates))
                .ReturnsAsync(new WriteBackResult
                {
                    ItemResults = [new WriteBackItemResult { WorkItemId = "42", TargetFieldReference = "Custom.Age", Success = true }]
                });

            var service = CreateService();

            _ = await service.WriteFieldsToWorkItems(connection, updates);

            connectorMock.Verify(c => c.WriteFieldsToWorkItems(connection, updates), Times.Once);
        }

        [Test]
        public async Task WriteFieldsToWorkItems_WorkItemAppearsInMultipleTeams_DoesNotThrow()
        {
            var connection = CreateConnection(WorkTrackingSystems.AzureDevOps);

            var update = CreateBackFieldUpdate("FWUP-3", "Custom.Age", "5", null, connection);
            AddItem("FWUP-3", connection.AdditionalFieldDefinitions.Single(f => f.Reference == "Custom.Age").Id, null);

            var updates = new List<WriteBackFieldUpdate> { update };

            connectorMock
                .Setup(c => c.WriteFieldsToWorkItems(connection, It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .ReturnsAsync(new WriteBackResult { ItemResults = [] });

            var service = CreateService();

            await service.WriteFieldsToWorkItems(connection, updates);

            connectorMock.Verify(c => c.WriteFieldsToWorkItems(connection, It.Is<IReadOnlyList<WriteBackFieldUpdate>>(u => u.Count == 1 && u[0].WorkItemId == "FWUP-3")), Times.Once);
        }

        private WriteBackFieldUpdate CreateBackFieldUpdate(string workItemReference, string targetFieldReference, string? newValue, string? oldValue, WorkTrackingSystemConnection connection, bool isFeature = false)
        {
            var additionalFieldId = AddAdditionalField(connection, targetFieldReference);
            AddItem(workItemReference, additionalFieldId, oldValue, isFeature);

            return new WriteBackFieldUpdate
            { WorkItemId = workItemReference, TargetFieldReference = targetFieldReference, Value = newValue };
        }

        private void AddItem(string referenceId, int additionalFieldId, string? additionalFieldValue, bool isFeature = false)
        {
            var workItemBase = new WorkItemBase
            {
                ReferenceId = referenceId,
                AdditionalFieldValues = new Dictionary<int, string?>
                {
                    { additionalFieldId, additionalFieldValue },
                }
            };

            if (isFeature)
            {
                features.Add(new Feature(workItemBase));
            }
            else
            {
                workItems.Add(new WorkItem(workItemBase, new Team()));
            }
        }

        private static int AddAdditionalField(WorkTrackingSystemConnection workTrackingSystemConnection, string additionalFieldReference)
        {
            var existingField =
                workTrackingSystemConnection.AdditionalFieldDefinitions.SingleOrDefault(af =>
                    af.Reference == additionalFieldReference);

            if (existingField != null)
            {
                return existingField.Id;
            }

            var count = workTrackingSystemConnection.AdditionalFieldDefinitions.Count;

            workTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                DisplayName = additionalFieldReference,
                Id = count,
                Reference = additionalFieldReference,
                WorkTrackingSystemConnection = workTrackingSystemConnection,
            });

            return count;
        }

        private WriteBackService CreateService()
        {
            return new WriteBackService(connectorFactoryMock.Object, loggerMock.Object, workItemRepositoryMock.Object, featureRepositoryMock.Object);
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
