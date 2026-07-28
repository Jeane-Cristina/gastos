using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using GastosApi.Data;
using GastosApi.Dtos;
using GastosApi.Models;
using GastosApi.Services;

namespace GastosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class JointAccountController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JointAccountService _service;
    private readonly JointExpenseService _expenseService;
    private readonly InsightService _insightService;

    public JointAccountController(AppDbContext context, JointAccountService service, JointExpenseService expenseService)
    {
        _context = context;
        _service = service;
        _expenseService = expenseService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetMyAccounts()
    {
        var userId = GetUserId();
        var accountIds = await _context.JointAccountMembers
            .Where(m => m.UserId == userId && m.Status == InviteStatus.Accepted)
            .Select(m => m.JointAccountId)
            .ToListAsync();

        var accounts = await _context.JointAccounts
            .Where(a => accountIds.Contains(a.Id))
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpGet("invites")]
    public async Task<IActionResult> GetPendingInvites()
    {
        var userId = GetUserId();
        var invites = await _context.JointAccountMembers
            .Where(m => m.UserId == userId && m.Status == InviteStatus.Pending)
            .Join(_context.JointAccounts, m => m.JointAccountId, a => a.Id, (m, a) => new { MemberId = m.Id, AccountName = a.Name })
            .ToListAsync();

        return Ok(invites);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateJointAccountDto dto)
    {
        var account = await _service.CreateAsync(GetUserId(), dto.Name);
        return Ok(account);
    }

    [HttpPost("{id}/invite")]
    public async Task<IActionResult> Invite(int id, InviteMemberDto dto)
    {
        var (success, error) = await _service.InviteAsync(id, GetUserId(), dto.Username);
        if (!success) return BadRequest(new { message = error });
        return Ok();
    }

    [HttpPost("invites/{memberId}/respond")]
    public async Task<IActionResult> RespondInvite(int memberId, [FromQuery] bool accept)
    {
        var success = await _service.RespondInviteAsync(memberId, GetUserId(), accept);
        if (!success) return NotFound();
        return Ok();
    }

    [HttpGet("{id}/expenses")]
    public async Task<IActionResult> GetExpenses(int id)
    {
        if (!await _service.IsMemberAsync(id, GetUserId())) return Forbid();
        var expenses = await _expenseService.GetAllAsync(id);
        return Ok(expenses);
    }

    [HttpPost("{id}/expenses")]
    public async Task<IActionResult> CreateExpense(int id, JointExpenseDto dto)
    {
        if (!await _service.IsMemberAsync(id, GetUserId())) return Forbid();
        var expense = await _expenseService.CreateAsync(id, GetUserId(), dto);
        return Ok(expense);
    }

    [HttpGet("{id}/contributions")]
    public async Task<IActionResult> GetContributions(int id)
    {
        if (!await _service.IsMemberAsync(id, GetUserId())) return Forbid();
        var contributions = await _expenseService.GetContributionsAsync(id);
        return Ok(contributions);
    }

    [HttpGet("{id}/summary")]
    public async Task<IActionResult> GetCategorySummary(int id)
    {
        if (!await _service.IsMemberAsync(id, GetUserId())) return Forbid();
        var summary = await _expenseService.GetSummaryByCategoryAsync(id);
        return Ok(summary);
    }

    [HttpPost("{id}/leave")]
    public async Task<IActionResult> Leave(int id)
    {
        var success = await _service.LeaveAsync(id, GetUserId());
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}/members/{targetUserId}")]
    public async Task<IActionResult> RemoveMember(int id, int targetUserId)
    {
        var (success, error) = await _service.RemoveMemberAsync(id, GetUserId(), targetUserId);
        if (!success) return BadRequest(new { message = error });
        return NoContent();
    }

    [HttpGet("{id}/scores")]
    public async Task<IActionResult> GetScores(int id)
    {
        if (!await _service.IsMemberAsync(id, GetUserId())) return Forbid();

        var contributions = await _expenseService.GetContributionsAsync(id);
        // Score simplificado: quem contribuiu mais próximo de 50% está "equilibrando" melhor a conta
        var scores = contributions.Select(c => new
        {
            c.Username,
            c.Percent,
            Balance = 100 - Math.Abs(50 - c.Percent) * 2 // 50% de contribuição = score 100; quanto mais desbalanceado, menor
        });

        return Ok(scores);
    }

    [HttpGet("{id}/insight")]
    public async Task<IActionResult> GetInsight(int id)
    {
        if (!await _service.IsMemberAsync(id, GetUserId())) return Forbid();

        var expenses = await _expenseService.GetAllAsync(id);
        var contributions = await _expenseService.GetContributionsAsync(id);
        var goal = await _context.JointGoals.FirstOrDefaultAsync(g => g.JointAccountId == id);

        var insight = await _insightService.GenerateJointInsightAsync(expenses, contributions, goal);
        return Ok(new { insight });
    }
}