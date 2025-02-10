using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{

    public class RandomNumberService : IRandomNumberService
    {
        public int GetRandomNumber(int maxValue)
        {
            return new Random().Next(maxValue);
        }
    }
}
