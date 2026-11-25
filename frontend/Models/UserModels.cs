namespace Frontend.Models;

// Forme supposée de UserResponse : à comparer avec votre DTO C#.
// Les champs de profil sont facultatifs : s'ils manquent, le formulaire de modification les enverra vides.
public sealed record UserResponse(
    int Id,
    string Email,
    string FullName,
    string? Phone,
    string Role,
    bool IsActive,
    bool MustChangePassword,
    string? Matricule = null,
    string? BirthDate = null,
    string? ClassName = null,
    List<int>? ParentIds = null,
    string? Speciality = null,
    string? Department = null,
    string? Profession = null,
    string? Address = null);

public sealed record CreateUserResult(UserResponse User, bool EmailSent);

public sealed record ResetPasswordResponse(bool EmailSent);