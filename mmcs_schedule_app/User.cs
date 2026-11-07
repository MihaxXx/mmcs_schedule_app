using Newtonsoft.Json;

namespace API;

public class User
{
    /// <summary>
    /// Type of user
    /// </summary>
    public UserInfo Info;
    /// <summary>
    /// Id of teacher
    /// </summary>
    [JsonProperty("teacherId")]
    public int TeacherId;
    /// <summary>
    /// Id of user`s group
    /// </summary>
    [JsonProperty("groupid")]
    public int GroupId;

    /// <summary>
    /// Id of student`s grade, 0 in settings saved before it was stored
    /// </summary>
    [JsonProperty("gradeId")]
    public int GradeId;

    [JsonProperty("course")]
    public int Course;

    [JsonProperty("group")]
    public int GroupNumber;

    [JsonProperty("list1selID")]
    public int GradeOrTeacherIndex = -1;

    [JsonProperty("list2selID")]
    public int GroupIndex = -1;

    [JsonProperty("header")]
    public string Header = "";

    /// <summary>
    /// Possible types of users
    /// </summary>
    public enum UserInfo { Teacher, Bachelor, Master, Graduate }
}
