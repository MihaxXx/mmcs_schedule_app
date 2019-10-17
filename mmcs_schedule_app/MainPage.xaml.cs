using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Newtonsoft.Json;

namespace mmcs_schedule_app
{
    // Learn more about making custom code visible in the Xamarin.Forms previewer
    // by visiting https://aka.ms/xamarinforms-previewer
    [DesignTimeVisible(false)]
    public partial class MainPage : ContentPage
    {
        API.Grade[] Grades;
        API.Teacher[] Teachers;
        API.User user = new API.User();
        string _fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "settings.json");
        public bool _coldstart = true;
        ScheduleView schedule;

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (File.Exists(_fileName)&& _coldstart)
            {
                user = JsonConvert.DeserializeObject<API.User>(File.ReadAllText(_fileName, Encoding.UTF8));
                Role.SelectedIndex = user.Info == API.User.UserInfo.teacher ? 1 : 0;
                List_NmOrGr.SelectedIndex = user.list1selID;
                if (user.Info != API.User.UserInfo.teacher)
                    List_Groups.SelectedIndex = user.list2selID;
                Ok_btnClicked(Ok_btn, new EventArgs());
            }
            _coldstart = false;
        }


        public MainPage()
        {
            InitializeComponent();
            try
            {
                Grades = API.GradeMethods.GetGradesList();
                Teachers = API.TeacherMethods.GetTeachersList();
                ErrorLabel.IsEnabled = ErrorLabel.IsVisible = false;
            }
            catch (System.Net.WebException)
            {
                ErrorLabel.IsEnabled = ErrorLabel.IsVisible = true;
            }

        }
        private void Role_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = sender as Picker;
            if (picker.SelectedIndex == 0)
            {
                //Set to fix user is a student, later reaasigned to correct grade
                user.Info = API.User.UserInfo.bachelor;
                List_NmOrGr.Items.Clear();
                List_NmOrGr.Title = "Курс";
                foreach (var gr in Grades)
                    List_NmOrGr.Items.Add(StuDegree(gr.degree) + " " + gr.num + " курс");
            }
            else if (picker.SelectedIndex == 1)
            {
                user.Info = API.User.UserInfo.teacher;
                List_NmOrGr.Items.Clear();
                List_NmOrGr.Title = "Имя";
                foreach (var t in Teachers)
                    List_NmOrGr.Items.Add(t.name);
            }
            List_NmOrGr.SelectedIndex = -1;
            List_NmOrGr.IsVisible = true;
            List_Groups.IsVisible = false;
            Ok_btn.IsEnabled = false;
        }
        private void List_NmOrGr_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = sender as Picker;
            if (picker.SelectedIndex == -1)
                return;
            user.list1selID = picker.SelectedIndex;
            if (user.Info == API.User.UserInfo.teacher)
            {
                List_Groups.IsVisible = false;
                user.teacherId = Teachers.First(t => t.name == picker.Items[picker.SelectedIndex]).id;
                Ok_btn.IsEnabled = true;                
            }
            else
            {
                switch(Grades[List_NmOrGr.SelectedIndex].degree)
                {
                    case "bachelor": user.Info = API.User.UserInfo.bachelor; break;
                    case "master": user.Info = API.User.UserInfo.master; break; 
                    case "specialist": user.Info = API.User.UserInfo.bachelor; break; 
                    case "postgraduate": user.Info = API.User.UserInfo.graduate; break;
                }
                user.course = Grades[List_NmOrGr.SelectedIndex].num;
                try
                {
                    for (int i = 0; i < Grades.Length; i++)
                    {
                        Grades[i].Groups = API.GradeMethods.GetGroupsList(Grades[i].id).ToList();
                    }

                    List_Groups.IsVisible = true;
                    List_Groups.Items.Clear();
                    foreach (var g in Grades[List_NmOrGr.SelectedIndex].Groups)
                        List_Groups.Items.Add(g.name + " группа " + g.num);
                    List_Groups.SelectedIndex = -1;
                    Ok_btn.IsEnabled = false;
                    ErrorLabel.IsEnabled = ErrorLabel.IsVisible = false;
                }
                catch (System.Net.WebException)
                {
                    ErrorLabel.IsEnabled = ErrorLabel.IsVisible = true;
                }
            }
        }
        private void List_Groups_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = sender as Picker;
            if (picker.SelectedIndex == -1)
                return;
            user.list2selID = List_Groups.SelectedIndex;
            user.group = Grades[List_NmOrGr.SelectedIndex].Groups[List_Groups.SelectedIndex].num;
            user.groupid = Grades[List_NmOrGr.SelectedIndex].Groups[List_Groups.SelectedIndex].id;
            Ok_btn.IsEnabled = true;
        }

        async private void Ok_btnClicked(object sender, EventArgs e)
        {
            File.WriteAllText(_fileName, JsonConvert.SerializeObject(user, Formatting.Indented), Encoding.UTF8);
            schedule = new ScheduleView(user.Info, user.Info == API.User.UserInfo.teacher ? user.teacherId : user.groupid)
            {
                Title = user.Info == API.User.UserInfo.teacher ? List_NmOrGr.Items[List_NmOrGr.SelectedIndex] :
                string.Join(" ", StuDegreeShort(user.Info.ToString()), Grades[List_NmOrGr.SelectedIndex].Groups[List_Groups.SelectedIndex].name, user.course + "." + user.group),
            };
          
            await Navigation.PushAsync(schedule);
            
        }

        public static string StuDegreeShort(string degree)
        {
            switch (degree)
            {
                case "bachelor": return "Бак.";
                case "master": return "Маг.";
                case "specialist": return "Спец.";
                case "graduate": return "Асп.";
                default: return "н/д";
            }
        }
        public static string StuDegree(string degree)
        {
            switch (degree)
            {
                case "bachelor": return "Бакалавриат";
                case "master": return "Магистратура";
                case "specialist": return "Специалитет";
                case "postgraduate": return "Аспирантура";
                default: return "н/д";
            }
        }
    }
}
