using Frontend.Models;

namespace Frontend.Services;

public sealed class UserService(ApiClient api)
{
    public async Task<List<UserResponse>> GetAllAsync(string? role = null) =>
        await api.GetAsync<List<UserResponse>>(role is null ? "users/all" : $"users/all?role={Uri.EscapeDataString(role)}") ?? [];

    public Task<UserResponse?> GetAsync(int id) => api.GetAsync<UserResponse>($"users/{id}");

    
    public async Task<CreateUserResult> CreateAsync(CreateUserForm form)
    {
        var result = await api.SendWithHeadersAsync<UserResponse>(HttpMethod.Post, "users/create", new
        {
            fullName = form.FullName.Trim(),
            email = form.Email.Trim(),
            role = form.Role,
            phone = string.IsNullOrWhiteSpace(form.Phone) ? null : form.Phone.Trim(),
        });
        var sent = result.Headers.TryGetValue("X-Email-Sent", out var v) && v == "true";
        return new CreateUserResult(result.Data!, sent);
    }

    public Task DeleteAsync(int id) => api.SendAsync<object>(HttpMethod.Delete, $"users/delete/{id}");

    public async Task<bool> ResetPasswordAsync(int id) =>
        (await api.SendAsync<ResetPasswordResponse>(HttpMethod.Post, $"users/{id}/reset-password"))?.EmailSent ?? false;
}