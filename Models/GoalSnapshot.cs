namespace GastosApi.Models;

public class GoalSnapshot
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Spent { get; set; }
    public decimal SavingsAchieved { get; set; }
    public decimal SavingsGoal { get; set; }
}