namespace GastosApi.Models;

public enum InviteStatus { Pending, Accepted, Declined }

public class JointAccountMember
{
    public int Id { get; set; }
    public int JointAccountId { get; set; }
    public int UserId { get; set; }
    public InviteStatus Status { get; set; } = InviteStatus.Pending;
    public DateTime InvitedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
}