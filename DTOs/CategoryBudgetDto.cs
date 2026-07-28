using System.ComponentModel.DataAnnotations;

namespace GastosApi.Dtos;

public class CategoryBudgetDto
{
    [Required] public string Category { get; set; } = string.Empty;
    [Range(0.01, double.MaxValue)] public decimal MonthlyLimit { get; set; }
}