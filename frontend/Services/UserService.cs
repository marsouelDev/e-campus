using Frontend.Models;

namespace Frontend.Services;

public sealed class UserService(ApiClient api)
{
    public async Task<List<UserResponse>> GetAllAsync(string? role = null) =>
        await api.GetAsync<List<UserResponse>>(role is null ? "users/all" : $"users/all?role={Uri.EscapeDataString(role)}") ?? [];

    public Task<UserResponse?> GetAsync(int id) => api.GetAsync<UserResponse>($"users/{id}");

    public async Task<CreateUserResult> CreateAsync(UserForm form)
    {
        var body = new
        {
            fullName = form.FullName.Trim(),
            email = form.Email.Trim(),
            role = form.Role,
            phone = Clean(form.Phone),
            birthDate = Date(form),
            className = Clean(form.ClassName),
            speciality = Clean(form.Speciality),
            department = Clean(form.Department),
            profession = Clean(form.Profession),
            address = Clean(form.Address),
            parentIds = form.Role == Roles.Etudiant ? form.ParentIds : null,
        };
        var result = await api.SendWithHeadersAsync<UserResponse>(HttpMethod.Post, "users/create", body);
        var sent = result.Headers.TryGetValue("X-Email-Sent", out var v) && v == "true";
        return new CreateUserResult(result.Data!, sent);
    }

    public Task<UserResponse?> UpdateAsync(int id, UserForm form) =>
        api.SendAsync<UserResponse>(HttpMethod.Put, $"users/update/{id}", new
        {
            fullName = form.FullName.Trim(),
            phone = Clean(form.Phone),
            isActive = form.IsActive,
            birthDate = Date(form),
            className = Clean(form.ClassName),
            speciality = Clean(form.Speciality),
            department = Clean(form.Department),
            profession = Clean(form.Profession),
            address = Clean(form.Address),
            parentIds = form.Role == Roles.Etudiant ? form.ParentIds : null,   // null = ne pas toucher aux parents
        });

    public Task DeleteAsync(int id) => api.SendAsync<object>(HttpMethod.Delete, $"users/delete/{id}");

    public async Task<bool> ResetPasswordAsync(int id) =>
        (await api.SendAsync<ResetPasswordResponse>(HttpMethod.Post, $"users/{id}/reset-password"))?.EmailSent ?? false;

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string? Date(UserForm f) => f.Role == Roles.Etudiant ? f.BirthDate?.ToString("yyyy-MM-dd") : null;
}