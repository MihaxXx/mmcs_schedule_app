using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

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
                //FIXME Incorrect assign (not always a bachelor)
                user.Info = API.User.UserInfo.bachelor;
                List_NmOrGr.Items.Clear();
                List_NmOrGr.Title = "Grade";
                foreach (var gr in Grades)
                    List_NmOrGr.Items.Add(StuDegree(gr.degree) + " " + gr.num + " курс");
            }
            else if (picker.SelectedIndex == 1)
            {
                user.Info = API.User.UserInfo.teacher;
                List_NmOrGr.Items.Clear();
                List_NmOrGr.Title = "Name";
                foreach (var t in Teachers)
                    List_NmOrGr.Items.Add(t.name);
            }
            List_NmOrGr.SelectedIndex = -1;
            List_Groups.IsVisible = false;
            Ok_btn.IsEnabled = false;
        }
        private void List_NmOrGr_SelectedIndexChanged(object sender, EventArgs e)
        {
            var picker = sender as Picker;
            if (picker.SelectedIndex == -1)
                return;
            if (user.Info == API.User.UserInfo.teacher)
            {
                List_Groups.IsVisible = false;
                user.teacherId = Teachers.First(t => t.name == picker.Items[picker.SelectedIndex]).id;
                Ok_btn.IsEnabled = true;                
            }
            else
            {
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
            user.groupid = Grades[List_NmOrGr.SelectedIndex].Groups[List_Groups.SelectedIndex].id;
            Ok_btn.IsEnabled = true;
        }

        async private void Ok_btnClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ScheduleView());
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
