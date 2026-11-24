namespace Lighthouse.Backend.Platform.Host
{
    public interface IFileSystemService
    {
        bool FileExists(string path);

        string ReadAllText(string path);

        void WriteAllText(string path, string contents);

        string[] GetFiles(string path, string searchPattern);

        Stream OpenFile(string path, FileMode mode, FileAccess access, FileShare share);
    }
}
