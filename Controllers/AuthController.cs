using BCrypt.Net;
using GastosApi.Data;
using GastosApi.Dtos;
using GastosApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace GastosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;
    private readonly EmailService _emailService;

    public AuthController(AppDbContext context, TokenService tokenService, EmailService emailService)
    {
        _context = context;
        _tokenService = tokenService;
        _emailService = emailService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Usuário ou senha inválidos." });
        }

        var token = _tokenService.GenerateToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        return Ok(new { token, refreshToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Refresh token inválido ou expirado." });
        }

        var newToken = _tokenService.GenerateToken(user);
        var newRefreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        return Ok(new { token = newToken, refreshToken = newRefreshToken });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var existente = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (existente != null)
        {
            return Conflict(new { message = "Esse nome de usuário já está em uso." });
        }

        var user = new Models.User
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);
        return Ok(new { token });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);

        // Sempre retorna sucesso, mesmo se o usuário não existir —
        // evita que alguém descubra quais usernames existem tentando resets em massa
        if (user == null) return Ok(new { message = "Se o usuário existir, um e-mail foi enviado." });

        var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        user.PasswordResetToken = token;
        user.PasswordResetExpiry = DateTime.UtcNow.AddMinutes(15);
        await _context.SaveChangesAsync();

        await _emailService.SendPasswordResetEmailAsync(user.Username, token);

        return Ok(new { message = "Se o usuário existir, um e-mail foi enviado." });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == dto.Token);

        if (user == null || user.PasswordResetExpiry < DateTime.UtcNow)
            return BadRequest(new { message = "Token inválido ou expirado." });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetExpiry = null;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Senha alterada com sucesso." });
    }
}