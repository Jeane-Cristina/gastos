using Microsoft.EntityFrameworkCore;
using GastosApi.Data;
using GastosApi.Models;
using GastosApi.Dtos;

namespace GastosApi.Services;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _context;

    public ExpenseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Expense>> GetAllAsync(int userId, int? month, int? year, string? category)
    {
        var query = _context.Expenses.Where(e => e.UserId == userId).AsQueryable();

        if (month.HasValue)
            query = query.Where(e => e.Date.Month == month.Value);

        if (year.HasValue)
            query = query.Where(e => e.Date.Year == year.Value);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(e => e.Category == category);

        return await query.OrderByDescending(e => e.Date).ToListAsync();
    }

    public async Task<Expense> CreateAsync(int userId, ExpenseDto dto)
    {
        var expense = new Expense
        {
            UserId = userId,
            Description = dto.Description,
            Amount = dto.Amount,
            Category = dto.Category,
            Date = dto.Date
        };

        _context.Expenses.Add(expense);
        await _context.SaveChangesAsync();
        return expense;
    }

    public async Task<bool> UpdateAsync(int userId, int id, ExpenseDto dto)
    {
        var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (expense == null) return false;

        expense.Description = dto.Description;
        expense.Amount = dto.Amount;
        expense.Category = dto.Category;
        expense.Date = dto.Date;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int userId, int id)
    {
        var expense = await _context.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);
        if (expense == null) return false;

        _context.Expenses.Remove(expense);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<CategorySummaryDto>> GetSummaryAsync(int userId)
    {
        return await _context.Expenses
            .Where(e => e.UserId == userId)
            .GroupBy(e => e.Category)
            .Select(g => new CategorySummaryDto { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync();
    }

    public async Task<List<Expense>> GetAllAsync(int userId, int? month, int? year, string? category, int? week)
    {
        var query = _context.Expenses.Where(e => e.UserId == userId).AsQueryable();

        if (month.HasValue) query = query.Where(e => e.Date.Month == month.Value);
        if (year.HasValue) query = query.Where(e => e.Date.Year == year.Value);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(e => e.Category == category);

        var result = await query.OrderByDescending(e => e.Date).ToListAsync();

        if (week.HasValue)
            result = result.Where(e => ((e.Date.Day - 1) / 7) + 1 == week.Value).ToList();

        return result;
    }
}