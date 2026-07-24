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
public class InsightController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ExpenseAnalyticsService _analyticsService;
    private readonly InsightService _insightService;

    public InsightController(AppDbContext context, ExpenseAnalyticsService analyticsService, InsightService insightService)
    {
        _context = context;
        _analyticsService = analyticsService;
        _insightService = insightService;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("generate")]
    public async Task<IActionResult> Generate(GenerateInsightDto dto)
    {
        var userId = GetUserId();
        var profile = await _context.FinancialProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile != null && profile.LastInsightGeneratedAt.HasValue)
        {
            var daysSinceLast = (DateTime.UtcNow - profile.LastInsightGeneratedAt.Value).TotalDays;
            if (daysSinceLast < 7)
            {
                var nextAvailable = profile.LastInsightGeneratedAt.Value.AddDays(7);
                return Ok(new
                {
                    insight = profile.LastInsight,
                    cached = true,
                    nextAvailableAt = nextAvailable
                });
            }
        }

        var analytics = await _analyticsService.GetAnalyticsAsync(userId);
        var purchaseGoals = await _context.PurchaseGoals.Where(g => g.UserId == userId).ToListAsync();
        var insightText = await _insightService.GenerateInsightAsync(analytics, profile, dto.Note, purchaseGoals);

        if (profile == null)
        {
            profile = new Models.FinancialProfile { UserId = userId };
            _context.FinancialProfiles.Add(profile);
        }

        profile.LastInsight = insightText;
        profile.LastInsightGeneratedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { insight = insightText, cached = false, nextAvailableAt = DateTime.UtcNow.AddDays(7) });
    }

    [HttpPost("reset")]
    public async Task<IActionResult> Reset()
    {
        var userId = GetUserId();
        var profile = await _context.FinancialProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile != null)
        {
            profile.LastInsight = null;
            profile.LastInsightGeneratedAt = null;
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest()
    {
        var userId = GetUserId();
        var profile = await _context.FinancialProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null || string.IsNullOrEmpty(profile.LastInsight))
            return Ok(new { insight = (string?)null, nextAvailableAt = (DateTime?)null });

        var nextAvailable = profile.LastInsightGeneratedAt?.AddDays(7);
        return Ok(new { insight = profile.LastInsight, nextAvailableAt = nextAvailable });
    }
}