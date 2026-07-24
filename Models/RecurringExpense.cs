namespace GastosApi.Models;

public class RecurringExpense
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
    public int DayOfMonth { get; set; }
    public DateTime? LastGeneratedAt { get; set; }
    public bool Active { get; set; } = true;
}