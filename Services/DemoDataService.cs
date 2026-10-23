using System.Globalization;
using GastosApi.Data;
using GastosApi.Models;
using Microsoft.EntityFrameworkCore;

namespace GastosApi.Services;

/// <summary>
/// Conta de demonstração: cada visitante recebe um usuário próprio ("demo-{unix}-{sufixo}") com dados
/// fictícios, para explorar o app sem cadastro. Os usuários demo expiram em 24 h e são removidos
/// automaticamente na próxima criação. O horário de criação vai no próprio nome, então não há migration.
/// </summary>
public class DemoDataService
{
    public const string UsernamePrefix = "demo-";
    public const int MaxActiveDemoUsers = 300;
    public static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    public const string AiDisabledMessage =
        "Na conta demo a IA mostra apenas o exemplo pronto. Crie uma conta para gerar insights de verdade.";

    private const decimal MonthlyIncome = 6500m;
    private const decimal MonthlySavingsGoal = 2200m;

    private sealed record Item(string Description, string Category, decimal Amount, int Day, bool InCurrentMonth);

    private static readonly Item[] Template =
    {
        new("Aluguel", "Moradia", 1800.00m, 5, true),
        new("Condomínio", "Moradia", 450.00m, 5, true),
        new("Conta de energia", "Moradia", 240.00m, 10, true),
        new("Internet", "Moradia", 120.00m, 10, true),
        new("Supermercado Bom Preço", "Alimentação", 412.80m, 3, true),
        new("Padaria Pão Quente", "Alimentação", 28.50m, 5, true),
        new("Restaurante Sabor Caseiro", "Lazer", 86.00m, 8, true),
        new("Combustível", "Transporte", 220.00m, 9, true),
        new("Farmácia Saúde+", "Saúde", 64.90m, 11, true),
        new("Streaming de filmes", "Lazer", 39.90m, 12, true),
        new("Aplicativo de transporte", "Transporte", 47.30m, 14, true),
        new("Curso online de inglês", "Educação", 119.00m, 15, true),
        new("Academia", "Saúde", 99.90m, 22, true),
        new("Supermercado Bom Preço", "Alimentação", 358.20m, 18, false),
        new("Cinema", "Lazer", 52.00m, 20, false),
        new("Lanchonete", "Alimentação", 34.70m, 25, false),
    };

    private readonly AppDbContext _context;

    public DemoDataService(AppDbContext context)
    {
        _context = context;
    }

    public static bool IsDemoUsername(string? username) =>
        username != null && username.StartsWith(UsernamePrefix, StringComparison.Ordinal);

