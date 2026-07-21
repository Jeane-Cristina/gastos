namespace GastosApi.Models;

public class FinancialProfile
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal MonthlyIncome { get; set; }
    public decimal SavingsGoal { get; set; }
    public string ShortTermGoal { get; set; } = string.Empty;
    public string MediumTermGoal { get; set; } = string.Empty;
    public string LongTermGoal { get; set; } = string.Empty;
    public string? LastInsight { get; set; }
    public DateTime? LastInsightGeneratedAt { get; set; }
}