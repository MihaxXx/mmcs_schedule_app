using System.ComponentModel;
using System.Text;
using API;
using Newtonsoft.Json;
using SchRequests;

namespace mmcs_schedule_app;

// Learn more about making custom code visible in the Xamarin.Forms previewer
// by visiting https://aka.ms/xamarinforms-previewer
[DesignTimeVisible(false)]
public partial class MainPage
{
    private Grade[] grades = [];
    private Teacher[] teachers = [];
    private Room[] rooms = [];
    private User user = new();

    private bool isDataLoaded;
    private bool isLoading;
    private int activeRequests;
    private int? pendingGroupId;
    private bool loadFailed;
    private bool groupsFailed;
    private FetchInfo dataInfo = new();
    private FetchInfo groupsInfo = new();

    // Static cached data with lazy loading
    private static Grade[]? _cachedGrades;
    private static Teacher[]? _cachedTeachers;
    private static Room[]? _cachedRooms;
    private static readonly Dictionary<int, Group[]> _cachedGroups = [];

    private static async Task<T> GetMemoizedAsync<T>(
        T? memo, Func<FetchInfo, Task<T>> request, Action<T> save,
        FetchInfo? info) where T : class
    {
        if (memo is not null)
        {
            return memo;
        }

        FetchInfo requestInfo = new();
        T result = await request(requestInfo);
        info?.Merge(requestInfo);
        if (requestInfo.StaleSince is null)
        {
            save(result);
        }
        return result;
    }

    public static Task<Grade[]> GetGradesAsync(FetchInfo? info = null)
    {
        return GetMemoizedAsync(
            _cachedGrades,
            GradeMethods.GetGradesListAsync,
            loaded => _cachedGrades = loaded,
            info);
    }

    public static Task<Teacher[]> GetTeachersAsync(FetchInfo? info = null)
    {
        return GetMemoizedAsync(
            _cachedTeachers,
            TeacherMethods.GetTeachersListAsync,
            loaded => _cachedTeachers = loaded,
            info);
    }

    public static Task<Room[]> GetRoomsAsync(FetchInfo? info = null)
    {
        return GetMemoizedAsync(
            _cachedRooms,
            RoomMethods.GetRoomsListAsync,
            loaded => _cachedRooms = loaded,
            info);
    }

    public static Task<Group[]> GetGroupsAsync(int gradeId, FetchInfo? info = null)
    {
        return GetMemoizedAsync(
            _cachedGroups.GetValueOrDefault(gradeId),
            requestInfo => GradeMethods.GetGroupsListAsync(gradeId, requestInfo),
            loaded => _cachedGroups[gradeId] = loaded,
            info);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = this.RunSafelyAsync(InitializeAsync);
    }

    private void Banner_RetryClicked(object? sender, EventArgs e)
    {
        _ = this.RunSafelyAsync(InitializeAsync);
    }

    private async Task InitializeAsync()
    {
        if (!await LoadDataAsync())
        {
            return;
        }

        if (App.IsLoggedIn)
        {
            user = App.User;
            switch (user.Info)
            {
                case User.UserInfo.Teacher:
                    RolePicker.SelectedIndex = 1;
                    OwnerPicker.SelectedIndex = Array.FindIndex(teachers, t => t.Id == user.TeacherId);
                    break;
                case User.UserInfo.Room:
                    RolePicker.SelectedIndex = 2;
                    OwnerPicker.SelectedIndex = Array.FindIndex(rooms, r => r.Id == user.RoomId);
                    break;
                default:
                    RolePicker.SelectedIndex = 0;
                    int gradeIndex = FindSavedGradeIndex();
                    if (gradeIndex >= 0)
                    {
                        pendingGroupId = user.GroupId;
                    }
                    OwnerPicker.SelectedIndex = gradeIndex;
                    break;
            }
            App.IsLoggedIn = false;
            File.Delete(App.FileName);
            App.User = new User();
        }
    }

