using GastosApi.Data;
using GastosApi.Dtos;
using GastosApi.Models;
using GastosApi.Services;
using Xunit;

namespace GastosApi.Tests;

public class ExpenseServiceTests
{
    private const int UserA = 1;
    private const int UserB = 2;

    private readonly AppDbContext _context;
    private readonly ExpenseService _service;

    public ExpenseServiceTests()
    {
        // Ajuste aqui se o helper do seu projeto tiver outro nome.
        _context = TestDbContextFactory.Create();
        _service = new ExpenseService(_context);
    }

    private static ExpenseDto NewDto(
        string description = "Mercado",
        decimal amount = 100m,
        string category = "Alimentação",
        DateTime? date = null,
        string? paidBy = null,
        bool? paid = null) => new()
    {
        Description = description,
        Amount = amount,
        Category = category,
        Date = date ?? new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc),
        PaidBy = paidBy,
        Paid = paid
    };

    private Task<PagedResult<Expense>> GetAll(
        int userId,
        int? month = null,
        int? year = null,
        string? category = null,
        int? week = null,
        string? paidBy = null,
        bool? paid = null,
        int page = 1,
        int pageSize = 20)
        => _service.GetAllAsync(userId, month, year, category, week, paidBy, paid, page, pageSize);

    // ---------- Create ----------

    [Fact]
    public async Task CreateAsync_SavesExpenseForTheGivenUser()
    {
        var created = await _service.CreateAsync(UserA, NewDto(description: "Padaria", amount: 25.5m));

        Assert.True(created.Id > 0);
        Assert.Equal(UserA, created.UserId);
        Assert.Equal("Padaria", created.Description);
        Assert.Equal(25.5m, created.Amount);
    }

    [Fact]
    public async Task CreateAsync_NormalizesCategory()
    {
        var created = await _service.CreateAsync(UserA, NewDto(category: "  alimentação  "));

        Assert.Equal(CategoryNormalizer.Normalize("  alimentação  "), created.Category);
    }

    [Fact]
    public async Task CreateAsync_TrimsPaidBy()
    {
        var created = await _service.CreateAsync(UserA, NewDto(paidBy: "  Maria  "));

        Assert.Equal("Maria", created.PaidBy);
    }

    // ---------- GetAll: isolamento e filtros ----------

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyExpensesOfTheUser()
    {
        await _service.CreateAsync(UserA, NewDto(description: "Do usuário A"));
        await _service.CreateAsync(UserB, NewDto(description: "Do usuário B"));

        var result = await GetAll(UserA);

        Assert.Single(result.Items);
        Assert.Equal("Do usuário A", result.Items[0].Description);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByMonthAndYear()
    {
        await _service.CreateAsync(UserA, NewDto(description: "Outubro", date: new DateTime(2026, 10, 5, 12, 0, 0, DateTimeKind.Utc)));
        await _service.CreateAsync(UserA, NewDto(description: "Setembro", date: new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc)));
        await _service.CreateAsync(UserA, NewDto(description: "Outubro 2025", date: new DateTime(2025, 10, 5, 12, 0, 0, DateTimeKind.Utc)));

        var result = await GetAll(UserA, month: 10, year: 2026);

        Assert.Single(result.Items);
        Assert.Equal("Outubro", result.Items[0].Description);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByCategory()
    {
        var food = CategoryNormalizer.Normalize("Alimentação");
        await _service.CreateAsync(UserA, NewDto(description: "Mercado", category: "Alimentação"));
        await _service.CreateAsync(UserA, NewDto(description: "Ônibus", category: "Transporte"));

        var result = await GetAll(UserA, category: food);

        Assert.Single(result.Items);
        Assert.Equal("Mercado", result.Items[0].Description);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByPaidStatus()
    {
        await _service.CreateAsync(UserA, NewDto(description: "Pago", paid: true));
        await _service.CreateAsync(UserA, NewDto(description: "Pendente", paid: false));

        var onlyPaid = await GetAll(UserA, paid: true);

        Assert.Single(onlyPaid.Items);
        Assert.Equal("Pago", onlyPaid.Items[0].Description);
    }

    [Fact]
    public async Task GetAllAsync_FiltersByPaidBy()
    {
        await _service.CreateAsync(UserA, NewDto(description: "Da Maria", paidBy: "Maria"));
        await _service.CreateAsync(UserA, NewDto(description: "Do João", paidBy: "João"));

        var result = await GetAll(UserA, paidBy: "Maria");

        Assert.Single(result.Items);
        Assert.Equal("Da Maria", result.Items[0].Description);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByDateDescending()
    {
        await _service.CreateAsync(UserA, NewDto(description: "Antiga", date: new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc)));
        await _service.CreateAsync(UserA, NewDto(description: "Recente", date: new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc)));

        var result = await GetAll(UserA);

        Assert.Equal("Recente", result.Items[0].Description);
        Assert.Equal("Antiga", result.Items[1].Description);
    }

    // ---------- GetAll: paginação ----------

    [Fact]
    public async Task GetAllAsync_PaginatesAndReportsTotalCount()
    {
        for (var i = 1; i <= 5; i++)
        {
            await _service.CreateAsync(UserA, NewDto(
                description: $"Despesa {i}",
                date: new DateTime(2026, 10, i, 12, 0, 0, DateTimeKind.Utc)));
        }

        var page1 = await GetAll(UserA, page: 1, pageSize: 2);
        var page3 = await GetAll(UserA, page: 3, pageSize: 2);

        Assert.Equal(5, page1.TotalCount);
        Assert.Equal(2, page1.Items.Count);
        Assert.Equal(1, page1.Page);
        Assert.Equal(2, page1.PageSize);
        Assert.Single(page3.Items);
    }

    // ---------- GetAll: semana do mês ----------

    [Fact]
    public async Task GetAllAsync_FiltersByWeek_BeforePagination()
    {
        await _service.CreateAsync(UserA, NewDto(description: "Semana 1", date: new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc)));
        await _service.CreateAsync(UserA, NewDto(description: "Semana 2", date: new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc)));
        await _service.CreateAsync(UserA, NewDto(description: "Semana 3", date: new DateTime(2026, 10, 18, 12, 0, 0, DateTimeKind.Utc)));
        await _service.CreateAsync(UserA, NewDto(description: "Semana 4", date: new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc)));
        await _service.CreateAsync(UserA, NewDto(description: "Ainda semana 4", date: new DateTime(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc)));

        // pageSize 1: se o filtro rodasse depois da paginação, a página poderia vir vazia e o total errado
        var week4 = await GetAll(UserA, week: 4, page: 1, pageSize: 1);

        Assert.Equal(2, week4.TotalCount);
        Assert.Single(week4.Items);
        Assert.Equal("Ainda semana 4", week4.Items[0].Description);
    }

    [Fact]
    public async Task GetSummaryAsync_FiltersByWeek()
    {
        await _service.CreateAsync(UserA, NewDto(amount: 10m, date: new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc)));
        await _service.CreateAsync(UserA, NewDto(amount: 90m, date: new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc)));

        var summary = await _service.GetSummaryAsync(UserA, 10, 2026, null, 2, null, null);

        Assert.Equal(90m, summary.Sum(s => s.Total));
    }

    // ---------- Normalização de categorias antigas ----------

    [Fact]
    public async Task NormalizeCategoriesAsync_UnifiesLegacyVariants_OnlyForTheUser()
    {
        _context.Expenses.AddRange(
            new Expense { UserId = UserA, Description = "a", Amount = 1m, Category = "IA", Date = DateTime.UtcNow },
            new Expense { UserId = UserA, Description = "b", Amount = 1m, Category = "Ia", Date = DateTime.UtcNow },
            new Expense { UserId = UserA, Description = "c", Amount = 1m, Category = "Alimentação(Lazer)", Date = DateTime.UtcNow },
            new Expense { UserId = UserB, Description = "d", Amount = 1m, Category = "IA", Date = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        var changed = await _service.NormalizeCategoriesAsync(UserA);
        var categories = (await GetAll(UserA)).Items.Select(e => e.Category).Distinct().OrderBy(c => c).ToList();

        Assert.Equal(2, changed);
        Assert.Equal(new[] { "Alimentação (Lazer)", "Ia" }, categories);
        Assert.Equal("IA", _context.Expenses.Single(e => e.UserId == UserB).Category);
    }

    // ---------- Update / Delete: isolamento ----------

    [Fact]
    public async Task UpdateAsync_UpdatesOwnExpense()
    {
        var created = await _service.CreateAsync(UserA, NewDto(description: "Antes", amount: 10m));

        var ok = await _service.UpdateAsync(UserA, created.Id, NewDto(description: "Depois", amount: 20m));
        var reloaded = (await GetAll(UserA)).Items.Single();

        Assert.True(ok);
        Assert.Equal("Depois", reloaded.Description);
        Assert.Equal(20m, reloaded.Amount);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_ForExpenseOfAnotherUser()
    {
        var created = await _service.CreateAsync(UserA, NewDto(description: "Privada"));

        var ok = await _service.UpdateAsync(UserB, created.Id, NewDto(description: "Invadida"));
        var reloaded = (await GetAll(UserA)).Items.Single();

        Assert.False(ok);
        Assert.Equal("Privada", reloaded.Description);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsFalse_WhenExpenseDoesNotExist()
    {
        var ok = await _service.UpdateAsync(UserA, 9999, NewDto());

        Assert.False(ok);
    }

    [Fact]
    public async Task DeleteAsync_RemovesOwnExpense()
    {
        var created = await _service.CreateAsync(UserA, NewDto());

        var ok = await _service.DeleteAsync(UserA, created.Id);
        var result = await GetAll(UserA);

        Assert.True(ok);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_ForExpenseOfAnotherUser()
    {
        var created = await _service.CreateAsync(UserA, NewDto());

        var ok = await _service.DeleteAsync(UserB, created.Id);
        var result = await GetAll(UserA);

        Assert.False(ok);
        Assert.Single(result.Items);
    }

    // ---------- Resumo ----------

    [Fact]
    public async Task GetSummaryAsync_GroupsTotalsByCategory_ForTheUserOnly()
    {
        await _service.CreateAsync(UserA, NewDto(amount: 100m, category: "Alimentação"));
        await _service.CreateAsync(UserA, NewDto(amount: 50m, category: "Alimentação"));
        await _service.CreateAsync(UserA, NewDto(amount: 30m, category: "Transporte"));
        await _service.CreateAsync(UserB, NewDto(amount: 999m, category: "Alimentação"));

        var summary = await _service.GetSummaryAsync(UserA, null, null, null, null, null, null);

        var food = summary.Single(s => s.Category == CategoryNormalizer.Normalize("Alimentação"));
        var transport = summary.Single(s => s.Category == CategoryNormalizer.Normalize("Transporte"));
        Assert.Equal(150m, food.Total);
        Assert.Equal(30m, transport.Total);
        Assert.Equal(2, summary.Count);
    }

    [Fact]
    public async Task GetSummaryAsync_RespectsMonthFilter()
    {
        await _service.CreateAsync(UserA, NewDto(amount: 40m, date: new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc)));
        await _service.CreateAsync(UserA, NewDto(amount: 60m, date: new DateTime(2026, 9, 3, 12, 0, 0, DateTimeKind.Utc)));

        var summary = await _service.GetSummaryAsync(UserA, 10, 2026, null, null, null, null);

        Assert.Equal(40m, summary.Sum(s => s.Total));
    }
}
