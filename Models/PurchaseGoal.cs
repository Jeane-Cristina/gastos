namespace GastosApi.Models;

public class PurchaseGoal
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal EstimatedCost { get; set; }
    public string Priority { get; set; } = "Média"; // "Alta", "Média", "Baixa"
    public bool IsEssential { get; set; } // desejo vs necessidade
}