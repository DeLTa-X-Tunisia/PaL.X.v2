using Microsoft.AspNetCore.Mvc;
using PaL.X.Core;
using PaL.X.Data;
using PaL.X.Shared.Models;

namespace PaL.X.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthenticationService _authService;

    public AuthController(PalContext context)
    {
        _authService = new AuthenticationService(context);
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "pong" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _authService.LoginAsync(request.Username, request.Password);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // Create Session
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        await _authService.CreateSessionAsync(user, ipAddress);

        // Return safe user object (without password hash)
        return Ok(new User 
        {
            Id = user.Id,
            Username = user.Username,
            IsAdmin = user.IsAdmin,
            CreatedAt = user.CreatedAt,
            LastLogin = user.LastLogin,
            Status = user.Status,
            PasswordHash = "" 
        });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var user = await _authService.RegisterAsync(request.Username, request.Password, request.IsAdmin);
            return Ok(user);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("logout/{userId}")]
    public async Task<IActionResult> Logout(int userId)
    {
        await _authService.LogoutAsync(userId);
        return Ok(new { message = "Logged out successfully" });
    }
}
