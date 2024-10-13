using Lighthouse.Backend.Services.Interfaces;
using Newtonsoft.Json.Linq;

namespace Lighthouse.Backend.Services.Implementation
{
    public class ConfigFileUpdater : IConfigFileUpdater
    {
        private readonly string configFilePath;
        private readonly string configFileDebugPath;
        private readonly IFileSystemService fileSystem;

        public ConfigFileUpdater(IFileSystemService fileSystem)
        {
            this.fileSystem = fileSystem;
            configFilePath = "appsettings.json";
            configFileDebugPath = "appsettings.Development.json";
        }

        public void UpdateConfigFile<T>(string key, T value) where T : class
        {
            if (value == null)
            {
                return;
            }

            var filePath = string.Empty;
            if (fileSystem.FileExists(configFileDebugPath))
            {
                filePath = configFileDebugPath;
            }
            else if (fileSystem.FileExists(configFilePath))
            {
                filePath = configFilePath;
            }
            else
            {
                throw new FileNotFoundException("File {file} not found", filePath);
            }

            var json = fileSystem.ReadAllText(filePath);
            var jObject = JObject.Parse(json);

            var sections = key.Split(':');
            var section = jObject;
            for (int i = 0; i < sections.Length - 1; i++)
            {
                section = section[sections[i]] as JObject;
                if (section == null)
                {
                    throw new InvalidOperationException($"Section '{string.Join(":", sections.Take(i + 1))}' not found in config file.");
                }
            }

            var lastSection = sections[sections.Length - 1];
            section[lastSection] = JToken.FromObject(value);
            fileSystem.WriteAllText(filePath, jObject.ToString());
        }
    }
}