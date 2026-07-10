using System.ComponentModel.DataAnnotations;

namespace GastosApi.Dtos;

public class ExpenseDto
{
    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [MaxLength(100, ErrorMessage = "A descrição deve ter no máximo 100 caracteres.")]
    public string Description { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "O valor deve ser maior que zero.")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "A categoria é obrigatória.")]
    public string Category { get; set; } = string.Empty;

    public DateTime Date { get; set; }
}