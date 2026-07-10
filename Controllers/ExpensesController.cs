using Microsoft.AspNetCore.Mvc;
using GastosApi.Services;
using GastosApi.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace GastosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _service;

    public ExpensesController(IExpenseService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? month, [FromQuery] int? year, [FromQuery] string? category)
    {
        var expenses = await _service.GetAllAsync(month, year, category);
        return Ok(expenses);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ExpenseDto dto)
    {
        var expense = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetAll), new { id = expense.Id }, expense);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(int id, ExpenseDto dto)
    {
        var success = await _service.UpdateAsync(id, dto);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _service.DeleteAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _service.GetSummaryAsync();
        return Ok(summary);
    }
}