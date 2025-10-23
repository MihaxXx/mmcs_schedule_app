using Newtonsoft.Json;
using System.Text;

namespace mmcs_schedule_app
{
    public partial class App : Application
    {
        public const string host = "http://schedule.sfedu.ru";

        public static API.User user;

        public static bool _isLoggedIn;

        readonly public static string _fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "settings.json");

        public App()
        {
            InitializeComponent();

            if (File.Exists(_fileName) && JsonConvert.DeserializeObject<API.User>(File.ReadAllText(_fileName, Encoding.UTF8)) is { } loggedInUser)
            {
                _isLoggedIn = true;
                user = loggedInUser;
            }
        }

        internal static Page GetStartPage()
        {
            NavigationPage page;

            if (_isLoggedIn)
            {
                page = new NavigationPage(new ScheduleView());
            }
            else
            {
                page = new NavigationPage(new MainPage());
            }

            if (OperatingSystem.IsAndroid())
            {
                page.BarBackgroundColor = (Color)Current.Resources["Tertiary"];
            }

            return page;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(GetStartPage());
        }
    }
}
