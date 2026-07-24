using GastosApi.Data;
using GastosApi.Dtos;
using GastosApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GastosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GoalController : ControllerBase
{
    private readonly GoalService _goalService;
    private readonly AppDbContext _context;

    public GoalController(GoalService goalService, AppDbContext context)
    {
        _goalService = goalService;
        _context = context;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("report")]
    public async Task<IActionResult> GetReport()
    {
        var report = await _goalService.GetReportAsync(GetUserId());
        if (report == null) return Ok(null);
        return Ok(report);
    }

    [HttpGet("status-summary")]
    public async Task<IActionResult> GetStatusSummary()
    {
        var report = await _goalService.GetReportAsync(GetUserId());
        if (report == null) return Ok(new StatusSummaryDto { MonthlyGoalPercent = 0 });
        return Ok(new StatusSummaryDto { MonthlyGoalPercent = report.MonthlyProgressPercent });
    }

    [HttpPost("snapshot")]
    public async Task<IActionResult> SaveSnapshot()
    {
        var userId = GetUserId();
        var report = await _goalService.GetReportAsync(userId);
        if (report == null) return BadRequest("Perfil financeiro não configurado.");

        var now = DateTime.UtcNow;
        var existing = await _context.GoalSnapshots
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Month == now.Month && s.Year == now.Year);

        if (existing == null)
        {
            existing = new Models.GoalSnapshot { UserId = userId, Month = now.Month, Year = now.Year };
            _context.GoalSnapshots.Add(existing);
        }

        existing.Spent = report.MonthlySpent;
        existing.SavingsAchieved = report.MonthlySavingsAchieved;
        existing.SavingsGoal = report.MonthlySavingsGoal;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory()
    {
        var history = await _context.GoalSnapshots
            .Where(s => s.UserId == GetUserId())
            .OrderBy(s => s.Year).ThenBy(s => s.Month)
            .ToListAsync();
        return Ok(history);
    }
}