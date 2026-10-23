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
        var period = StoredInsight.NormalizePeriod(dto.Period);
        var profile = await _context.FinancialProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile != null && profile.LastInsightGeneratedAt.HasValue)
        {
            var (cachedPeriod, cachedText) = StoredInsight.Split(profile.LastInsight);
            var daysSinceLast = (DateTime.UtcNow - profile.LastInsightGeneratedAt.Value).TotalDays;

            // Só reaproveita o texto se for do mesmo período pedido.
            if (daysSinceLast < 7 && cachedPeriod == period && !string.IsNullOrEmpty(cachedText))
            {
                var nextAvailable = profile.LastInsightGeneratedAt.Value.AddDays(7);
                return Ok(new
                {
                    insight = cachedText,
                    period,
                    cached = true,
                    nextAvailableAt = nextAvailable
                });
            }
        }

        // Na conta demo a IA só mostra o exemplo pronto, para não gastar a cota do Gemini.
        if (DemoDataService.IsDemoUsername(User.FindFirstValue(ClaimTypes.Name)))
            return StatusCode(403, new { message = DemoDataService.AiDisabledMessage });

        var periodAnalytics = await _analyticsService.GetPeriodAnalyticsAsync(userId, period);
        var purchaseGoals = await _context.PurchaseGoals.Where(g => g.UserId == userId).ToListAsync();
        var insightText = await _insightService.GenerateInsightForPeriodAsync(periodAnalytics, profile, dto.Note, purchaseGoals, period);

        if (profile == null)
        {
            profile = new Models.FinancialProfile { UserId = userId };
            _context.FinancialProfiles.Add(profile);
        }

        profile.LastInsight = StoredInsight.Wrap(period, insightText);
        profile.LastInsightGeneratedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { insight = insightText, period, cached = false, nextAvailableAt = DateTime.UtcNow.AddDays(7) });
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
            return Ok(new { insight = (string?)null, period = (string?)null, nextAvailableAt = (DateTime?)null });

        var (period, text) = StoredInsight.Split(profile.LastInsight);
        var nextAvailable = profile.LastInsightGeneratedAt?.AddDays(7);
        return Ok(new { insight = text, period, nextAvailableAt = nextAvailable });
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask(ChatQueryDto dto)
    {
        if (DemoDataService.IsDemoUsername(User.FindFirstValue(ClaimTypes.Name)))
            return StatusCode(403, new { message = DemoDataService.AiDisabledMessage });

        var userId = GetUserId();
        var analytics = await _analyticsService.GetAnalyticsAsync(userId);
        var profile = await _context.FinancialProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        var answer = await _insightService.AnswerQuestionAsync(dto.Question, analytics, profile);
        return Ok(new { answer });
    }
}