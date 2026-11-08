using API;
using System.Collections.ObjectModel;

namespace mmcs_schedule_app;

public class LessonItem
{
    public string Time { get; set; }
    public string Name { get; set; }
    public string Notes { get; set; }
    public string Room { get; set; }
    public string Who { get; set; }
    public TimeOfLesson Timeslot { get; set; }
    public (Lesson, List<Curriculum>, List<TechGroup>) TeacherData { get; }
    public (Lesson, List<Curriculum>) StudentData { get; }

    public LessonItem(string time, string name, string notes, string room, string who,
        TimeOfLesson timeslot, (Lesson, List<Curriculum>, List<TechGroup>) teacherData)
    {
        Time = time;
        Name = name;
        Notes = notes;
        Room = room;
        Who = who;
        Timeslot = timeslot;
        TeacherData = teacherData;
    }
    public LessonItem(string time, string name, string notes, string room, string who,
        TimeOfLesson timeslot, (Lesson, List<Curriculum>) studentData)
    {
        Time = time;
        Name = name;
        Notes = notes;
        Room = room;
        Who = who;
        Timeslot = timeslot;
        StudentData = studentData;
    }
}
public partial class ScheduleView
{
    private Week currentWeek = new();

    private readonly Week selectedWeek = new() { Type = WeekType.Current };

    private List<LessonItem> allLessons = [];

    public ObservableCollection<IGrouping<string, LessonItem>> GroupedLessons { get; } = [];

    private readonly List<string> dayNames;

    private readonly User.UserInfo userInfo;
    private readonly int userId; // teacherId or groupid

    private bool isScheduleLoaded;
    private bool isLoading;

    // Static cache for day names
    private static List<string>? _cachedDayNames;

    public static string GetDayName(int dayIndex)
    {
        _cachedDayNames ??= [.. new System.Globalization.CultureInfo("ru-RU").DateTimeFormat.DayNames];
        return _cachedDayNames[(dayIndex + 1) % 7];
    }

    public ScheduleView(User.UserInfo info, int id, string header, bool isFromModal = false)
    {
        InitializeComponent();
        userInfo = info;
        userId = id;
        Title = header;

        if (isFromModal)
        {
            ToolbarItems.RemoveAt(1); // Remove Exit button
        }

        //Gets russian day names, possible to use CurrentInfo, but app has no localization,
        //so no reason for that
        dayNames = [.. new System.Globalization.CultureInfo("ru-RU").DateTimeFormat.DayNames];

        //Must be at the end!!!
        BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = this.RunSafelyAsync(LoadScheduleAsync);
    }

    private void Banner_RetryClicked(object? sender, EventArgs e)
    {
        _ = this.RunSafelyAsync(LoadScheduleAsync);
    }

    private async Task LoadScheduleAsync()
    {
        if (isScheduleLoaded || isLoading)
        {
            return;
        }

        isLoading = true;
        LoadingIndicator.IsRunning = LoadingIndicator.IsVisible = true;
        try
        {
            SchRequests.FetchInfo info = new();
            currentWeek = await WeekMethods.RequestCurrentWeekAsync(info);
            allLessons = userInfo switch
            {
                User.UserInfo.Teacher => ToLessonItems(
                    await TeacherMethods.RequestWeekScheduleAsync(userId, info), false),
                User.UserInfo.Room => ToLessonItems(
                    await RoomMethods.RequestWeekScheduleAsync(userId, info), true),
                _ => await RequestStudentLessonsAsync(info)
            };
            UpdateGroupedLessons();
            isScheduleLoaded = info.StaleSince is null;
            Banner.ShowStale(info, true);
        }
        catch (SchRequests.ScheduleUnavailableException)
        {
            Banner.ShowError(true);
        }
        finally
        {
            LoadingIndicator.IsRunning = LoadingIndicator.IsVisible = false;
            isLoading = false;
        }
    }

    private static List<LessonItem> ToLessonItems(
        List<(Lesson, List<Curriculum>, List<TechGroup>)> schedule, bool forRoom)
    {
        List<LessonItem> lessons = [];
        foreach ((Lesson, List<Curriculum>, List<TechGroup>) lessonData in schedule)
        {
            TimeOfLesson slot = TimeOfLesson.Parse(lessonData.Item1.Timeslot);
            string teachers = forRoom
                ? string.Join(", ", lessonData.Item2
                    .Select(c => c.TeacherName).Where(name => name != "").Distinct())
                : "";
            lessons.Add(new LessonItem(
                slot.ToString(),
                lessonData.Item2[0].SubjectName,
                (teachers.Length == 0 ? "" : teachers + "\n")
                    + (string.IsNullOrEmpty(lessonData.Item1.Info) ? "" : lessonData.Item1.Info + "\n")
                    + slot.WeekName,
                forRoom
                    ? string.Join("\n", lessonData.Item3.Select(
                        g => $"{MainPage.StuDegreeShort(g.Degree)} {g.GradeNumber}.{g.GroupNumber}"))
                    : lessonData.Item2.First().RoomName,
                string.Join("\n", lessonData.Item3.Select(
                    g => $"• {StuDegreeShort(g.Degree)} {g.GradeNumber}.{g.GroupNumber}")),
                slot,
                lessonData));
        }
        return lessons;
    }

