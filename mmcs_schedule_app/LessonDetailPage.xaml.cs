using API;
using System.Collections.ObjectModel;

namespace mmcs_schedule_app
{
    public class TeacherInfo
    {
        public string TeacherName { get; set; }
        public string RoomName { get; set; }
        public int TeacherId { get; set; }

        public TeacherInfo(string teacherName, string roomName, int teacherId)
        {
            TeacherName = teacherName;
            RoomName = roomName;
            TeacherId = teacherId;
        }
    }

    public class GroupInfo
    {
        public string GroupName { get; set; }
        public string GroupDetails { get; set; }
        public int GroupId { get; set; }

        public GroupInfo(string groupName, string groupDetails, int groupId)
        {
            GroupName = groupName;
            GroupDetails = groupDetails;
            GroupId = groupId;
        }
    }

    public partial class LessonDetailPage : ContentPage
    {
        private readonly List<string> DayNames;
        private readonly INavigation parentNavigation;
        private readonly Teacher[] allTeachers;
        private readonly Grade[] allGrades;

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
            ListLabel.Text = "Преподаватели:";
            
            // Populate teachers list with clickable labels
            foreach (var curriculum in curricula)
            {
                var teacherBorder = CreateClickableItem(
                    curriculum.teachername,
                    $"ауд. {curriculum.roomname}",
                    async () => await OnTeacherTapped(curriculum.teacherid)
                );
                TeachersStackLayout.Children.Add(teacherBorder);
            }
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
            ListLabel.Text = "Группы:";
            
            // Populate groups list with clickable labels
            foreach (var techGroup in groups)
            {
                string groupDisplay = $"{StuDegreeShort(techGroup.degree)} {techGroup.name} {techGroup.gradenum}.{techGroup.groupnum}";
                var groupBorder = CreateClickableItem(
                    groupDisplay,
                    $"ауд. {roomName}",
                    async () => await OnGroupTapped(techGroup)
                );
                TeachersStackLayout.Children.Add(groupBorder);
            }
        }

        private void SetupCommonInfo(string disciplineName, TimeOfLesson timeslot)
        {
            // Set discipline name
            DisciplineLabel.Text = disciplineName;
            Title = disciplineName;
            
            // Set weekday
            WeekdayLabel.Text = DayNames[(timeslot.day + 1) % 7];
            
            // Set timeslot
            TimeslotLabel.Text = $"{timeslot.starth:D2}:{timeslot.startm:D2} - {timeslot.finishh:D2}:{timeslot.finishm:D2}";
            
            // Set week type
            WeekTypeLabel.Text = timeslot.week == -1 ? "" : timeslot.week == 0 ? "верхняя неделя" : "нижняя неделя";
        }

        private Border CreateClickableItem(string mainText, string subText, Func<Task> onTapped)
        {
            var border = new Border
            {
                Padding = new Thickness(5, 5, 5, 5),
                Margin = new Thickness(0, 0, 0, 0),
                Stroke = Colors.Transparent,
                BackgroundColor = Colors.Transparent,
                StrokeThickness = 0
            };
            
            var horizontalStack = new HorizontalStackLayout
            {
                Spacing = 8
            };
            
            // Bullet point
            var bulletLabel = new Label
            {
                Text = "•",
                FontSize = 16,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 0, 0, 0)
            };
            
            var verticalStack = new VerticalStackLayout
            {
                Spacing = 2,
                HorizontalOptions = LayoutOptions.FillAndExpand
            };
            
            var mainLabel = new Label
            {
                Text = mainText,
                FontSize = 16
            };
            
            var subLabel = new Label
            {
                Text = subText,
                FontSize = 14,
                TextColor = Colors.Gray
            };
            
            verticalStack.Children.Add(mainLabel);
            verticalStack.Children.Add(subLabel);
            
            horizontalStack.Children.Add(bulletLabel);
            horizontalStack.Children.Add(verticalStack);
            
            border.Content = horizontalStack;
            
            // Add tap gesture
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => await onTapped();
            border.GestureRecognizers.Add(tapGesture);
            
            return border;
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
    }
}
