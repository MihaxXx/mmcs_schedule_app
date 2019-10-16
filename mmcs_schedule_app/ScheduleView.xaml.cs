using System;
using System.Collections.Generic;
using System.Linq;

using API;

using Xamarin.Forms;

namespace mmcs_schedule_app
{
    public class LessonItem
    {
        public string time { get; set; }
        public string name { get; set; }
        public string weektypes { get; set; }
        public TimeOfLesson timeslot;
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
        public API.User.UserInfo usertype;

        public int id;

        Week weektype;

        public IEnumerable<IGrouping<string,LessonItem>> GroupedShed { get; private set; }

        public ScheduleView(API.User.UserInfo utype, int idd)
        {
            InitializeComponent();
            var Shed = new List<LessonItem>();
            usertype = utype;
            id = idd;
            if (usertype == User.UserInfo.teacher)
            {
                foreach (var LLC in TeacherMethods.RequestWeekSchedule(id))
                {
                    var tol = TimeOfLesson.Parse(LLC.Item1.timeslot);
                    Shed.Add(new LessonItem(tol.ToString(), LLC.Item2[0].subjectname, tol.week == -1 ? "" : tol.week == 0 ? "верхняя неделя" : "нижняя неделя", tol, LLC));
                }
            }
            else
            {
                foreach (var LLC in StudentMethods.RequestWeekSchedule(id))
                {
                    var tol = TimeOfLesson.Parse(LLC.Item1.timeslot);
                    Shed.Add(new LessonItem(tol.ToString(), LLC.Item2[0].subjectname, tol.week == -1 ? "" : tol.week == 0 ? "верхняя неделя" : "нижняя неделя", tol, LLC));
                }
            }
            //Gets russian day names, possible to use CurrentInfo, but app has no localization, so no reason for that
            var DayNames = new System.Globalization.CultureInfo("ru-RU").DateTimeFormat.DayNames;
            GroupedShed = Shed.OrderBy(l => l.timeslot.day).ThenBy(l => l.timeslot.starth * 60 + l.timeslot.startm).ThenBy(l=> l.timeslot.week).
                GroupBy(l => DayNames[(l.timeslot.day+1)%7]);
            weektype = CurrentSubject.RequestCurrentWeek();
            BindingContext = this;

        }
    }
}
