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
        public bool HasSubText => !string.IsNullOrEmpty(SubText);

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

            ListLabel = "Преподаватели:";

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

            ListLabel = "Группы:";

            RoomInfo = $"\nАудитория: {roomName}";

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
            DisciplineName = disciplineName;
            Title = disciplineName;

            Weekday = "\n" + ScheduleView.GetDayName(timeslot.day);

            Timeslot = "\n" + $"{timeslot.starth:D2}:{timeslot.startm:D2} - {timeslot.finishh:D2}:{timeslot.finishm:D2}";

            WeekType = timeslot.week == -1 ? "" : "\n" + (timeslot.week == 0 ? "верхняя неделя" : "нижняя неделя");
        }

        private async Task OnTeacherTapped(int teacherId)
        {
            var teacher = MainPage.GetTeachers().FirstOrDefault(t => t.id == teacherId);
            if (teacher == null)
                return;

            await ClosePopup();

            var scheduleView = new ScheduleView(User.UserInfo.teacher, teacherId, teacher.name);
            await parentNavigation.PushAsync(scheduleView);
        }

        private async Task OnGroupTapped(TechGroup techGroup)
        {
            var grade = MainPage.GetGrades().FirstOrDefault(g => g.num == techGroup.gradenum && g.degree == techGroup.degree);
            if (grade == null)
                return;

            var group = MainPage.GetGroups(grade.id).FirstOrDefault(g => g.num == techGroup.groupnum && g.name == techGroup.name);
            if (group == null)
                return;

            await ClosePopup();

            User.UserInfo userInfo = techGroup.degree switch
            {
                "bachelor" => User.UserInfo.bachelor,
                "master" => User.UserInfo.master,
                "specialist" => User.UserInfo.bachelor,
                "postgraduate" => User.UserInfo.graduate,
                _ => User.UserInfo.bachelor
            };

            string header = $"{MainPage.StuDegreeShort(techGroup.degree)} {techGroup.name} {techGroup.gradenum}.{techGroup.groupnum}";

            var scheduleView = new ScheduleView(userInfo, group.id, header);
            await parentNavigation.PushAsync(scheduleView);
        }

        private async Task ClosePopup()
        {
            if (Navigation.ModalStack.Count > 0)
            {
                await Navigation.PopModalAsync();
            }
        }

        private async void ClosePopup(object sender, EventArgs e) => await ClosePopup();
    }
}
