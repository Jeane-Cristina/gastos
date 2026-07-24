using System.ComponentModel.DataAnnotations;

namespace GastosApi.Dtos;

public class InvestmentDto
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public string Type { get; set; } = string.Empty;
    [Range(0, double.MaxValue)] public decimal AmountInvested { get; set; }
    [Range(0, double.MaxValue)] public decimal CurrentValue { get; set; }
    public double AnnualReturnRate { get; set; }
    public string RiskProfile { get; set; } = "Moderado";
}