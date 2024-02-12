namespace CMFTAspNet.Services
{
    public interface IRandomNumberService
    {
        int GetRandomNumber(int maxValue);
    }

    public class RandomNumberService : IRandomNumberService
    {
        public int GetRandomNumber(int maxValue)
        {
            return new Random().Next(maxValue);
        }
    }
}
