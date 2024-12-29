namespace Lighthouse.Backend.Services.Implementation.Update
{
    public enum UpdateType
    {
        Team,
        Project,
        Forecasts,
    }

    public enum UpdateProgress
    {
        Queued,
        InProgress,
        Completed,
        Failed,
    }

    public class UpdateStatus
    {
        public UpdateType UpdateType { get; set; }

        public int Id { get; set; }

        public UpdateProgress Status { get; set; } = UpdateProgress.Queued;
    }
}