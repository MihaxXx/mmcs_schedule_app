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
        public string room { get; set; }
        public string who { get; set; }
        public TimeOfLesson timeslot { get; set; }
        public (Lesson, List<Curriculum>, List<TechGroup>) TData { get; private set; }
        public (Lesson, List<Curriculum>) SData { get; private set; }

    public LessonItem(string tm,string nm,string wt,string r, string w, TimeOfLesson ts, (Lesson, List<Curriculum>, List<TechGroup>) TD)
        {
            time = tm;
            name = nm;
            weektypes = wt;
            room = r;
            who = w;
            timeslot = ts;
            TData = TD;
        }
        public LessonItem(string tm, string nm, string wt, string r, string w, TimeOfLesson ts,  (Lesson, List<Curriculum>) SD)
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
        Week weektype;

        List<LessonItem> Shed = new List<LessonItem>();

        public IEnumerable<IGrouping<string,LessonItem>> GroupedShed { get; private set; }

        List<string> DayNames;

        public ScheduleView()
        {
            InitializeComponent();
            Title = App.user.header;
            if (App.user.Info == User.UserInfo.teacher)
            {
                foreach (var LLC in TeacherMethods.RequestWeekSchedule(App.user.teacherId))
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
                foreach (var LLC in StudentMethods.RequestWeekSchedule(App.user.groupid))
                {
                    var tol = TimeOfLesson.Parse(LLC.Item1.timeslot);
                    //Go thought list of Curriculums (present subj for timeslot)
                    foreach (var LC in LLC.Item2.ToLookup(lc => lc.subjectid).Select(coll => coll.First()))
                        Shed.Add(new LessonItem(tol.ToString(), LC.subjectname,
                            tol.week == -1 ? "" : tol.week == 0 ? "верхняя неделя" : "нижняя неделя",LC.roomname,
                            string.Join("\n", LLC.Item2.Select(c => $"• {c.teachername}\n  {c.roomname}")),
                            tol, (LLC.Item1,LLC.Item2.Where(c =>c.subjectid==LC.subjectid).ToList())));
                }
            }
            //Gets russian day names, possible to use CurrentInfo, but app has no localization, so no reason for that
            DayNames = new System.Globalization.CultureInfo("ru-RU").DateTimeFormat.DayNames.ToList();
            GroupedShed = Shed.OrderBy(l => l.timeslot.day).ThenBy(l => l.timeslot.starth * 60 + l.timeslot.startm).ThenBy(l => l.timeslot.week).
                GroupBy(l => DayNames[(l.timeslot.day + 1) % 7]);
            weektype = CurrentSubject.RequestCurrentWeek();
            //Must be at the end!!!
            BindingContext = this;
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
        //TODO: make popup intractive https://forums.xamarin.com/discussion/102828/best-way-to-create-non-full-screen-popup
        void OnListItemTapped(object sender, ItemTappedEventArgs e)
        {
            ((ListView)sender).SelectedItem = null;
            var item = (LessonItem)e.Item;
            List<string> info = new List<string>{ DayNames[(item.timeslot.day + 1) % 7], item.time.Replace("\n", " - "), item.who };
            if (item.TData.Item1 != null)
                info.Insert(2, "a." + item.room);
            DisplayAlert(item.name, string.Join("\n\n", info), "OK");
        }
    }
}
