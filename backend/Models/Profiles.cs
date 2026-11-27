using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class StudentProfile
{
    [Key] public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, MaxLength(30)]
    public string Matricule { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    [MaxLength(100)]
    public string? ClassName { get; set; }

    public List<ParentStudent> Parents { get; set; } = [];
}

public class TeacherProfile
{
    [Key] public int UserId { get; set; }
    public User User { get; set; } = null!;

    [Required, MaxLength(30)]
    public string Matricule { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Speciality { get; set; }
}

public class AcademicManagerProfile
{
    [Key] public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(150)]
    public string? Department { get; set; }
}

public class ParentProfile
{
    [Key] public int UserId { get; set; }
    public User User { get; set; } = null!;

    [MaxLength(150)]
    public string? Profession { get; set; }

    [MaxLength(250)]
    public string? Address { get; set; }

    public List<ParentStudent> Children { get; set; } = [];
}

public class ParentStudent
{
    public int ParentUserId { get; set; }
    public ParentProfile Parent { get; set; } = null!;

    public int StudentUserId { get; set; }
    public StudentProfile Student { get; set; } = null!;
}