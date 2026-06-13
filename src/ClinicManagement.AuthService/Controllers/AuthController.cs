using BC = BCrypt.Net.BCrypt;
using AppUser = ClinicManagement.AuthService.Entities.User;
using ClinicManagement.AuthService.Persistence;
using ClinicManagement.AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.AuthService.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly TokenService _tokenService;

    public AuthController(AuthDbContext db, TokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (await _db.Users.AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), ct))
            return Conflict(new { success = false, error = "Email is already registered." });

        var hash = BC.HashPassword(request.Password);
        var user = AppUser.Create(request.Email, hash, request.Role ?? "Staff");

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        var token = _tokenService.GenerateToken(user);
        return Ok(new { success = true, data = new { token, user.Email, user.Role } });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), ct);

        if (user is null || !user.VerifyPassword(request.Password))
            return Unauthorized(new { success = false, error = "Invalid email or password." });

        var token = _tokenService.GenerateToken(user);
        return Ok(new { success = true, data = new { token, user.Email, user.Role } });
    }
}

public record RegisterRequest(string Email, string Password, string? Role);
public record LoginRequest(string Email, string Password);
