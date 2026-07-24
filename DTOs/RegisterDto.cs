using System.ComponentModel.DataAnnotations;

namespace GastosApi.Dtos;

public class RegisterDto
{
    [Required(ErrorMessage = "O usuário é obrigatório.")]
    [MinLength(3, ErrorMessage = "O usuário deve ter pelo menos 3 caracteres.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(8, ErrorMessage = "A senha deve ter pelo menos 8 caracteres.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$",
    ErrorMessage = "A senha deve conter letra maiúscula, minúscula e número.")]
    public string Password { get; set; } = string.Empty;
}