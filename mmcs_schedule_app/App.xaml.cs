using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.IO;
using Newtonsoft.Json;
using System.Text;

namespace mmcs_schedule_app
{
    public partial class App : Application
    {
        public const string host = "http://schedule.sfedu.ru/";

        public static API.User user;

        public static bool _isLoggedIn;

        readonly public static string _fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "settings.json");

        public App()
        {
            InitializeComponent();
            if (_isLoggedIn = File.Exists(_fileName))
            {
                user = JsonConvert.DeserializeObject<API.User>(File.ReadAllText(_fileName, Encoding.UTF8));
                MainPage = new NavigationPage(new ScheduleView());
            }
            else
                MainPage = new NavigationPage(new MainPage());
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
