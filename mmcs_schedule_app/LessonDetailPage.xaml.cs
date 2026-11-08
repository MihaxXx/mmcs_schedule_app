using API;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace mmcs_schedule_app;

public class LessonItemInfo
{
    public string MainText { get; set; }
    public string SubText { get; set; }

    /// <summary>
    /// Run when the main (sub) text is tapped; null when there is nothing to open
    /// </summary>
    public Func<Task>? MainAction { get; }
    public Func<Task>? SubAction { get; }
    public bool HasSubText => !string.IsNullOrEmpty(SubText);

    public LessonItemInfo(string mainText, string subText, Func<Task>? mainAction,
        Func<Task>? subAction = null)
    {
        MainText = mainText;
        SubText = subText;
        MainAction = mainAction;
        SubAction = subAction;
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

    public ObservableCollection<LessonItemInfo> Links { get; set; } = [];

    public ObservableCollection<LessonItemInfo> Items { get; set; } = [];

    public ICommand ItemTappedCommand { get; }

    private LessonDetailPage(INavigation parentNav)
    {
        InitializeComponent();

        parentNavigation = parentNav;

        ItemTappedCommand = new Command<Func<Task>?>(action =>
        {
            if (action != null)
            {
                _ = this.RunSafelyAsync(action);
            }
        });
    }

    // Constructor for student schedule (shows teachers)
    public LessonDetailPage(string disciplineName, TimeOfLesson timeslot, string lessonInfo,
        List<Curriculum> curricula, INavigation parentNav) : this(parentNav)
    {
        SetupCommonInfo(disciplineName, timeslot, lessonInfo);

        ListLabel = "Преподаватели:";

        foreach (Curriculum curriculum in curricula)
        {
            Items.Add(new LessonItemInfo(
                curriculum.TeacherName,
                $"ауд. {curriculum.RoomName}",
                OpenTeacher(curriculum),
                OpenRoom(curriculum)
            ));
        }

        BindingContext = this;
    }

    // Constructor for teacher and room schedules (shows groups, links lead to the rooms or the teachers)
    public LessonDetailPage(string disciplineName, TimeOfLesson timeslot, string lessonInfo,
        List<TechGroup> groups, List<Curriculum> curricula, bool forRoom, INavigation parentNav)
        : this(parentNav)
    {
        SetupCommonInfo(disciplineName, timeslot, lessonInfo);

        ListLabel = "Группы:";

        if (forRoom)
        {
            foreach (Curriculum curriculum in curricula
                .Where(c => !string.IsNullOrWhiteSpace(c.TeacherName)).DistinctBy(c => c.TeacherId))
            {
                Links.Add(new LessonItemInfo(curriculum.TeacherName, "", OpenTeacher(curriculum)));
            }
        }
        else
        {
            foreach (Curriculum curriculum in curricula
                .Where(c => !string.IsNullOrWhiteSpace(c.RoomName)).DistinctBy(c => c.RoomId))
            {
                Links.Add(new LessonItemInfo("Аудитория: " + curriculum.RoomName, "", OpenRoom(curriculum)));
            }
        }

        foreach (TechGroup techGroup in groups)
        {
            string groupDisplay = $"{MainPage.StuDegreeShort(techGroup.Degree)} {techGroup.Name} " +
                $"{techGroup.GradeNumber}.{techGroup.GroupNumber}";
            Items.Add(new LessonItemInfo(
                groupDisplay,
                "", // No room info per item
                () => OnGroupTapped(techGroup)
            ));
        }

        BindingContext = this;
    }

    /// <returns>Null if there is no teacher name, as there is no teacher to open</returns>
    private Func<Task>? OpenTeacher(Curriculum curriculum)
    {
        return string.IsNullOrWhiteSpace(curriculum.TeacherName)
            ? null
            : () => OpenScheduleAsync(User.UserInfo.Teacher, curriculum.TeacherId, curriculum.TeacherName);
    }

    /// <returns>Null if there is no room name, as there is no room to open</returns>
    private Func<Task>? OpenRoom(Curriculum curriculum)
    {
        return string.IsNullOrWhiteSpace(curriculum.RoomName)
            ? null
            : () => OpenScheduleAsync(User.UserInfo.Room, curriculum.RoomId, "Ауд. " + curriculum.RoomName);
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

    private async Task OpenScheduleAsync(User.UserInfo info, int id, string header)
    {
        await ClosePopup();

        await parentNavigation.PushAsync(new ScheduleView(info, id, header, true));
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