    /// <summary>Lê o horário de criação embutido em "demo-{unix}-{sufixo}". Devolve null se o nome não segue o padrão.</summary>
    public static DateTime? ParseCreatedAt(string? username)
    {
        if (!IsDemoUsername(username)) return null;

        var parts = username!.Split('-');
        if (parts.Length < 3 || !long.TryParse(parts[1], out var seconds)) return null;

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>Cria um usuário demo com dados de exemplo. Devolve null quando o limite de contas demo foi atingido.</summary>
    public async Task<User?> CreateDemoUserAsync(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        await DeleteExpiredAsync(now);

        var active = await _context.Users.CountAsync(u => u.Username.StartsWith(UsernamePrefix));
        if (active >= MaxActiveDemoUsers) return null;

        var suffix = Guid.NewGuid().ToString("N").Substring(0, 6);
        var user = new User
        {
            Username = $"{UsernamePrefix}{new DateTimeOffset(now).ToUnixTimeSeconds()}-{suffix}",
            // senha aleatória e descartada: ninguém entra nessa conta por login
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"))
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(); // precisa do Id antes de criar os dados

        Seed(user.Id, now);
        await _context.SaveChangesAsync();

        return user;
    }

    /// <summary>Remove usuários demo com mais de 24 h (ou com nome fora do padrão) e todos os dados deles.</summary>
    public async Task<int> DeleteExpiredAsync(DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;

        var demoUsers = await _context.Users.Where(u => u.Username.StartsWith(UsernamePrefix)).ToListAsync();
        var expired = demoUsers
            .Where(u =>
            {
                var created = ParseCreatedAt(u.Username);
                return created == null || now - created.Value > MaxAge;
            })
            .ToList();

        if (expired.Count == 0) return 0;

        var ids = expired.Select(u => u.Id).ToList();

        _context.Expenses.RemoveRange(await _context.Expenses.Where(e => ids.Contains(e.UserId)).ToListAsync());
        _context.FinancialProfiles.RemoveRange(await _context.FinancialProfiles.Where(p => ids.Contains(p.UserId)).ToListAsync());
        _context.PurchaseGoals.RemoveRange(await _context.PurchaseGoals.Where(g => ids.Contains(g.UserId)).ToListAsync());
        _context.Investments.RemoveRange(await _context.Investments.Where(i => ids.Contains(i.UserId)).ToListAsync());
        _context.GoalSnapshots.RemoveRange(await _context.GoalSnapshots.Where(s => ids.Contains(s.UserId)).ToListAsync());
        _context.RecurringExpenses.RemoveRange(await _context.RecurringExpenses.Where(r => ids.Contains(r.UserId)).ToListAsync());
        _context.CategoryBudgets.RemoveRange(await _context.CategoryBudgets.Where(b => ids.Contains(b.UserId)).ToListAsync());
        _context.Users.RemoveRange(expired);

        await _context.SaveChangesAsync();
        return expired.Count;
    }

    private void Seed(int userId, DateTime now)
    {
        var currentMonth = new List<Expense>();

        // o ano inteiro até hoje (o relatório anual soma o ano todo), com no mínimo 3 meses para o gráfico de histórico
        var pastMonths = Math.Max(now.Month - 1, 3);

        for (var back = 0; back <= pastMonths; back++)
        {
            var first = new DateTime(now.Year, now.Month, 1, 12, 0, 0, DateTimeKind.Utc).AddMonths(-back);
            var isCurrent = back == 0;
            var maxDay = isCurrent ? now.Day : DateTime.DaysInMonth(first.Year, first.Month);
            var factor = isCurrent ? 1.00m : 0.93m + 0.04m * (back % 4); // meses anteriores variam um pouco
            var items = isCurrent ? Template.Where(t => t.InCurrentMonth) : Template;

            var monthTotal = 0m;
            foreach (var item in items)
            {
                var day = Math.Min(item.Day, maxDay);
                var expense = new Expense
                {
                    UserId = userId,
                    Description = item.Description,
                    Amount = Math.Round(item.Amount * factor, 2),
                    Category = item.Category,
                    Date = new DateTime(first.Year, first.Month, day, 12, 0, 0, DateTimeKind.Utc),
                    Paid = isCurrent ? day < now.Day : true
                };

                _context.Expenses.Add(expense);
                monthTotal += expense.Amount;
                if (isCurrent) currentMonth.Add(expense);
            }

            if (!isCurrent)
            {
                _context.GoalSnapshots.Add(new GoalSnapshot
                {
                    UserId = userId,
                    Month = first.Month,
                    Year = first.Year,
                    Spent = monthTotal,
                    SavingsAchieved = MonthlyIncome - monthTotal,
                    SavingsGoal = MonthlySavingsGoal
                });
            }
        }

        _context.FinancialProfiles.Add(new FinancialProfile
        {
            UserId = userId,
            MonthlyIncome = MonthlyIncome,
            SavingsGoal = MonthlySavingsGoal,
            AnnualSavingsGoal = MonthlySavingsGoal * 12,
            CurrentSavings = 8500m,
            ShortTermGoal = "Montar uma reserva de emergência",
            MediumTermGoal = "Trocar de notebook",
            LongTermGoal = "Entrada de um imóvel",
            LastInsight = StoredInsight.Wrap("monthly", BuildSampleInsight(currentMonth)),
            LastInsightGeneratedAt = now
        });

        // Limites escolhidos para mostrar os alertas: Lazer perto do limite e Transporte estourado.
        foreach (var (category, limit) in new[]
        {
            ("Alimentação", 900m), ("Transporte", 260m), ("Lazer", 150m), ("Saúde", 250m), ("Educação", 150m)
        })
        {
            _context.CategoryBudgets.Add(new CategoryBudget { UserId = userId, Category = category, MonthlyLimit = limit });
        }

        _context.Investments.AddRange(
            new Investment { UserId = userId, Name = "Tesouro Selic (exemplo)", Type = "Renda Fixa", AmountInvested = 12000m, CurrentValue = 12850m, AnnualReturnRate = 11.2, RiskProfile = "Conservador" },
            new Investment { UserId = userId, Name = "Fundo imobiliário (exemplo)", Type = "Fundo", AmountInvested = 3500m, CurrentValue = 3720m, AnnualReturnRate = 9.0, RiskProfile = "Moderado" },
            new Investment { UserId = userId, Name = "Ações de empresa (exemplo)", Type = "Renda Variável", AmountInvested = 2000m, CurrentValue = 2310m, AnnualReturnRate = 14.0, RiskProfile = "Arrojado" });

        _context.PurchaseGoals.AddRange(
            new PurchaseGoal { UserId = userId, Description = "Notebook novo", EstimatedCost = 4200m, Priority = "Alta", IsEssential = false },
            new PurchaseGoal { UserId = userId, Description = "Cadeira ergonômica", EstimatedCost = 900m, Priority = "Média", IsEssential = true });

        // LastGeneratedAt = agora evita que o app gere de novo despesas que já estão na lista.
        _context.RecurringExpenses.AddRange(
            new RecurringExpense { UserId = userId, Description = "Streaming de filmes", Amount = 39.90m, Category = "Lazer", DayOfMonth = 12, LastGeneratedAt = now, Active = true },
            new RecurringExpense { UserId = userId, Description = "Academia", Amount = 99.90m, Category = "Saúde", DayOfMonth = 22, LastGeneratedAt = now, Active = true });
    }

    private static string Brl(decimal value)
    {
        // formata como 1.234,56 sem depender da cultura instalada no servidor
        var text = value.ToString("N2", CultureInfo.InvariantCulture);
        return "R$ " + text.Replace(",", "#").Replace(".", ",").Replace("#", ".");
    }

    private static string BuildSampleInsight(List<Expense> currentMonth)
    {
        var spent = currentMonth.Sum(e => e.Amount);
        // Moradia é custo fixo; o foco do insight fica nos gastos variáveis
        var top = currentMonth
            .Where(e => e.Category != "Moradia")
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .OrderByDescending(g => g.Total)
            .First();

        return
$@"DETALHAMENTO DO PERÍODO:
Este é um insight de exemplo da conta demonstração. Neste mês você gastou {Brl(spent)} de uma renda de {Brl(MonthlyIncome)}, o que mantém a meta de economia de {Brl(MonthlySavingsGoal)} ao alcance. Entre os gastos variáveis, **{top.Category}** foi a categoria que mais pesou ({Brl(top.Total)}); a Moradia é custo fixo.

ONDE ECONOMIZAR:
- **{top.Category}**: é onde está o maior volume. Comparar preços e planejar a lista de compras costuma reduzir esse valor sem cortar o essencial.
- **Lazer**: já está perto do limite do orçamento. Vale escolher quais saídas realmente valem a pena neste mês.
- **Transporte**: passou do limite definido. Agrupar deslocamentos ajuda a reduzir o gasto.

ESTRATÉGIA PARA SUAS COMPRAS:
O **Notebook novo** ({Brl(4200m)}) é prioridade alta. Guardando cerca de {Brl(350m)} por mês, ele cabe em 12 meses sem comprometer a meta. A **Cadeira ergonômica** é uma necessidade e pode vir antes.

SUGESTÃO PRÁTICA:
Defina um limite semanal para Lazer e acompanhe pela tela de orçamento por categoria. Para gerar insights com os seus próprios dados, crie uma conta.";
    }
}
