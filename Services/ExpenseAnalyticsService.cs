using Microsoft.EntityFrameworkCore;
using GastosApi.Data;
using GastosApi.Models;

namespace GastosApi.Services;

public class WeekOfMonthSpending
{
    public int Week { get; set; }
    public decimal Total { get; set; }
}

public class WeeklyCategoryBreakdown
{
    public int Week { get; set; }
    public string TopCategory { get; set; } = string.Empty;
    public decimal TopCategoryAmount { get; set; }
}

public class CategorySpending
{
    public string Category { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

public class ExpenseAnalytics
{
    public decimal TotalLast3Months { get; set; }
    public List<WeekOfMonthSpending> SpendingByWeekOfMonth { get; set; } = new();
    public List<CategorySpending> SpendingByCategory { get; set; } = new();
    public List<WeeklyCategoryBreakdown> WeeklyTopCategories { get; set; } = new();
}

public class BreakdownUnit
{
    public string Label { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string? TopCategory { get; set; }
    public decimal TopCategoryAmount { get; set; }
}

public class PeriodAnalytics
{
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal TotalSpent { get; set; }
    public List<BreakdownUnit> Breakdown { get; set; } = new();
    public List<CategorySpending> SpendingByCategory { get; set; } = new();
}

public class ExpenseAnalyticsService
{
    private readonly AppDbContext _context;

    public ExpenseAnalyticsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ExpenseAnalytics> GetAnalyticsAsync(int userId)
    {
        var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);

        var expenses = await _context.Expenses
            .Where(e => e.UserId == userId && e.Date >= threeMonthsAgo)
            .ToListAsync();

        var byWeek = expenses
            .GroupBy(e => WeekOfMonth(e.Date))
            .Select(g => new WeekOfMonthSpending { Week = g.Key, Total = g.Sum(e => e.Amount) })
            .OrderBy(w => w.Week)
            .ToList();

        var byCategory = expenses
            .GroupBy(e => e.Category)
            .Select(g => new CategorySpending { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .OrderByDescending(c => c.Total)
            .ToList();

        var weeklyTopCategories = expenses
            .GroupBy(e => WeekOfMonth(e.Date))
            .Select(weekGroup => new WeeklyCategoryBreakdown
            {
                Week = weekGroup.Key,
                TopCategory = weekGroup.GroupBy(e => e.Category)
                    .OrderByDescending(g => g.Sum(e => e.Amount))
                    .First().Key,
                TopCategoryAmount = weekGroup.GroupBy(e => e.Category)
                    .OrderByDescending(g => g.Sum(e => e.Amount))
                    .First().Sum(e => e.Amount)
            })
            .OrderBy(w => w.Week)
            .ToList();

        return new ExpenseAnalytics
        {
            TotalLast3Months = expenses.Sum(e => e.Amount),
            SpendingByWeekOfMonth = byWeek,
            SpendingByCategory = byCategory,
            WeeklyTopCategories = weeklyTopCategories
        };
    }

    private static int WeekOfMonth(DateTime date)
    {
        return ((date.Day - 1) / 7) + 1;
    }

    public async Task<(List<CategorySpending> current, List<CategorySpending> previous)> CompareMonthsAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var startOfCurrentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfPreviousMonth = startOfCurrentMonth.AddMonths(-1);

        var currentExpenses = await _context.Expenses
            .Where(e => e.UserId == userId && e.Date >= startOfCurrentMonth)
            .ToListAsync();

        var previousExpenses = await _context.Expenses
            .Where(e => e.UserId == userId && e.Date >= startOfPreviousMonth && e.Date < startOfCurrentMonth)
            .ToListAsync();

        var current = currentExpenses
            .GroupBy(e => e.Category)
            .Select(g => new CategorySpending { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToList();

        var previous = previousExpenses
            .GroupBy(e => e.Category)
            .Select(g => new CategorySpending { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToList();

        return (current, previous);
    }

    public async Task<int> GetStreakAsync(int userId)
    {
        var dates = await _context.Expenses
            .Where(e => e.UserId == userId)
            .Select(e => e.Date.Date)
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync();

        if (dates.Count == 0) return 0;

        var today = DateTime.UtcNow.Date;
        var streak = 0;
        var expectedDate = today;

        if (dates[0] != today)
            expectedDate = today.AddDays(-1);

        foreach (var date in dates)
        {
            if (date == expectedDate)
            {
                streak++;
                expectedDate = expectedDate.AddDays(-1);
            }
            else if (date < expectedDate)
            {
                break;
            }
        }

        return streak;
    }

    public async Task<PeriodAnalytics> GetPeriodAnalyticsAsync(int userId, string period)
    {
        var now = DateTime.UtcNow;
        DateTime start;
        string periodLabel;

        switch (period)
        {
            case "weekly":
                var diff = (7 + (now.DayOfWeek - DayOfWeek.Monday) % 7) % 7;
                start = now.Date.AddDays(-diff);
                start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
                periodLabel = $"Semana de {start:dd/MM} a {start.AddDays(6):dd/MM}";
                break;

            case "quarterly":
                start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-2);
                periodLabel = $"{start:MMM/yyyy} a {now:MMM/yyyy}";
                break;

            case "monthly":
            default:
                start = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                periodLabel = $"{start:MMMM/yyyy}";
                break;
        }

        var expenses = await _context.Expenses
            .Where(e => e.UserId == userId && e.Date >= start)
            .ToListAsync();

        List<BreakdownUnit> breakdown;

        if (period == "weekly")
        {
            var dayOrder = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday };
            var dayNames = new Dictionary<DayOfWeek, string>
            {
                { DayOfWeek.Monday, "Segunda" }, { DayOfWeek.Tuesday, "Terça" }, { DayOfWeek.Wednesday, "Quarta" },
                { DayOfWeek.Thursday, "Quinta" }, { DayOfWeek.Friday, "Sexta" }, { DayOfWeek.Saturday, "Sábado" }, { DayOfWeek.Sunday, "Domingo" }
            };

            breakdown = expenses
                .GroupBy(e => e.Date.DayOfWeek)
                .Select(g => new BreakdownUnit
                {
                    Label = dayNames[g.Key],
                    Total = g.Sum(e => e.Amount),
                    TopCategory = g.GroupBy(e => e.Category).OrderByDescending(x => x.Sum(e => e.Amount)).First().Key,
                    TopCategoryAmount = g.GroupBy(e => e.Category).OrderByDescending(x => x.Sum(e => e.Amount)).First().Sum(e => e.Amount)
                })
                .OrderBy(b => Array.IndexOf(dayOrder, dayOrder.First(d => dayNames[d] == b.Label)))
                .ToList();
        }
        else if (period == "monthly")
        {
            breakdown = expenses
                .GroupBy(e => ((e.Date.Day - 1) / 7) + 1)
                .Select(g => new BreakdownUnit
                {
                    Label = $"Semana {g.Key}",
                    Total = g.Sum(e => e.Amount),
                    TopCategory = g.GroupBy(e => e.Category).OrderByDescending(x => x.Sum(e => e.Amount)).First().Key,
                    TopCategoryAmount = g.GroupBy(e => e.Category).OrderByDescending(x => x.Sum(e => e.Amount)).First().Sum(e => e.Amount)
                })
                .OrderBy(b => b.Label)
                .ToList();
        }
        else
        {
            breakdown = expenses
                .GroupBy(e => new { e.Date.Year, e.Date.Month })
                .Select(g => new BreakdownUnit
                {
                    Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM/yyyy"),
                    Total = g.Sum(e => e.Amount),
                    TopCategory = g.GroupBy(e => e.Category).OrderByDescending(x => x.Sum(e => e.Amount)).First().Key,
                    TopCategoryAmount = g.GroupBy(e => e.Category).OrderByDescending(x => x.Sum(e => e.Amount)).First().Sum(e => e.Amount)
                })
                .OrderBy(b => b.Label)
                .ToList();
        }

        var byCategory = expenses
            .GroupBy(e => e.Category)
            .Select(g => new CategorySpending { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .OrderByDescending(c => c.Total)
            .ToList();

        return new PeriodAnalytics
        {
            PeriodLabel = periodLabel,
            TotalSpent = expenses.Sum(e => e.Amount),
            Breakdown = breakdown,
            SpendingByCategory = byCategory
        };
    }
}