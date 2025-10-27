using API;
using System.Collections.ObjectModel;
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

    public partial class LessonDetailPage : ContentPage
    {
        private readonly INavigation parentNavigation;

        public string DisciplineName { get; set; }
        public string Weekday { get; set; }
        public string Timeslot { get; set; }
        public string WeekType { get; set; }
        public string ListLabel { get; set; }
        public string RoomInfo { get; set; }

        public ObservableCollection<LessonItemInfo> Items { get; set; } = new();

        public ICommand ItemTappedCommand { get; }

        // Constructor for student schedule (shows teachers)
        public LessonDetailPage(string disciplineName, TimeOfLesson timeslot, List<Curriculum> curricula, INavigation parentNav)
        {
            InitializeComponent();
            
            parentNavigation = parentNav;
            
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
            
            parentNavigation = parentNav;
            
            SetupCommonInfo(disciplineName, timeslot);
            
            // Set label for groups
            ListLabel = "Группы:";
            
            // Set room info at top for teacher schedule
            RoomInfo = $"Аудитория: {roomName}";
            
            // Populate groups list - no room per item since teacher can't be in two places at once
            foreach (var techGroup in groups)
            {
                string groupDisplay = $"{MainPage.StuDegreeShort(techGroup.degree)} {techGroup.name} {techGroup.gradenum}.{techGroup.groupnum}";
                Items.Add(new LessonItemInfo(
                    groupDisplay,
                    "", // No room info per item
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
            
            // Set weekday using ScheduleView's static method
            Weekday = ScheduleView.GetDayName(timeslot.day);
            
            // Set timeslot
            Timeslot = $"{timeslot.starth:D2}:{timeslot.startm:D2} - {timeslot.finishh:D2}:{timeslot.finishm:D2}";
            
            // Set week type
            WeekType = timeslot.week == -1 ? "" : timeslot.week == 0 ? "верхняя неделя" : "нижняя неделя";
        }

        private async Task OnTeacherTapped(int teacherId)
        {
            // Get teacher from cached list
            var teacher = MainPage.GetTeachers().FirstOrDefault(t => t.id == teacherId);
            if (teacher == null)
                return;
            
            await CloseAndNavigate(async () =>
            {
                var scheduleView = new ScheduleView(User.UserInfo.teacher, teacherId, teacher.name);
                await parentNavigation.PushAsync(scheduleView);
            });
        }

        private async Task OnGroupTapped(TechGroup techGroup)
        {
            // Get grade from cached list
            var grade = MainPage.GetGrades().FirstOrDefault(g => g.num == techGroup.gradenum && g.degree == techGroup.degree);
            if (grade == null)
                return;
            
            // Load groups for this grade
            var groups = GradeMethods.GetGroupsList(grade.id);
            var group = groups.FirstOrDefault(g => g.num == techGroup.groupnum && g.name == techGroup.name);
            if (group == null)
                return;
            
            await CloseAndNavigate(async () =>
            {
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
                string header = $"{MainPage.StuDegreeShort(techGroup.degree)} {techGroup.name} {techGroup.gradenum}.{techGroup.groupnum}";
                
                // Navigate to group's schedule on the main navigation stack
                var scheduleView = new ScheduleView(userInfo, group.id, header);
                await parentNavigation.PushAsync(scheduleView);
            });
        }

        private async Task CloseAndNavigate(Func<Task> navigationAction)
        {
            // Close current modal first
            await Navigation.PopModalAsync();
            
            // Then navigate
            await navigationAction();
        }

        private async void OnBackgroundTapped(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void OnSwipeDown(object sender, SwipedEventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
