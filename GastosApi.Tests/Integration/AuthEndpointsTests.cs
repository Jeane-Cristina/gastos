using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace GastosApi.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public AuthEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ReturnsTokenAndRefreshToken()
    {
        var client = _factory.CreateClientWithOwnIp();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { username = $"user-{Guid.NewGuid():N}", password = ApiTestHelpers.ValidPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
    }

    [Fact]
    public async Task Register_WithTakenUsername_Returns409()
    {
        var (_, _, username) = await _factory.RegisterUserAsync();
        var client = _factory.CreateClientWithOwnIp();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { username, password = ApiTestHelpers.ValidPassword });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400()
    {
        var client = _factory.CreateClientWithOwnIp();

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { username = $"user-{Guid.NewGuid():N}", password = "fraca" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsTokens()
    {
        var (_, _, username) = await _factory.RegisterUserAsync();
        var client = _factory.CreateClientWithOwnIp();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { username, password = ApiTestHelpers.ValidPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.False(string.IsNullOrWhiteSpace(auth!.Token));
        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var (_, _, username) = await _factory.RegisterUserAsync();
        var client = _factory.CreateClientWithOwnIp();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password = "SenhaErrada1" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_RateLimit_BlocksTheSameIpButNotAnotherOne()
    {
        var attacker = _factory.CreateClientWithOwnIp();
        var attempt = new { username = "nao-existe", password = "qualquer" };

        for (var i = 0; i < 5; i++)
        {
            var allowed = await attacker.PostAsJsonAsync("/api/auth/login", attempt);
            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        var blocked = await attacker.PostAsJsonAsync("/api/auth/login", attempt);
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);
        Assert.True(blocked.Headers.Contains("Retry-After"));
        var body = await blocked.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("message").GetString()));

        // outro usuário, outro IP: não é afetado pelo bloqueio do primeiro
        var innocent = _factory.CreateClientWithOwnIp();
        var response = await innocent.PostAsJsonAsync("/api/auth/login", attempt);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesTheTokens_AndTheOldRefreshTokenStopsWorking()
    {
        var (_, auth, _) = await _factory.RegisterUserAsync();
        var client = _factory.CreateClientWithOwnIp();

        var first = await client.PostAsync("/api/auth/refresh", auth.RefreshToken.AsJsonBody());
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var renewed = await first.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotEqual(auth.RefreshToken, renewed!.RefreshToken);

        var reuse = await client.PostAsync("/api/auth/refresh", auth.RefreshToken.AsJsonBody());
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        var withNew = await client.PostAsync("/api/auth/refresh", renewed.RefreshToken.AsJsonBody());
        Assert.Equal(HttpStatusCode.OK, withNew.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithUnknownOrEmptyToken_Returns401()
    {
        var client = _factory.CreateClientWithOwnIp();

        var unknown = await client.PostAsync("/api/auth/refresh", "token-que-nao-existe".AsJsonBody());
        var empty = await client.PostAsync("/api/auth/refresh", "".AsJsonBody());

        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        // vazio nem chega ao banco: a validação do [ApiController] (400) ou o controller (401) barram; o que não pode é passar
        Assert.True(empty.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_InvalidatesTheRefreshToken()
    {
        var (client, auth, _) = await _factory.RegisterUserAsync();

        var logout = await client.PostAsync("/api/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await _factory.CreateClientWithOwnIp().PostAsync("/api/auth/refresh", auth.RefreshToken.AsJsonBody());
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = _factory.CreateClientWithOwnIp();

        var response = await client.GetAsync("/api/expenses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithForgedToken_Returns401()
    {
        var client = _factory.CreateClientWithOwnIp();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "isto.nao.e-um-jwt-valido");

        var response = await client.GetAsync("/api/expenses");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
