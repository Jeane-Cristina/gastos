namespace GastosApi.Models;

public class JointExpense
{
    public int Id { get; set; }
    public int JointAccountId { get; set; }
    public int PaidByUserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}