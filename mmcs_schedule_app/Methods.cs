namespace API;

/// <summary>
/// Time-slot struct
/// </summary>
public class TimeOfLesson
{
    //0..6 = пн..вс
    public int Day { get; set; }
    public int StartHour { get; set; }
    public int StartMinute { get; set; }
    public int FinishHour { get; set; }
    public int FinishMinute { get; set; }
    //"full" = -1,"upper"= 0, "lower"= 1
    public int Week { get; set; }

    public string WeekName => Week switch
    {
        -1 => "",
        0 => "верхняя неделя",
        _ => "нижняя неделя"
    };

    public override string ToString()
    {
        return $"{StartHour:D2}:{StartMinute:D2}\n{FinishHour:D2}:{FinishMinute:D2}";
    }

    /// <summary>
    /// Silent convert time units from string to integer
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    private static int ToIntegerTime(string time)
    {
        return int.TryParse(time, out int parsed) ? parsed : -1;
    }
    /// <summary>
    /// Converts the string representation of a time-slot to its TimeOfLesson equivalent.
    /// </summary>
    /// <param name="s">A string containing a time-slot to convert.</param>
    /// <returns></returns>
    public static TimeOfLesson Parse(string s)
    {
        s = s[1..^1];
        TimeOfLesson t = new();
        string[] times = s.Split(',');
        t.Day = ToIntegerTime(times[0]);
        t.StartHour = ToIntegerTime(times[1][..2]);
        t.StartMinute = ToIntegerTime(times[1][3..5]);
        t.FinishHour = ToIntegerTime(times[2][..2]);
        t.FinishMinute = ToIntegerTime(times[2][3..5]);
        t.Week = times[3] switch
        {
            "full" => -1,
            "upper" => 0,
            "lower" => 1,
            _ => 0
        };
        return t;
    }
}

public static class WeekMethods
{
    public static int GetCurDayOfWeek() => ((int)DateTime.Now.DayOfWeek + 6) % 7;

    /// <summary>
    /// Request current week
    /// </summary>
    /// <returns></returns>
    public static async Task<Week> RequestCurrentWeekAsync(SchRequests.FetchInfo? info = null)
    {
        const string url = mmcs_schedule_app.App.Host + "/APIv1/week/";
        SchRequests.FetchInfo weekInfo = new();
        Week week = await SchRequests.SchRequests.GetAsync<Week>(url, weekInfo);
        if (weekInfo.StaleSince is { } cachedAt)
        {
            info?.Add(cachedAt);
            if (WeeksSince(cachedAt) % 2 == 1)
            {
                week.Type = week.ReversedWeek();
            }
        }
        return week;
    }

    private static int WeeksSince(DateTime cachedAtUtc)
    {
        DateTime cachedAt = cachedAtUtc.ToLocalTime().Date;
        DateTime now = DateTime.Now.Date;
        DateTime cachedWeekStart = cachedAt.AddDays(-(((int)cachedAt.DayOfWeek + 6) % 7));
        DateTime nowWeekStart = now.AddDays(-GetCurDayOfWeek());
        return (int)((nowWeekStart - cachedWeekStart).TotalDays / 7);
    }
}
public static class StudentMethods
{
    /// <summary>
    /// Comparison (Lesson, List of Curriculum) by day of week and time
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    private static int CompareByDayAndTime(
        (Lesson, List<Curriculum>) first, (Lesson, List<Curriculum>) second)
    {
        TimeOfLesson firstSlot = TimeOfLesson.Parse(first.Item1.Timeslot);
        TimeOfLesson secondSlot = TimeOfLesson.Parse(second.Item1.Timeslot);
        return firstSlot.Day * 24 + firstSlot.StartHour * 60 + firstSlot.StartMinute
            - (secondSlot.Day * 24 + secondSlot.StartHour * 60 + secondSlot.StartMinute);
    }
    /// <summary>
    /// Request week schedule for <paramref name="groupId"/>
    /// </summary>
    /// <param name="groupId"></param>
    /// <param name="info">Receives the date of the offline cache if it was used</param>
    /// <returns>Ordered by day of week and time week schedule</returns>
    public static async Task<List<(Lesson, List<Curriculum>)>> RequestWeekScheduleAsync(
        int groupId, SchRequests.FetchInfo? info = null)
    {
        string url = mmcs_schedule_app.App.Host + "/APIv1/schedule/group/" + groupId;
        SchOfGroup schedule = await SchRequests.SchRequests.GetAsync<SchOfGroup>(url, info);
        List<(Lesson, List<Curriculum>)> result = [];
        foreach (Lesson lesson in schedule.Lessons)
        {
            result.Add((lesson, schedule.Curricula.FindAll(c => c.LessonId == lesson.Id)));
        }
        result.Sort(CompareByDayAndTime);
        return result;
    }
}

