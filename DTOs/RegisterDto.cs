using System.ComponentModel.DataAnnotations;

namespace GastosApi.Dtos;

public class RegisterDto
{
    [Required(ErrorMessage = "O usuário é obrigatório.")]
    [MinLength(3, ErrorMessage = "O usuário deve ter pelo menos 3 caracteres.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "A senha é obrigatória.")]
    [MinLength(6, ErrorMessage = "A senha deve ter pelo menos 6 caracteres.")]
    public string Password { get; set; } = string.Empty;
}