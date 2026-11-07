using System.Runtime.Serialization;

namespace API;

/// <summary>
/// Занятие
/// </summary>
[DataContract]
public class Lesson
{
    [DataMember(Name = "id")]
    // Индентификатор
    public int Id { get; set; }

    [DataMember(Name = "uberid")]
    // Другой идентификатор
    public int UberId { get; set; }

    [DataMember(Name = "subcount")]
    // Количество предметов
    public int SubCount { get; set; }

    [DataMember(Name = "timeslot")]
    // Время предмета
    public string Timeslot { get; set; } = "";

    [DataMember(Name = "info")]
    // Доп информация
    public string Info { get; set; } = "";

    public override string ToString()
    {
        return $"{Id} {UberId} {SubCount} {Timeslot}";
    }
}
/// <summary>
/// Данные о занятии
/// </summary>
[DataContract]
public class Curriculum
{
    [DataMember(Name = "id")]
    // Идентификатор
    public int Id { get; set; }

    [DataMember(Name = "lessonid")]
    // Идентификатор предмета
    public int LessonId { get; set; }

    [DataMember(Name = "subnum")]
    // Число предметов
    public int SubNum { get; set; }

    [DataMember(Name = "subjectid")]
    // Идентификатор предмета
    public int SubjectId { get; set; }

    [DataMember(Name = "subjectname")]
    // Имя предмеита
    public string SubjectName { get; set; } = "";

    [DataMember(Name = "subjectabbr")]
    // Аббревиатура предмета
    public string SubjectAbbr { get; set; } = "";

    [DataMember(Name = "teacherid")]
    // Идентификатор преподавателя
    public int TeacherId { get; set; }

    [DataMember(Name = "teachername")]
    // ФИО преподавателя
    public string TeacherName { get; set; } = "";

    [DataMember(Name = "teacherdegree")]
    // Ученая степень преподавателя
    public string TeacherDegree { get; set; } = "";

    [DataMember(Name = "roomid")]
    // Идентификатор аудитории
    public int RoomId { get; set; }

    [DataMember(Name = "roomname")]
    // Наименование аудитории
    public string RoomName { get; set; } = "";

    public override string ToString()
    {
        return $"{SubjectName}, преп. {TeacherName}, ауд. {RoomName}";
    }
}

/// <summary>
/// Расписание группы
/// </summary>
[DataContract]
public class SchOfGroup
{
    [DataMember(Name = "lessons")]
    // Список предметов
    public List<Lesson> Lessons { get; set; } = [];

    [DataMember(Name = "curricula")]
    // Список расписаний
    public List<Curriculum> Curricula { get; set; } = [];
}

/// <summary>
/// Расписание преподавателя
/// </summary>
[DataContract]
public class SchOfTeacher
{
    [DataMember(Name = "lessons")]
    // Список предметов
    public List<Lesson> Lessons { get; set; } = [];

    [DataMember(Name = "curricula")]
    // Список расписаний
    public List<Curriculum> Curricula { get; set; } = [];
    [DataMember(Name = "groups")]
    //список групп
    public List<TechGroup> Groups { get; set; } = [];
}
/// <summary>
/// Группа
/// </summary>
[DataContract]
public class Group
{
    [DataMember(Name = "id")]
    // Идентификатор группы
    public int Id { get; set; }

    [DataMember(Name = "name")]
    // Имя группы
    public string Name { get; set; } = "";

    [DataMember(Name = "num")]
    // Номер группы
    public int Number { get; set; }

    [DataMember(Name = "gradeid")]
    // Уровень группы
    public int GradeId { get; set; }
}

/// <summary>
/// Тип недели
/// </summary>
[DataContract]
public enum WeekType
{
    /// <summary>
    /// Полная неделя
    /// </summary>
    [EnumMember]
    Full = -1,

    /// <summary>
    /// Верхняя неделя
    /// </summary>
    [EnumMember]
    Upper = 0,

    /// <summary>
    /// Нижняя неделя
    /// </summary>
    [EnumMember]
    Lower = 1,

    /// <summary>
    /// Текущая неделя
    /// </summary>
    [EnumMember]
    Current = 2
}

/// <summary>
/// Неделя
/// </summary>
[DataContract]
public class Week
{
    [DataMember(Name = "week")]
    public WeekType Type { get; set; }

    public override string ToString()
    {
        return Type switch
        {
            WeekType.Upper => "верхняя",
            WeekType.Lower => "нижняя",
            WeekType.Current => "текущая",
            WeekType.Full => "полная",
            _ => Type.ToString()
        };
    }

    public WeekType ReversedWeek()
    {
        return Type switch
        {
            WeekType.Upper => WeekType.Lower,
            WeekType.Lower => WeekType.Upper,
            _ => Type
        };
    }
}

/// <summary>
/// Преподаватель (для списка преподов)
/// </summary>
[DataContract]
public class Teacher
{
    [DataMember(Name = "id")]
    // Индентификатор
    public int Id { get; set; }

    [DataMember(Name = "name")]
    // ФИО
    public string Name { get; set; } = "";

    [DataMember(Name = "degree")]
    // Учёная степень
    public string Degree { get; set; } = "";

    public override string ToString()
    {
        return $"{Id}, {Name}, {Degree}";
    }
}


[DataContract]
public class Grade
{
    [DataMember(Name = "id")]
    // Индентификатор
    public int Id { get; set; }

    [DataMember(Name = "num")]
    // Номер
    public int Number { get; set; }

    [DataMember(Name = "degree")]
    // Звание студента
    public string Degree { get; set; } = "";

    // Список групп
    public List<Group> Groups { get; set; } = [];

    public override string ToString()
    {
        return $"{Id}, {Degree}";
    }
}
/// <summary>
/// Класс группы
/// </summary>
[DataContract]
public class TechGroup
{
    [DataMember(Name = "uberid")]
    public int UberId { get; set; }

    [DataMember(Name = "groupnum")]
    public int GroupNumber { get; set; }

    [DataMember(Name = "gradenum")]
    public int GradeNumber { get; set; }

    [DataMember(Name = "degree")]
    public string Degree { get; set; } = "";

    [DataMember(Name = "name")]
    public string Name { get; set; } = "";

    public override string ToString()
    {
        return $"{UberId}, {GroupNumber}, {GradeNumber}, {Degree}, {Name}";
    }
}
