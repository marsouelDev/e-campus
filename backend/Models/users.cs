using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public class User
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? Phone { get; set; }

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Role { get; set; } = Roles.Etudiant;

    // true = proposer le changement de mot de passe à la prochaine connexion (une seule fois)
    public bool MustChangePassword { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public StudentProfile? StudentProfile { get; set; }
    public TeacherProfile? TeacherProfile { get; set; }
    public AcademicManagerProfile? ManagerProfile { get; set; }
    public ParentProfile? ParentProfile { get; set; }
}