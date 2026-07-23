using System.ComponentModel.DataAnnotations;

namespace GastosApi.Dtos;

public class PurchaseGoalDto
{
    [Required] public string Description { get; set; } = string.Empty;
    [Range(0.01, double.MaxValue)] public decimal EstimatedCost { get; set; }
    public string Priority { get; set; } = "Média";
    public bool IsEssential { get; set; }
}