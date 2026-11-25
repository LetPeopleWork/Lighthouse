namespace Lighthouse.Backend.Tests.TestHelpers
{
    public static class TaskFinishing
    {
        /// <summary>
        /// Says whether the task has finished, in any way, within the time. Unlike a race against
        /// <c>Task.Delay</c> it leaves no timer running once the task is done, and it never throws
        /// for the task's own failure or cancellation: that is for the caller to observe by awaiting.
        /// </summary>
        public static async Task<bool> FinishesWithinAsync(this Task task, TimeSpan timeout)
        {
            await Task.WhenAny(task.WaitAsync(timeout));

            return task.IsCompleted;
        }

        public static Task<bool> FinishesWithinAsync(this Task task, int milliseconds) =>
            task.FinishesWithinAsync(TimeSpan.FromMilliseconds(milliseconds));
    }
}
