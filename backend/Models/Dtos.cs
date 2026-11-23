using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);

public record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, MinLength(8)] string NewPassword);

public record CreateUserRequest(
    [Required, EmailAddress] string Email,
    [Required] string FullName,
    [Required] string Role,
    string? Phone = null,
    DateOnly? BirthDate = null,
    string? ClassName = null,
    List<int>? ParentIds = null,
    string? Speciality = null,
    string? Department = null,
    string? Profession = null,
    string? Address = null);

// L'email et le rôle ne sont pas modifiables
public record UpdateUserRequest(
    [Required] string FullName,
    bool? IsActive = null,          // null = ne pas changer
    string? Phone = null,
    DateOnly? BirthDate = null,
    string? ClassName = null,
    List<int>? ParentIds = null,    // null = ne pas changer les parents
    string? Speciality = null,
    string? Department = null,
    string? Profession = null,
    string? Address = null);

public record UserResponse(
    int Id, string Email, string FullName, string Role, string? Phone,
    bool MustChangePassword, bool IsActive, DateTime CreatedAt,
    string? Matricule, DateOnly? BirthDate, string? ClassName, List<int> ParentIds,
    string? Speciality, string? Department,
    string? Profession, string? Address, List<int> ChildrenIds)
{
    public static UserResponse From(User u) => new(
        u.Id, u.Email, u.FullName, u.Role, u.Phone,
        u.MustChangePassword, u.IsActive, u.CreatedAt,
        u.StudentProfile?.Matricule ?? u.TeacherProfile?.Matricule,
        u.StudentProfile?.BirthDate,
        u.StudentProfile?.ClassName,
        u.StudentProfile?.Parents.Select(p => p.ParentUserId).ToList() ?? [],
        u.TeacherProfile?.Speciality,
        u.ManagerProfile?.Department,
        u.ParentProfile?.Profession,
        u.ParentProfile?.Address,
        u.ParentProfile?.Children.Select(c => c.StudentUserId).ToList() ?? []);
}