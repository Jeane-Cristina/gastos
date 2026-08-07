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

    public async Task<PagedResult<Expense>> GetAllAsync(int userId, int? month, int? year, string? category, int? week, string? paidBy, bool? paid, int page, int pageSize)
    {
        var query = _context.Expenses.Where(e => e.UserId == userId).AsQueryable();

        if (month.HasValue) query = query.Where(e => e.Date.Month == month.Value);
        if (year.HasValue) query = query.Where(e => e.Date.Year == year.Value);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(e => e.Category == category);
        if (!string.IsNullOrWhiteSpace(paidBy)) query = query.Where(e => e.PaidBy == paidBy);
        if (paid.HasValue) query = query.Where(e => e.Paid == paid.Value);

        query = query.OrderByDescending(e => e.Date);

        var totalCount = await query.CountAsync();
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        if (week.HasValue)
            items = items.Where(e => ((e.Date.Day - 1) / 7) + 1 == week.Value).ToList();

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

    public async Task<List<CategorySummaryDto>> GetSummaryAsync(int userId, int? month, int? year, string? category, int? week, string? paidBy, bool? paid)
    {
        var query = _context.Expenses.Where(e => e.UserId == userId).AsQueryable();

        if (month.HasValue) query = query.Where(e => e.Date.Month == month.Value);
        if (year.HasValue) query = query.Where(e => e.Date.Year == year.Value);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(e => e.Category == category);
        if (!string.IsNullOrWhiteSpace(paidBy)) query = query.Where(e => e.PaidBy == paidBy);
        if (paid.HasValue) query = query.Where(e => e.Paid == paid.Value);

        var expenses = await query.ToListAsync();

        if (week.HasValue)
            expenses = expenses.Where(e => ((e.Date.Day - 1) / 7) + 1 == week.Value).ToList();

        return expenses
            .GroupBy(e => e.Category)
            .Select(g => new CategorySummaryDto { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToList();
    }
}