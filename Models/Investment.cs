namespace GastosApi.Models;

public class Investment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "Renda Fixa", "Renda Variável", "Fundo", "Cripto", "Outro"
    public decimal AmountInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public double AnnualReturnRate { get; set; } // rentabilidade anual estimada, em %
    public string RiskProfile { get; set; } = "Moderado"; // "Conservador", "Moderado", "Arrojado"
}