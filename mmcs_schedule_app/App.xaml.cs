using API;
using Newtonsoft.Json;
using System.Text;

namespace mmcs_schedule_app;

public partial class App
{
    public const string Host = "https://schedule.sfedu.ru";

    public static User User { get; set; } = new();

    public static bool IsLoggedIn { get; set; }

    public static readonly string FileName = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "settings.json");

    public App()
    {
        InitializeComponent();

        if (File.Exists(FileName)
            && JsonConvert.DeserializeObject<User>(File.ReadAllText(FileName, Encoding.UTF8))
                is { } loggedInUser)
        {
            IsLoggedIn = true;
            User = loggedInUser;
        }
    }

    private static NavigationPage GetStartPage()
    {
        if (!IsLoggedIn)
        {
            return new NavigationPage(new MainPage());
        }

        string title = mmcs_schedule_app.MainPage.UserTitle(User);
        return new NavigationPage(new ScheduleView(User.Info, User.OwnerId, title));
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(GetStartPage());
    }
}