    /// <summary>
    /// Index of the saved grade in <see cref="grades"/>, or -1 if it no longer exists. Settings saved before
    /// the grade id was stored are matched by their picker index, accepted only if the course still agrees.
    /// </summary>
    private int FindSavedGradeIndex()
    {
        if (user.GradeId != 0)
        {
            return Array.FindIndex(grades, g => g.Id == user.GradeId);
        }

        int legacyIndex = user.GradeOrTeacherIndex;
        bool isSameGrade = legacyIndex >= 0 && legacyIndex < grades.Length
            && grades[legacyIndex].Number == user.Course;
        return isSameGrade ? legacyIndex : -1;
    }

    private async Task<bool> LoadDataAsync()
    {
        if (isDataLoaded)
        {
            return true;
        }
        if (isLoading)
        {
            return false;
        }

        isLoading = true;
        loadFailed = false;
        UpdateBanner();
        SetLoading(true);
        try
        {
            FetchInfo info = new();
            grades = await GetGradesAsync(info);
            teachers = [.. (await GetTeachersAsync(info)).Where(t => !string.IsNullOrWhiteSpace(t.Name))];
            rooms = [.. (await GetRoomsAsync(info)).Where(r => !string.IsNullOrWhiteSpace(r.Name))];
            dataInfo = info;
            isDataLoaded = info.StaleSince is null;
            RolePicker.IsEnabled = true;
            return true;
        }
        catch (ScheduleUnavailableException)
        {
            loadFailed = true;
            return false;
        }
        finally
        {
            SetLoading(false);
            isLoading = false;
            UpdateBanner();
        }
    }

    private void SetLoading(bool isStarting)
    {
        activeRequests += isStarting ? 1 : -1;
        LoadingIndicator.IsRunning = LoadingIndicator.IsVisible = activeRequests > 0;
    }

    private void UpdateBanner()
    {
        if (loadFailed)
        {
            Banner.ShowError(true);
            return;
        }
        if (groupsFailed)
        {
            Banner.ShowError(false);
            return;
        }

        FetchInfo combined = new();
        combined.Merge(dataInfo);
        combined.Merge(groupsInfo);
        Banner.ShowStale(combined, dataInfo.StaleSince is not null);
    }

    private async Task LoadGroupsAsync(int gradeIndex)
    {
        GroupPicker.IsEnabled = false;
        GroupPicker.Items.Clear();
        GroupPicker.SelectedIndex = -1;
        OkButton.IsEnabled = false;
        groupsFailed = false;
        groupsInfo = new FetchInfo();
        UpdateBanner();
        SetLoading(true);
        try
        {
            Grade grade = grades[gradeIndex];
            FetchInfo info = new();
            grade.Groups = [.. await GetGroupsAsync(grade.Id, info)];
            groupsInfo = info;
            if (OwnerPicker.SelectedIndex != gradeIndex)
            {
                return;
            }

            foreach (Group group in grade.Groups)
            {
                GroupPicker.Items.Add(group.Name + " группа " + group.Number);
            }
            GroupPicker.IsEnabled = true;
            if (pendingGroupId is { } groupId)
            {
                GroupPicker.SelectedIndex = grade.Groups.FindIndex(g => g.Id == groupId);
            }
            pendingGroupId = null;
        }
        catch (ScheduleUnavailableException)
        {
            pendingGroupId = null;
            OwnerPicker.SelectedIndex = -1;
            groupsFailed = true;
        }
        finally
        {
            SetLoading(false);
            UpdateBanner();
        }
    }


