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

    public partial class LessonDetailPage : ContentPage
    {
        private readonly List<string> DayNames;
        private readonly ObservableCollection<TeacherInfo> teachers = new();
        private readonly Teacher[] allTeachers;
        private readonly INavigation parentNavigation;

        public LessonDetailPage(string disciplineName, TimeOfLesson timeslot, List<Curriculum> curricula, INavigation parentNav)
        {
            InitializeComponent();
            
            DayNames = new System.Globalization.CultureInfo("ru-RU").DateTimeFormat.DayNames.ToList();
            parentNavigation = parentNav;
            
            // Load all teachers for ID lookup
            allTeachers = TeacherMethods.GetTeachersList();
            
            // Set discipline name
            DisciplineLabel.Text = disciplineName;
            Title = disciplineName;
            
            // Set weekday
            WeekdayLabel.Text = DayNames[(timeslot.day + 1) % 7];
            
            // Set timeslot
            TimeslotLabel.Text = $"{timeslot.starth:D2}:{timeslot.startm:D2} - {timeslot.finishh:D2}:{timeslot.finishm:D2}";
            
            // Set week type
            WeekTypeLabel.Text = timeslot.week == -1 ? "" : timeslot.week == 0 ? "верхняя неделя" : "нижняя неделя";
            
            // Populate teachers list with clickable labels
            foreach (var curriculum in curricula)
            {
                teachers.Add(new TeacherInfo(curriculum.teachername, $"ауд. {curriculum.roomname}", curriculum.teacherid));
                
                // Create a clickable teacher item
                var teacherBorder = new Border
                {
                    Padding = 10,
                    Margin = new Thickness(0, 0, 0, 5),
                    Stroke = Colors.Transparent,
                    BackgroundColor = Colors.Transparent,
                    StrokeThickness = 0
                };
                
                var stackLayout = new VerticalStackLayout
                {
                    Spacing = 2
                };
                
                var nameLabel = new Label
                {
                    Text = curriculum.teachername,
                    FontSize = 16,
                    TextColor = Colors.Blue,
                    TextDecorations = TextDecorations.Underline
                };
                
                var roomLabel = new Label
                {
                    Text = $"ауд. {curriculum.roomname}",
                    FontSize = 14,
                    TextColor = Colors.Gray
                };
                
                stackLayout.Children.Add(nameLabel);
                stackLayout.Children.Add(roomLabel);
                teacherBorder.Content = stackLayout;
                
                // Add tap gesture
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += async (s, e) => await OnTeacherTapped(curriculum.teacherid);
                teacherBorder.GestureRecognizers.Add(tapGesture);
                
                TeachersStackLayout.Children.Add(teacherBorder);
            }
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

        private async void OnBackgroundTapped(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
