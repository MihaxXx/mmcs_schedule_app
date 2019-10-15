using System;
using System.Collections.Generic;

using API;

using Xamarin.Forms;

namespace mmcs_schedule_app
{
    public class LessonItem
    {
        public string timeslot { get; set; }
        public string name { get; set; }
        public string weektypes { get; set; }
        (Lesson, List<Curriculum>, List<TechGroup>) TData;
        (Lesson, List<Curriculum>) SData;

        public LessonItem(string ts,string nm,string wt, (Lesson, List<Curriculum>, List<TechGroup>) TD)
        {
            timeslot = ts;
            name = nm;
            weektypes = wt;
            TData = TD;
        }
        public LessonItem(string ts, string nm, string wt,  (Lesson, List<Curriculum>) SD)
        {
            timeslot = ts;
            name = nm;
            weektypes = wt;
            SData = SD;
        }
    }
    public partial class ScheduleView : ContentPage
    {
        public API.User.UserInfo usertype;

        public int id;

        Week weektype;

        public List<LessonItem> Shed { get; private set; } 

        public ScheduleView(API.User.UserInfo utype, int idd)
        {
            InitializeComponent();
            Shed = new List<LessonItem>();
            usertype = utype;
            id = idd;
            if (usertype == User.UserInfo.teacher)
            {
                var TeacheShed = API.TeacherMethods.RequestWeekSchedule(id);
                foreach (var LLC in TeacheShed)
                {
                    var tol = TimeOfLesson.Parse(LLC.Item1.timeslot);
                    Shed.Add(new LessonItem(tol.ToString(), LLC.Item2[0].subjectname, tol.week == -1 ? "" : tol.week.ToString(), LLC));
                }
            }
            else
            {
                var StudShed = StudentMethods.RequestWeekSchedule(id);
                foreach (var LLC in StudShed)
                {
                    var tol = TimeOfLesson.Parse(LLC.Item1.timeslot);
                    Shed.Add(new LessonItem(tol.ToString(), LLC.Item2[0].subjectname, tol.week == -1 ? "" : tol.week.ToString(),LLC));
                }
            }
            weektype = CurrentSubject.RequestCurrentWeek();
            BindingContext = this;

        }
    }
}
