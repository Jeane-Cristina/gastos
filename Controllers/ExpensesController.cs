using GastosApi.Dtos;
using GastosApi.Models;
using GastosApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GastosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _service;
    private readonly CategorySuggestionService _suggestionService;

    public ExpensesController(IExpenseService service, CategorySuggestionService suggestionService)
    {
        _service = service;
        _suggestionService = suggestionService;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? month, [FromQuery] int? year, [FromQuery] string? category, [FromQuery] int? week)
    {
        var expenses = await _service.GetAllAsync(GetUserId(), month, year, category, week);
        return Ok(expenses);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ExpenseDto dto)
    {
        var expense = await _service.CreateAsync(GetUserId(), dto);
        return CreatedAtAction(nameof(GetAll), new { id = expense.Id }, expense);
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> Update(int id, ExpenseDto dto)
    {
        var success = await _service.UpdateAsync(GetUserId(), id, dto);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _service.DeleteAsync(GetUserId(), id);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _service.GetSummaryAsync(GetUserId());
        return Ok(summary);
    }

    [HttpPost("suggest-categories")]
    public async Task<IActionResult> SuggestCategories(CategorySuggestionRequest request)
    {
        var suggestions = await _suggestionService.SuggestAsync(GetUserId(), request.Descriptions);
        return Ok(suggestions);
    }

    [HttpPost("bulk-import")]
    public async Task<IActionResult> BulkImport(BulkImportDto dto)
    {
        var userId = GetUserId();
        var created = new List<Expense>();

        foreach (var expenseDto in dto.Expenses)
        {
            created.Add(await _service.CreateAsync(userId, expenseDto));
        }

        return Ok(created);
    }
}