public static class TeacherMethods
{

    /// <summary>
    /// Get teachers list (not cached)
    /// </summary>
    /// <returns></returns>
    public static async Task<Teacher[]> GetTeachersListAsync(SchRequests.FetchInfo? info = null)
    {
        const string url = mmcs_schedule_app.App.Host + "/APIv1/teacher/list";
        return await SchRequests.SchRequests.GetAsync<Teacher[]>(url, info);
    }

    /// <summary>
    /// Comparison(Lesson, List of Curriculum, List of TechGroup)
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    private static int CompareByDayAndTime(
        (Lesson, List<Curriculum>, List<TechGroup>) first,
        (Lesson, List<Curriculum>, List<TechGroup>) second)
    {
        TimeOfLesson firstSlot = TimeOfLesson.Parse(first.Item1.Timeslot);
        TimeOfLesson secondSlot = TimeOfLesson.Parse(second.Item1.Timeslot);
        return firstSlot.Day * 24 + firstSlot.StartHour * 60 + firstSlot.StartMinute
            - (secondSlot.Day * 24 + secondSlot.StartHour * 60 + secondSlot.StartMinute);
    }

    /// <summary>
    /// Request week schedule for <paramref name="teacherId"/>
    /// </summary>
    /// <param name="teacherId"></param>
    /// <param name="info">Receives the date of the offline cache if it was used</param>
    /// <returns>Ordered by day of week and time week schedule</returns>
    public static async Task<List<(Lesson, List<Curriculum>, List<TechGroup>)>> RequestWeekScheduleAsync(
        int teacherId, SchRequests.FetchInfo? info = null)
    {
        string url = mmcs_schedule_app.App.Host + "/APIv1/schedule/teacher/" + teacherId;
        SchOfTeacher schedule = await SchRequests.SchRequests.GetAsync<SchOfTeacher>(url, info);
        List<(Lesson, List<Curriculum>, List<TechGroup>)> result = [];
        foreach (Lesson lesson in schedule.Lessons)
        {
            result.Add((
                lesson,
                schedule.Curricula.FindAll(c => c.LessonId == lesson.Id),
                schedule.Groups.FindAll(g => g.UberId == lesson.UberId)));
        }
        result.Sort(CompareByDayAndTime);
        return result;
    }
}

public static class GradeMethods
{
    public static async Task<Grade[]> GetGradesListAsync(SchRequests.FetchInfo? info = null)
    {
        const string url = mmcs_schedule_app.App.Host + "/APIv1/grade/list";
        return await SchRequests.SchRequests.GetAsync<Grade[]>(url, info);
    }

    public static async Task<Group[]> GetGroupsListAsync(int gradeId, SchRequests.FetchInfo? info = null)
    {
        string url = mmcs_schedule_app.App.Host + "/APIv1/group/forGrade/" + gradeId;
        return await SchRequests.SchRequests.GetAsync<Group[]>(url, info);
    }
}
