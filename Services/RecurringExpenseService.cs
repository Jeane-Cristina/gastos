using Microsoft.EntityFrameworkCore;
using GastosApi.Data;
using GastosApi.Models;

namespace GastosApi.Services;

public class RecurringExpenseService
{
    private readonly AppDbContext _context;

    public RecurringExpenseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> GenerateDueExpensesAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var recurring = await _context.RecurringExpenses
            .Where(r => r.UserId == userId && r.Active)
            .ToListAsync();

        var generated = 0;

        foreach (var r in recurring)
        {
            var alreadyGeneratedThisMonth = r.LastGeneratedAt.HasValue
                && r.LastGeneratedAt.Value.Month == now.Month
                && r.LastGeneratedAt.Value.Year == now.Year;

            if (alreadyGeneratedThisMonth || now.Day < r.DayOfMonth)
                continue;

            _context.Expenses.Add(new Expense
            {
                UserId = userId,
                Description = $"{r.Description} (recorrente)",
                Amount = r.Amount,
                Category = r.Category,
                Date = new DateTime(now.Year, now.Month, Math.Min(r.DayOfMonth, DateTime.DaysInMonth(now.Year, now.Month)), 0, 0, 0, DateTimeKind.Utc)
            });

            r.LastGeneratedAt = now;
            generated++;
        }

        await _context.SaveChangesAsync();
        return generated;
    }
}