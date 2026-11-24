namespace Lighthouse.Backend.Platform.Logging
{
    public interface IConfigFileUpdater
    {
        void UpdateConfigFile<T>(string key, T value) where T : class;
    }
}
