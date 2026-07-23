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
public class PurchaseGoalController : ControllerBase
{
    private readonly AppDbContext _context;

    public PurchaseGoalController(AppDbContext context)
    {
        _context = context;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var goals = await _context.PurchaseGoals.Where(g => g.UserId == GetUserId()).ToListAsync();
        return Ok(goals);
    }

    [HttpPost]
    public async Task<IActionResult> Create(PurchaseGoalDto dto)
    {
        var goal = new PurchaseGoal
        {
            UserId = GetUserId(),
            Description = dto.Description,
            EstimatedCost = dto.EstimatedCost,
            Priority = dto.Priority,
            IsEssential = dto.IsEssential
        };
        _context.PurchaseGoals.Add(goal);
        await _context.SaveChangesAsync();
        return Ok(goal);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var goal = await _context.PurchaseGoals.FirstOrDefaultAsync(g => g.Id == id && g.UserId == GetUserId());
        if (goal == null) return NotFound();
        _context.PurchaseGoals.Remove(goal);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}