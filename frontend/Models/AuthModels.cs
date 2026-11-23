namespace Frontend.Models;

public sealed record CurrentUser(int Id, string Email, string FullName, string Role, bool MustChangePassword);

public sealed record LoginResponse(string Token, bool MustChangePassword);

public sealed record ChangePasswordResponse(string? Token);