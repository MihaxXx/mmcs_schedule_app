using System;


namespace API
{
    public class User
    {
        /// <summary>
        /// Type of user
        /// </summary>
        public UserInfo Info;
        /// <summary>
        /// Id of teacher
        /// </summary>
        public int teacherId = 0;
        /// <summary>
        /// Id of user`s group
        /// </summary>
        public int groupid = 0;

        public int course = 0;

        public int group = 0;

        public int list1selID = -1;

        public int list2selID = -1;

        public string header;

        /// <summary>
        /// Possible types of users
        /// </summary>
        public enum UserInfo { teacher, bachelor, master, graduate };
    }
}
