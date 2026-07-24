using GastosApi.Data;
using GastosApi.Dtos;
using GastosApi.Models;
using GastosApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GastosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvestmentController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly InvestmentAdvisorService _advisorService;

    public InvestmentController(AppDbContext context, InvestmentAdvisorService advisorService)
    {
        _context = context;
        _advisorService = advisorService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var investments = await _context.Investments.Where(i => i.UserId == GetUserId()).ToListAsync();
        return Ok(investments);
    }

    [HttpPost]
    public async Task<IActionResult> Create(InvestmentDto dto)
    {
        var investment = new Investment
        {
            UserId = GetUserId(),
            Name = dto.Name,
            Type = dto.Type,
            AmountInvested = dto.AmountInvested,
            CurrentValue = dto.CurrentValue,
            AnnualReturnRate = dto.AnnualReturnRate,
            RiskProfile = dto.RiskProfile
        };
        _context.Investments.Add(investment);
        await _context.SaveChangesAsync();
        return Ok(investment);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var investment = await _context.Investments.FirstOrDefaultAsync(i => i.Id == id && i.UserId == GetUserId());
        if (investment == null) return NotFound();
        _context.Investments.Remove(investment);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions()
    {
        var userId = GetUserId();
        var investments = await _context.Investments.Where(i => i.UserId == userId).ToListAsync();
        var profile = await _context.FinancialProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        var suggestions = await _advisorService.GetSuggestionsAsync(investments, profile);
        return Ok(new { suggestions });
    }
}