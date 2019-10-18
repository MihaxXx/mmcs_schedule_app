using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


using API;

using Xamarin.Forms;

namespace mmcs_schedule_app
{
    public class LessonItem
    {
        public string time { get; set; }
        public string name { get; set; }
        public string weektypes { get; set; }
        public TimeOfLesson timeslot { get; set; }
        (Lesson, List<Curriculum>, List<TechGroup>) TData;
        (Lesson, List<Curriculum>) SData;

        public LessonItem(string tm,string nm,string wt, TimeOfLesson ts, (Lesson, List<Curriculum>, List<TechGroup>) TD)
        {
            time = tm;
            name = nm;
            weektypes = wt;
            timeslot = ts;
            TData = TD;
        }
        public LessonItem(string tm, string nm, string wt, TimeOfLesson ts,  (Lesson, List<Curriculum>) SD)
        {
            time = tm;
            name = nm;
            weektypes = wt;
            timeslot = ts;
            SData = SD;
        }
    }
    public partial class ScheduleView : ContentPage
    {
        Week weektype;

        List<LessonItem> Shed = new List<LessonItem>();

        public IEnumerable<IGrouping<string,LessonItem>> GroupedShed { get; private set; }

        public ScheduleView()
        {
            InitializeComponent();
            Title = App.user.header;
            if (App.user.Info == User.UserInfo.teacher)
            {
                foreach (var LLC in TeacherMethods.RequestWeekSchedule(App.user.teacherId))
                {
                    var tol = TimeOfLesson.Parse(LLC.Item1.timeslot);
                    Shed.Add(new LessonItem(tol.ToString(), LLC.Item2[0].subjectname, tol.week == -1 ? "" : tol.week == 0 ? "верхняя неделя" : "нижняя неделя", tol, LLC));
                }
            }
            else
            {
                foreach (var LLC in StudentMethods.RequestWeekSchedule(App.user.groupid))
                {
                    var tol = TimeOfLesson.Parse(LLC.Item1.timeslot);
                    Shed.Add(new LessonItem(tol.ToString(), LLC.Item2[0].subjectname, tol.week == -1 ? "" : tol.week == 0 ? "верхняя неделя" : "нижняя неделя", tol, LLC));
                }
            }
            //Gets russian day names, possible to use CurrentInfo, but app has no localization, so no reason for that
            var DayNames = new System.Globalization.CultureInfo("ru-RU").DateTimeFormat.DayNames;
            GroupedShed = Shed.OrderBy(l => l.timeslot.day).ThenBy(l => l.timeslot.starth * 60 + l.timeslot.startm).ThenBy(l => l.timeslot.week).
                GroupBy(l => DayNames[(l.timeslot.day + 1) % 7]);
            weektype = CurrentSubject.RequestCurrentWeek();
            //Must be at the end!!!
            BindingContext = this;
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
    }
}
