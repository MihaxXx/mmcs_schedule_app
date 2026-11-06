namespace mmcs_schedule_app
{
    public static class FetchInfoExtensions
    {
        /// <summary>
        /// Shows the offline data banner colored by data age: green under a day, yellow under a week, red otherwise.
        /// Hides the label if all data is fresh.
        /// </summary>
        public static void ShowOn(this SchRequests.FetchInfo info, Label label)
        {
            label.Text = info.StaleMessage;
            label.IsVisible = info.StaleSince is not null;
            if (info.StaleSince is not { } cachedAt)
            {
                return;
            }

            TimeSpan age = DateTime.UtcNow - cachedAt;
            (label.BackgroundColor, label.TextColor) = age.TotalDays switch
            {
                < 1 => (Color.FromArgb("#dff0d8"), Color.FromArgb("#3c763d")),
                < 7 => (Color.FromArgb("#fcf8e3"), Color.FromArgb("#8a6d3b")),
                _ => (Color.FromArgb("#f2dede"), Color.FromArgb("#a94442"))
            };
        }
    }
}
