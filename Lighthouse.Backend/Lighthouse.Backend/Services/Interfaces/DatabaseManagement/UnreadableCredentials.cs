namespace Lighthouse.Backend.Services.Interfaces.DatabaseManagement
{
    // What a restore does with a backup whose stored credentials this installation's key reads none of: stop, and
    // change nothing, or restore it and give up those credentials so they are entered again.
    public enum UnreadableCredentials
    {
        StopRestore,
        Remove,
    }
}
