using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace GastosApi.Tests.Integration;

/// <summary>
/// O teste mais importante da API: um usuário nunca enxerga nem mexe nos dados de outro.
/// Aqui a prova é feita por HTTP, passando por JWT, controller, service e banco.
/// </summary>
public class DataIsolationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public DataIsolationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UserB_DoesNotSee_UserA_Expenses()
    {
        var (userA, _, _) = await _factory.RegisterUserAsync();
        var (userB, _, _) = await _factory.RegisterUserAsync();
        var marker = $"Despesa secreta {Guid.NewGuid():N}";

        await userA.CreateExpenseAsync(marker);

        Assert.Contains(marker, await userA.ListExpenseDescriptionsAsync());
        Assert.DoesNotContain(marker, await userB.ListExpenseDescriptionsAsync());
    }

    [Fact]
    public async Task UserB_CannotEditOrDelete_UserA_Expense()
    {
        var (userA, _, _) = await _factory.RegisterUserAsync();
        var (userB, _, _) = await _factory.RegisterUserAsync();
        var marker = $"Despesa da A {Guid.NewGuid():N}";
        var expenseId = await userA.CreateExpenseAsync(marker);

        var patch = await userB.PatchAsJsonAsync($"/api/expenses/{expenseId}", new
        {
            description = "Alterada pelo B",
            amount = 1m,
            category = "Outros",
            date = DateTime.UtcNow
        });
        var delete = await userB.DeleteAsync($"/api/expenses/{expenseId}");

        Assert.Equal(HttpStatusCode.NotFound, patch.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Contains(marker, await userA.ListExpenseDescriptionsAsync());
    }

    [Fact]
    public async Task UserB_CannotUpdateOrDelete_UserA_Investment()
    {
        var (userA, _, _) = await _factory.RegisterUserAsync();
        var (userB, _, _) = await _factory.RegisterUserAsync();

        var created = await userA.PostAsJsonAsync("/api/investment", new
        {
            name = "Tesouro da A",
            type = "Renda Fixa",
            amountInvested = 1000m,
            currentValue = 1100m,
            annualReturnRate = 10.0,
            riskProfile = "Conservador"
        });
        created.EnsureSuccessStatusCode();
        var investmentId = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var put = await userB.PutAsJsonAsync($"/api/investment/{investmentId}", new
        {
            name = "Invadido pelo B",
            type = "Outro",
            amountInvested = 1m,
            currentValue = 1m,
            annualReturnRate = 0.0,
            riskProfile = "Arrojado"
        });
        var delete = await userB.DeleteAsync($"/api/investment/{investmentId}");

        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        var ownerView = await userA.GetFromJsonAsync<JsonElement>("/api/investment");
        var names = ownerView.EnumerateArray().Select(i => i.GetProperty("name").GetString()).ToList();
        Assert.Contains("Tesouro da A", names);
        Assert.DoesNotContain("Invadido pelo B", names);
    }

    [Fact]
    public async Task UserB_Listing_OfInvestments_DoesNotIncludeUserA()
    {
        var (userA, _, _) = await _factory.RegisterUserAsync();
        var (userB, _, _) = await _factory.RegisterUserAsync();

        var created = await userA.PostAsJsonAsync("/api/investment", new
        {
            name = "Só da A",
            type = "Fundo",
            amountInvested = 500m,
            currentValue = 520m,
            annualReturnRate = 8.0,
            riskProfile = "Moderado"
        });
        created.EnsureSuccessStatusCode();

        var bView = await userB.GetFromJsonAsync<JsonElement>("/api/investment");

        Assert.Equal(0, bView.GetArrayLength());
    }
}
