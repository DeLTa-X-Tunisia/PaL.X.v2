using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaL.X.Api.Services;
using PaL.X.API.Services;
using PaL.X.Data;
using PaL.X.Shared.DTOs;
using PaL.X.Shared.Enums;
using PaL.X.Shared.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PaL.X.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ServiceManager _serviceManager;
        private readonly IGeoLocationService _geoLocationService;

        public AuthController(AppDbContext context, IConfiguration configuration, ServiceManager serviceManager, IGeoLocationService geoLocationService)
        {
            _context = context;
            _configuration = configuration;
            _serviceManager = serviceManager;
            _geoLocationService = geoLocationService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Validation
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                return BadRequest(new LoginResponse 
                { 
                    Success = false, 
                    Message = "Username already exists" 
                });
            }

            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return BadRequest(new LoginResponse 
                { 
                    Success = false, 
                    Message = "Email already exists" 
                });
            }

            // Hash password
            var (hash, salt) = PasswordHasher.HashPassword(request.Password);

            // Create user
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = hash,
                Salt = salt,
                IsAdmin = request.IsAdmin,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new LoginResponse 
            { 
                Success = true, 
                Message = "Registration successful" 
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .Include(u => u.Profile)
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !PasswordHasher.VerifyPassword(request.Password, user.PasswordHash, user.Salt))
            {
                return Unauthorized(new LoginResponse 
                { 
                    Success = false, 
                    Message = "Invalid username or password" 
                });
            }

            // Check Public Chat Status
            if (!_serviceManager.IsPublicChatEnabled && !user.IsAdmin)
            {
                return StatusCode(403, new LoginResponse 
                { 
                    Success = false, 
                    Message = "Le chat public est actuellement désactivé par l'administrateur." 
                });
            }

            // Update last login
            user.LastLogin = DateTime.UtcNow;
            
            // Désactiver toutes les sessions actives de cet utilisateur avant d'en créer une nouvelle
            var existingSessions = await _context.Sessions
                .Where(s => s.UserId == user.Id && s.IsActive)
                .ToListAsync();
            
            foreach (var existingSession in existingSessions)
            {
                existingSession.IsActive = false;
                existingSession.DisconnectedAt = DateTime.UtcNow;
                existingSession.DisplayedStatus = UserStatus.Offline;
                existingSession.RealStatus = UserStatus.Offline;
            }
            
            // Create session
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var country = await _geoLocationService.GetCountryFromIpAsync(ipAddress);
            
            var session = new Session
            {
                UserId = user.Id,
                Username = user.Username,
                IpAddress = ipAddress,
                Country = country,
                DeviceSerial = string.IsNullOrWhiteSpace(request.DeviceSerial) ? null : request.DeviceSerial.Trim(),
                ConnectedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                DisplayedStatus = request.ConnectOffline ? UserStatus.Offline : UserStatus.Online,
                RealStatus = UserStatus.Online,
                IsActive = true
            };

            // LOG: Afficher le statut enregistré
            Console.WriteLine($"[LOGIN] User '{user.Username}' connected with ConnectOffline={request.ConnectOffline}, DisplayedStatus={session.DisplayedStatus}");

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = GenerateJwtToken(user);

            return Ok(new LoginResponse
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                User = new UserData
                {
                    Id = user.Id,
                    Username = user.Username,
                    FirstName = user.Profile?.FirstName ?? "",
                    LastName = user.Profile?.LastName ?? "",
                    Email = user.Email,
                    IsAdmin = user.IsAdmin,
                    IsProfileComplete = user.Profile != null
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username), // Ensure User.Identity.Name is populated
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("userId", user.Id.ToString()),
                new Claim("isAdmin", user.IsAdmin.ToString().ToLower()),
                new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User"), // Ajout du rôle
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(3),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}