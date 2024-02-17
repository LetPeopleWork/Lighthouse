using CMFTAspNet.Services.Interfaces;

namespace CMFTAspNet.Services.Implementation
{

    public class RandomNumberService : IRandomNumberService
    {
        public int GetRandomNumber(int maxValue)
        {
            return new Random().Next(maxValue);
        }
    }
}
