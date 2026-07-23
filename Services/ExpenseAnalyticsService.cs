using Microsoft.EntityFrameworkCore;
using GastosApi.Data;

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
}