    private async Task<List<LessonItem>> RequestStudentLessonsAsync(SchRequests.FetchInfo info)
    {
        List<LessonItem> lessons = [];
        //Go thought list of lessons (present timeslots for group)
        foreach ((Lesson, List<Curriculum>) lessonData
            in await StudentMethods.RequestWeekScheduleAsync(userId, info))
        {
            TimeOfLesson slot = TimeOfLesson.Parse(lessonData.Item1.Timeslot);
            //Go thought list of Curriculums (present subj for timeslot)
            foreach (Curriculum curriculum
                in lessonData.Item2.ToLookup(c => c.SubjectId).Select(bySubject => bySubject.First()))
            {
                List<Curriculum> subjectCurricula =
                    [.. lessonData.Item2.Where(c => c.SubjectId == curriculum.SubjectId)];

                lessons.Add(new LessonItem(
                    slot.ToString(),
                    curriculum.SubjectName,
                    (string.IsNullOrEmpty(lessonData.Item1.Info) ? "" : lessonData.Item1.Info + "\n")
                        + slot.WeekName,
                    subjectCurricula.Count > 2
                        ? "..."
                        : string.Join("\n", subjectCurricula.Select(c => c.RoomName)),
                    string.Join("\n", subjectCurricula.Select(c => $"• {c.TeacherName}\n  {c.RoomName}")),
                    slot,
                    (lessonData.Item1, subjectCurricula)));
            }
        }
        return lessons;
    }

    private void UpdateGroupedLessons()
    {
        GroupedLessons.Clear();

        foreach (IGrouping<string, LessonItem> dayLessons in allLessons
            .Where(l => IsOnSelectedWeek(l.Timeslot.Week))
            .OrderBy(l => l.Timeslot.Day)
            .ThenBy(l => l.Timeslot.StartHour * 60 + l.Timeslot.StartMinute)
            .ThenBy(l => l.Timeslot.Week)
            .GroupBy(l => dayNames[(l.Timeslot.Day + 1) % 7]))
        {
            GroupedLessons.Add(dayLessons);
        }
    }

    private bool IsOnSelectedWeek(int lessonWeek)
    {
        return selectedWeek.Type == WeekType.Full
            || lessonWeek == -1
            || (selectedWeek.Type == WeekType.Current && lessonWeek == (int)currentWeek.Type)
            || (selectedWeek.Type != WeekType.Current && lessonWeek == (int)selectedWeek.Type);
    }

    public static string StuDegreeShort(string degree)
    {
        return degree switch
        {
            "bachelor" => "бак.",
            "master" => "маг.",
            "specialist" => "спец.",
            "postgraduate" => "асп.",
            _ => "н/д"
        };
    }

    private void OnExitClicked(object sender, EventArgs e)
    {
        _ = this.RunSafelyAsync(async () =>
        {
            Navigation.InsertPageBefore(new MainPage(), this);
            await Navigation.PopAsync();
        });
    }

    private void OnChangeWeekClicked(object sender, EventArgs e)
    {
        _ = this.RunSafelyAsync(ChangeWeekAsync);
    }

    private async Task ChangeWeekAsync()
    {
        string[] filterOptions =
            ["Текущая неделя", "Только верхняя", "Только нижняя", "Полное расписание"];
        string action = await DisplayActionSheetAsync("Выберите тип недели", "Отмена", null,
            [.. filterOptions.Select(
                o => FilterTextToWeek(o) == selectedWeek.Type ? $"◉ {o}" : $"○ {o}")]);

        if (action != null && action != "Отмена")
        {
            selectedWeek.Type = FilterTextToWeek(action.TrimStart('○', '◉', ' '));

            UpdateGroupedLessons();
        }
    }

    private static WeekType FilterTextToWeek(string action)
    {
        return action switch
        {
            "Текущая неделя" => WeekType.Current,
            "Только верхняя" => WeekType.Upper,
            "Только нижняя" => WeekType.Lower,
            "Полное расписание" => WeekType.Full,
            _ => WeekType.Current
        };
    }

    private void OnListItemTapped(object sender, ItemTappedEventArgs e)
    {
        ((ListView)sender).SelectedItem = null;
        LessonItem item = (LessonItem)e.Item;
        _ = this.RunSafelyAsync(() => ShowLessonDetailAsync(item));
    }

    private async Task ShowLessonDetailAsync(LessonItem item)
    {

        LessonDetailPage detailPage;

        bool listsGroups = userInfo is User.UserInfo.Teacher or User.UserInfo.Room;
        if (listsGroups && item.TeacherData.Item1 != null)
        {
            detailPage = new LessonDetailPage(item.Name, item.Timeslot, item.TeacherData.Item1.Info,
                item.TeacherData.Item3, item.TeacherData.Item2, userInfo == User.UserInfo.Room, Navigation);
        }
        else if (!listsGroups && item.StudentData.Item1 != null)
        {
            detailPage = new LessonDetailPage(item.Name, item.Timeslot, item.StudentData.Item1.Info,
                item.StudentData.Item2, Navigation);
        }
        else
        {
            return;
        }

        await Navigation.PushModalAsync(detailPage);
    }
}
