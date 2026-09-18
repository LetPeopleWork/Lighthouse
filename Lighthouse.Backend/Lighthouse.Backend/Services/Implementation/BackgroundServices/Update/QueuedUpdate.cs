namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public sealed record QueuedUpdate(UpdateKey Key, Func<Task> Run);
}
