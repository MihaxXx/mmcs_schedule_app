namespace mmcs_schedule_app
{
    public partial class StatusBanner : ContentView
    {
        private static readonly (Color Background, Color Text) _freshColors = (Color.FromArgb("#dff0d8"), Color.FromArgb("#3c763d"));
        private static readonly (Color Background, Color Text) _agingColors = (Color.FromArgb("#fcf8e3"), Color.FromArgb("#8a6d3b"));
        private static readonly (Color Background, Color Text) _errorColors = (Color.FromArgb("#f2dede"), Color.FromArgb("#a94442"));

        private bool canRetry;

        public event EventHandler? RetryClicked;

        public StatusBanner()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Shows the offline data banner colored by data age: green under a day, yellow under a week, red otherwise.
        /// Hides the banner if all data is fresh.
        /// </summary>
        public void ShowStale(SchRequests.FetchInfo info, bool canRetry)
        {
            if (info.StaleSince is not { } cachedAt)
            {
                Hide();
                return;
            }

            TimeSpan age = DateTime.UtcNow - cachedAt;
            Show(info.StaleMessage, age.TotalDays switch
            {
                < 1 => _freshColors,
                < 7 => _agingColors,
                _ => _errorColors
            }, canRetry);
        }

        public void ShowError(bool canRetry)
        {
            Show("Ошибка: Нет подключения", _errorColors, canRetry);
        }

        public void Hide()
        {
            IsVisible = false;
        }

        private void Show(string? message, (Color Background, Color Text) colors, bool canRetry)
        {
            MessageLabel.Text = message;
            BannerGrid.BackgroundColor = colors.Background;
            MessageLabel.TextColor = RetryIcon.TextColor = colors.Text;
            this.canRetry = RetryIcon.IsVisible = canRetry;
            IsVisible = true;
        }

        private void BannerTapped(object? sender, TappedEventArgs e)
        {
            if (canRetry)
            {
                RetryClicked?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
