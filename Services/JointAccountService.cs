using Microsoft.EntityFrameworkCore;
using GastosApi.Data;
using GastosApi.Models;
using GastosApi.Dtos;

namespace GastosApi.Services;

public class JointAccountService
{
    private readonly AppDbContext _context;

    public JointAccountService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<JointAccount> CreateAsync(int userId, string name)
    {
        var account = new JointAccount { Name = name, CreatedByUserId = userId };
        _context.JointAccounts.Add(account);
        await _context.SaveChangesAsync();

        // o criador já entra automaticamente como membro aceito
        _context.JointAccountMembers.Add(new JointAccountMember
        {
            JointAccountId = account.Id,
            UserId = userId,
            Status = InviteStatus.Accepted,
            RespondedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return account;
    }

    public async Task<(bool success, string? error)> InviteAsync(int jointAccountId, int inviterUserId, string invitedUsername)
    {
        var isMember = await _context.JointAccountMembers.AnyAsync(
            m => m.JointAccountId == jointAccountId && m.UserId == inviterUserId && m.Status == InviteStatus.Accepted);

        if (!isMember) return (false, "Você não faz parte dessa conta conjunta.");

        var invitedUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == invitedUsername);
        if (invitedUser == null) return (false, "Usuário não encontrado.");

        var alreadyMember = await _context.JointAccountMembers.AnyAsync(
            m => m.JointAccountId == jointAccountId && m.UserId == invitedUser.Id);
        if (alreadyMember) return (false, "Esse usuário já foi convidado ou já é membro.");

        _context.JointAccountMembers.Add(new JointAccountMember
        {
            JointAccountId = jointAccountId,
            UserId = invitedUser.Id,
            Status = InviteStatus.Pending
        });
        await _context.SaveChangesAsync();

        return (true, null);
    }

    public async Task<bool> RespondInviteAsync(int memberId, int userId, bool accept)
    {
        var member = await _context.JointAccountMembers.FirstOrDefaultAsync(m => m.Id == memberId && m.UserId == userId);
        if (member == null || member.Status != InviteStatus.Pending) return false;

        member.Status = accept ? InviteStatus.Accepted : InviteStatus.Declined;
        member.RespondedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsMemberAsync(int jointAccountId, int userId)
    {
        return await _context.JointAccountMembers.AnyAsync(
            m => m.JointAccountId == jointAccountId && m.UserId == userId && m.Status == InviteStatus.Accepted);
    }
}