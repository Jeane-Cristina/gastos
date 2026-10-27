using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GastosApi.Tests.Integration;

public sealed record AuthResponse(string Token, string RefreshToken);

public static class ApiTestHelpers
{
    public const string ValidPassword = "Senha1234";

    private static int _ipCounter;

    /// <summary>Cliente HTTP que "chega" de um IP próprio, para o rate limit de um teste não afetar os outros.</summary>
    public static HttpClient CreateClientWithOwnIp(this ApiFactory factory)
    {
        var n = Interlocked.Increment(ref _ipCounter);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Ip", $"10.1.{n / 250}.{n % 250 + 1}");
        return client;
    }

    /// <summary>Cria um usuário novo pela API (cadastro) e devolve um cliente já autenticado como ele.</summary>
    public static async Task<(HttpClient Client, AuthResponse Auth, string Username)> RegisterUserAsync(this ApiFactory factory)
    {
        var client = factory.CreateClientWithOwnIp();
        var username = $"user-{Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/api/auth/register", new { username, password = ValidPassword });
        response.EnsureSuccessStatusCode();

        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth, username);
    }

    /// <summary>Entra na conta demo pela API e devolve um cliente autenticado nela.</summary>
    public static async Task<(HttpClient Client, AuthResponse Auth)> StartDemoAsync(this ApiFactory factory)
    {
        var client = factory.CreateClientWithOwnIp();

        var response = await client.PostAsync("/api/auth/demo", content: null);
        response.EnsureSuccessStatusCode();

        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, auth);
    }

    public static async Task<int> CreateExpenseAsync(this HttpClient client, string description, decimal amount = 25m)
    {
        var response = await client.PostAsJsonAsync("/api/expenses", new
        {
            description,
            amount,
            category = "Alimentação",
            date = DateTime.UtcNow
        });
        response.EnsureSuccessStatusCode();

        var created = await response.Content.ReadFromJsonAsync<JsonElement>();
        return created.GetProperty("id").GetInt32();
    }

    /// <summary>Descrições das despesas que o usuário enxerga na listagem.</summary>
    public static async Task<List<string>> ListExpenseDescriptionsAsync(this HttpClient client)
    {
        var page = await client.GetFromJsonAsync<JsonElement>("/api/expenses?page=1&pageSize=100");
        return page.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("description").GetString()!)
            .ToList();
    }

    public static JsonContent AsJsonBody(this string value) => JsonContent.Create(value);
}
