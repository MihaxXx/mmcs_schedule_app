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
            
            // Populate teachers list
            foreach (var curriculum in curricula)
            {
                teachers.Add(new TeacherInfo(curriculum.teachername, $"ауд. {curriculum.roomname}", curriculum.teacherid));
            }
            
            TeachersCollection.ItemsSource = teachers;
        }

        private async void OnTeacherSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count == 0)
                return;
                
            var selectedTeacher = e.CurrentSelection[0] as TeacherInfo;
            if (selectedTeacher == null)
                return;
            
            // Deselect the item
            ((CollectionView)sender).SelectedItem = null;
            
            // Find the teacher in the list to get the full name
            var teacher = allTeachers.FirstOrDefault(t => t.id == selectedTeacher.TeacherId);
            if (teacher == null)
                return;
            
            // Close current modal first
            await Navigation.PopModalAsync();
            
            // Navigate to teacher's schedule on the main navigation stack
            var scheduleView = new ScheduleView(User.UserInfo.teacher, selectedTeacher.TeacherId, teacher.name);
            await parentNavigation.PushAsync(scheduleView);
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
