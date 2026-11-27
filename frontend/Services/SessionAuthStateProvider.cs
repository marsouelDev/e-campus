using System.Security.Claims;
using Frontend.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Frontend.Services;

public sealed class SessionAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly AuthSession _session;
    private readonly AuthService _auth;

    public SessionAuthStateProvider(AuthSession session, AuthService auth)
    {
        _session = session;
        _auth = auth;
        _session.Changed += OnChanged;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await _auth.EnsureRestoredAsync();
        return new AuthenticationState(Build(_session.User));
    }

    private void OnChanged() =>
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Build(_session.User))));

    private static ClaimsPrincipal Build(CurrentUser? user)
    {
        if (user is null) return new ClaimsPrincipal(new ClaimsIdentity());

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
        ], authenticationType: "ecampus-jwt");
        return new ClaimsPrincipal(identity);
    }

    public void Dispose() => _session.Changed -= OnChanged;
}