using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GastosApi.Data;
using GastosApi.Dtos;
using GastosApi.Services;
using BCrypt.Net;

namespace GastosApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Usuário ou senha inválidos." });
        }

        var token = _tokenService.GenerateToken(user);
        return Ok(new { token });
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

}