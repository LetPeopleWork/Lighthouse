using Lighthouse.Services.Interfaces;

namespace Lighthouse.Services.Implementation
{

    public class RandomNumberService : IRandomNumberService
    {
        public int GetRandomNumber(int maxValue)
        {
            return new Random().Next(maxValue);
        }
    }
}
