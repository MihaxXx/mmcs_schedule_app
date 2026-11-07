using API;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace mmcs_schedule_app;

public class LessonItemInfo
{
    public string MainText { get; set; }
    public string SubText { get; set; }
    public object ItemData { get; set; }
    public bool HasSubText => !string.IsNullOrEmpty(SubText);

    public LessonItemInfo(string mainText, string subText, object itemData)
    {
        MainText = mainText;
        SubText = subText;
        ItemData = itemData;
    }
}

public partial class LessonDetailPage
{
    private readonly INavigation parentNavigation;

    public string DisciplineName { get; set; } = "";
    public string Weekday { get; set; } = "";
    public string Timeslot { get; set; } = "";
    public string LessonInfo { get; set; } = "";
    public string WeekType { get; set; } = "";
    public string ListLabel { get; set; } = "";
    public string RoomInfo { get; set; } = "";

    public ObservableCollection<LessonItemInfo> Items { get; set; } = [];

    public ICommand ItemTappedCommand { get; }

    // Constructor for student schedule (shows teachers)
    public LessonDetailPage(string disciplineName, TimeOfLesson timeslot, string lessonInfo,
        List<Curriculum> curricula, INavigation parentNav)
    {
        InitializeComponent();

        parentNavigation = parentNav;

        SetupCommonInfo(disciplineName, timeslot, lessonInfo);

        ListLabel = "Преподаватели:";

        foreach (Curriculum curriculum in curricula)
        {
            Items.Add(new LessonItemInfo(
                curriculum.TeacherName,
                $"ауд. {curriculum.RoomName}",
                curriculum.TeacherId
            ));
        }

        ItemTappedCommand = new Command<LessonItemInfo>(
            item => _ = this.RunSafelyAsync(() => OnTeacherTapped((int)item.ItemData)));

        BindingContext = this;
    }

    // Constructor for teacher schedule (shows groups)
    public LessonDetailPage(string disciplineName, TimeOfLesson timeslot, string lessonInfo,
        List<TechGroup> groups, string roomName, INavigation parentNav)
    {
        InitializeComponent();

        parentNavigation = parentNav;

        SetupCommonInfo(disciplineName, timeslot, lessonInfo);

        ListLabel = "Группы:";

        RoomInfo = $"\nАудитория: {roomName}";

        foreach (TechGroup techGroup in groups)
        {
            string groupDisplay = $"{MainPage.StuDegreeShort(techGroup.Degree)} {techGroup.Name} " +
                $"{techGroup.GradeNumber}.{techGroup.GroupNumber}";
            Items.Add(new LessonItemInfo(
                groupDisplay,
                "", // No room info per item
                techGroup
            ));
        }

        ItemTappedCommand = new Command<LessonItemInfo>(
            item => _ = this.RunSafelyAsync(() => OnGroupTapped((TechGroup)item.ItemData)));

        BindingContext = this;
    }

    private void SetupCommonInfo(string disciplineName, TimeOfLesson timeslot, string lessonInfo)
    {
        DisciplineName = disciplineName;
        Title = disciplineName;

        Weekday = "\n" + ScheduleView.GetDayName(timeslot.Day);

        Timeslot = "\n" + $"{timeslot.StartHour:D2}:{timeslot.StartMinute:D2}"
            + $" - {timeslot.FinishHour:D2}:{timeslot.FinishMinute:D2}";

        LessonInfo = string.IsNullOrEmpty(lessonInfo) ? "" : "\n" + lessonInfo;

        WeekType = timeslot.Week == -1 ? "" : "\n" + timeslot.WeekName;
    }

    private async Task OnTeacherTapped(int teacherId)
    {
        Teacher? teacher;
        try
        {
            teacher = (await MainPage.GetTeachersAsync()).FirstOrDefault(t => t.Id == teacherId);
        }
        catch (SchRequests.ScheduleUnavailableException)
        {
            await ShowUnavailableAlert();
            return;
        }
        if (teacher == null)
        {
            return;
        }

        await ClosePopup();

        ScheduleView scheduleView = new(User.UserInfo.Teacher, teacherId, teacher.Name, true);
        await parentNavigation.PushAsync(scheduleView);
    }

    private async Task OnGroupTapped(TechGroup techGroup)
    {
        Group? group;
        try
        {
            Grade? grade = (await MainPage.GetGradesAsync())
                .FirstOrDefault(g => g.Number == techGroup.GradeNumber && g.Degree == techGroup.Degree);
            if (grade == null)
            {
                return;
            }

            group = (await MainPage.GetGroupsAsync(grade.Id))
                .FirstOrDefault(g => g.Number == techGroup.GroupNumber && g.Name == techGroup.Name);
        }
        catch (SchRequests.ScheduleUnavailableException)
        {
            await ShowUnavailableAlert();
            return;
        }
        if (group == null)
        {
            return;
        }

        await ClosePopup();

        User.UserInfo userInfo = techGroup.Degree switch
        {
            "bachelor" => User.UserInfo.Bachelor,
            "master" => User.UserInfo.Master,
            "specialist" => User.UserInfo.Bachelor,
            "postgraduate" => User.UserInfo.Graduate,
            _ => User.UserInfo.Bachelor
        };

        string header = $"{MainPage.StuDegreeShort(techGroup.Degree)} {techGroup.Name} " +
            $"{techGroup.GradeNumber}.{techGroup.GroupNumber}";

        ScheduleView scheduleView = new(userInfo, group.Id, header, true);
        await parentNavigation.PushAsync(scheduleView);
    }

    private async Task ShowUnavailableAlert()
    {
        await DisplayAlertAsync("Ошибка", "Нет подключения", "OK");
    }

    private async Task ClosePopup()
    {
        if (Navigation.ModalStack.Count > 0)
        {
            await Navigation.PopModalAsync();
        }
    }

    private void ClosePopup(object sender, EventArgs e) => _ = this.RunSafelyAsync(ClosePopup);
}
