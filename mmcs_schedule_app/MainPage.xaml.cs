using Newtonsoft.Json;
using System.ComponentModel;
using System.Text;

namespace mmcs_schedule_app;

// Learn more about making custom code visible in the Xamarin.Forms previewer
// by visiting https://aka.ms/xamarinforms-previewer
[DesignTimeVisible(false)]
public partial class MainPage
{
    private API.Grade[] grades = [];
    private API.Teacher[] teachers = [];
    private API.User user = new();

    private bool isDataLoaded;
    private bool isLoading;
    private int activeRequests;
    private int? pendingGroupIndex;
    private bool loadFailed;
    private bool groupsFailed;
    private SchRequests.FetchInfo dataInfo = new();
    private SchRequests.FetchInfo groupsInfo = new();

    // Static cached data with lazy loading
    private static API.Grade[]? _cachedGrades;
    private static API.Teacher[]? _cachedTeachers;
    private static readonly Dictionary<int, API.Group[]> _cachedGroups = [];

    private static async Task<T> GetMemoizedAsync<T>(
        T? memo, Func<SchRequests.FetchInfo, Task<T>> request, Action<T> save,
        SchRequests.FetchInfo? info) where T : class
    {
        if (memo is not null)
        {
            return memo;
        }

        SchRequests.FetchInfo requestInfo = new();
        T result = await request(requestInfo);
        info?.Merge(requestInfo);
        if (requestInfo.StaleSince is null)
        {
            save(result);
        }
        return result;
    }

    public static Task<API.Grade[]> GetGradesAsync(SchRequests.FetchInfo? info = null)
    {
        return GetMemoizedAsync(
            _cachedGrades,
            API.GradeMethods.GetGradesListAsync,
            loaded => _cachedGrades = loaded,
            info);
    }

    public static Task<API.Teacher[]> GetTeachersAsync(SchRequests.FetchInfo? info = null)
    {
        return GetMemoizedAsync(
            _cachedTeachers,
            API.TeacherMethods.GetTeachersListAsync,
            loaded => _cachedTeachers = loaded,
            info);
    }

    public static Task<API.Group[]> GetGroupsAsync(int gradeId, SchRequests.FetchInfo? info = null)
    {
        return GetMemoizedAsync(
            _cachedGroups.GetValueOrDefault(gradeId),
            requestInfo => API.GradeMethods.GetGroupsListAsync(gradeId, requestInfo),
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
            bool isTeacher = user.Info == API.User.UserInfo.Teacher;
            RolePicker.SelectedIndex = isTeacher ? 1 : 0;
            if (!isTeacher)
            {
                pendingGroupIndex = user.GroupIndex;
            }
            GradeOrTeacherPicker.SelectedIndex = user.GradeOrTeacherIndex;
            App.IsLoggedIn = false;
            File.Delete(App.FileName);
            App.User = new API.User();
        }
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
            SchRequests.FetchInfo info = new();
            grades = await GetGradesAsync(info);
            teachers = await GetTeachersAsync(info);
            dataInfo = info;
            isDataLoaded = info.StaleSince is null;
            RolePicker.IsEnabled = true;
            return true;
        }
        catch (SchRequests.ScheduleUnavailableException)
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

        SchRequests.FetchInfo combined = new();
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
        groupsInfo = new SchRequests.FetchInfo();
        UpdateBanner();
        SetLoading(true);
        try
        {
            API.Grade grade = grades[gradeIndex];
            SchRequests.FetchInfo info = new();
            grade.Groups = [.. await GetGroupsAsync(grade.Id, info)];
            groupsInfo = info;
            if (GradeOrTeacherPicker.SelectedIndex != gradeIndex)
            {
                return;
            }

            foreach (API.Group group in grade.Groups)
            {
                GroupPicker.Items.Add(group.Name + " группа " + group.Number);
            }
            GroupPicker.IsEnabled = true;
            if (pendingGroupIndex is { } pendingIndex && pendingIndex < grade.Groups.Count)
            {
                GroupPicker.SelectedIndex = pendingIndex;
            }
            pendingGroupIndex = null;
        }
        catch (SchRequests.ScheduleUnavailableException)
        {
            pendingGroupIndex = null;
            GradeOrTeacherPicker.SelectedIndex = -1;
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
            user.Info = API.User.UserInfo.Bachelor;
            GradeOrTeacherPicker.Items.Clear();
            GradeOrTeacherPicker.Title = "Курс";
            foreach (API.Grade grade in grades)
            {
                GradeOrTeacherPicker.Items.Add(StuDegree(grade.Degree) + " " + grade.Number + " курс");
            }
        }
        else if (picker.SelectedIndex == 1)
        {
            user.Info = API.User.UserInfo.Teacher;
            GradeOrTeacherPicker.Items.Clear();
            GradeOrTeacherPicker.Title = "Имя";
            foreach (API.Teacher teacher in teachers)
            {
                GradeOrTeacherPicker.Items.Add(teacher.Name);
            }
        }
        GradeOrTeacherPicker.SelectedIndex = -1;
        GradeOrTeacherPicker.IsEnabled = true;
        GroupPicker.SelectedIndex = -1;
        GroupPicker.IsEnabled = false;
        OkButton.IsEnabled = false;
    }
    private void GradeOrTeacherPicker_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (sender is not Picker picker || picker.SelectedIndex == -1)
        {
            return;
        }

        user.GradeOrTeacherIndex = picker.SelectedIndex;
        if (user.Info == API.User.UserInfo.Teacher)
        {
            GroupPicker.IsEnabled = false;
            user.TeacherId = teachers.First(t => t.Name == picker.Items[picker.SelectedIndex]).Id;
            OkButton.IsEnabled = true;
        }
        else
        {
            user.Info = grades[GradeOrTeacherPicker.SelectedIndex].Degree switch
            {
                "bachelor" => API.User.UserInfo.Bachelor,
                "master" => API.User.UserInfo.Master,
                "specialist" => API.User.UserInfo.Bachelor,
                "postgraduate" => API.User.UserInfo.Graduate,
                _ => user.Info
            };
            user.Course = grades[GradeOrTeacherPicker.SelectedIndex].Number;
            int gradeIndex = GradeOrTeacherPicker.SelectedIndex;
            _ = this.RunSafelyAsync(() => LoadGroupsAsync(gradeIndex));
        }
    }
    private API.Group SelectedGroup =>
        grades[GradeOrTeacherPicker.SelectedIndex].Groups[GroupPicker.SelectedIndex];

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
        user.Header = user.Info == API.User.UserInfo.Teacher
            ? GradeOrTeacherPicker.Items[GradeOrTeacherPicker.SelectedIndex]
            : string.Join(" ", StuDegreeShort(user.Info.ToString().ToLowerInvariant()),
                SelectedGroup.Name, user.Course + "." + user.GroupNumber);
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
        int ownerId = user.Info == API.User.UserInfo.Teacher ? user.TeacherId : user.GroupId;
        Navigation.InsertPageBefore(new ScheduleView(user.Info, ownerId, user.Header), this);
        await Navigation.PopAsync();
    }

    public static string StuDegreeShort(string degree)
    {
        return degree switch
        {
            "bachelor" => "Бак.",
            "master" => "Маг.",
            "specialist" => "Спец.",
            "graduate" => "Асп.",
            _ => "н/д"
        };
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
