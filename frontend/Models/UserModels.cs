namespace Frontend.Models;

// Forme supposée de UserResponse : à comparer avec votre DTO C#
public sealed record UserResponse(
    int Id,
    string Email,
    string FullName,
    string? Phone,
    string Role,
    bool IsActive,
    bool MustChangePassword);

public sealed record CreateUserResult(UserResponse User, bool EmailSent);

public sealed record ResetPasswordResponse(bool EmailSent);