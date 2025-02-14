namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IConfigFileUpdater
    {
        void UpdateConfigFile<T>(string key, T value) where T : class;
    }
}
