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

        /// <summary>
        /// The last access time.
        /// </summary>
        public DateTime LastAccess = new DateTime(2019,4,23,17,30,00); //presentation date

        /// <summary>
        /// Possible types of users
        /// </summary>
        public enum UserInfo { teacher, bachelor, master, graduate };
    }
}
