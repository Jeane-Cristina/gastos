using Microsoft.EntityFrameworkCore;
using GastosApi.Data;
using GastosApi.Models;
using GastosApi.Dtos;

namespace GastosApi.Services;

public class ContributionSummary
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public decimal TotalPaid { get; set; }
    public double Percent { get; set; }
}

public class JointExpenseService
{
    private readonly AppDbContext _context;

    public JointExpenseService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<JointExpense>> GetAllAsync(int jointAccountId)
    {
        return await _context.JointExpenses
            .Where(e => e.JointAccountId == jointAccountId)
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    public async Task<JointExpense> CreateAsync(int jointAccountId, int paidByUserId, JointExpenseDto dto)
    {
        var expense = new JointExpense
        {
            JointAccountId = jointAccountId,
            PaidByUserId = paidByUserId,
            Description = dto.Description,
            Amount = dto.Amount,
            Category = dto.Category,
            Date = dto.Date
        };
        _context.JointExpenses.Add(expense);
        await _context.SaveChangesAsync();
        return expense;
    }

    public async Task<List<ContributionSummary>> GetContributionsAsync(int jointAccountId)
    {
        var expenses = await _context.JointExpenses
            .Where(e => e.JointAccountId == jointAccountId)
            .ToListAsync();

        var total = expenses.Sum(e => e.Amount);
        if (total == 0) return new List<ContributionSummary>();

        var byUser = expenses.GroupBy(e => e.PaidByUserId);
        var result = new List<ContributionSummary>();

        foreach (var group in byUser)
        {
            var user = await _context.Users.FindAsync(group.Key);
            var paid = group.Sum(e => e.Amount);
            result.Add(new ContributionSummary
            {
                UserId = group.Key,
                Username = user?.Username ?? "Desconhecido",
                TotalPaid = paid,
                Percent = (double)(paid / total) * 100
            });
        }

        return result;
    }

    public async Task<List<CategorySummaryDto>> GetSummaryByCategoryAsync(int jointAccountId)
    {
        return await _context.JointExpenses
            .Where(e => e.JointAccountId == jointAccountId)
            .GroupBy(e => e.Category)
            .Select(g => new CategorySummaryDto { Category = g.Key, Total = g.Sum(e => e.Amount) })
            .ToListAsync();
    }
}