using CMFTAspNet.Services;

namespace CMFTAspNet.Tests.TestDoubles
{
    internal class NotSoRandomNumberService : IRandomNumberService
    {
        internal static readonly int[] defaultRandomNumberSequence = [0];

        private int[] numbers = [];
        private int currentIndex = 0;

        public NotSoRandomNumberService()
        {
            InitializeRandomNumbers(defaultRandomNumberSequence);
        }

        public void InitializeRandomNumbers(int[] numbers)
        {
            this.numbers = numbers;
        }

        public int GetRandomNumber(int maxValue)
        {
            if (numbers.Length == 0)
            {
                return maxValue;
            }

            var nextNumber = numbers[currentIndex];
            currentIndex = (currentIndex + 1) % numbers.Length;
            return nextNumber;
        }
    }
}
