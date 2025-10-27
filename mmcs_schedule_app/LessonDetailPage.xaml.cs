using API;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace mmcs_schedule_app
{
    public class LessonItemInfo
    {
        public string MainText { get; set; }
        public string SubText { get; set; }
        public object ItemData { get; set; }

        public LessonItemInfo(string mainText, string subText, object itemData)
        {
            MainText = mainText;
            SubText = subText;
            ItemData = itemData;
        }
    }

    public partial class LessonDetailPage : ContentPage, INotifyPropertyChanged
    {
        private readonly List<string> DayNames;
        private readonly INavigation parentNavigation;
        private readonly Teacher[] allTeachers;
        private readonly Grade[] allGrades;

        private string _disciplineName;
        public string DisciplineName
        {
            get => _disciplineName;
            set { _disciplineName = value; OnPropertyChanged(); }
        }

        private string _weekday;
        public string Weekday
        {
            get => _weekday;
            set { _weekday = value; OnPropertyChanged(); }
        }

        private string _timeslot;
        public string Timeslot
        {
            get => _timeslot;
            set { _timeslot = value; OnPropertyChanged(); }
        }

        private string _weekType;
        public string WeekType
        {
            get => _weekType;
            set { _weekType = value; OnPropertyChanged(); }
        }

        private string _listLabel;
        public string ListLabel
        {
            get => _listLabel;
            set { _listLabel = value; OnPropertyChanged(); }
        }

        public ObservableCollection<LessonItemInfo> Items { get; set; } = new();

        public ICommand ItemTappedCommand { get; }

        // Constructor for student schedule (shows teachers)
        public LessonDetailPage(string disciplineName, TimeOfLesson timeslot, List<Curriculum> curricula, INavigation parentNav)
        {
            InitializeComponent();
            
            DayNames = new System.Globalization.CultureInfo("ru-RU").DateTimeFormat.DayNames.ToList();
            parentNavigation = parentNav;
            
            // Load all teachers for ID lookup
            allTeachers = TeacherMethods.GetTeachersList();
            
            SetupCommonInfo(disciplineName, timeslot);
            
            // Set label for teachers
            ListLabel = "Преподаватели:";
            
            // Populate teachers list
            foreach (var curriculum in curricula)
            {
                Items.Add(new LessonItemInfo(
                    curriculum.teachername,
                    $"ауд. {curriculum.roomname}",
                    curriculum.teacherid
                ));
            }

            ItemTappedCommand = new Command<LessonItemInfo>(async (item) => await OnTeacherTapped((int)item.ItemData));
            
            BindingContext = this;
        }

        // Constructor for teacher schedule (shows groups)
        public LessonDetailPage(string disciplineName, TimeOfLesson timeslot, List<TechGroup> groups, string roomName, INavigation parentNav)
        {
            InitializeComponent();
            
            DayNames = new System.Globalization.CultureInfo("ru-RU").DateTimeFormat.DayNames.ToList();
            parentNavigation = parentNav;
            
            // Load all grades for group lookup
            allGrades = GradeMethods.GetGradesList();
            
            SetupCommonInfo(disciplineName, timeslot);
            
            // Set label for groups
            ListLabel = "Группы:";
            
            // Populate groups list
            foreach (var techGroup in groups)
            {
                string groupDisplay = $"{StuDegreeShort(techGroup.degree)} {techGroup.name} {techGroup.gradenum}.{techGroup.groupnum}";
                Items.Add(new LessonItemInfo(
                    groupDisplay,
                    $"ауд. {roomName}",
                    techGroup
                ));
            }

            ItemTappedCommand = new Command<LessonItemInfo>(async (item) => await OnGroupTapped((TechGroup)item.ItemData));
            
            BindingContext = this;
        }

        private void SetupCommonInfo(string disciplineName, TimeOfLesson timeslot)
        {
            // Set discipline name
            DisciplineName = disciplineName;
            Title = disciplineName;
            
            // Set weekday
            Weekday = DayNames[(timeslot.day + 1) % 7];
            
            // Set timeslot
            Timeslot = $"{timeslot.starth:D2}:{timeslot.startm:D2} - {timeslot.finishh:D2}:{timeslot.finishm:D2}";
            
            // Set week type
            WeekType = timeslot.week == -1 ? "" : timeslot.week == 0 ? "верхняя неделя" : "нижняя неделя";
        }

        private async Task OnTeacherTapped(int teacherId)
        {
            // Find the teacher in the list to get the full name
            var teacher = allTeachers.FirstOrDefault(t => t.id == teacherId);
            if (teacher == null)
                return;
            
            // Close current modal first
            await Navigation.PopModalAsync();
            
            // Navigate to teacher's schedule on the main navigation stack
            var scheduleView = new ScheduleView(User.UserInfo.teacher, teacherId, teacher.name);
            await parentNavigation.PushAsync(scheduleView);
        }

        private async Task OnGroupTapped(TechGroup techGroup)
        {
            // Find the matching group by looking up grades
            var grade = allGrades.FirstOrDefault(g => g.num == techGroup.gradenum && g.degree == techGroup.degree);
            if (grade == null)
                return;
            
            // Load groups for this grade
            var groups = GradeMethods.GetGroupsList(grade.id);
            var group = groups.FirstOrDefault(g => g.num == techGroup.groupnum && g.name == techGroup.name);
            if (group == null)
                return;
            
            // Close current modal first
            await Navigation.PopModalAsync();
            
            // Determine user info based on degree
            User.UserInfo userInfo = techGroup.degree switch
            {
                "bachelor" => User.UserInfo.bachelor,
                "master" => User.UserInfo.master,
                "specialist" => User.UserInfo.bachelor,
                "postgraduate" => User.UserInfo.graduate,
                _ => User.UserInfo.bachelor
            };
            
            // Create header like in MainPage
            string header = $"{StuDegreeShort(techGroup.degree)} {techGroup.name} {techGroup.gradenum}.{techGroup.groupnum}";
            
            // Navigate to group's schedule on the main navigation stack
            var scheduleView = new ScheduleView(userInfo, group.id, header);
            await parentNavigation.PushAsync(scheduleView);
        }

        private static string StuDegreeShort(string degree)
        {
            return degree switch
            {
                "bachelor" => "Бак.",
                "master" => "Маг.",
                "specialist" => "Спец.",
                "postgraduate" => "Асп.",
                _ => "н/д"
            };
        }

        private async void OnBackgroundTapped(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void OnSwipeDown(object sender, SwipedEventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        public new event PropertyChangedEventHandler PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
