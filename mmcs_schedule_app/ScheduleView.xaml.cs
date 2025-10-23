using API;
using System.Collections.ObjectModel;

namespace mmcs_schedule_app
{
    public class LessonItem
    {
        public string time { get; set; }
        public string name { get; set; }
        public string weektypes { get; set; }
        public string room { get; set; }
        public string who { get; set; }
        public TimeOfLesson timeslot { get; set; }
        public (Lesson, List<Curriculum>, List<TechGroup>) TData { get; private set; }
        public (Lesson, List<Curriculum>) SData { get; private set; }

        public LessonItem(string tm, string nm, string wt, string r, string w, TimeOfLesson ts, (Lesson, List<Curriculum>, List<TechGroup>) TD)
        {
            time = tm;
            name = nm;
            weektypes = wt;
            room = r;
            who = w;
            timeslot = ts;
            TData = TD;
        }
        public LessonItem(string tm, string nm, string wt, string r, string w, TimeOfLesson ts, (Lesson, List<Curriculum>) SD)
        {
            time = tm;
            name = nm;
            weektypes = wt;
            room = r;
            who = w;
            timeslot = ts;
            SData = SD;
        }
    }
    public partial class ScheduleView : ContentPage
    {
        Week currentWeek;

        Week selectedWeek = new Week() { week = (WeekType)2 };

        List<LessonItem> Shed = new List<LessonItem>();

        public ObservableCollection<IGrouping<string, LessonItem>> GroupedShed { get; private set; } = [];

        List<string> DayNames;

        private readonly User.UserInfo userInfo;
        private readonly int userId; // teacherId or groupid
        private readonly string userHeader;

        public ScheduleView(User.UserInfo info, int id, string header)
        {
            InitializeComponent();
            userInfo = info;
            userId = id;
            userHeader = header;
            Title = userHeader;
            
            if (userInfo == User.UserInfo.teacher)
            {
                foreach (var LLC in TeacherMethods.RequestWeekSchedule(userId))
                {
                    var tol = TimeOfLesson.Parse(LLC.Item1.timeslot);
                    Shed.Add(new LessonItem(tol.ToString(), LLC.Item2[0].subjectname,
                        tol.week == -1 ? "" : tol.week == 0 ? "верхняя неделя" : "нижняя неделя",
                        LLC.Item2.First().roomname,
                        string.Join("\n", LLC.Item3.Select(g => $"• {StuDegreeShort(g.degree)} {g.gradenum}.{g.groupnum}")),
                        tol, LLC));
                }
            }
            else
            {
                //Go thought list of lessons (present timeslots for group)
                foreach (var LLC in StudentMethods.RequestWeekSchedule(userId))
                {
                    var tol = TimeOfLesson.Parse(LLC.Item1.timeslot);
                    //Go thought list of Curriculums (present subj for timeslot)
                    foreach (var LC in LLC.Item2.ToLookup(lc => lc.subjectid).Select(coll => coll.First()))
                    {
                        var FilteredCurs = LLC.Item2.Where(c => c.subjectid == LC.subjectid).ToList();
                        Shed.Add(new LessonItem(tol.ToString(), LC.subjectname,
                            tol.week == -1 ? "" : tol.week == 0 ? "верхняя неделя" : "нижняя неделя", LC.roomname,
                            string.Join("\n", FilteredCurs.Select(c => $"• {c.teachername}\n  {c.roomname}")),
                            tol, (LLC.Item1, FilteredCurs)));
                    }
                }
            }
            //Gets russian day names, possible to use CurrentInfo, but app has no localization, so no reason for that
            DayNames = new System.Globalization.CultureInfo("ru-RU").DateTimeFormat.DayNames.ToList();
            currentWeek = CurrentSubject.RequestCurrentWeek();

            UpdateGroupedShed();

            //Must be at the end!!!
            BindingContext = this;
        }

        // Keep constructor for backward compatibility with App.user
        public ScheduleView() : this(App.user.Info, App.user.Info == User.UserInfo.teacher ? App.user.teacherId : App.user.groupid, App.user.header)
        {
        }

        private void UpdateGroupedShed()
        {
            GroupedShed.Clear();

            foreach (var lesson in Shed
                .Where(l => IsOnSelectedWeek(l.timeslot.week))
                .OrderBy(l => l.timeslot.day).ThenBy(l => l.timeslot.starth * 60 + l.timeslot.startm).ThenBy(l => l.timeslot.week)
                .GroupBy(l => DayNames[(l.timeslot.day + 1) % 7]))
            {
                GroupedShed.Add(lesson);
            }
        }

        private bool IsOnSelectedWeek(int leesonWeek)
        {
            return selectedWeek.week == WeekType.Full
                || leesonWeek == -1
                || (selectedWeek.week == WeekType.Current && leesonWeek == (int)currentWeek.week)
                || (selectedWeek.week != WeekType.Current && leesonWeek == (int)selectedWeek.week);
        }

        public static string StuDegreeShort(string degree)
        {
            switch (degree)
            {
                case "bachelor": return "бак.";
                case "master": return "маг.";
                case "specialist": return "спец.";
                case "postgraduate": return "асп.";
                default: return "н/д";
            }
        }

        protected override void OnAppearing()
        {

        }
        async private void OnExitClicked(object sender, EventArgs e)
        {
            //await Navigation.PushModalAsync(new MainPage());
            Navigation.InsertPageBefore(new MainPage(), this);
            await Navigation.PopAsync();
        }

        private async void OnChangeWeekClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Выберите тип недели", "Отмена", null, ["Текущая неделя", "Только верхняя", "Только нижняя", "Полное расписание"]);

            if (action != null && action != "Отмена")
            {
                selectedWeek.week = action switch
                {
                    "Текущая неделя" => WeekType.Current,
                    "Только верхняя" => WeekType.Upper,
                    "Только нижняя" => WeekType.Lower,
                    "Полное расписание" => WeekType.Full,
                    _ => WeekType.Current,
                };

                UpdateGroupedShed();
            }
        }

        async void OnListItemTapped(object sender, ItemTappedEventArgs e)
        {
            ((ListView)sender).SelectedItem = null;
            var item = (LessonItem)e.Item;
            
            // Get the list of curricula
            List<Curriculum> curricula;
            if (userInfo == User.UserInfo.teacher && item.TData.Item1 != null)
            {
                curricula = item.TData.Item2;
            }
            else if (userInfo != User.UserInfo.teacher && item.SData.Item1 != null)
            {
                curricula = item.SData.Item2;
            }
            else
            {
                return;
            }
            
            // Open modal popup with lesson details
            var detailPage = new LessonDetailPage(item.name, item.timeslot, curricula);
            await Navigation.PushModalAsync(new NavigationPage(detailPage));
        }
    }
}
