using Lighthouse.Services.Implementation.WorkItemServices;

namespace Lighthouse.Tests.Services.Implementation.WorkItemServices
{
    public class LexoRankServiceTest
    {
        private ILexoRankService _lexoRankService;

        [SetUp]
        public void Setup()
        {
            _lexoRankService = new LexoRankService();
        }

        [Test]
        public void GetHigherPriority_IncrementLastCharacter_ReturnsHigherPriority()
        {
            // Arrange
            string currentRank = "00000|";

            // Act
            string higherPriority = _lexoRankService.GetHigherPriority(currentRank);

            // Assert
            Assert.That(higherPriority, Is.EqualTo("00001|"));
        }

        [Test]
        public void GetLowerPriority_DecrementLastCharacter_ReturnsLowerPriority()
        {
            // Arrange
            string currentRank = "00001|";

            // Act
            string lowerPriority = _lexoRankService.GetLowerPriority(currentRank);

            // Assert
            Assert.That(lowerPriority, Is.EqualTo("00000|"));
        }

        [Test]
        public void GetHigherPriority_LastCharacterIsMaxValue_ReturnsOverflowedPriority()
        {
            // Arrange
            string currentRank = "00009|";

            // Act
            string higherPriority = _lexoRankService.GetHigherPriority(currentRank);

            // Assert
            Assert.That(higherPriority, Is.EqualTo("0000:|"));
        }

        [Test]
        public void GetLowerPriority_LastCharacterIsMinValue_ReturnsUnderflowedPriority()
        {
            // Arrange
            string currentRank = "00000|";

            // Act
            string lowerPriority = _lexoRankService.GetLowerPriority(currentRank);

            // Assert
            Assert.That(lowerPriority, Is.EqualTo("0000/|"));
        }
    }
}
