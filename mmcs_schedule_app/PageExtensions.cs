using System.Diagnostics;

namespace mmcs_schedule_app;

public static class PageExtensions
{
    /// <summary>
    /// Runs the async work of a UI handler without letting an exception escape to the UI thread, where it
    /// would crash the app: an unexpected exception is logged and reported to the user in an alert.
    /// Never throws.
    /// </summary>
    public static async Task RunSafelyAsync(this Page page, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            try
            {
                await page.DisplayAlertAsync("Ошибка", "Произошла непредвиденная ошибка", "OK");
            }
            catch (Exception alertException)
            {
                Debug.WriteLine(alertException);
            }
        }
    }
}
