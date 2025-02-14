using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class ConfigFileUpdaterTests
    {
        private Mock<IFileSystemService> fileSystemMock;
        private ConfigFileUpdater subject;

        [SetUp]
        public void Setup()
        {
            fileSystemMock = new Mock<IFileSystemService>();
            subject = new ConfigFileUpdater(fileSystemMock.Object);
        }

        [Test]
        public void UpdateConfigFile_UpdatesExistingKey()
        {
            const string key = "SomeSection:SomeSubSection:SomeKey";
            const string newValue = "NewValue";
            var existingJson = @"{ 'SomeSection': { 'SomeSubSection': { 'SomeKey': 'OldValue' } } }";
            fileSystemMock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            fileSystemMock.Setup(fs => fs.ReadAllText(It.IsAny<string>())).Returns(existingJson);

            subject.UpdateConfigFile(key, newValue);
            fileSystemMock.Verify(fs => fs.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Test]
        public void UpdateConfigFile_FileNotExists_ThrowsException()
        {
            const string key = "SomeSection:SomeSubSection:SomeKey";
            fileSystemMock.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(false);

            Assert.Throws<FileNotFoundException>(() => subject.UpdateConfigFile(key, "NewValue"));
        }
    }
}
