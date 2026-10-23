using Microsoft.EntityFrameworkCore;
using GastosApi.Data;
using GastosApi.Models;
using GastosApi.Dtos;
using Microsoft.Extensions.Caching.Memory;

namespace GastosApi.Services;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _context;
    private readonly IMemoryCache? _cache;

    // O cache é opcional para que os testes possam criar o serviço só com o DbContext.
    public ExpenseService(AppDbContext context, IMemoryCache? cache = null)
    {
        _context = context;
        _cache = cache;
    }

    private void InvalidateGoalReport(int userId) => _cache?.Remove(GoalReportCache.Key(userId));

    /// <summary>
    /// Semana do mês aplicada na consulta SQL (antes da paginação):
    /// 1 = dias 1–7, 2 = 8–14, 3 = 15–21, 4 = dia 22 em diante.
    /// </summary>
    private static IQueryable<Expense> ApplyWeek(IQueryable<Expense> query, int? week)
    {
        if (!week.HasValue) return query;

        return week.Value switch
        {
            1 => query.Where(e => e.Date.Day <= 7),
            2 => query.Where(e => e.Date.Day >= 8 && e.Date.Day <= 14),
            3 => query.Where(e => e.Date.Day >= 15 && e.Date.Day <= 21),
            4 => query.Where(e => e.Date.Day >= 22),
            _ => query
        };
    }

    public async Task<PagedResult<Expense>> GetAllAsync(int userId, int? month, int? year, string? category, int? week, string? paidBy, bool? paid, int page, int pageSize)
    {
        var query = _context.Expenses.Where(e => e.UserId == userId).AsQueryable();

        if (month.HasValue) query = query.Where(e => e.Date.Month == month.Value);
        if (year.HasValue) query = query.Where(e => e.Date.Year == year.Value);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(e => e.Category == category);
        if (!string.IsNullOrWhiteSpace(paidBy)) query = query.Where(e => e.PaidBy == paidBy);
        if (paid.HasValue) query = query.Where(e => e.Paid == paid.Value);
        query = ApplyWeek(query, week);

        query = query.OrderByDescending(e => e.Date);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<Expense> { Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize };
    }

    public async Task<Expense> CreateAsync(int userId, ExpenseDto dto)
    {
        var expense = new Expense
        {
            UserId = userId,
            Description = dto.Description,
            Amount = dto.Amount,
            Category = CategoryNormalizer.Normalize(dto.Category),
            Date = dto.Date,
            PaidBy = string.IsNullOrWhiteSpace(dto.PaidBy) ? null : dto.PaidBy.Trim(),
            Paid = dto.Paid
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();
        InvalidateGoalReport(userId);
        return expense;
    }

    public async Task<bool> UpdateAsync(int userId, int id, ExpenseDto dto)
    {
        var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (expense == null) return false;

        expense.Description = dto.Description;
        expense.Amount = dto.Amount;
        expense.Category = CategoryNormalizer.Normalize(dto.Category);
        expense.Date = dto.Date;
        expense.PaidBy = string.IsNullOrWhiteSpace(dto.PaidBy) ? null : dto.PaidBy.Trim();
        expense.Paid = dto.Paid;

        await _context.SaveChangesAsync();
        InvalidateGoalReport(userId);
        return true;
    }

    public async Task<bool> DeleteAsync(int userId, int id)
    {
        var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (expense == null) return false;

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();
        InvalidateGoalReport(userId);
        return true;
    }

    /// <summary>Reaplica o normalizador às categorias antigas do usuário ("IA"/"Ia", "Alimentação(Lazer)"...). Devolve quantas mudaram.</summary>
    public async Task<int> NormalizeCategoriesAsync(int userId)
    {
        var expenses = await _context.Expenses.Where(e => e.UserId == userId).ToListAsync();
        var changed = 0;

        foreach (var expense in expenses)
        {
            var normalized = CategoryNormalizer.Normalize(expense.Category);
            if (normalized != expense.Category)
            {
                expense.Category = normalized;
                changed++;
            }
        }

        if (changed > 0)
        {
            await _context.SaveChangesAsync();
            InvalidateGoalReport(userId);
        }

        return changed;
    }

    public async Task<List<CategorySummaryDto>> GetSummaryAsync(int userId, int? month, int? year, string? category, int? week, string? paidBy, bool? paid)
    {
        var query = _context.Expenses.Where(e => e.UserId == userId).AsQueryable();

        if (month.HasValue) query = query.Where(e => e.Date.Month == month.Value);
        if (year.HasValue) query = query.Where(e => e.Date.Year == year.Value);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(e => e.Category == category);
        if (!string.IsNullOrWhiteSpace(paidBy)) query = query.Where(e => e.PaidBy == paidBy);
        if (paid.HasValue) query = query.Where(e => e.Paid == paid.Value);
        query = ApplyWeek(query, week);

        var expenses = await query.ToListAsync();

        return expenses
            .GroupBy(e => e.Category)
            .Select(g => new CategorySummaryDto { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToList();
    }
}