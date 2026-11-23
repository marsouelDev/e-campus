using Frontend.Models;

namespace Frontend.Services;

// Équivalent du authStore : l'état d'authentification. Enregistré en Scoped = un exemplaire par circuit Blazor,
// donc jamais partagé entre deux visiteurs.
public sealed class AuthSession
{
    public string? Token { get; private set; }
    public CurrentUser? User { get; private set; }
    public bool IsAuthenticated => Token is not null && User is not null;

    public event Action? Changed;

    public void SignIn(string token, CurrentUser user)
    {
        Token = token;
        User = user;
        Changed?.Invoke();
    }

    public void UpdateToken(string token) => Token = token;

    public void UpdateUser(CurrentUser user)
    {
        User = user;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        Token = null;
        User = null;
        Changed?.Invoke();
    }
}