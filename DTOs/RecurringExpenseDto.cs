using System.ComponentModel.DataAnnotations;

namespace GastosApi.Dtos;

public class RecurringExpenseDto
{
    [Required] public string Description { get; set; } = string.Empty;
    [Range(0.01, double.MaxValue)] public decimal Amount { get; set; }
    [Required] public string Category { get; set; } = string.Empty;
    [Range(1, 31)] public int DayOfMonth { get; set; }
}