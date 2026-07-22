namespace GastosApi.Dtos;

public class GoalReportDto
{
    public decimal MonthlySpent { get; set; }
    public decimal MonthlySavingsGoal { get; set; }
    public decimal MonthlySavingsAchieved { get; set; }
    public bool MonthlyGoalMet { get; set; }

    public decimal AnnualSpent { get; set; }
    public decimal AnnualSavingsGoal { get; set; }
    public decimal AnnualSavingsAchieved { get; set; }
    public bool AnnualGoalMet { get; set; }
}