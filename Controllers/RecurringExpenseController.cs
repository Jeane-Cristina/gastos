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
public class RecurringExpenseController : ControllerBase
{
    private readonly AppDbContext _context;

    public RecurringExpenseController(AppDbContext context)
    {
        _context = context;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var recurring = await _context.RecurringExpenses.Where(r => r.UserId == GetUserId()).ToListAsync();
        return Ok(recurring);
    }

    [HttpPost]
    public async Task<IActionResult> Create(RecurringExpenseDto dto)
    {
        var recurring = new RecurringExpense
        {
            UserId = GetUserId(),
            Description = dto.Description,
            Amount = dto.Amount,
            Category = dto.Category,
            DayOfMonth = dto.DayOfMonth,
            Active = true
        };
        _context.RecurringExpenses.Add(recurring);
        await _context.SaveChangesAsync();
        return Ok(recurring);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var recurring = await _context.RecurringExpenses.FirstOrDefaultAsync(r => r.Id == id && r.UserId == GetUserId());
        if (recurring == null) return NotFound();
        _context.RecurringExpenses.Remove(recurring);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}