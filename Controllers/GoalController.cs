using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GastosApi.Services;

namespace GastosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GoalController : ControllerBase
{
    private readonly GoalService _goalService;

    public GoalController(GoalService goalService)
    {
        _goalService = goalService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("report")]
    public async Task<IActionResult> GetReport()
    {
        var report = await _goalService.GetReportAsync(GetUserId());
        if (report == null) return Ok(null);
        return Ok(report);
    }
}