using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Frontend.Models;

public sealed class LoginForm
{
    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Email invalide")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Le mot de passe est obligatoire")]
    public string Password { get; set; } = "";
}

public sealed class ChangePasswordForm : IValidatableObject
{
    [Required(ErrorMessage = "Le mot de passe actuel est obligatoire")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Le nouveau mot de passe est obligatoire")]
    [MinLength(8, ErrorMessage = "8 caractères minimum")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Veuillez confirmer le mot de passe")]
    [Compare(nameof(NewPassword), ErrorMessage = "Les mots de passe ne correspondent pas")]
    public string ConfirmPassword { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!string.IsNullOrEmpty(NewPassword) && NewPassword == CurrentPassword)
            yield return new ValidationResult("Le nouveau mot de passe doit être différent de l'ancien", [nameof(NewPassword)]);
    }
}

public sealed class UserForm
{
    public int? Id { get; set; }                      
    public bool IsEdit => Id is not null;

    [Required(ErrorMessage = "Le nom est obligatoire")]
    [StringLength(150, MinimumLength = 6, ErrorMessage = "Entre 2 et 150 caractères")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "L'email est obligatoire")]
    [EmailAddress(ErrorMessage = "Email invalide")]
    [StringLength(190, ErrorMessage = "190 caractères maximum")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Choisissez un rôle")]
    public string Role { get; set; } = Roles.Etudiant;

    [StringLength(30, ErrorMessage = "30 caractères maximum")]
    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    // Étudiant
    public DateOnly? BirthDate { get; set; }
    [StringLength(100, ErrorMessage = "100 caractères maximum")]
    public string? ClassName { get; set; }
    public List<int> ParentIds { get; set; } = [];

    // Enseignant
    [StringLength(100, ErrorMessage = "100 caractères maximum")]
    public string? Speciality { get; set; }

    // Responsable
    [StringLength(100, ErrorMessage = "100 caractères maximum")]
    public string? Department { get; set; }

    // Parent
    [StringLength(100, ErrorMessage = "100 caractères maximum")]
    public string? Profession { get; set; }
    [StringLength(255, ErrorMessage = "255 caractères maximum")]
    public string? Address { get; set; }

    public static UserForm From(UserResponse u) => new()
    {
        Id = u.Id,
        FullName = u.FullName,
        Email = u.Email,
        Role = u.Role,
        Phone = u.Phone,
        IsActive = u.IsActive,
        BirthDate = ParseDate(u.BirthDate),
        ClassName = u.ClassName,
        ParentIds = [.. u.ParentIds ?? []],
        Speciality = u.Speciality,
        Department = u.Department,
        Profession = u.Profession,
        Address = u.Address,
    };

    private static DateOnly? ParseDate(string? s) =>
        s is { Length: >= 10 } && DateOnly.TryParse(s[..10], CultureInfo.InvariantCulture, out var d) ? d : null;
}