using Microsoft.EntityFrameworkCore;
using GastosApi.Data;
using GastosApi.Dtos;

namespace GastosApi.Services;

public class GoalService
{
    private readonly AppDbContext _context;

    public GoalService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GoalReportDto?> GetReportAsync(int userId)
    {
        var profile = await _context.FinancialProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return null;

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);
        var startOfYear = new DateTime(now.Year, 1, 1);

        var monthlySpent = await _context.Expenses
            .Where(e => e.UserId == userId && e.Date >= startOfMonth)
            .SumAsync(e => e.Amount);

        var annualSpent = await _context.Expenses
            .Where(e => e.UserId == userId && e.Date >= startOfYear)
            .SumAsync(e => e.Amount);

        var monthlySavingsAchieved = profile.MonthlyIncome - monthlySpent;
        var annualSavingsAchieved = (profile.MonthlyIncome * 12) - annualSpent;

        var monthlyProgress = profile.SavingsGoal > 0
        ? (double)(monthlySavingsAchieved / profile.SavingsGoal) * 100
        : 0;

        var annualProgress = profile.AnnualSavingsGoal > 0
            ? (double)(annualSavingsAchieved / profile.AnnualSavingsGoal) * 100
            : 0;

        return new GoalReportDto
        {
            MonthlySpent = monthlySpent,
            MonthlySavingsGoal = profile.SavingsGoal,
            MonthlySavingsAchieved = monthlySavingsAchieved,
            MonthlyGoalMet = monthlySavingsAchieved >= profile.SavingsGoal,

            AnnualSpent = annualSpent,
            AnnualSavingsGoal = profile.AnnualSavingsGoal,
            AnnualSavingsAchieved = annualSavingsAchieved,
            AnnualGoalMet = annualSavingsAchieved >= profile.AnnualSavingsGoal
        };
    }
}