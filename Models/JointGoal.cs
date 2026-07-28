namespace GastosApi.Models;

public class JointGoal
{
    public int Id { get; set; }
    public int JointAccountId { get; set; }
    public decimal MonthlySpendingLimit { get; set; }
    public decimal MonthlySavingsGoal { get; set; }
}