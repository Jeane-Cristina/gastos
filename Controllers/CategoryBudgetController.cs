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
public class CategoryBudgetController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoryBudgetController(AppDbContext context)
    {
        _context = context;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var budgets = await _context.CategoryBudgets.Where(b => b.UserId == GetUserId()).ToListAsync();
        return Ok(budgets);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CategoryBudgetDto dto)
    {
        var budget = new CategoryBudget
        {
            UserId = GetUserId(),
            Category = dto.Category,
            MonthlyLimit = dto.MonthlyLimit
        };
        _context.CategoryBudgets.Add(budget);
        await _context.SaveChangesAsync();
        return Ok(budget);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var budget = await _context.CategoryBudgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == GetUserId());
        if (budget == null) return NotFound();
        _context.CategoryBudgets.Remove(budget);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = GetUserId();
        var budgets = await _context.CategoryBudgets.Where(b => b.UserId == userId).ToListAsync();
        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var spentByCategory = await _context.Expenses
            .Where(e => e.UserId == userId && e.Date >= startOfMonth)
            .GroupBy(e => e.Category)
            .Select(g => new { Category = g.Key, Spent = g.Sum(e => e.Amount) })
            .ToListAsync();

        var status = budgets.Select(b => new
        {
            b.Category,
            b.MonthlyLimit,
            Spent = spentByCategory.FirstOrDefault(s => s.Category == b.Category)?.Spent ?? 0,
            Percent = b.MonthlyLimit > 0
                ? (double)((spentByCategory.FirstOrDefault(s => s.Category == b.Category)?.Spent ?? 0) / b.MonthlyLimit) * 100
                : 0
        });

        return Ok(status);
    }
}