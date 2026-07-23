using System.ComponentModel.DataAnnotations;

namespace GastosApi.Dtos;

public class FinancialProfileDto
{
    [Range(0, double.MaxValue, ErrorMessage = "A renda não pode ser negativa.")]
    public decimal MonthlyIncome { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "A meta de economia não pode ser negativa.")]
    public decimal SavingsGoal { get; set; }

    [MaxLength(200)]
    public string ShortTermGoal { get; set; } = string.Empty;

    [MaxLength(200)]
    public string MediumTermGoal { get; set; } = string.Empty;

    [MaxLength(200)]
    public string LongTermGoal { get; set; } = string.Empty;

    public decimal CurrentSavings { get; set; }
}