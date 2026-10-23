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

    // O período do insight fica guardado no próprio texto ("[[period:monthly]]" + quebra de linha),
    // assim o cache respeita o período escolhido sem precisar de migration no banco.
    private const string PeriodMarker = "[[period:";

    private static string NormalizePeriod(string? period) =>
        (period ?? "").Trim().ToLowerInvariant() switch
        {
            "weekly" => "weekly",
            "quarterly" => "quarterly",
            _ => "monthly"
        };

    private static string WithPeriod(string period, string text) => $"{PeriodMarker}{period}]]\n{text}";

    private static (string? Period, string Text) SplitStored(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return (null, string.Empty);
        if (!stored.StartsWith(PeriodMarker, StringComparison.Ordinal)) return (null, stored);

        var end = stored.IndexOf("]]", StringComparison.Ordinal);
        if (end < 0) return (null, stored);

        var period = stored.Substring(PeriodMarker.Length, end - PeriodMarker.Length);
        return (period, stored.Substring(end + 2).TrimStart('\r', '\n'));
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate(GenerateInsightDto dto)
    {
        var userId = GetUserId();
        var period = NormalizePeriod(dto.Period);
        var profile = await _context.FinancialProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile != null && profile.LastInsightGeneratedAt.HasValue)
        {
            var (cachedPeriod, cachedText) = SplitStored(profile.LastInsight);
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

        var periodAnalytics = await _analyticsService.GetPeriodAnalyticsAsync(userId, period);
        var purchaseGoals = await _context.PurchaseGoals.Where(g => g.UserId == userId).ToListAsync();
        var insightText = await _insightService.GenerateInsightForPeriodAsync(periodAnalytics, profile, dto.Note, purchaseGoals, period);

        if (profile == null)
        {
            profile = new Models.FinancialProfile { UserId = userId };
            _context.FinancialProfiles.Add(profile);
        }

        profile.LastInsight = WithPeriod(period, insightText);
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

        var (period, text) = SplitStored(profile.LastInsight);
        var nextAvailable = profile.LastInsightGeneratedAt?.AddDays(7);
        return Ok(new { insight = text, period, nextAvailableAt = nextAvailable });
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask(ChatQueryDto dto)
    {
        var userId = GetUserId();
        var analytics = await _analyticsService.GetAnalyticsAsync(userId);
        var profile = await _context.FinancialProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        var answer = await _insightService.AnswerQuestionAsync(dto.Question, analytics, profile);
        return Ok(new { answer });
    }
}