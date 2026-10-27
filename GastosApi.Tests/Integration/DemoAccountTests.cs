using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GastosApi.Tests.Integration;

public class DemoAccountTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public DemoAccountTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Demo_ReturnsTokens_AndTheAccountComesWithData()
    {
        var (client, auth) = await _factory.StartDemoAsync();

        Assert.False(string.IsNullOrWhiteSpace(auth.RefreshToken));

        var page = await client.GetFromJsonAsync<JsonElement>("/api/expenses?page=1&pageSize=5");
        Assert.True(page.GetProperty("totalCount").GetInt32() > 0);

        var investments = await client.GetFromJsonAsync<JsonElement>("/api/investment");
        Assert.True(investments.GetArrayLength() > 0);
    }

    [Fact]
    public async Task Demo_EachVisitorGetsAnIsolatedAccount()
    {
        var (visitorA, _) = await _factory.StartDemoAsync();
        var (visitorB, _) = await _factory.StartDemoAsync();
        var marker = $"Só do visitante A {Guid.NewGuid():N}";

        await visitorA.CreateExpenseAsync(marker);

        Assert.Contains(marker, await visitorA.ListExpenseDescriptionsAsync());
        Assert.DoesNotContain(marker, await visitorB.ListExpenseDescriptionsAsync());
    }

    [Fact]
    public async Task Demo_AiEndpoints_AreBlockedWithAnExplanation()
    {
        var (client, _) = await _factory.StartDemoAsync();

        var ask = await client.PostAsJsonAsync("/api/insight/ask", new { question = "Quanto gastei com Lazer?" });
        var suggestions = await client.GetAsync("/api/investment/suggestions");

        Assert.Equal(HttpStatusCode.Forbidden, ask.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, suggestions.StatusCode);
        var body = await ask.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task Demo_CreationIsRateLimitedPerIp()
    {
        var client = _factory.CreateClientWithOwnIp();

        for (var i = 0; i < 10; i++)
        {
            var allowed = await client.PostAsync("/api/auth/demo", content: null);
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        var blocked = await client.PostAsync("/api/auth/demo", content: null);
        Assert.Equal(HttpStatusCode.TooManyRequests, blocked.StatusCode);

        var otherIp = _factory.CreateClientWithOwnIp();
        var response = await otherIp.PostAsync("/api/auth/demo", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
