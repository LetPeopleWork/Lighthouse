using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace Lighthouse.Backend.Services.Implementation
{
    public class LighthouseReleaseService : ILighthouseReleaseService
    {
        private readonly IHostEnvironment hostEnvironment;
        private readonly IGitHubService gitHubService;
        private readonly IAssemblyService assemblyService;
        private readonly ILogger<LighthouseReleaseService> logger;
        private readonly HttpClient httpClient;

        public LighthouseReleaseService(
            IHostEnvironment hostEnvironment, IGitHubService gitHubService, IAssemblyService assemblyService, ILogger<LighthouseReleaseService> logger)
        {
            this.hostEnvironment = hostEnvironment;
            this.gitHubService = gitHubService;
            this.assemblyService = assemblyService;
            this.logger = logger;
            
            httpClient = new HttpClient();
        }

        public string GetCurrentVersion()
        {
            if (hostEnvironment.IsDevelopment())
            {
                return "DEV";
            }

            var version = assemblyService.GetAssemblyVersion();

            if (version.EndsWith(".0"))
            {
                // Remove the trailing ".0" if it exists
                version = version[..^2];
            }

            return $"v{version}";
        }

        public async Task<bool> UpdateAvailable()
        {
            var currentRelease = GetCurrentVersion();

            if (!currentRelease.StartsWith('v'))
            {
                return true;
            }

            var latestRelease = await GetLatestReleaseTag();
            if (!latestRelease.StartsWith('v'))
            {
                return false;
            }

            var currentReleaseVersion = new Version(currentRelease[1..]);
            var latestReleaseVersion = new Version(latestRelease[1..]);

            return latestReleaseVersion > currentReleaseVersion;
        }

        public async Task<IEnumerable<LighthouseRelease>> GetNewReleases()
        {
            var newReleases = new List<LighthouseRelease>();

            var allReleases = await gitHubService.GetAllReleases();
            var currentVersion = GetCurrentVersion();

            foreach (var release in allReleases)
            {
                if (release.Version == currentVersion)
                {
                    break;
                }

                newReleases.Add(release);
            }

            return newReleases;
        }

        private async Task<string> GetLatestReleaseTag()
        {
            return await gitHubService.GetLatestReleaseVersion();
        }

        public bool IsUpdateSupported()
        {
            if (IsRunningInDocker())
            {
                return false;
            }

            return true;
        }

        public async Task<bool> InstallUpdate()
        {
            logger.LogDebug("Trying to install update...");
            if (!IsUpdateSupported())
            {
                return false;
            }

            try
            {
                logger.LogInformation("Installing latest update...");

                var latestRelease = await GetLatestRelease();
                var operatingSystem = GetOperatingSystemIdentifier();
                var releaseAsset = GetLatestReleaseAssetForOperatingSystem(latestRelease, operatingSystem);

                if (releaseAsset == null)
                {
                    logger.LogError("Could not find a compatible release asset for the current platform: {OperatingSystem}", operatingSystem);
                    return false;
                }

                logger.LogInformation("Download asset for release {ReleaseVersion} for {OperatingSystem}", latestRelease.Version.ToString(), operatingSystem);

                var tempDir = CreateTempDirectoryForUpdate();

                var assetPath = await DownloadAssetToTempDirectory(releaseAsset, tempDir);
                if (string.IsNullOrEmpty(assetPath))
                {
                    return false;
                }

                var extractPath = ExtractAsset(releaseAsset, tempDir, assetPath);

                (var currentProcess, var currentProcessPath, var currentProcessDir) = GetProcessInformation();

                if (string.IsNullOrEmpty(currentProcessDir))
                {
                    logger.LogError("Could not determine current process directory");
                    return false;
                }

                var args = GetCommandLineArguments();

                logger.LogDebug("Current process path: {Path}, args: {Args}", currentProcessPath, args);

                // Create update script based on OS
                var updateScriptPath = CreateUpdateScript(
                    operatingSystem,
                    extractPath,
                    currentProcessDir,
                    currentProcess.Id,
                    currentProcessPath,
                    args);

                if (string.IsNullOrEmpty(updateScriptPath))
                {
                    logger.LogError("Failed to create update script");
                    return false;
                }

                logger.LogInformation("Update script created: {path}", updateScriptPath);

                _ = Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    ExecuteUpdateScript(updateScriptPath, operatingSystem);
                });

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error installing update");
                return false;
            }
        }

        private static (Process currentProcess, string currentProcessPath, string currentProcessDir) GetProcessInformation()
        {
            var currentProcess = Process.GetCurrentProcess();
            var currentProcessPath = currentProcess.MainModule?.FileName ?? string.Empty;
            var currentProcessDir = Path.GetDirectoryName(currentProcessPath) ?? string.Empty;

            return (currentProcess, currentProcessPath, currentProcessDir);
        }

        private static string GetCommandLineArguments()
        {
            var commandLineArgs = Environment.GetCommandLineArgs();
            var args = commandLineArgs.Length > 1 ?
                string.Join(" ", commandLineArgs.Skip(1).Select(arg => $"\"{arg}\"")) :
                string.Empty;
            return args;
        }

        private string ExtractAsset(LighthouseReleaseAsset releaseAsset, string tempDir, string assetPath)
        {
            var extractPath = Path.Combine(tempDir, "extracted");
            Directory.CreateDirectory(extractPath);

            if (releaseAsset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogDebug("Extracting zip file to {ExtractPath}", extractPath);
                ZipFile.ExtractToDirectory(assetPath, extractPath, true);
                logger.LogInformation("Extraction completed to {ExtractPath}", extractPath);
            }
            else
            {
                // Copy the asset directly if it's not a zip (like an executable)
                logger.LogDebug("Asset is not a zip file, copying directly to {ExtractPath}", extractPath);
                File.Copy(assetPath, Path.Combine(extractPath, releaseAsset.Name), true);
            }

            return extractPath;
        }

        private async Task<string> DownloadAssetToTempDirectory(LighthouseReleaseAsset releaseAsset, string tempDir)
        {
            var assetPath = Path.Combine(tempDir, releaseAsset.Name);
            logger.LogDebug("Downloading asset {AssetName} from {Link} to {Path}", releaseAsset.Name, releaseAsset.Link, assetPath);

            using (var response = await httpClient.GetAsync(releaseAsset.Link))
            {
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogError("Failed to download asset: {StatusCode}", response.StatusCode);
                    return string.Empty;
                }

                using (var fileStream = new FileStream(assetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await response.Content.CopyToAsync(fileStream);
                }
            }

            logger.LogInformation("Asset downloaded successfully to {Path}", assetPath);
            return assetPath;
        }

        private string CreateTempDirectoryForUpdate()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), $"lighthouse_update_{Guid.NewGuid()}");
            Directory.CreateDirectory(tempDir);
            logger.LogDebug("Created temporary directory for update: {TempDir}", tempDir);
            return tempDir;
        }

        private async Task<LighthouseRelease> GetLatestRelease()
        {
            var allReleases = await gitHubService.GetAllReleases();
            var latestRelease = allReleases.First();
            return latestRelease;
        }

        private static LighthouseReleaseAsset? GetLatestReleaseAssetForOperatingSystem(LighthouseRelease latestRelease, string operatingSystem)
        {
            if (operatingSystem != "osx")
            {
                return latestRelease.Assets.SingleOrDefault(a => a.Name.Contains(operatingSystem, StringComparison.OrdinalIgnoreCase));
            }

            var (currentProcess, currentProcessPath, currentProcessDir) = GetProcessInformation();
            var isAppBundle = currentProcessPath.Contains(".app/Contents/MacOS/", StringComparison.OrdinalIgnoreCase);

            if (isAppBundle)
            {
                var appAsset = latestRelease.Assets.SingleOrDefault(a => a.Name.Contains("osx-x64-app", StringComparison.OrdinalIgnoreCase));
                if (appAsset != null)
                {
                    return appAsset;
                }

                return latestRelease.Assets.SingleOrDefault(a => 
                    a.Name.Contains("osx", StringComparison.OrdinalIgnoreCase) && 
                    !a.Name.Contains("-app", StringComparison.OrdinalIgnoreCase));
            }

            return latestRelease.Assets.SingleOrDefault(a => 
                a.Name.Contains("osx", StringComparison.OrdinalIgnoreCase) && 
                !a.Name.Contains("-app", StringComparison.OrdinalIgnoreCase));
        }

        private static string GetOperatingSystemIdentifier()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                                 RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                                 RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" :
                                 throw new PlatformNotSupportedException("Current OS platform is not supported for updates");
        }

        private string CreateUpdateScript(string operatingSystem, string sourcePath, string destinationPath, int processId, string executablePath, string arguments)
        {
            try
            {
                string scriptPath;
                string scriptContent;

                if (operatingSystem == "win")
                {
                    scriptPath = Path.Combine(Path.GetTempPath(), $"lighthouse_update_{Guid.NewGuid()}.bat");
                    scriptContent = $@"@echo off
echo Waiting for process {processId} to exit...
:checkProcess
timeout /t 1 /nobreak > NUL
tasklist /FI ""PID eq {processId}"" 2>NUL | find ""{processId}"" > NUL
if %ERRORLEVEL% == 0 goto checkProcess

echo Process has exited. Installing update...
xcopy ""{sourcePath}\*.*"" ""{destinationPath}"" /E /I /Y
if %ERRORLEVEL% neq 0 (
    echo Failed to copy files. Update aborted.
    exit /b 1
)

echo Update installed successfully, restarting application...
start """" ""{executablePath}"" {arguments}
exit";
                }
                else // Linux/MacOS
                {
                    scriptPath = Path.Combine(Path.GetTempPath(), $"lighthouse_update_{Guid.NewGuid()}.sh");
                    
                    // Enhanced script that handles both regular installs and macOS .app bundles
                    scriptContent = $@"#!/bin/bash
set -e

EXECUTABLE=""{executablePath}""
SOURCE=""{sourcePath}""
DEST=""{destinationPath}""
ARGS=""{arguments}""

echo ""Waiting for process {processId} to exit...""
while ps -p {processId} > /dev/null 2>&1; do
    sleep 1
done

echo ""Process has exited. Installing update...""

# Check if the executable is running from inside a macOS .app bundle
if [[ ""$EXECUTABLE"" == *"".app/Contents/MacOS/""* ]]; then
    echo ""Detected macOS .app bundle installation""
    
    # Extract the .app bundle root path (e.g., /path/to/Lighthouse.app)
    APP_ROOT=$(echo ""$EXECUTABLE"" | sed -E 's|(.*\.app)/Contents/MacOS/.*|\1|')
    APP_PARENT=$(dirname ""$APP_ROOT"")
    APP_NAME=$(basename ""$APP_ROOT"")
    
    echo ""App bundle: $APP_ROOT""
    echo ""App parent directory: $APP_PARENT""
    
    # Check if the downloaded update contains a .app bundle
    NEW_APP=$(find ""$SOURCE"" -maxdepth 1 -type d -name '*.app' -print -quit 2>/dev/null || true)
    
    if [[ -n ""$NEW_APP"" && -d ""$NEW_APP"" ]]; then
        echo ""Found new .app bundle in update: $NEW_APP""
        
        # Remove the old .app bundle and replace with the new one
        echo ""Removing old app bundle: $APP_ROOT""
        rm -rf ""$APP_ROOT""
        
        echo ""Installing new app bundle to: $APP_PARENT/""
        mv ""$NEW_APP"" ""$APP_PARENT/""
        
        # Ensure the executable is runnable
        chmod +x ""$EXECUTABLE""
        
        echo ""App bundle updated successfully""
    else
        echo ""No .app bundle found in update, updating files inside existing bundle...""
        
        # Fallback: copy files directly into the MacOS directory (preserving bundle structure)
        MACOS_DIR=""$APP_ROOT/Contents/MacOS""
        cp -R ""$SOURCE/""* ""$MACOS_DIR/""
        
        if [ $? -ne 0 ]; then
            echo ""Failed to copy files. Update aborted.""
            exit 1
        fi
        
        chmod +x ""$EXECUTABLE""
        echo ""Bundle contents updated successfully""
    fi
else
    echo ""Standard installation (not an .app bundle)""
    
    # Standard update: copy all files to destination
    cp -R ""$SOURCE/""* ""$DEST/""
    
    if [ $? -ne 0 ]; then
        echo ""Failed to copy files. Update aborted.""
        exit 1
    fi
    
    chmod +x ""$EXECUTABLE""
    echo ""Files updated successfully""
fi

echo ""Update installed successfully, restarting application...""
""$EXECUTABLE"" $ARGS &
exit 0";

                    // Make the script executable on Linux/MacOS
                    File.WriteAllText(scriptPath, scriptContent);
                    var makeExecutableProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "chmod",
                            Arguments = $"+x \"{scriptPath}\"",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        }
                    };
                    makeExecutableProcess.Start();
                    makeExecutableProcess.WaitForExit();
                    return scriptPath;
                }

                File.WriteAllText(scriptPath, scriptContent);
                return scriptPath;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating update script");
                return string.Empty;
            }
        }

        private void ExecuteUpdateScript(string scriptPath, string operatingSystem)
        {
            try
            {
                ProcessStartInfo startInfo;

                if (operatingSystem == "win")
                {
                    startInfo = new ProcessStartInfo
                    {
                        FileName = scriptPath,
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                }
                else // Linux/MacOS
                {
                    startInfo = new ProcessStartInfo
                    {
                        FileName = "/bin/bash",
                        Arguments = $"\"{scriptPath}\"",
                        UseShellExecute = true,
                        CreateNoWindow = false
                    };
                }

                logger.LogInformation("Executing update script: {Script}", scriptPath);
                Process.Start(startInfo);

                // Exit the application to allow the update script to work
                logger.LogInformation("Exiting application for update installation...");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error executing update script");
            }
        }

        private static bool IsRunningInDocker()
        {
            // Check for common Docker environment indicators
            return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" ||
                   File.Exists("/.dockerenv") ||
                   Environment.GetEnvironmentVariable("LIGHTHOUSE_DOCKER") == "true";
        }
    }
}
