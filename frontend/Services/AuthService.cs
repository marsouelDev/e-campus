using Frontend.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Frontend.Services;

// Les « actions » du store : login, changement de mot de passe, déconnexion, restauration de la session
public sealed class AuthService(ApiClient api, AuthSession session, ProtectedSessionStorage storage)
{
    private const string StorageKey = "ecampus.token";
    private Task? _restore;

    // Une seule restauration par circuit (appelée par le fournisseur d'authentification et les pages)
    public Task EnsureRestoredAsync() => _restore ??= RestoreAsync();

    private async Task RestoreAsync()
    {
        try
        {
            var stored = await storage.GetAsync<string>(StorageKey);
            if (!stored.Success || string.IsNullOrEmpty(stored.Value)) return;

            // Revalide le jeton auprès de l'API : compte désactivé, jeton expiré...
            var user = await api.GetAsync<CurrentUser>("auth/me", stored.Value);
            if (user is not null) session.SignIn(stored.Value, user);
        }
        catch (ApiException)
        {
            await storage.DeleteAsync(StorageKey);
        }
        catch (InvalidOperationException)
        {
            _restore = null;   // JavaScript pas encore disponible : on réessaiera
        }
    }

    public async Task LoginAsync(LoginForm form)
    {
        var login = await api.SendAsync<LoginResponse>(HttpMethod.Post, "auth/login",
            new { email = form.Email.Trim(), password = form.Password })
            ?? throw new ApiException(0, "Réponse du serveur invalide.");

        var me = await api.GetAsync<CurrentUser>("auth/me", login.Token)
            ?? throw new ApiException(0, "Réponse du serveur invalide.");

        session.SignIn(login.Token, me with { MustChangePassword = login.MustChangePassword });
        await storage.SetAsync(StorageKey, login.Token);
    }

    public async Task ChangePasswordAsync(ChangePasswordForm form)
    {
        var result = await api.SendAsync<ChangePasswordResponse>(HttpMethod.Post, "auth/change-password",
            new { currentPassword = form.CurrentPassword, newPassword = form.NewPassword });

        if (result?.Token is { Length: > 0 } token)
        {
            session.UpdateToken(token);
            await storage.SetAsync(StorageKey, token);
        }
        if (session.User is { } user) session.UpdateUser(user with { MustChangePassword = false });
    }

    public async Task LogoutAsync()
    {
        await storage.DeleteAsync(StorageKey);
        session.SignOut();
    }
}