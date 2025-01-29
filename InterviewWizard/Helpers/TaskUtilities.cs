namespace InterviewWizard.Helpers
{
    public static class TaskUtilities
    {
        public static void FireAndForgetSafeAsync(Task task)
        {
            _ = task.ContinueWith(t =>
            {
                var exception = t.Exception;
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
}
