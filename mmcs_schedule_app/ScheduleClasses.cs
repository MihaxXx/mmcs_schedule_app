using Newtonsoft.Json;

namespace API;

/// <summary>
/// Занятие
/// </summary>
public class Lesson
{
    // Индентификатор
    public int Id { get; set; }

    // Другой идентификатор
    public int UberId { get; set; }

    // Количество предметов
    public int SubCount { get; set; }

    // Время предмета
    public string Timeslot { get; set; } = "";

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
public class Curriculum
{
    // Идентификатор
    public int Id { get; set; }

    // Идентификатор предмета
    public int LessonId { get; set; }

    // Число предметов
    public int SubNum { get; set; }

    // Идентификатор предмета
    public int SubjectId { get; set; }

    // Имя предмеита
    public string SubjectName { get; set; } = "";

    // Аббревиатура предмета
    public string SubjectAbbr { get; set; } = "";

    // Идентификатор преподавателя
    public int TeacherId { get; set; }

    // ФИО преподавателя
    public string TeacherName { get; set; } = "";

    // Ученая степень преподавателя
    public string TeacherDegree { get; set; } = "";

    // Идентификатор аудитории
    public int RoomId { get; set; }

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
public class SchOfGroup
{
    // Список предметов
    public List<Lesson> Lessons { get; set; } = [];

    // Список расписаний
    public List<Curriculum> Curricula { get; set; } = [];
}

/// <summary>
/// Расписание преподавателя
/// </summary>
public class SchOfTeacher
{
    // Список предметов
    public List<Lesson> Lessons { get; set; } = [];

    // Список расписаний
    public List<Curriculum> Curricula { get; set; } = [];
    //список групп
    public List<TechGroup> Groups { get; set; } = [];
}
/// <summary>
/// Группа
/// </summary>
public class Group
{
    // Идентификатор группы
    public int Id { get; set; }

    // Имя группы
    public string Name { get; set; } = "";

    [JsonProperty("num")]
    // Номер группы
    public int Number { get; set; }

    // Уровень группы
    public int GradeId { get; set; }
}

/// <summary>
/// Тип недели
/// </summary>
public enum WeekType
{
    /// <summary>
    /// Полная неделя
    /// </summary>
    Full = -1,

    /// <summary>
    /// Верхняя неделя
    /// </summary>
    Upper = 0,

    /// <summary>
    /// Нижняя неделя
    /// </summary>
    Lower = 1,

    /// <summary>
    /// Текущая неделя
    /// </summary>
    Current = 2
}

/// <summary>
/// Неделя
/// </summary>
public class Week
{
    [JsonProperty("week")]
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
public class Teacher
{
    // Индентификатор
    public int Id { get; set; }

    // ФИО
    public string Name { get; set; } = "";

    // Учёная степень
    public string Degree { get; set; } = "";

    public override string ToString()
    {
        return $"{Id}, {Name}, {Degree}";
    }
}


public class Grade
{
    // Индентификатор
    public int Id { get; set; }

    [JsonProperty("num")]
    // Номер
    public int Number { get; set; }

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
public class TechGroup
{
    public int UberId { get; set; }

    [JsonProperty("groupnum")]
    public int GroupNumber { get; set; }

    [JsonProperty("gradenum")]
    public int GradeNumber { get; set; }

    public string Degree { get; set; } = "";

    public string Name { get; set; } = "";

    public override string ToString()
    {
        return $"{UberId}, {GroupNumber}, {GradeNumber}, {Degree}, {Name}";
    }
}