    public MainPage()
    {
        InitializeComponent();
    }
    private void RolePicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender is not Picker picker || picker.SelectedIndex == 0)
        {
            //Set to fix user is a student, later reaasigned to correct grade
            user.Info = User.UserInfo.Bachelor;
            OwnerPicker.Items.Clear();
            OwnerPicker.Title = "Курс";
            foreach (Grade grade in grades)
            {
                OwnerPicker.Items.Add(StuDegree(grade.Degree) + " " + grade.Number + " курс");
            }
        }
        else if (picker.SelectedIndex == 1)
        {
            user.Info = User.UserInfo.Teacher;
            OwnerPicker.Items.Clear();
            OwnerPicker.Title = "ФИО";
            foreach (Teacher teacher in teachers)
            {
                OwnerPicker.Items.Add(teacher.Name);
            }
        }
        else if (picker.SelectedIndex == 2)
        {
            user.Info = User.UserInfo.Room;
            OwnerPicker.Items.Clear();
            OwnerPicker.Title = "Аудитория";
            foreach (Room room in rooms)
            {
                OwnerPicker.Items.Add(room.Name.Trim());
            }
        }
        OwnerPicker.SelectedIndex = -1;
        OwnerPicker.IsEnabled = true;
        GroupPicker.SelectedIndex = -1;
        GroupPicker.IsEnabled = false;
        OkButton.IsEnabled = false;
    }
    private void OwnerPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender is not Picker picker || picker.SelectedIndex == -1)
        {
            return;
        }

        user.GradeOrTeacherIndex = picker.SelectedIndex;
        if (user.Info == User.UserInfo.Teacher)
        {
            GroupPicker.IsEnabled = false;
            user.TeacherId = teachers.First(t => t.Name == picker.Items[picker.SelectedIndex]).Id;
            OkButton.IsEnabled = true;
        }
        else if (user.Info == User.UserInfo.Room)
        {
            GroupPicker.IsEnabled = false;
            user.RoomId = rooms[picker.SelectedIndex].Id;
            OkButton.IsEnabled = true;
        }
        else
        {
            user.Info = grades[OwnerPicker.SelectedIndex].Degree switch
            {
                "bachelor" => User.UserInfo.Bachelor,
                "master" => User.UserInfo.Master,
                "specialist" => User.UserInfo.Bachelor,
                "postgraduate" => User.UserInfo.Graduate,
                _ => user.Info
            };
            user.GradeId = grades[OwnerPicker.SelectedIndex].Id;
            user.Course = grades[OwnerPicker.SelectedIndex].Number;
            int gradeIndex = OwnerPicker.SelectedIndex;
            _ = this.RunSafelyAsync(() => LoadGroupsAsync(gradeIndex));
        }
    }
    private Group SelectedGroup =>
        grades[OwnerPicker.SelectedIndex].Groups[GroupPicker.SelectedIndex];

    private void GroupPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender is not Picker picker || picker.SelectedIndex == -1)
        {
            return;
        }

        user.GroupIndex = GroupPicker.SelectedIndex;
        user.GroupNumber = SelectedGroup.Number;
        user.GroupId = SelectedGroup.Id;
        OkButton.IsEnabled = true;
    }

    private void OkButton_Clicked(object sender, EventArgs e)
    {
        _ = this.RunSafelyAsync(SaveUserAndOpenScheduleAsync);
    }

    private async Task SaveUserAndOpenScheduleAsync()
    {
        string selectedName = OwnerPicker.Items[OwnerPicker.SelectedIndex];
        user.Header = user.Info switch
        {
            User.UserInfo.Teacher => selectedName,
            User.UserInfo.Room => "Ауд. " + selectedName,
            _ => string.Join(" ", StuDegreeShort(user.Info.ToString().ToLowerInvariant()),
                SelectedGroup.Name, user.Course + "." + user.GroupNumber)
        };
        App.User = user;
        try
        {
            string json = JsonConvert.SerializeObject(App.User, Formatting.Indented);
            await File.WriteAllTextAsync(App.FileName, json, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            await DisplayAlertAsync(
                "Ошибка",
                "Не удалось сохранить выбор, при следующем запуске его придется выбрать заново",
                "OK");
        }
        App.IsLoggedIn = true;
        Navigation.InsertPageBefore(new ScheduleView(user.Info, user.OwnerId, user.Header), this);
        await Navigation.PopAsync();
    }

    public static string StuDegreeShort(string degree)
    {
        return degree switch
        {
            "bachelor" => "Бак.",
            "master" => "Маг.",
            "specialist" => "Спец.",
            "graduate" or "postgraduate" => "Асп.",
            _ => "н/д"
        };
    }

    /// <summary>
    /// Compact group name for lists, e.g. "Бак-1 ФИИТ-1". The group number restarts in every program,
    /// so it is only unique together with the program name.
    /// </summary>
    public static string GroupShortName(API.TechGroup group)
    {
        return $"{GroupProgramName(group)}-{group.GroupNumber}";
    }

    /// <summary>
    /// Compact name of the group's course within its program, e.g. "Бак-1 ФИИТ"
    /// </summary>
    public static string GroupProgramName(API.TechGroup group)
    {
        return $"{GroupCourseName(group)} {ProgramShortName(group.Name)}";
    }

    /// <summary>
    /// Compact name of the group's degree and course, e.g. "Бак-1"
    /// </summary>
    public static string GroupCourseName(API.TechGroup group)
    {
        return $"{StuDegreeShort(group.Degree).TrimEnd('.')}-{group.GradeNumber}";
    }

    /// <summary>
    /// Shortens a program name of more than <see cref="MaxProgramNameLetters"/> letters: an ordinary
    /// word becomes its capital initial, "и" stays, anything else (abbreviations, Latin text) is kept,
    /// e.g. "КН и прикладной ИИ" gives "КНиПИИ". Initials and "и" attach to their neighbours.
    /// A single ordinary word is cut to its first three letters ("Математика" gives "Мат").
    /// A short name made of abbreviations only still gets its "и" attached ("ММ и ИИ" gives "ММиИИ"),
    /// so that the same program spelled in both ways looks the same.
    /// </summary>
    public static string ProgramShortName(string name)
    {
        string[] tokens = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool isLong = name.Count(char.IsLetter) > MaxProgramNameLetters;
        if (tokens.Length == 1)
        {
            return isLong && IsOrdinaryWord(name) ? name[..3] : name;
        }
        if (!isLong && tokens.Any(IsOrdinaryWord))
        {
            return name;
        }

        StringBuilder result = new();
        bool previousIsGlued = false;
        foreach (string token in tokens)
        {
            bool isWord = IsOrdinaryWord(token);
            bool isGlued = isWord || token == "и";
            if (result.Length > 0 && !isGlued && !previousIsGlued)
            {
                result.Append(' ');
            }
            result.Append(isWord ? char.ToUpperInvariant(token[0]).ToString() : token);
            previousIsGlued = isGlued;
        }
        return result.ToString();
    }

    private const int MaxProgramNameLetters = 7;

    /// <returns>True for a Cyrillic word with only the first letter capital, e.g. "Механика"</returns>
    private static bool IsOrdinaryWord(string token)
    {
        string letters = token.TrimEnd('.');
        return letters.Length > 1
            && IsCyrillic(letters[0])
            && letters.Skip(1).All(c => IsCyrillic(c) && char.IsLower(c));
    }

    private static bool IsCyrillic(char c) => c is >= 'А' and <= 'я' or 'Ё' or 'ё';

    /// <summary>
    /// Group name for titles and popups, e.g. "Бак. 1к ФИИТ 1гр"
    /// </summary>
    public static string GroupFullName(API.TechGroup group)
    {
        return $"{StuDegreeShort(group.Degree)} {group.GradeNumber}к {group.Name} {group.GroupNumber}гр";
    }

    public static string StuDegree(string degree)
    {
        return degree switch
        {
            "bachelor" => "Бакалавриат",
            "master" => "Магистратура",
            "specialist" => "Специалитет",
            "postgraduate" => "Аспирантура",
            _ => "н/д"
        };
    }
}
