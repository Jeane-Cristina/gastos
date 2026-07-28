namespace GastosApi.Dtos;

public class CreateJointAccountDto
{
    public string Name { get; set; } = string.Empty;
}

public class InviteMemberDto
{
    public string Username { get; set; } = string.Empty;
}

public class JointExpenseDto
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}