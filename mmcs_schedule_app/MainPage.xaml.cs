using Newtonsoft.Json;
using System.ComponentModel;
using System.Text;

namespace mmcs_schedule_app
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        private API.Grade[] Grades = [];
        private API.Teacher[] Teachers = [];
        API.User user = new API.User();

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
        private static Dictionary<int, API.Group[]> _cachedGroups = [];

        private static async Task<T> GetMemoizedAsync<T>(T? memo, Func<SchRequests.FetchInfo, Task<T>> request, Action<T> save, SchRequests.FetchInfo? info) where T : class
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
            return GetMemoizedAsync(_cachedGrades, API.GradeMethods.GetGradesListAsync, grades => _cachedGrades = grades, info);
        }

        public static Task<API.Teacher[]> GetTeachersAsync(SchRequests.FetchInfo? info = null)
        {
            return GetMemoizedAsync(_cachedTeachers, API.TeacherMethods.GetTeachersListAsync, teachers => _cachedTeachers = teachers, info);
        }

        public static Task<API.Group[]> GetGroupsAsync(int gradeId, SchRequests.FetchInfo? info = null)
        {
            return GetMemoizedAsync(_cachedGroups.GetValueOrDefault(gradeId), requestInfo => API.GradeMethods.GetGroupsListAsync(gradeId, requestInfo), groups => _cachedGroups[gradeId] = groups, info);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await InitializeAsync();
        }

        private async void Banner_RetryClicked(object? sender, EventArgs e)
        {
            await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            if (!await LoadDataAsync())
            {
                return;
            }

            if (App._isLoggedIn)
            {
                user = App.user;
                bool isTeacher = user.Info == API.User.UserInfo.teacher;
                Role.SelectedIndex = isTeacher ? 1 : 0;
                if (!isTeacher)
                {
                    pendingGroupIndex = user.list2selID;
                }
                List_NmOrGr.SelectedIndex = user.list1selID;
                App._isLoggedIn = false;
                File.Delete(App._fileName);
                App.user = new API.User();
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
                Grades = await GetGradesAsync(info);
                Teachers = await GetTeachersAsync(info);
                dataInfo = info;
                isDataLoaded = info.StaleSince is null;
                Role.IsEnabled = true;
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

        private void SetLoading(bool isLoading)
        {
            activeRequests += isLoading ? 1 : -1;
            activityIndicator.IsRunning = activityIndicator.IsVisible = activeRequests > 0;
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
            List_Groups.IsEnabled = false;
            List_Groups.Items.Clear();
            List_Groups.SelectedIndex = -1;
            Ok_btn.IsEnabled = false;
            groupsFailed = false;
            groupsInfo = new();
            UpdateBanner();
            SetLoading(true);
            try
            {
                API.Grade grade = Grades[gradeIndex];
                SchRequests.FetchInfo info = new();
                grade.Groups = [.. await GetGroupsAsync(grade.id, info)];
                groupsInfo = info;
                if (List_NmOrGr.SelectedIndex != gradeIndex)
                {
                    return;
                }

                foreach (API.Group g in grade.Groups)
                {
                    List_Groups.Items.Add(g.name + " группа " + g.num);
                }
                List_Groups.IsEnabled = true;
                if (pendingGroupIndex is { } pendingIndex && pendingIndex < grade.Groups.Count)
                {
                    List_Groups.SelectedIndex = pendingIndex;
                }
                pendingGroupIndex = null;
            }
            catch (SchRequests.ScheduleUnavailableException)
            {
                pendingGroupIndex = null;
                List_NmOrGr.SelectedIndex = -1;
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
        private void Role_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is not Picker picker || picker.SelectedIndex == 0)
            {
                //Set to fix user is a student, later reaasigned to correct grade
                user.Info = API.User.UserInfo.bachelor;
                List_NmOrGr.Items.Clear();
                List_NmOrGr.Title = "Курс";
                foreach (var gr in Grades)
                    List_NmOrGr.Items.Add(StuDegree(gr.degree) + " " + gr.num + " курс");
            }
            else if (picker.SelectedIndex == 1)
            {
                user.Info = API.User.UserInfo.teacher;
                List_NmOrGr.Items.Clear();
                List_NmOrGr.Title = "Имя";
                foreach (var t in Teachers)
                    List_NmOrGr.Items.Add(t.name);
            }
            List_NmOrGr.SelectedIndex = -1;
            List_NmOrGr.IsEnabled = true;
            List_Groups.SelectedIndex = -1;
            List_Groups.IsEnabled = false;
            Ok_btn.IsEnabled = false;
        }
        private async void List_NmOrGr_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is not Picker picker || picker.SelectedIndex == -1)
                return;

            user.list1selID = picker.SelectedIndex;
            if (user.Info == API.User.UserInfo.teacher)
            {
                List_Groups.IsEnabled = false;
                user.teacherId = Teachers.First(t => t.name == picker.Items[picker.SelectedIndex]).id;
                Ok_btn.IsEnabled = true;
            }
            else
            {
                switch (Grades[List_NmOrGr.SelectedIndex].degree)
                {
                    case "bachelor": user.Info = API.User.UserInfo.bachelor; break;
                    case "master": user.Info = API.User.UserInfo.master; break;
                    case "specialist": user.Info = API.User.UserInfo.bachelor; break;
                    case "postgraduate": user.Info = API.User.UserInfo.graduate; break;
                }
                user.course = Grades[List_NmOrGr.SelectedIndex].num;
                await LoadGroupsAsync(List_NmOrGr.SelectedIndex);
            }
        }
        private void List_Groups_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (sender is not Picker picker || picker.SelectedIndex == -1)
                return;

            user.list2selID = List_Groups.SelectedIndex;
            user.group = Grades[List_NmOrGr.SelectedIndex].Groups[List_Groups.SelectedIndex].num;
            user.groupid = Grades[List_NmOrGr.SelectedIndex].Groups[List_Groups.SelectedIndex].id;
            Ok_btn.IsEnabled = true;
        }

        async private void Ok_btnClicked(object sender, EventArgs e)
        {
            user.header = user.Info == API.User.UserInfo.teacher ? List_NmOrGr.Items[List_NmOrGr.SelectedIndex] :
                string.Join(" ", StuDegreeShort(user.Info.ToString()), Grades[List_NmOrGr.SelectedIndex].Groups[List_Groups.SelectedIndex].name, user.course + "." + user.group);
            App.user = user;
            File.WriteAllText(App._fileName, JsonConvert.SerializeObject(App.user, Formatting.Indented), Encoding.UTF8);
            App._isLoggedIn = true;
            int id = user.Info == API.User.UserInfo.teacher ? user.teacherId : user.groupid;
            Navigation.InsertPageBefore(new ScheduleView(user.Info, id, user.header), this);
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
                _ => "н/д",
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
                _ => "н/д",
            };
        }
    }
}
