using System.Data.Common;

namespace Lighthouse.Backend.Platform.DatabaseManagement
{
    // A backup laid out where it can be read like a database, apart from the live one, so its content can be
    // looked at before anything is restored. Disposing it removes whatever the provider had to create for it.
    public interface IStagedBackup : IAsyncDisposable
    {
        // Returns a connection that is not open yet, so the caller decides when to open it and owns closing it.
        DbConnection OpenConnection();
    }
}
