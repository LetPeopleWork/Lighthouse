namespace Lighthouse.Backend.Services.Implementation.BackgroundServices.Update
{
    public class UpdateStatus
    {
        public UpdateType UpdateType { get; set; }

        public int Id { get; set; }

        public UpdateProgress Status { get; set; } = UpdateProgress.Queued;
    }
}