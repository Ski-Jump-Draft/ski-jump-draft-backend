using App.Application.Utility;

namespace App.Application.Extensions;

public static class TaskExtensions
{
    public static void FireAndForget(this Task task, IMyLogger logger)
    {
        _ = task.ContinueWith(t =>
        {
            if (t.Exception != null)
                logger.Error("Fire-and-forget task failed", t.Exception);
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
}