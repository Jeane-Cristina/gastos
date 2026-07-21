using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GastosApi.Data;
using GastosApi.Dtos;
using GastosApi.Models;

namespace GastosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FinancialProfileController : ControllerBase
{
    private readonly AppDbContext _context;

    public FinancialProfileController(AppDbContext context)
    {
        _context = context;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var profile = await _context.FinancialProfiles
            .FirstOrDefaultAsync(p => p.UserId == GetUserId());

        if (profile == null) return Ok(null);
        return Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> Upsert(FinancialProfileDto dto)
    {
        var userId = GetUserId();
        var profile = await _context.FinancialProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            profile = new FinancialProfile { UserId = userId };
            _context.FinancialProfiles.Add(profile);
        }

        profile.MonthlyIncome = dto.MonthlyIncome;
        profile.SavingsGoal = dto.SavingsGoal;
        profile.ShortTermGoal = dto.ShortTermGoal;
        profile.MediumTermGoal = dto.MediumTermGoal;
        profile.LongTermGoal = dto.LongTermGoal;

        await _context.SaveChangesAsync();
        return Ok(profile);
    }
}