using BCrypt.Net;
using GastosApi.Data;
using GastosApi.Dtos;
using GastosApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GastosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;
    private readonly EmailService _emailService;
    private readonly DemoDataService _demoService;

    public AuthController(AppDbContext context, TokenService tokenService, EmailService emailService, DemoDataService demoService)
    {
        _context = context;
        _tokenService = tokenService;
        _emailService = emailService;
        _demoService = demoService;
    }

    /// <summary>Cria uma conta de demonstração temporária (24 h) com dados fictícios e devolve o token.</summary>
    [HttpPost("demo")]
    [EnableRateLimiting("DemoPolicy")]
    public async Task<IActionResult> Demo()
    {
        var user = await _demoService.CreateDemoUserAsync();
        if (user == null)
        {
            return StatusCode(503, new { message = "A demonstração está lotada no momento. Tente novamente em alguns minutos ou crie uma conta." });
        }

        var (token, refreshToken) = await IssueTokensAsync(user);
        return Ok(new { token, refreshToken, demo = true });
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

        var (token, refreshToken) = await IssueTokensAsync(user);
        return Ok(new { token, refreshToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Unauthorized(new { message = "Refresh token inválido ou expirado." });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

        if (user == null || user.RefreshTokenExpiry == null || user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Refresh token inválido ou expirado." });
        }

        // rotação: cada refresh gera um novo par e o anterior deixa de valer
        var (newToken, newRefreshToken) = await IssueTokensAsync(user);
        return Ok(new { token = newToken, refreshToken = newRefreshToken });
    }

    /// <summary>Encerra a sessão no servidor: o refresh token deixa de valer.</summary>
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                await _context.SaveChangesAsync();
            }
        }

        return NoContent();
    }

    private async Task<(string Token, string RefreshToken)> IssueTokensAsync(Models.User user)
    {
        var token = _tokenService.GenerateToken(user);
        var refreshToken = _tokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync();

        return (token, refreshToken);
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

        var (token, refreshToken) = await IssueTokensAsync(user);
        return Ok(new { token, refreshToken });
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