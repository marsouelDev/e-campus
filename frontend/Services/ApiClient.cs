using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Frontend.Services;

public sealed class ApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

public sealed record ApiResult<T>(T? Data, IReadOnlyDictionary<string, string> Headers);

public sealed class ApiClient(IHttpClientFactory factory, AuthSession session)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Task<T?> GetAsync<T>(string path, string? token = null, CancellationToken ct = default) =>
        SendAsync<T>(HttpMethod.Get, path, null, token, ct);

    public async Task<T?> SendAsync<T>(HttpMethod method, string path, object? body = null, string? token = null, CancellationToken ct = default) =>
        (await SendWithHeadersAsync<T>(method, path, body, token, ct)).Data;

    public async Task<ApiResult<T>> SendWithHeadersAsync<T>(HttpMethod method, string path, object? body = null, string? token = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(method, path.TrimStart('/'));
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);

        var jwt = token ?? session.Token;
        if (jwt is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);

        HttpResponseMessage response;
        try
        {
            response = await factory.CreateClient("api").SendAsync(request, ct);
        }
        catch (HttpRequestException)
        {
            throw new ApiException(0, "Impossible de joindre le serveur. Réessayez dans un instant.");
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                throw new ApiException(response.StatusCode, ErrorMessage(response.StatusCode, text));

            var headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
            var data = string.IsNullOrWhiteSpace(text) ? default : JsonSerializer.Deserialize<T>(text, Json);
            return new ApiResult<T>(data, headers);
        }
    }

    // { "message": "..." } (vos contrôleurs), ou erreurs de validation ASP.NET
    private static string ErrorMessage(HttpStatusCode status, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String) return m.GetString()!;
                if (root.TryGetProperty("errors", out var e) && e.ValueKind == JsonValueKind.Object)
                    foreach (var field in e.EnumerateObject())
                        foreach (var msg in field.Value.EnumerateArray())
                            return msg.GetString() ?? "Données invalides.";
                if (root.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String) return t.GetString()!;
            }
        }
        catch (JsonException) { /* corps vide ou non JSON */ }

        return status switch
        {
            HttpStatusCode.Unauthorized => "Session expirée. Veuillez vous reconnecter.",
            HttpStatusCode.Forbidden => "Accès refusé : droits insuffisants.",
            HttpStatusCode.NotFound => "Ressource introuvable.",
            _ => $"Erreur {(int)status}",
        };
    }
}